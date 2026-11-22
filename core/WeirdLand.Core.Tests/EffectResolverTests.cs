using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class EffectResolverTests
    {
        private Combatant src, tgt, gk;

        [SetUp]
        public void Setup()
        {
            src = new Combatant { Player = Player("s", Slot.ST, new Stats { Dribbling = 4, Power = 3 }), IsUser = true, Hp = 20, MaxHp = 20 };
            tgt = new Combatant { Player = Player("t", Slot.CM), Hp = 20, MaxHp = 20 };
            gk = new Combatant { Player = Player("g", Slot.GK), Hp = 20, MaxHp = 20 };
        }

        private int Compute(EffectDef e) => EffectResolver.ComputeDamage(e, src, tgt, DuelKind.Field);
        private int ComputeShot(EffectDef e) => EffectResolver.ComputeDamage(e, src, gk, DuelKind.Shot);
        private List<GameEvent> Apply(EffectDef e)
            => EffectResolver.Apply(e, src, tgt, DuelKind.Field, (c, n) => { }).ToList();

        [Test] public void ComputeBlock_Flat() => Assert.That(EffectResolver.ComputeBlock(Blk(4), src), Is.EqualTo(4));
        [Test] public void ComputeBlock_ScalesWithStat()
        {
            src.Player.Stats.Power = 2;
            var e = new EffectDef { Type = EffectType.Block, Target = EffectTarget.Self, Amount = 3, ScaleStat = Stat.Power, Scale = 1 };
            Assert.That(EffectResolver.ComputeBlock(e, src), Is.EqualTo(5));
        }
        [Test] public void ComputeBlock_NonBlockEffect_IsZero() => Assert.That(EffectResolver.ComputeBlock(Dmg(5), src), Is.EqualTo(0));

        [Test] public void Damage_AddsScaledStat() => Assert.That(Compute(Dmg(4, Stat.Dribbling, 1)), Is.EqualTo(8));
        [Test] public void Momentum_AddsFlat() { src.StatusMap[StatusId.Momentum] = 2; Assert.That(Compute(Dmg(4)), Is.EqualTo(6)); }
        [Test] public void Tired_Reduces() { src.StatusMap[StatusId.Tired] = 1; Assert.That(Compute(Dmg(8)), Is.EqualTo(6)); }
        [Test] public void Unbalanced_Increases() { tgt.StatusMap[StatusId.Unbalanced] = 1; Assert.That(Compute(Dmg(5)), Is.EqualTo(7)); }
        [Test]
        public void BonusIfTargetHas_Multiplies()
        {
            tgt.StatusMap[StatusId.Unbalanced] = 1;
            var e = Dmg(4); e.BonusIfTargetHas = StatusId.Unbalanced; e.BonusMultiplier = 2;
            Assert.That(Compute(e), Is.EqualTo(12));
        }
        [Test] public void Block_AbsorbsFirst() { tgt.Block = 3; Apply(Dmg(5)); Assert.That(tgt.Hp, Is.EqualTo(18)); Assert.That(tgt.Block, Is.EqualTo(0)); }
        [Test] public void Shot_NonCorner_Halved() => Assert.That(ComputeShot(Dmg(8)), Is.EqualTo(4));
        [Test] public void Shot_NearCorner_ReducedByGkNear() { gk.Player.Stats.Near = 3; Assert.That(ComputeShot(AtCorner(Dmg(8), Corner.Near)), Is.EqualTo(5)); }
        [Test] public void Shot_UncoveredCorner_Doubled() { gk.Player.Stats.Far = 2; gk.StatusMap[StatusId.CoverNear] = 1; Assert.That(ComputeShot(AtCorner(Dmg(8), Corner.Far)), Is.EqualTo(12)); }
        [Test] public void Shot_CoveredCorner_NotDoubled() { gk.Player.Stats.Near = 2; gk.StatusMap[StatusId.CoverNear] = 1; Assert.That(ComputeShot(AtCorner(Dmg(8), Corner.Near)), Is.EqualTo(6)); }
        [Test] public void Shot_Top_NoReductionNoDouble() { gk.StatusMap[StatusId.CoverNear] = 1; Assert.That(ComputeShot(AtCorner(Dmg(6), Corner.Top)), Is.EqualTo(6)); }
        [Test] public void CornerCard_VsFieldPlayer_Halved() => Assert.That(Compute(AtCorner(Dmg(9), Corner.Near)), Is.EqualTo(4));
        [Test] public void CornerCard_TopVsFieldPlayer_Halved() => Assert.That(Compute(AtCorner(Dmg(8), Corner.Top)), Is.EqualTo(4));
        [Test]
        public void CornerCard_VsNonGkInShotDuel_Halved()
            => Assert.That(EffectResolver.ComputeDamage(AtCorner(Dmg(8), Corner.Far), src, tgt, DuelKind.Shot), Is.EqualTo(4));
        [Test] public void NonCornerCard_VsFieldPlayer_Full() => Assert.That(Compute(Dmg(9)), Is.EqualTo(9));
        [Test] public void PowerScale_Applies() { src.PowerScalePercent = 120; Assert.That(Compute(Dmg(10)), Is.EqualTo(12)); }
        [Test] public void Damage_NeverNegative() { gk.Player.Stats.Near = 9; Assert.That(ComputeShot(AtCorner(Dmg(2), Corner.Near)), Is.EqualTo(0)); }

        [Test]
        public void Shot_GoalkeeperDamageToShooter_NotHalved()
        {
            var shooter = new Combatant { Player = Player("sh", Slot.ST), Hp = 20, MaxHp = 20 };
            Assert.That(EffectResolver.ComputeDamage(Dmg(8), gk, shooter, DuelKind.Shot), Is.EqualTo(8));
        }

        [Test]
        public void StatusEffect_TargetsSelfOrOpponent()
        {
            Apply(Sts(StatusId.Momentum, 1, EffectTarget.Self));
            Apply(Sts(StatusId.Unbalanced, 2));
            var evs = Apply(Sts(StatusId.Unbalanced, 2));
            Assert.That(src.StatusOf(StatusId.Momentum), Is.EqualTo(1));
            Assert.That(tgt.StatusOf(StatusId.Unbalanced), Is.EqualTo(4));
            Assert.That(evs.Single(), Is.EqualTo(new StatusApplied(false, StatusId.Unbalanced, 4)));
        }

        [Test]
        public void BlockEnergyKondisyonDraw_Apply()
        {
            Apply(Blk(3));
            Assert.That(src.Block, Is.EqualTo(3));
            src.Energy = 1;
            Apply(new EffectDef { Type = EffectType.Energy, Target = EffectTarget.Self, Amount = 2 });
            Assert.That(src.Energy, Is.EqualTo(3));
            var evs = Apply(new EffectDef { Type = EffectType.Kondisyon, Amount = 2 });
            Assert.That(src.KondisyonSpent, Is.EqualTo(2));
            Assert.That(evs, Is.Empty);
            int drawn = 0;
            EffectResolver.Apply(new EffectDef { Type = EffectType.Draw, Amount = 2 }, src, tgt, DuelKind.Field, (c, n) => drawn += n).ToList();
            Assert.That(drawn, Is.EqualTo(2));
        }

        [Test]
        public void CardInstance_Upgraded_UsesUpgradedValues()
        {
            var def = Card("c", 2, CardRole.Attack, Dmg(4));
            def.UpgradedCost = 1;
            def.UpgradedEffects = new List<EffectDef> { Dmg(7) };
            var plain = new CardInstance(def, false);
            var up = new CardInstance(def, true);
            Assert.That(plain.Cost, Is.EqualTo(2));
            Assert.That(plain.Effects[0].Amount, Is.EqualTo(4));
            Assert.That(up.Cost, Is.EqualTo(1));
            Assert.That(up.Effects[0].Amount, Is.EqualTo(7));
            var noUp = new CardInstance(Card("d", 2, CardRole.Attack, Dmg(4)), true);
            Assert.That(noUp.Cost, Is.EqualTo(2));
        }
    }
}
