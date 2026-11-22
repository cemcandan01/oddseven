using System;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class DuelScenarioTests
    {
        static Duel Make(ScenarioKind k, Slot u, Slot e, uint seed = 1)
            => DuelScenarios.Create(RealData(), new DuelScenario("mavi", "kirmizi", k, u, e), seed);

        [Test] public void FieldAttack_UserCarriesAgainstChosenDefender()
        {
            var d = Make(ScenarioKind.FieldAttack, Slot.LW, Slot.RB); d.Begin();
            Assert.That(d.Kind, Is.EqualTo(DuelKind.Field));
            Assert.That(d.UserIsAttacker, Is.True);
            Assert.That(d.User.Player.Id, Is.EqualTo("mavi_lw"));
            Assert.That(d.Enemy.Player.Id, Is.EqualTo("kirmizi_rb"));
            Assert.That(d.UserPassTargets.Select(p => p.Slot),
                Is.EqualTo(new[] { Slot.LB, Slot.RB, Slot.CM, Slot.RW, Slot.ST }));
        }
        [Test] public void FieldDefense_EnemyCarries_EnemyHasPassTargets()
        { var d = Make(ScenarioKind.FieldDefense, Slot.LB, Slot.RW); d.Begin();
          Assert.That(d.UserIsAttacker, Is.False); Assert.That(d.EnemyPassTargets.Count, Is.EqualTo(5)); Assert.That(d.UserPassTargets, Is.Empty); }
        [Test] public void ShotAsShooter_EnemyIsKeeper_TwoTurns()
        { var d = Make(ScenarioKind.ShotAsShooter, Slot.ST, Slot.GK); d.Begin();
          Assert.That(d.Kind, Is.EqualTo(DuelKind.Shot)); Assert.That(d.MaxTurns, Is.EqualTo(2));
          Assert.That(d.Enemy.Player.Slot, Is.EqualTo(Slot.GK)); Assert.That(d.UserPassTargets, Is.Empty); }
        [Test] public void ShotAsKeeper_UserIsKeeper()
        { var d = Make(ScenarioKind.ShotAsKeeper, Slot.GK, Slot.ST); d.Begin();
          Assert.That(d.UserIsAttacker, Is.False); Assert.That(d.User.Player.Slot, Is.EqualTo(Slot.GK)); }
        [Test] public void EnemyPower_FollowsOpponentTier()
        { var d = Make(ScenarioKind.FieldAttack, Slot.ST, Slot.CM);
          Assert.That(d.Enemy.PowerScalePercent, Is.EqualTo(MatchRules.PowerScalePercent(RealData().Team("kirmizi").Tier))); }
        [TestCase(ScenarioKind.FieldAttack, Slot.GK, Slot.CM)]
        [TestCase(ScenarioKind.ShotAsShooter, Slot.ST, Slot.CM)]
        [TestCase(ScenarioKind.ShotAsKeeper, Slot.ST, Slot.ST)]
        public void SlotNotAllowedForKind_Throws(ScenarioKind k, Slot u, Slot e)
            => Assert.Throws<ArgumentException>(() => Make(k, u, e));
        [Test] public void UnknownTeam_ThrowsNamingId()
        { var ex = Assert.Throws<ArgumentException>(() => DuelScenarios.Create(RealData(),
              new DuelScenario("yok_boyle", "kirmizi", ScenarioKind.FieldAttack, Slot.ST, Slot.CM), 1));
          Assert.That(ex.Message, Does.Contain("yok_boyle")); }
        [Test] public void SameSeed_SameOpeningHandAndIntent()
        { var a = Make(ScenarioKind.FieldAttack, Slot.ST, Slot.CM, 5); a.Begin();
          var b = Make(ScenarioKind.FieldAttack, Slot.ST, Slot.CM, 5); b.Begin();
          Assert.That(b.User.Hand.Select(c => c.Def.Id), Is.EqualTo(a.User.Hand.Select(c => c.Def.Id)));
          Assert.That(b.Intent.Card?.Def.Id, Is.EqualTo(a.Intent.Card?.Def.Id)); }
        [TestCase(ScenarioKind.FieldAttack, Slot.LW, Slot.RB)] [TestCase(ScenarioKind.FieldAttack, Slot.RB, Slot.LB)]
        [TestCase(ScenarioKind.FieldAttack, Slot.ST, Slot.CM)] [TestCase(ScenarioKind.FieldDefense, Slot.LB, Slot.RW)]
        [TestCase(ScenarioKind.FieldDefense, Slot.RW, Slot.LW)] [TestCase(ScenarioKind.FieldDefense, Slot.CM, Slot.CM)]
        [TestCase(ScenarioKind.ShotAsShooter, Slot.LW, Slot.GK)] [TestCase(ScenarioKind.ShotAsKeeper, Slot.GK, Slot.ST)]
        public void DefaultEnemySlot_Table(ScenarioKind k, Slot u, Slot expected)
            => Assert.That(DuelScenarios.DefaultEnemySlot(k, u), Is.EqualTo(expected));
        [Test] public void Slots_PerKind()
        { Assert.That(DuelScenarios.EnemySlots(ScenarioKind.ShotAsShooter), Is.EqualTo(new[] { Slot.GK }));
          Assert.That(DuelScenarios.UserSlots(ScenarioKind.ShotAsKeeper), Is.EqualTo(new[] { Slot.GK }));
          Assert.That(DuelScenarios.UserSlots(ScenarioKind.FieldAttack), Does.Not.Contain(Slot.GK).And.Count.EqualTo(6)); }
    }
}
