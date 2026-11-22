using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using WeirdLand.Core;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity.Tests
{
    public class DuelScreenTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            TestRig.Teardown();
            yield return null;
        }

        private static IEnumerable<TestCaseData> Kinds()
        {
            foreach (ScenarioKind k in Enum.GetValues(typeof(ScenarioKind)))
                foreach (uint s in new uint[] { 1, 2, 3 })
                    yield return new TestCaseData(k, s).Returns(null); // UnityTest cases must declare a null result
        }

        [UnityTest, TestCaseSource(nameof(Kinds)), Timeout(60000)]
        public IEnumerator Autoplay_ReachesResultWithoutErrors(ScenarioKind kind, uint seed)
        {
            var (screen, duel) = TestRig.Create(kind, seed);
            yield return TestRig.Autoplay(screen, duel, seed);
            Assert.That(duel.Outcome.HasValue, Is.True, "duel did not end");
            Assert.That(screen.ResultShown, Is.True);
            Assert.That(screen.ResultText, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator CardClick_Twice_SpendsEnergyOnce()
        {
            var (screen, duel) = TestRig.Create(ScenarioKind.FieldAttack, 1);
            yield return TestRig.Idle(screen);
            Assert.That(DuelPreview.CanPlay(duel, 0), Is.True, "precondition: first card playable");
            int energy = duel.User.Energy, cost = duel.User.Hand[0].Cost;

            screen.ClickCard(0);
            screen.ClickCard(0);
            yield return TestRig.Idle(screen);

            Assert.That(duel.User.Energy, Is.EqualTo(energy - cost));
            Assert.That(duel.User.Discard.Count, Is.EqualTo(1), "exactly one card played");
            Assert.That(screen.HandCardCount, Is.EqualTo(duel.User.Hand.Count));
        }

        [UnityTest]
        public IEnumerator Bars_AfterEachCommand_MatchState()
        {
            var (screen, duel) = TestRig.Create(ScenarioKind.FieldAttack, 3);
            var policy = new GoodPolicy();
            var rng = new Rng(3);
            yield return TestRig.Idle(screen);
            AssertBars(screen, duel);
            for (int i = 0; i < TestRig.MaxDecisions && !duel.Outcome.HasValue; i++)
            {
                TestRig.Act(screen, policy.Decide(duel, null, rng));
                yield return TestRig.Idle(screen);
                AssertBars(screen, duel);
            }
            Assert.That(duel.Outcome.HasValue, Is.True);
            Assert.That(screen.IntentText, Is.Empty, "a finished duel shows no intent");
        }

        private static void AssertBars(DuelScreen screen, Duel duel)
        {
            Assert.That(screen.UserBarValue, Is.EqualTo((float)duel.User.Hp), "user bar");
            Assert.That(screen.EnemyBarValue, Is.EqualTo((float)duel.Enemy.Hp), "enemy bar");
        }

        [UnityTest]
        public IEnumerator PassButton_HiddenWhenDefending()
        {
            var (screen, _) = TestRig.Create(ScenarioKind.FieldDefense, 1);
            yield return TestRig.Idle(screen);
            Assert.That(screen.PassButtonVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator PassPicker_CancelDoesNotSpendEnergy()
        {
            var (screen, duel) = TestRig.Create(ScenarioKind.FieldAttack, 1);
            yield return TestRig.Idle(screen);
            Assert.That(screen.PassButtonVisible, Is.True, "precondition: attacker can pass");
            Assert.That(screen.PassButtonText, Does.Contain(UiText.Percent(duel.PassChance())));
            int energy = duel.User.Energy;

            screen.ClickPass();
            Assert.That(screen.PassPickerOpen, Is.True);
            screen.ClickCancelPass();
            yield return null;

            Assert.That(screen.PassPickerOpen, Is.False);
            Assert.That(duel.User.Energy, Is.EqualTo(energy));
            Assert.That(duel.Outcome.HasValue, Is.False);
        }

        [UnityTest]
        public IEnumerator EndTurnOnly_TimeoutLineShown()
        {
            // The striker survives three turns against the centre mid (the AppFlow default pairing), so the duel
            // runs out of time; the first field slot (LB) gets tackled before that with every seed.
            var (screen, duel) = TestRig.Create(ScenarioKind.FieldAttack, 1, Slot.ST);
            for (int i = 0; i < 10 && !screen.ResultShown; i++)
            {
                yield return TestRig.Idle(screen);
                screen.ClickEndTurn();
                yield return null;
            }
            yield return TestRig.Idle(screen);

            Assert.That(duel.Outcome, Is.EqualTo(DuelOutcome.DefenderWon));
            Assert.That(duel.User.Hp, Is.GreaterThan(0));
            Assert.That(screen.ResultText, Does.Contain("Süre doldu"));
        }

        [UnityTest]
        public IEnumerator HandMatchesStateAfterDrawEffects()
        {
            // The keeper's deck has derin_nefes (block 2, draw 1); play draw cards first whenever one is affordable.
            var (screen, duel) = TestRig.Create(ScenarioKind.ShotAsKeeper, 1);
            var policy = new GoodPolicy();
            var rng = new Rng(1);
            int drawPlays = 0;
            yield return TestRig.Idle(screen);
            Assert.That(screen.HandCardCount, Is.EqualTo(duel.User.Hand.Count), "after begin");
            for (int i = 0; i < TestRig.MaxDecisions && !duel.Outcome.HasValue; i++)
            {
                int draw = PlayableDrawCard(duel);
                if (draw >= 0)
                {
                    drawPlays++;
                    screen.ClickCard(draw);
                }
                else TestRig.Act(screen, policy.Decide(duel, null, rng));
                yield return TestRig.Idle(screen);
                Assert.That(screen.HandCardCount, Is.EqualTo(duel.User.Hand.Count), "after command " + i);
            }
            Assert.That(drawPlays, Is.GreaterThan(0), "no draw card was played");
        }

        private static int PlayableDrawCard(Duel duel)
        {
            for (int i = 0; i < duel.User.Hand.Count; i++)
            {
                if (!DuelPreview.CanPlay(duel, i)) continue;
                foreach (var e in duel.User.Hand[i].Effects)
                    if (e.Type == EffectType.Draw) return i;
            }
            return -1;
        }
    }
}
