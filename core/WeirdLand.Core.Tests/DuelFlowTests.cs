using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class DuelFlowTests
    {
        private static CardInstance Inst(string id, int cost, CardRole role, params EffectDef[] fx)
            => new CardInstance(Card(id, cost, role, fx), false);

        private static List<CardInstance> Many(int n, CardRole role, string prefix = "c", int cost = 1, params EffectDef[] fx)
            => Enumerable.Range(0, n).Select(i => Inst(prefix + i, cost, role, fx)).ToList();

        private static Duel Make(DuelKind kind, bool userAtt, List<CardInstance> user, List<CardInstance> enemy,
            Action<DuelSetup> tweak = null!, uint seed = 1)
        {
            var s = new DuelSetup
            {
                Kind = kind,
                UserIsAttacker = userAtt,
                UserPlayer = Player("u", Slot.ST, new Stats { Power = 3, Dribbling = 4, Defending = 4, Near = 3, Far = 3 }),
                EnemyPlayer = Player("e", Slot.CM, new Stats { Power = 3, Dribbling = 4, Defending = 4, Near = 3, Far = 3 }),
                UserDeck = user,
                EnemyDeck = enemy,
            };
            tweak?.Invoke(s);
            return new Duel(s, new Rng(seed));
        }

        [Test]
        public void Begin_SetsHpFromStats()
        {
            var a = Make(DuelKind.Field, true, Many(6, CardRole.Both), Many(6, CardRole.Both));
            a.Begin();
            Assert.That(a.User.Hp, Is.EqualTo(17)); // 10 + Power3 + Dribbling4
            Assert.That(a.Enemy.Hp, Is.EqualTo(17)); // 10 + Defending4 + Power3
            Assert.That(a.Turn, Is.EqualTo(1));
            Assert.That(a.MaxTurns, Is.EqualTo(3));
        }

        [Test]
        public void Begin_DrawsFour_EnergyThree()
        {
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Both), Many(6, CardRole.Both));
            var ev = d.Begin();
            Assert.That(d.User.Hand.Count, Is.EqualTo(4));
            Assert.That(d.User.Energy, Is.EqualTo(3));
            Assert.That(ev, Does.Contain(new CardsDrawn(true, 4)));
            Assert.That(ev, Does.Contain(new EnergyChanged(true, 3)));
            Assert.That(ev.OfType<IntentRevealed>().Count(), Is.EqualTo(1));
            Assert.That(d.Intent, Is.Not.Null);
        }

        [Test]
        public void Begin_OnlyRoleCardsInDeck()
        {
            var deck = Many(3, CardRole.Attack, "a").Concat(Many(3, CardRole.Defense, "d")).ToList();
            var d = Make(DuelKind.Field, true, deck, Many(6, CardRole.Both));
            d.Begin();
            var all = d.User.Hand.Concat(d.User.DrawPile).Concat(d.User.Discard).ToList();
            Assert.That(all.Count, Is.EqualTo(3));
            Assert.That(all.All(c => c.Def.Role != CardRole.Defense));
            Assert.That(d.User.Hand.Count, Is.EqualTo(3));
        }

        [Test]
        public void Begin_WithNoUsableCards_HandEmptyButTurnCanEnd()
        {
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Defense), Many(6, CardRole.Both));
            d.Begin();
            Assert.That(d.User.Hand, Is.Empty);
            Assert.DoesNotThrow(() => d.EndTurn());
            Assert.That(d.Turn, Is.EqualTo(2));
        }

        [Test]
        public void Draw_ReshufflesDiscardWhenDrawPileEmpty()
        {
            var d = Make(DuelKind.Field, true, Many(5, CardRole.Both), Many(6, CardRole.Both));
            d.Begin();
            d.EndTurn();
            Assert.That(d.Turn, Is.EqualTo(2));
            Assert.That(d.User.Hand.Count, Is.EqualTo(4));
        }

        [Test]
        public void Draw_WithFewerCardsThanHandSize_DrawsAll()
        {
            var d = Make(DuelKind.Field, true, Many(2, CardRole.Both), Many(6, CardRole.Both));
            d.Begin();
            Assert.That(d.User.Hand.Count, Is.EqualTo(2));
        }

        [Test]
        public void PlayCard_SpendsEnergy_AndRejectsTooExpensive()
        {
            var deck = new List<CardInstance> { Inst("cheap", 1, CardRole.Both, Blk(1)), Inst("big", 3, CardRole.Both, Blk(1)) };
            var d = Make(DuelKind.Field, true, deck, new List<CardInstance>());
            d.Begin();
            int cheap = d.User.Hand.FindIndex(c => c.Def.Id == "cheap");
            var ev = d.PlayCard(cheap);
            Assert.That(d.User.Energy, Is.EqualTo(2));
            Assert.That(ev, Does.Contain(new EnergyChanged(true, 2)));
            Assert.That(ev, Does.Contain(new CardPlayed(true, "cheap")));
            Assert.That(d.User.Discard.Count, Is.EqualTo(1));
            Assert.That(d.User.Hand.Count, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => d.PlayCard(0)); // cost 3 > energy 2
            Assert.That(d.User.Energy, Is.EqualTo(2));
            Assert.That(d.User.Hand.Count, Is.EqualTo(1));
        }

        [Test]
        public void LethalDamage_EndsDuel_AttackerWon_StopsRemainingEffects()
        {
            var deck = new List<CardInstance> { Inst("kill", 1, CardRole.Both, Dmg(20), Blk(5)) };
            var d = Make(DuelKind.Field, true, deck, new List<CardInstance>());
            d.Begin();
            var ev = d.PlayCard(0);
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.AttackerWon));
            Assert.That(ev.OfType<DuelEnded>().Count(), Is.EqualTo(1));
            Assert.That(ev.OfType<BlockGained>(), Is.Empty);
            Assert.That(d.User.Block, Is.EqualTo(0));
        }

        [Test]
        public void EnemyIntent_IsRevealedBeforeUserActs_AndResolvedOnEndTurn()
        {
            var enemy = new List<CardInstance> { Inst("hit", 0, CardRole.Both, Dmg(5)) };
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Both), enemy);
            var ev = d.Begin();
            Assert.That(ev, Does.Contain(new IntentRevealed("hit", null)));
            Assert.That(d.Intent.Card.Def.Id, Is.EqualTo("hit"));
            Assert.That(d.Intent.PassTarget, Is.Null);
            Assert.That(d.IncomingDamage(), Is.EqualTo(5));
            Assert.That(d.User.Hp, Is.EqualTo(17));
            var end = d.EndTurn();
            Assert.That(d.User.Hp, Is.EqualTo(12));
            Assert.That(end, Does.Contain(new CardPlayed(false, "hit")));
            Assert.That(end, Does.Contain(new DamageDealt(true, 5, 0, 12)));
        }

        [Test]
        public void EnemyIntent_NoCards_IsWait()
        {
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Both), new List<CardInstance>());
            d.Begin();
            Assert.That(d.Intent.Card, Is.Null);
            Assert.That(d.Intent.PassTarget, Is.Null);
            Assert.That(d.IncomingDamage(), Is.EqualTo(0));
            d.EndTurn();
            Assert.That(d.User.Hp, Is.EqualTo(17));
        }

        [Test]
        public void EnemyAi_PrefersLethal()
        {
            var enemy = new List<CardInstance>
            {
                Inst("wall", 0, CardRole.Both, Blk(100)),
                Inst("kill", 0, CardRole.Both, Dmg(17)),
            };
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Both), enemy);
            d.Begin();
            Assert.That(d.Intent.Card.Def.Id, Is.EqualTo("kill"));
            d.EndTurn();
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.DefenderWon));
        }

        [Test]
        public void EnemyAi_ScoresBlockAndStatuses()
        {
            var enemy = new List<CardInstance>
            {
                Inst("dmg", 0, CardRole.Both, Dmg(4)),
                Inst("tire", 0, CardRole.Both, Dmg(2), Sts(StatusId.Tired, 1)), // 2 + 3 = 5
            };
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Both), enemy);
            d.Begin();
            Assert.That(d.Intent.Card.Def.Id, Is.EqualTo("tire"));
            Assert.That(d.Enemy.Hand, Is.Empty);
            Assert.That(d.Enemy.Discard.Count, Is.EqualTo(1));
        }

        [Test]
        public void Timeout_DefenderWins_AfterMaxTurns()
        {
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Both), new List<CardInstance>());
            d.Begin();
            d.EndTurn();
            d.EndTurn();
            Assert.That(d.Outcome, Is.Null);
            var ev = d.EndTurn();
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.DefenderWon));
            Assert.That(ev.OfType<DuelEnded>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void StatusesTickDown_MomentumStays()
        {
            var enemy = new List<CardInstance> { Inst("trip", 0, CardRole.Both, Sts(StatusId.Unbalanced, 2)) };
            var d = Make(DuelKind.Field, true, Many(6, CardRole.Both), enemy, s => { s.UserMomentum = 2; s.EnemyMomentum = 1; });
            d.Begin();
            d.EndTurn();
            Assert.That(d.User.StatusOf(StatusId.Unbalanced), Is.EqualTo(1));
            Assert.That(d.User.StatusOf(StatusId.Momentum), Is.EqualTo(2));
            Assert.That(d.Enemy.StatusOf(StatusId.Momentum), Is.EqualTo(1));
        }

        [Test]
        public void Turn1Bonus_AndPenalty_ApplyToEnergy()
        {
            var a = Make(DuelKind.Field, true, Many(6, CardRole.Both), new List<CardInstance>(), s => s.UserEnergyBonusTurn1 = 1);
            a.Begin();
            Assert.That(a.User.Energy, Is.EqualTo(4));
            a.EndTurn();
            Assert.That(a.User.Energy, Is.EqualTo(3));

            var b = Make(DuelKind.Field, true, Many(6, CardRole.Both), new List<CardInstance>(), s => s.UserEnergyPenalty = 1);
            b.Begin();
            Assert.That(b.User.Energy, Is.EqualTo(2));
        }

        [Test]
        public void Commands_AfterOutcome_Throw()
        {
            var deck = new List<CardInstance> { Inst("kill", 1, CardRole.Both, Dmg(20)), Inst("x", 0, CardRole.Both, Blk(1)) };
            var d = Make(DuelKind.Field, true, deck, new List<CardInstance>());
            d.Begin();
            d.PlayCard(d.User.Hand.FindIndex(c => c.Def.Id == "kill"));
            int hand = d.User.Hand.Count;
            Assert.Throws<InvalidOperationException>(() => d.PlayCard(0));
            Assert.Throws<InvalidOperationException>(() => d.EndTurn());
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.AttackerWon));
            Assert.That(d.User.Hand.Count, Is.EqualTo(hand));
        }

        [Test]
        public void Shot_UsesShooterAndKeeperHp()
        {
            var d = Make(DuelKind.Shot, true, Many(6, CardRole.Both), Many(6, CardRole.Both), s => s.DefenderMaxHpBonus = 2);
            d.Begin();
            Assert.That(d.User.Hp, Is.EqualTo(11)); // 8 + Power3
            Assert.That(d.Enemy.Hp, Is.EqualTo(16)); // 8 + 3 + 3 + 2
        }

        [Test]
        public void SameSeed_SameEvents()
        {
            List<GameEvent> Run()
            {
                var deck = Many(5, CardRole.Both, "u", 1, Dmg(2), Blk(1))
                    .Concat(Many(3, CardRole.Both, "v", 0, Dmg(1))).ToList();
                var enemy = Many(4, CardRole.Both, "e", 0, Dmg(2)).Concat(Many(3, CardRole.Both, "f", 0, Blk(3))).ToList();
                var d = Make(DuelKind.Field, true, deck, enemy, _ => { }, 42);
                var all = new List<GameEvent>(d.Begin());
                all.AddRange(d.PlayCard(0));
                all.AddRange(d.EndTurn());
                all.AddRange(d.PlayCard(0));
                all.AddRange(d.EndTurn());
                return all;
            }
            var a = Run();
            var b = Run();
            Assert.That(a, Is.Not.Empty);
            Assert.That(a.SequenceEqual(b));
        }
    }
}
