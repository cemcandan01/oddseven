using System;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Tests
{
    public class GridMatchTests
    {
        [Test] public void Kickoff_PlacesAll14OnHomeCells_Unique()
        {
            var m = TestKit.NewGridMatch(seed: 1);
            m.Kickoff();
            Assert.That(m.Players.Count, Is.EqualTo(14));
            Assert.That(m.Players.Select(p => p.Pos).Distinct().Count(), Is.EqualTo(14));
            foreach (var p in m.Players) Assert.That(p.Pos, Is.EqualTo(GridGeometry.Home(p.Def.Slot, p.IsUser)));
            Assert.That(m.Players.Take(7).All(p => p.IsUser), Is.True);
            Assert.That(m.Players.Take(7).Select(p => p.Def.Slot), Is.EqualTo(new[] { Slot.GK, Slot.LB, Slot.RB, Slot.CM, Slot.LW, Slot.RW, Slot.ST }));
        }

        [Test] public void Kickoff_CarrierIsCmOfTeamWithBall()
        {
            var m = TestKit.NewGridMatch(seed: 1); var ev = m.Kickoff();
            Assert.That(m.Carrier.Def.Slot, Is.EqualTo(Slot.CM));
            Assert.That(m.Carrier.IsUser, Is.EqualTo(m.UserHasBall));
            Assert.That(ev.OfType<GridKickoff>().Single().CarrierId, Is.EqualTo(m.Carrier.Def.Id));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
        }

        [Test] public void Kickoff_SameSeed_SamePossession()
        {
            var seen = new System.Collections.Generic.HashSet<bool>();
            for (uint s = 1; s <= 20; s++)
            {
                var a = TestKit.NewGridMatch(s); a.Kickoff();
                var b = TestKit.NewGridMatch(s); b.Kickoff();
                Assert.That(a.UserHasBall, Is.EqualTo(b.UserHasBall), "seed " + s);
                Assert.That(a.Carrier.Def.Id, Is.EqualTo(b.Carrier.Def.Id));
                seen.Add(a.UserHasBall);
            }
            Assert.That(seen.Count, Is.EqualTo(2), "both possession outcomes across seeds 1..20");
        }

        [Test] public void PlayerAt_ReturnsPlayerOrNull()
        {
            var m = TestKit.NewGridMatch(1); m.Kickoff();
            Assert.That(m.PlayerAt(GridGeometry.Home(Slot.GK, true)).Def.Slot, Is.EqualTo(Slot.GK));
            Assert.That(m.PlayerAt(new Cell(1, 0)), Is.Null);
        }

        // mavi_cm speed 3 (slow), mavi_rw speed 4 (fast); mavi is the user team.
        private static GridMatch Setup(string carrierId, Cell at)
        {
            var m = TestKit.NewGridMatch(seed: 1); m.Kickoff();
            // park everyone far away on distinct cells so tests control the neighbourhood
            int i = 0;
            foreach (var p in m.Players) m.SetPos(p.Def.Id, new Cell(9 + (i / 8), i++ % 8));
            m.SetCarrier(carrierId);
            m.SetPos(carrierId, at);
            return m;
        }

        [Test] public void Reachable_SlowCarrier_OnlyDistance1()
        {
            var m = Setup("mavi_cm", new Cell(5, 3));
            var r = m.ReachableCells();
            Assert.That(r, Is.Not.Empty);
            Assert.That(r.All(c => GridGeometry.Distance(c, m.Carrier.Pos) == 1), Is.True);
        }

        [Test] public void Reachable_FastCarrier_IncludesDistance2()
        {
            var m = Setup("mavi_rw", new Cell(5, 3));
            Assert.That(m.ReachableCells().Any(c => GridGeometry.Distance(c, m.Carrier.Pos) == 2), Is.True);
        }

        [Test] public void Reachable_SortedByXThenY()
        {
            var m = Setup("mavi_rw", new Cell(5, 3));
            var r = m.ReachableCells();
            Assert.That(r, Is.EqualTo(r.OrderBy(c => c.X).ThenBy(c => c.Y).ToList()));
        }

        [Test] public void Reachable_CannotHopOverOpponentWall()
        {
            var m = Setup("mavi_rw", new Cell(5, 3));
            m.SetPos("kirmizi_cm", new Cell(6, 2));
            m.SetPos("kirmizi_lb", new Cell(6, 3));
            m.SetPos("kirmizi_rb", new Cell(6, 4));
            Assert.That(m.ReachableCells().Contains(new Cell(7, 3)), Is.False);
        }

        [Test] public void Reachable_AllNeighboursOccupied_ReturnsEmpty()
        {
            var m = Setup("mavi_rw", new Cell(5, 3));
            var ids = new[] { "kirmizi_gk", "kirmizi_lb", "kirmizi_rb", "kirmizi_cm", "kirmizi_lw", "kirmizi_rw", "kirmizi_st", "mavi_gk" };
            int k = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) m.SetPos(ids[k++], new Cell(5 + dx, 3 + dy));
            Assert.That(m.ReachableCells(), Is.Empty);
        }

        [Test] public void Advance_ToUnreachableCell_Throws()
        {
            var m = Setup("mavi_cm", new Cell(5, 3));
            Assert.Throws<ArgumentException>(() => m.Advance(new Cell(8, 3)));
        }

        [Test] public void Advance_WhenNotAwaitingAction_Throws()
        {
            var m = Setup("mavi_cm", new Cell(5, 3));
            m.SetPhase(GridPhase.Ended);
            Assert.Throws<InvalidOperationException>(() => m.Advance(new Cell(6, 3)));
        }

        [Test] public void Advance_MovesCarrier_EmitsPlayerMoved()
        {
            var m = Setup("mavi_cm", new Cell(5, 3));
            var ev = m.Advance(new Cell(6, 3));
            Assert.That(m.Carrier.Pos, Is.EqualTo(new Cell(6, 3)));
            var pm = ev.OfType<PlayerMoved>().Single();
            Assert.That((pm.PlayerId, pm.FromX, pm.FromY, pm.ToX, pm.ToY), Is.EqualTo(("mavi_cm", 5, 3, 6, 3)));
        }
    }
}
