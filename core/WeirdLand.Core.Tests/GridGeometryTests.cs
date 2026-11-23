using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Tests
{
    public class GridGeometryTests
    {
        [Test] public void Distance_IsChebyshev() => Assert.That(GridGeometry.Distance(new Cell(0,0), new Cell(3,1)), Is.EqualTo(3));
        [Test] public void Adjacent_IncludesDiagonal() => Assert.That(GridGeometry.Adjacent(new Cell(2,2), new Cell(3,3)), Is.True);
        [Test] public void Line_HasBothEnds_AndIsContiguous()
        {
            var l = GridGeometry.Line(new Cell(0,0), new Cell(5,2));
            Assert.That(l[0], Is.EqualTo(new Cell(0,0)));
            Assert.That(l[l.Count-1], Is.EqualTo(new Cell(5,2)));
            for (int i = 1; i < l.Count; i++) Assert.That(GridGeometry.Adjacent(l[i-1], l[i]), Is.True);
        }
        [Test] public void Home_UserCm_Is_4_3() => Assert.That(GridGeometry.Home(Slot.CM, true), Is.EqualTo(new Cell(4,3)));
        [Test] public void Home_OpponentIsMirrored() => Assert.That(GridGeometry.Home(Slot.LB, false), Is.EqualTo(new Cell(9,1)));
        [Test] public void ShotZone_User() { Assert.That(GridGeometry.InShotZone(new Cell(10,2), true), Is.True); Assert.That(GridGeometry.InShotZone(new Cell(10,1), true), Is.False); Assert.That(GridGeometry.InShotZone(new Cell(1,4), false), Is.True); }
        [Test] public void InBounds_RejectsOutside() { Assert.That(GridGeometry.InBounds(new Cell(11,7)), Is.True); Assert.That(GridGeometry.InBounds(new Cell(12,0)), Is.False); Assert.That(GridGeometry.InBounds(new Cell(0,-1)), Is.False); }
        [Test] public void AttackDir_UserPositive() { Assert.That(GridGeometry.AttackDir(true), Is.EqualTo(1)); Assert.That(GridGeometry.AttackDir(false), Is.EqualTo(-1)); }
    }
}
