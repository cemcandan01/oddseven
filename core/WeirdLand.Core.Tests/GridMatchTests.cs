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
    }
}
