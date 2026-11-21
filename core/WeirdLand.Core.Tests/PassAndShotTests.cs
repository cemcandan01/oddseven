using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class PassAndShotTests
    {
        private static CardInstance Inst(string id, int cost, CardRole role, params EffectDef[] fx)
            => new CardInstance(Card(id, cost, role, fx), false);

        private static List<CardInstance> Many(int n, CardRole role = CardRole.Both)
            => Enumerable.Range(0, n).Select(i => Inst("c" + i, 1, role)).ToList();

        private static Combatant Comb(int passing, int hp, int maxHp, bool unbalanced = false)
        {
            var c = new Combatant { Player = Player("x", Slot.CM, new Stats { Passing = passing }), Hp = hp, MaxHp = maxHp };
            if (unbalanced) c.Statuses[StatusId.Unbalanced] = 1;
            return c;
        }

        private static Duel MakeField(bool userAtt, int userPassing, int enemyPassing, uint seed, int targets = 1,
            Action<DuelSetup> tweak = null!)
        {
            var s = new DuelSetup
            {
                Kind = DuelKind.Field,
                UserIsAttacker = userAtt,
                UserPlayer = Player("u", Slot.ST, new Stats { Power = 3, Dribbling = 4, Defending = 4, Passing = userPassing }),
                EnemyPlayer = Player("e", Slot.CM, new Stats { Power = 3, Dribbling = 4, Defending = 4, Passing = enemyPassing }),
                UserDeck = Many(6),
                EnemyDeck = Many(6),
            };
            for (int i = 0; i < targets; i++) s.UserPassTargets.Add(Player("t" + i, Slot.CM));
            tweak?.Invoke(s);
            return new Duel(s, new Rng(seed));
        }

        [Test]
        public void PassChance_Formula()
        {
            Assert.That(DuelRules.PassChance(Comb(4, 10, 10), Comb(0, 10, 10)), Is.EqualTo(0.54).Within(1e-9));
            Assert.That(DuelRules.PassChance(Comb(4, 10, 10), Comb(0, 5, 10)), Is.EqualTo(0.69).Within(1e-9));
            Assert.That(DuelRules.PassChance(Comb(4, 10, 10), Comb(0, 10, 10, true)), Is.EqualTo(0.69).Within(1e-9));
            Assert.That(DuelRules.PassChance(Comb(4, 5, 10), Comb(0, 10, 10)), Is.EqualTo(0.415).Within(1e-9));
            Assert.That(DuelRules.EnemyPassThreshold, Is.EqualTo(0.65));
            Assert.That(DuelRules.PassCost, Is.EqualTo(1));
        }

        [Test]
        public void PassChance_IsClamped()
        {
            Assert.That(DuelRules.PassChance(Comb(10, 10, 10), Comb(0, 0, 10, true)), Is.EqualTo(0.95));
            Assert.That(DuelRules.PassChance(Comb(0, 0, 10), Comb(0, 10, 10)), Is.EqualTo(0.05));
        }

        private static Duel FindPass(int userPassing, bool wantSuccess, Action<Duel> arrange = null!)
        {
            for (uint seed = 1; seed < 500; seed++)
            {
                var d = MakeField(true, userPassing, 0, seed);
                d.Begin();
                arrange?.Invoke(d);
                var probe = MakeField(true, userPassing, 0, seed);
                probe.Begin();
                arrange?.Invoke(probe);
                var ev = probe.Pass("t0");
                if (ev.OfType<PassAttempted>().Single().Success == wantSuccess) return d;
            }
            throw new InvalidOperationException("no seed found");
        }

        [Test]
        public void Pass_Success_SetsOutcomeAndTarget()
        {
            var d = FindPass(10, true);
            double chance = d.PassChance();
            Assert.That(chance, Is.EqualTo(0.9).Within(1e-9));
            var ev = d.Pass("t0");
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.PassSucceeded));
            Assert.That(d.PassTarget.Id, Is.EqualTo("t0"));
            Assert.That(d.User.Energy, Is.EqualTo(2));
            Assert.That(ev, Does.Contain(new EnergyChanged(true, 2)));
            Assert.That(ev, Does.Contain(new PassAttempted(true, "t0", chance, true)));
            Assert.That(ev.OfType<DuelEnded>().Single().Outcome, Is.EqualTo(DuelOutcome.PassSucceeded));
        }

        [Test]
        public void Pass_Failure_SetsPassFailed()
        {
            var d = FindPass(0, false, x => x.User.Hp = 1);
            var ev = d.Pass("t0");
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.PassFailed));
            Assert.That(d.PassTarget, Is.Null);
            Assert.That(ev.OfType<PassAttempted>().Single().Success, Is.False);
            Assert.That(ev.OfType<DuelEnded>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Pass_NotAllowedWhenDefending_OrShotDuel_OrNoEnergy_Throws()
        {
            var def = MakeField(false, 3, 0, 1);
            def.Begin();
            Assert.That(def.CanPass, Is.False);
            Assert.Throws<InvalidOperationException>(() => def.Pass("t0"));

            var shot = MakeField(true, 3, 0, 1, 1, s => s.Kind = DuelKind.Shot);
            shot.Begin();
            Assert.That(shot.CanPass, Is.False);
            Assert.Throws<InvalidOperationException>(() => shot.Pass("t0"));

            var none = MakeField(true, 3, 0, 1);
            none.Begin();
            none.User.Energy = 0;
            Assert.That(none.CanPass, Is.False);
            Assert.Throws<InvalidOperationException>(() => none.Pass("t0"));

            var noTargets = MakeField(true, 3, 0, 1, 0);
            noTargets.Begin();
            Assert.That(noTargets.CanPass, Is.False);

            var ok = MakeField(true, 3, 0, 1);
            ok.Begin();
            Assert.That(ok.CanPass, Is.True);
        }

        [Test]
        public void Pass_UnknownTarget_Throws()
        {
            var d = MakeField(true, 3, 0, 1);
            d.Begin();
            Assert.Throws<InvalidOperationException>(() => d.Pass("nobody"));
            Assert.That(d.User.Energy, Is.EqualTo(3));
            Assert.That(d.Outcome, Is.Null);
        }

        [Test]
        public void EnemyCarrier_PassesWhenChanceHigh()
        {
            // Enemy Passing 6 -> 0.66 >= 0.65. Best target = highest Dribbling + Shooting.
            Duel Build(uint seed, bool targets) => MakeField(false, 0, 6, seed, 0, s =>
            {
                if (!targets) return;
                s.EnemyPassTargets.Add(Player("weak", Slot.CM, new Stats { Dribbling = 1, Shooting = 1 }));
                s.EnemyPassTargets.Add(Player("best", Slot.ST, new Stats { Dribbling = 4, Shooting = 5 }));
                s.EnemyPassTargets.Add(Player("mid", Slot.CM, new Stats { Dribbling = 6, Shooting = 1 }));
            });

            var d = Build(1, true);
            var begin = d.Begin();
            Assert.That(d.Intent.PassTarget.Id, Is.EqualTo("best"));
            Assert.That(d.Intent.Card, Is.Null);
            Assert.That(d.Intent.PassChance, Is.EqualTo(0.66).Within(1e-9));
            Assert.That(begin, Does.Contain(new IntentRevealed(null, "best")));
            Assert.That(d.Enemy.Hand, Is.Empty);

            var ev = d.EndTurn();
            var att = ev.OfType<PassAttempted>().Single();
            Assert.That(att.ByUser, Is.False);
            Assert.That(att.TargetId, Is.EqualTo("best"));
            Assert.That(d.Outcome, Is.EqualTo(att.Success ? DuelOutcome.PassSucceeded : DuelOutcome.PassFailed));
            Assert.That(ev.OfType<DuelEnded>().Count(), Is.EqualTo(1));
            Assert.That(d.PassTarget?.Id, Is.EqualTo(att.Success ? "best" : null));

            // No targets: falls back to normal card choice.
            var f = Build(1, false);
            f.Begin();
            Assert.That(f.Intent.PassTarget, Is.Null);
            Assert.That(f.Intent.Card, Is.Not.Null);
        }

        private static Duel MakeShot(bool userAtt, List<CardInstance> user, List<CardInstance> enemy)
        {
            var gk = Player("gk", Slot.GK, new Stats { Power = 3, Near = 3, Far = 3 });
            var st = Player("st", Slot.ST, new Stats { Power = 3 });
            var s = new DuelSetup
            {
                Kind = DuelKind.Shot,
                UserIsAttacker = userAtt,
                UserPlayer = userAtt ? st : gk,
                EnemyPlayer = userAtt ? gk : st,
                UserDeck = user,
                EnemyDeck = enemy,
            };
            return new Duel(s, new Rng(1));
        }

        [Test]
        public void ShotDuel_GoalWhenKaleZero_AttackerWon()
        {
            var d = MakeShot(true, new List<CardInstance> { Inst("blast", 1, CardRole.Attack, AtCorner(Dmg(25), Corner.Near)) },
                new List<CardInstance>());
            d.Begin();
            Assert.That(d.MaxTurns, Is.EqualTo(2));
            Assert.That(d.Enemy.Hp, Is.EqualTo(18)); // KaleBase 12 + Near3 + Far3
            var ev = d.PlayCard(0);
            Assert.That(d.Enemy.Hp, Is.EqualTo(0));
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.AttackerWon));
            Assert.That(ev.OfType<DuelEnded>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void ShotDuel_TimeoutAfterTwoTurns_DefenderWon()
        {
            var d = MakeShot(true, Many(4, CardRole.Attack), new List<CardInstance>());
            d.Begin();
            d.EndTurn();
            Assert.That(d.Outcome, Is.Null);
            var ev = d.EndTurn();
            Assert.That(d.Outcome, Is.EqualTo(DuelOutcome.DefenderWon));
            Assert.That(ev.OfType<DuelEnded>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UserGoalkeeper_CoverMatchingIntentCorner_ReducesDamage()
        {
            int Run(StatusId cover)
            {
                var d = MakeShot(false,
                    new List<CardInstance> { Inst("cover", 1, CardRole.Defense, Sts(cover, 1, EffectTarget.Self)) },
                    new List<CardInstance> { Inst("shot", 0, CardRole.Attack, AtCorner(Dmg(10), Corner.Near)) });
                var begin = d.Begin();
                Assert.That(begin, Does.Contain(new IntentRevealed("shot", null)));
                Assert.That(d.User.Hand.Count, Is.EqualTo(1));
                d.PlayCard(0);
                int before = d.User.Hp;
                d.EndTurn();
                return before - d.User.Hp;
            }
            Assert.That(Run(StatusId.CoverNear), Is.EqualTo(7));  // 10 - Near 3, covered: x1
            Assert.That(Run(StatusId.CoverFar), Is.EqualTo(14));  // uncovered Near corner: x2
        }
    }
}
