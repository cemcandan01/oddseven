using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    /// <summary>Status timing (ruling R14) and the turn / expiry / block events Unity animates.</summary>
    public class StatusTimingTests
    {
        private static CardInstance Inst(string id, int cost, CardRole role, params EffectDef[] fx)
            => new CardInstance(Card(id, cost, role, fx), false);

        private static List<CardInstance> Many(int n, string id, int cost, params EffectDef[] fx)
            => Enumerable.Range(0, n).Select(i => Inst(id, cost, CardRole.Both, fx)).ToList();

        private static Duel Field(List<CardInstance> user, List<CardInstance> enemy)
        {
            var s = new DuelSetup
            {
                Kind = DuelKind.Field,
                UserIsAttacker = true,
                UserPlayer = Player("u", Slot.ST, new Stats { Power = 3, Dribbling = 4, Defending = 4 }),
                EnemyPlayer = Player("e", Slot.CM, new Stats { Power = 3, Dribbling = 4, Defending = 4 }),
                UserDeck = user,
                EnemyDeck = enemy,
            };
            return new Duel(s, new Rng(1));
        }

        /// <summary>User shooter (Power 3) against an enemy keeper with Near 3 / Far 3: Kale 18.</summary>
        private static Duel Shot(List<CardInstance> user, List<CardInstance> enemy)
        {
            var s = new DuelSetup
            {
                Kind = DuelKind.Shot,
                UserIsAttacker = true,
                UserPlayer = Player("st", Slot.ST, new Stats { Power = 3 }),
                EnemyPlayer = Player("gk", Slot.GK, new Stats { Power = 3, Near = 3, Far = 3 }),
                UserDeck = user,
                EnemyDeck = enemy,
            };
            return new Duel(s, new Rng(1));
        }

        private static CardInstance LeanNear() => Inst("lean_near", 1, CardRole.Defense, Sts(StatusId.CoverNear, 1, EffectTarget.Self), Blk(3));

        // ---- R14 (a): stacks gained during the enemy's action skip that EndTurn's tick ----

        [Test]
        public void EnemyAppliedTired_OneStack_ReducesUserDamageOnNextTurn()
        {
            var d = Field(Many(6, "hit", 1, Dmg(8)), new List<CardInstance> { Inst("tire", 0, CardRole.Both, Sts(StatusId.Tired, 1)) });
            d.Begin();
            d.EndTurn();
            Assert.That(d.User.StatusOf(StatusId.Tired), Is.EqualTo(1));
            int before = d.Enemy.Hp;
            d.PlayCard(0);
            Assert.That(before - d.Enemy.Hp, Is.EqualTo(6)); // floor(8 * 0.75)
        }

        [Test]
        public void EnemyAppliedUnbalanced_OneStack_IncreasesEnemyNextHit()
        {
            var enemy = new List<CardInstance> { Inst("hit_trip", 0, CardRole.Both, Dmg(4), Sts(StatusId.Unbalanced, 1)) };
            var d = Field(Many(6, "wait", 1, Blk(0)), enemy);
            d.Begin();
            Assert.That(d.User.Hp, Is.EqualTo(12));
            d.EndTurn();
            Assert.That(d.User.Hp, Is.EqualTo(8)); // 4, then Unbalanced applied
            Assert.That(d.User.StatusOf(StatusId.Unbalanced), Is.EqualTo(1));
            var ev = d.EndTurn();
            Assert.That(ev, Does.Contain(new DamageDealt(true, 6, 0, 2))); // 4 * 1.5
            // Old stack ticked, the stack gained this action survives.
            Assert.That(d.User.StatusOf(StatusId.Unbalanced), Is.EqualTo(1));
        }

        [Test]
        public void UserAppliedStatus_StillTicksAtEndOfSameRound()
        {
            var user = Many(6, "trip", 0, Sts(StatusId.Unbalanced, 1));
            var d = Field(user, new List<CardInstance>());
            d.Begin();
            d.PlayCard(0);
            Assert.That(d.Enemy.StatusOf(StatusId.Unbalanced), Is.EqualTo(1));
            d.EndTurn();
            Assert.That(d.Enemy.StatusOf(StatusId.Unbalanced), Is.EqualTo(0));
        }

        // ---- R14 (b): keeper lean is applied when the intent is revealed ----

        [Test]
        public void RevealedLean_IsAppliedOnReveal_WithEvent()
        {
            var d = Shot(Many(4, "far", 1, AtCorner(Dmg(10), Corner.Far)), new List<CardInstance> { LeanNear() });
            var ev = d.Begin().ToList();
            Assert.That(d.Enemy.StatusOf(StatusId.CoverNear), Is.EqualTo(1));
            int reveal = ev.IndexOf(new IntentRevealed("lean_near", null));
            int applied = ev.IndexOf(new StatusApplied(false, StatusId.CoverNear, 1));
            Assert.That(reveal, Is.GreaterThanOrEqualTo(0));
            Assert.That(applied, Is.GreaterThan(reveal));
            Assert.That(d.Enemy.Block, Is.EqualTo(0)); // the rest of the card waits for EndTurn
        }

        [Test]
        public void RevealedLean_UncoveredFarCorner_DoublesOnSameTurn()
        {
            var d = Shot(Many(4, "far", 1, AtCorner(Dmg(10), Corner.Far)), new List<CardInstance> { LeanNear() });
            d.Begin();
            Assert.That(d.Enemy.Hp, Is.EqualTo(18));
            d.PlayCard(0);
            Assert.That(d.Enemy.Hp, Is.EqualTo(4)); // (10 - Far 3) * 2 = 14
        }

        [Test]
        public void RevealedLean_CoveredNearCorner_NotDoubled()
        {
            var d = Shot(Many(4, "near", 1, AtCorner(Dmg(10), Corner.Near)), new List<CardInstance> { LeanNear() });
            d.Begin();
            d.PlayCard(0);
            Assert.That(d.Enemy.Hp, Is.EqualTo(11)); // 10 - Near 3 = 7
        }

        [Test]
        public void RevealedLean_NotAppliedAgainAtEndTurn_RestOfCardResolves()
        {
            var d = Shot(Many(4, "far", 1, AtCorner(Dmg(1), Corner.Far)), new List<CardInstance> { LeanNear() });
            d.Begin();
            var ev = d.EndTurn().ToList();
            int played = ev.IndexOf(new CardPlayed(false, "lean_near"));
            int turn = ev.IndexOf(new TurnStarted(2));
            Assert.That(played, Is.GreaterThanOrEqualTo(0));
            Assert.That(turn, Is.GreaterThan(played));
            var enemyAction = ev.Skip(played).Take(turn - played).ToList();
            Assert.That(enemyAction, Does.Contain(new BlockGained(false, 3)));
            Assert.That(enemyAction.OfType<StatusApplied>(), Is.Empty);
            // Reveal cover lasted the user's turn and ticked at the end of the round.
            Assert.That(enemyAction, Does.Contain(new StatusChanged(false, StatusId.CoverNear, 0)));
        }

        [Test]
        public void RevealedLean_GoneNextTurn_WhenNextIntentDoesNotLean()
        {
            var d = Shot(Many(4, "far", 1, AtCorner(Dmg(10), Corner.Far)), new List<CardInstance> { LeanNear() });
            d.Begin();
            // Next intent: a big block (score 16 beats the lean's 4.4), drawn before the reshuffled lean.
            d.Enemy.DrawPile.Add(Inst("wall", 1, CardRole.Defense, Blk(20)));
            d.EndTurn();
            Assert.That(d.Intent.Card.Def.Id, Is.EqualTo("wall"));
            Assert.That(d.Enemy.StatusOf(StatusId.CoverNear), Is.EqualTo(0));
            Assert.That(d.Enemy.Block, Is.EqualTo(3)); // from the lean card's Block at EndTurn
            int before = d.Enemy.Hp;
            d.PlayCard(0);
            Assert.That(before - d.Enemy.Hp, Is.EqualTo(4)); // 10 - Far 3 = 7, not doubled, 3 blocked
        }

        // ---- I2: turn / block / expiry events ----

        [Test]
        public void TurnStarted_EmittedAtEachUserTurn_BeforeEnergyAndDraw()
        {
            var d = Field(Many(6, "hit", 1, Dmg(1)), new List<CardInstance>());
            var begin = d.Begin().ToList();
            Assert.That(begin[0], Is.EqualTo(new TurnStarted(1)));
            Assert.That(begin.IndexOf(new EnergyChanged(true, 3)), Is.GreaterThan(0));
            Assert.That(begin.IndexOf(new CardsDrawn(true, 4)), Is.GreaterThan(0));

            var end = d.EndTurn().ToList();
            int t = end.IndexOf(new TurnStarted(2));
            Assert.That(t, Is.GreaterThanOrEqualTo(0));
            Assert.That(end.IndexOf(new EnergyChanged(true, 3)), Is.GreaterThan(t));
            Assert.That(end.IndexOf(new CardsDrawn(true, 4)), Is.GreaterThan(t));
            Assert.That(end.OfType<TurnStarted>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void NoTurnStarted_WhenDuelEndsOnTimeout()
        {
            var d = Field(Many(6, "hit", 1, Dmg(1)), new List<CardInstance>());
            d.Begin();
            d.EndTurn();
            d.EndTurn();
            var last = d.EndTurn();
            Assert.That(last.OfType<TurnStarted>(), Is.Empty);
            Assert.That(last.Last(), Is.EqualTo(new DuelEnded(DuelOutcome.DefenderWon)));
        }

        [Test]
        public void BlockReset_EmittedOnlyWhenBlockWasPositive_InOrder()
        {
            // Enemy blocks 5 on its action; user blocks 4 on turn 1.
            var d = Field(Many(6, "guard", 1, Blk(4)), new List<CardInstance> { Inst("wall", 0, CardRole.Both, Blk(5)) });
            var begin = d.Begin();
            Assert.That(begin.OfType<BlockReset>(), Is.Empty);
            d.PlayCard(0);
            var end1 = d.EndTurn().ToList();
            // Enemy had no block before acting: no enemy reset. User block 4 resets at the start of turn 2.
            Assert.That(end1.OfType<BlockReset>().ToList(), Is.EqualTo(new[] { new BlockReset(true) }));
            int t2 = end1.IndexOf(new TurnStarted(2));
            int userReset = end1.IndexOf(new BlockReset(true));
            Assert.That(userReset, Is.GreaterThan(t2));
            Assert.That(end1.IndexOf(new EnergyChanged(true, 3)), Is.GreaterThan(userReset));

            var end2 = d.EndTurn().ToList(); // user played nothing on turn 2
            Assert.That(end2.OfType<BlockReset>().ToList(), Is.EqualTo(new[] { new BlockReset(false) }));
            Assert.That(end2[0], Is.EqualTo(new BlockReset(false)));
            Assert.That(end2.IndexOf(new CardPlayed(false, "wall")), Is.GreaterThan(0));
        }

        [Test]
        public void StatusChanged_EmittedOnEveryTickDecrement()
        {
            var user = Many(6, "pile", 0, Sts(StatusId.Unbalanced, 2), Sts(StatusId.Tired, 1));
            var d = Field(user, new List<CardInstance>());
            d.Begin();
            d.PlayCard(0); // enemy: Unbalanced 2, Tired 1
            var end1 = d.EndTurn().ToList();
            var changes1 = end1.OfType<StatusChanged>().ToList();
            Assert.That(changes1, Is.EqualTo(new[]
            {
                new StatusChanged(false, StatusId.Unbalanced, 1),
                new StatusChanged(false, StatusId.Tired, 0),
            }));
            Assert.That(end1.IndexOf(changes1[1]), Is.LessThan(end1.IndexOf(new TurnStarted(2))));

            var end2 = d.EndTurn().ToList();
            Assert.That(end2.OfType<StatusChanged>().ToList(), Is.EqualTo(new[] { new StatusChanged(false, StatusId.Unbalanced, 0) }));
            Assert.That(d.Enemy.Statuses.ContainsKey(StatusId.Unbalanced), Is.False);
        }
    }
}
