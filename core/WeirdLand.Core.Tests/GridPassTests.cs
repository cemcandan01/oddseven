using System;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Tests
{
    public class GridPassTests
    {
        // everyone parked far away; mavi_cm carries at `from`, mavi_st sits at `to`.
        private static GridMatch Setup(Cell from, Cell to)
        {
            var m = TestKit.NewGridMatch(seed: 1); m.Kickoff();
            int i = 0;
            foreach (var p in m.Players) m.SetPos(p.Def.Id, new Cell(9 + (i / 8), i++ % 8));
            m.SetCarrier("mavi_cm");
            m.SetPos("mavi_cm", from);
            m.SetPos("mavi_st", to);
            return m;
        }

        private static GridPlayer P(GridMatch m, string id) => m.Players.First(p => p.Def.Id == id);

        private static double Base(GridMatch m, int distance)
            => MatchRules.PassBase - MatchRules.PassPerDistance * distance
               + MatchRules.PassPerStat * P(m, "mavi_cm").Def.Stats.Passing;

        [Test] public void Chance_OpenShortPass_IsHigh()
        {
            var m = Setup(new Cell(4, 3), new Cell(6, 3));
            Assert.That(GridPass.Chance(m, P(m, "mavi_cm"), P(m, "mavi_st")), Is.EqualTo(MatchRules.PassMax));
        }

        [Test] public void Chance_EachInterceptorCosts020()
        {
            var m = Setup(new Cell(1, 3), new Cell(7, 3));
            double open = GridPass.Chance(m, P(m, "mavi_cm"), P(m, "mavi_st"));
            Assert.That(open, Is.EqualTo(Base(m, 6)).Within(1e-9));
            Assert.That(open, Is.LessThan(MatchRules.PassMax));
            m.SetPos("kirmizi_cm", new Cell(4, 3));
            double one = GridPass.Chance(m, P(m, "mavi_cm"), P(m, "mavi_st"));
            Assert.That(open - one, Is.EqualTo(0.20).Within(1e-9));
        }

        [Test] public void Chance_ReceiverMarkerCosts010()
        {
            var m = Setup(new Cell(1, 3), new Cell(7, 3));
            double open = GridPass.Chance(m, P(m, "mavi_cm"), P(m, "mavi_st"));
            m.SetPos("kirmizi_cm", new Cell(8, 3)); // adjacent to receiver only
            Assert.That(GridPass.Interceptors(m, P(m, "mavi_cm"), P(m, "mavi_st")), Is.Empty);
            double marked = GridPass.Chance(m, P(m, "mavi_cm"), P(m, "mavi_st"));
            Assert.That(open - marked, Is.EqualTo(0.10).Within(1e-9));
        }

        [Test] public void Chance_ClampedTo005()
        {
            var m = Setup(new Cell(1, 3), new Cell(7, 3));
            m.SetPos("kirmizi_cm", new Cell(2, 3));
            m.SetPos("kirmizi_lb", new Cell(3, 2));
            m.SetPos("kirmizi_rb", new Cell(4, 4));
            m.SetPos("kirmizi_lw", new Cell(5, 2));
            m.SetPos("kirmizi_st", new Cell(3, 4));
            Assert.That(GridPass.Interceptors(m, P(m, "mavi_cm"), P(m, "mavi_st")).Count, Is.EqualTo(5));
            Assert.That(GridPass.Chance(m, P(m, "mavi_cm"), P(m, "mavi_st")), Is.EqualTo(MatchRules.PassMin));
        }

        [Test] public void Interceptors_IncludeAdjacentToLine_ExcludeEndpointsAndKeepers()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            m.SetPos("kirmizi_lb", new Cell(3, 4));  // adjacent to interior cell: yes
            m.SetPos("kirmizi_cm", new Cell(2, 4));  // adjacent to interior cell: yes
            m.SetPos("kirmizi_rb", new Cell(5, 3));  // only adjacent to receiver endpoint: no
            m.SetPos("kirmizi_gk", new Cell(2, 2));  // keeper: no
            var ids = GridPass.Interceptors(m, P(m, "mavi_cm"), P(m, "mavi_st")).Select(p => p.Def.Id).ToList();
            Assert.That(ids, Is.EqualTo(new[] { "kirmizi_lb", "kirmizi_cm" }));
            Assert.That(m.PassLineContested("mavi_st"), Is.True);
        }

        [Test] public void Pass_Success_ReceiverBecomesCarrier()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            m.PassRollOverride = _ => true;
            var ev = m.Pass("mavi_st");
            Assert.That(ev.OfType<BallPassed>().Single(), Is.EqualTo(new BallPassed("mavi_cm", "mavi_st")));
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("mavi_st"));
            Assert.That(m.UserHasBall, Is.True);
            Assert.That(ev.OfType<PassIntercepted>(), Is.Empty);
        }

        [Test] public void Pass_Fail_BallGoesToInterceptorNearestReceiver()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            m.SetPos("kirmizi_lb", new Cell(2, 4));
            m.SetPos("kirmizi_cm", new Cell(3, 4));
            m.PassRollOverride = _ => false;
            var ev = m.Pass("mavi_st");
            Assert.That(ev.OfType<PassIntercepted>().Single(),
                Is.EqualTo(new PassIntercepted("mavi_cm", "mavi_st", "kirmizi_cm")));
            Assert.That(ev.OfType<PossessionChanged>().First(), Is.EqualTo(new PossessionChanged(false, "kirmizi_cm")));
            Assert.That(ev.OfType<BallPassed>(), Is.Empty);
            Assert.That(ev.ToList().FindIndex(e => e is PassIntercepted), Is.LessThan(ev.ToList().FindIndex(e => e is PossessionChanged)));
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("kirmizi_cm"));
            Assert.That(m.UserHasBall, Is.False);
        }

        [Test] public void Pass_Fail_EquidistantInterceptors_PlayerListOrderWins()
        {
            var m = Setup(new Cell(1, 1), new Cell(5, 5)); // line (2,2),(3,3),(4,4)
            m.SetPos("kirmizi_cm", new Cell(2, 3)); // touches the line first, distance 3 to receiver
            m.SetPos("kirmizi_lb", new Cell(4, 2)); // touches the line later, distance 3 to receiver
            var ids = GridPass.Interceptors(m, P(m, "mavi_cm"), P(m, "mavi_st")).Select(p => p.Def.Id).ToList();
            Assert.That(ids, Is.EqualTo(new[] { "kirmizi_cm", "kirmizi_lb" }));
            m.PassRollOverride = _ => false;
            m.Pass("mavi_st");
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("kirmizi_lb")); // lb precedes cm in player list
        }

        [Test] public void Pass_Fail_NoCandidates_AdjacentToReceiverFirst()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            m.SetPos("kirmizi_st", new Cell(5, 3)); // adjacent to receiver, not to the line interior
            m.PassRollOverride = _ => false;
            m.Pass("mavi_st");
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("kirmizi_st"));
        }

        [Test] public void Pass_Fail_NothingNearby_NearestOpponentNonKeeper()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            m.SetPos("kirmizi_rw", new Cell(7, 3));
            m.PassRollOverride = _ => false;
            m.Pass("mavi_st");
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("kirmizi_rw"));
            Assert.That(m.Carrier.IsUser, Is.False);
        }

        [Test] public void Pass_RunsMoveTail_DuelCanOpen()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            m.SetPos("kirmizi_lb", new Cell(5, 3)); // adjacent to receiver
            m.PassRollOverride = _ => true;
            var ev = m.Pass("mavi_st");
            Assert.That(ev.OfType<DuelTriggered>().Count(), Is.EqualTo(1));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.InDuel));
        }

        [Test] public void Pass_ToSelf_OrKeeper_OrOpponent_Throws()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            Assert.Throws<ArgumentException>(() => m.Pass("mavi_cm"));
            Assert.Throws<ArgumentException>(() => m.Pass("mavi_gk"));
            Assert.Throws<ArgumentException>(() => m.Pass("kirmizi_st"));
            Assert.Throws<ArgumentException>(() => m.Pass("nobody"));
        }

        [Test] public void Pass_WrongPhase_Throws()
        {
            var m = Setup(new Cell(1, 3), new Cell(4, 3));
            m.SetPhase(GridPhase.InDuel);
            Assert.Throws<InvalidOperationException>(() => m.Pass("mavi_st"));
        }

        [Test] public void PassTargets_NeverEmpty()
        {
            for (uint s = 1; s <= 10; s++)
            {
                var m = TestKit.NewGridMatch(s); m.Kickoff();
                var t = m.PassTargets();
                Assert.That(t.Count, Is.EqualTo(5));
                Assert.That(t.All(p => p.IsUser == m.Carrier.IsUser && p != m.Carrier && p.Def.Slot != Slot.GK), Is.True);
            }
        }
    }
}
