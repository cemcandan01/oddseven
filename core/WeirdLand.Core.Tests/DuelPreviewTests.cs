using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class DuelPreviewTests
    {
        private static CardInstance Inst(string id, int cost, params EffectDef[] fx)
            => new CardInstance(Card(id, cost, CardRole.Both, fx), false);

        private static Duel Make(bool userAtt, List<CardInstance> user, List<CardInstance> enemy, Action<DuelSetup> tweak = null!)
        {
            var s = new DuelSetup
            {
                Kind = DuelKind.Field,
                UserIsAttacker = userAtt,
                UserPlayer = Player("u", Slot.ST, new Stats { Power = 3, Dribbling = 4, Defending = 4, Near = 3, Far = 3 }),
                EnemyPlayer = Player("e", Slot.CM, new Stats { Power = 3, Dribbling = 4, Defending = 4, Near = 3, Far = 3 }),
                UserDeck = user,
                EnemyDeck = enemy,
            };
            tweak?.Invoke(s);
            var d = new Duel(s, new Rng(1));
            d.Begin();
            return d;
        }

        private static List<CardInstance> Six(string id, params EffectDef[] fx)
            => Enumerable.Range(0, 6).Select(_ => Inst(id, 1, fx)).ToList();

        private static List<CardInstance> UserDeck(params CardInstance[] cards)
        {
            var l = cards.ToList();
            while (l.Count < 4) l.Add(Inst("filler" + l.Count, 1));
            return l;
        }

        private static CardInstance Find(Duel d, string id) => d.User.Hand.First(c => c.Def.Id == id);

        [Test]
        public void Card_Damage_IncludesMomentumAndUnbalanced()
        {
            var d = Make(true, UserDeck(Inst("hit", 1, Dmg(4))), Six("x"), s => s.UserMomentum = 2);
            d.Enemy.StatusMap[StatusId.Unbalanced] = 1;
            Assert.That(DuelPreview.Card(d, Find(d, "hit")).Damage, Is.EqualTo(9));
        }

        [Test]
        public void Card_Block_UsesComputeBlock()
        {
            var d = Make(true, UserDeck(Inst("wall", 1, Blk(4))), Six("x"));
            Assert.That(DuelPreview.Card(d, Find(d, "wall")), Is.EqualTo(new CardPreview(0, 4)));
        }

        [Test]
        public void CanPlay_TrueWhenAffordable()
        {
            var d = Make(true, UserDeck(), Six("x"));
            Assert.That(DuelPreview.CanPlay(d, 0), Is.True);
        }

        [Test]
        public void CanPlay_FalseWhenTooExpensive()
        {
            var d = Make(true, UserDeck(Inst("big", 4, Dmg(9))), Six("x"));
            int i = d.User.Hand.ToList().FindIndex(c => c.Def.Id == "big");
            Assert.That(d.User.Energy, Is.EqualTo(3));
            Assert.That(DuelPreview.CanPlay(d, i), Is.False);
        }

        [Test]
        public void CanPlay_FalseAfterOutcome()
        {
            var d = Make(true, UserDeck(), Six("x"));
            d.ForceOutcome(DuelOutcome.AttackerWon);
            Assert.That(DuelPreview.CanPlay(d, 0), Is.False);
        }

        [Test]
        public void CanPlay_FalseForBadIndex()
        {
            var d = Make(true, UserDeck(), Six("x"));
            Assert.That(DuelPreview.CanPlay(d, 99), Is.False);
            Assert.That(DuelPreview.CanPlay(d, -1), Is.False);
        }

        [Test]
        public void Intent_DamageCard_ReportsNameAndIncomingDamage()
        {
            var d = Make(true, UserDeck(), Six("tackle", Dmg(6)));
            var i = DuelPreview.Intent(d);
            Assert.That(i.Kind, Is.EqualTo(IntentKind.Card));
            Assert.That(i.CardName, Is.EqualTo("tackle"));
            Assert.That(i.CardId, Is.EqualTo("tackle"));
            Assert.That(i.Damage, Is.EqualTo(d.IncomingDamage()));
            Assert.That(i.Damage, Is.GreaterThan(0));
            Assert.That(i.Statuses, Is.Empty);
        }

        [Test]
        public void Intent_BlockCard_ReportsBlock()
        {
            var d = Make(true, UserDeck(), Six("wall", Blk(5)));
            Assert.That(DuelPreview.Intent(d).Block, Is.EqualTo(5));
        }

        [Test]
        public void Intent_StatusCard_ListsStatusOnUser()
        {
            var d = Make(true, UserDeck(), Six("trip", Sts(StatusId.Unbalanced, 1)));
            Assert.That(DuelPreview.Intent(d).Statuses, Is.EqualTo(new[] { new StatusPreview(StatusId.Unbalanced, 1, true) }));
        }

        [Test]
        public void Intent_SelfStatus_IsNotOnUser()
        {
            var d = Make(true, UserDeck(), Six("pump", Sts(StatusId.Momentum, 1, EffectTarget.Self)));
            Assert.That(DuelPreview.Intent(d).Statuses, Is.EqualTo(new[] { new StatusPreview(StatusId.Momentum, 1, false) }));
        }

        [Test]
        public void Intent_EnemyPass_ReportsTargetAndChance()
        {
            var d = Make(false, UserDeck(), Six("x"), s =>
            {
                s.EnemyPlayer = Player("e", Slot.CM, new Stats { Passing = 10 });
                s.EnemyPassTargets.Add(Player("mate", Slot.ST));
            });
            var i = DuelPreview.Intent(d);
            Assert.That(i.Kind, Is.EqualTo(IntentKind.Pass));
            Assert.That(i.PassTargetId, Is.EqualTo("mate"));
            Assert.That(i.PassTargetName, Is.EqualTo("mate"));
            Assert.That(i.PassChance, Is.EqualTo(0.9).Within(1e-9));
            Assert.That(i.Statuses, Is.Not.Null);
        }

        [Test]
        public void Intent_NoEnemyCards_IsWait()
        {
            var d = Make(true, UserDeck(), new List<CardInstance>());
            var i = DuelPreview.Intent(d);
            Assert.That(i.Kind, Is.EqualTo(IntentKind.Wait));
            Assert.That(i.Statuses, Is.Not.Null);
        }
    }
}
