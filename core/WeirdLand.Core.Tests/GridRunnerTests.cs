using System.Collections.Generic;
using NUnit.Framework;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Tests
{
    public class GridRunnerTests
    {
        private static GridResult Run(uint seed)
        {
            var m = TestKit.NewGridMatch(seed);
            return GridRunner.Play(m, new GreedyGridPolicy(), new GoodPolicy(), new Rng(seed + 1000));
        }

        [Test] public void Runner_FinishesMatch_Seeds1to50()
        {
            for (uint s = 1; s <= 50; s++)
            {
                var r = Run(s);
                Assert.That(r.Moves, Is.EqualTo(MatchRules.GridMatchMoves), "seed " + s);
                Assert.That(r.Events[r.Events.Count - 1], Is.TypeOf<MatchEnded>(), "seed " + s);
            }
        }

        [Test] public void Runner_RandomPolicies_FinishMatch()
        {
            for (uint s = 1; s <= 20; s++)
            {
                var m = TestKit.NewGridMatch(s);
                var r = GridRunner.Play(m, new RandomGridPolicy(), new RandomPolicy(), new Rng(s));
                Assert.That(r.Moves, Is.EqualTo(MatchRules.GridMatchMoves), "seed " + s);
            }
        }

        [Test] public void Runner_NoSamePairDuelsMoreThan3InARow_200Seeds()
        {
            int worst = 0; uint worstSeed = 0;
            for (uint s = 1; s <= 200; s++)
            {
                var r = Run(s);
                if (r.MaxSameDuelStreak > worst) { worst = r.MaxSameDuelStreak; worstSeed = s; }
            }
            Assert.That(worst, Is.LessThanOrEqualTo(3), "max streak " + worst + " at seed " + worstSeed);
        }

        [Test] public void Greedy_PrefersOpenForwardPassOverBlockedOne()
        {
            var m = TestKit.NewGridMatch(1); m.Kickoff();
            int i = 0;
            foreach (var p in m.Players) m.SetPos(p.Def.Id, new Cell(9 + (i / 8), i++ % 8));
            m.SetCarrier("mavi_cm");
            m.SetPos("mavi_cm", new Cell(3, 3));
            m.SetPos("mavi_lw", new Cell(8, 1));   // blocked
            m.SetPos("mavi_rw", new Cell(8, 5));   // open
            m.SetPos("kirmizi_cm", new Cell(5, 2));
            m.SetPos("kirmizi_lb", new Cell(6, 2));
            var a = new GreedyGridPolicy().Decide(m, new Rng(1));
            Assert.That(a, Is.EqualTo(new GridPassTo("mavi_rw")));
        }

        [Test] public void Greedy_IsDeterministic_AndOpponentResolves()
        {
            var a = Run(5); var b = Run(5);
            Assert.That(EventLog.Fingerprint(a.Events), Is.EqualTo(EventLog.Fingerprint(b.Events)));
            Assert.That(a.UserScore, Is.EqualTo(b.UserScore));
        }

        [Test] public void ResolveOpponentMove_Throws_WhenUserHasBall()
        {
            var m = TestKit.NewGridMatch(1); m.Kickoff();
            m.SetCarrier("mavi_cm");
            Assert.Throws<System.InvalidOperationException>(() => m.ResolveOpponentMove());
        }
    }
}
