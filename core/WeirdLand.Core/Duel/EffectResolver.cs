using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public static class EffectResolver
    {
        private static int ScaledBonus(EffectDef e, Combatant src)
            => e.ScaleStat.HasValue ? (int)Math.Floor(e.Scale * src.Player.Stats.Get(e.ScaleStat.Value)) : 0;

        public static int ComputeDamage(EffectDef e, Combatant src, Combatant tgt, DuelKind kind)
        {
            double d = e.Amount + ScaledBonus(e, src);
            d += src.StatusOf(StatusId.Momentum);
            if (src.StatusOf(StatusId.Tired) > 0) d = Math.Floor(d * DuelRules.TiredMult);
            if (e.BonusIfTargetHas.HasValue && tgt.StatusOf(e.BonusIfTargetHas.Value) > 0)
                d = Math.Floor(d * e.BonusMultiplier);
            if (tgt.StatusOf(StatusId.Unbalanced) > 0) d = Math.Floor(d * DuelRules.UnbalancedMult);

            if ((kind == DuelKind.Shot || kind == DuelKind.Penalty) && tgt.Player.Slot == Slot.GK)
            {
                var gk = tgt.Player.Stats;
                switch (e.Corner)
                {
                    case Corner.None:
                        d = Math.Floor(d * DuelRules.NonCornerShotMult);
                        break;
                    case Corner.Near:
                        d = Math.Max(d - gk.Near, 0);
                        if (tgt.StatusOf(StatusId.CoverFar) > 0 && tgt.StatusOf(StatusId.CoverNear) == 0)
                            d = Math.Floor(d * DuelRules.UncoveredCornerMult);
                        break;
                    case Corner.Far:
                        d = Math.Max(d - gk.Far, 0);
                        if (tgt.StatusOf(StatusId.CoverNear) > 0 && tgt.StatusOf(StatusId.CoverFar) == 0)
                            d = Math.Floor(d * DuelRules.UncoveredCornerMult);
                        break;
                }
            }

            d = Math.Floor(d * src.PowerScalePercent / 100.0);
            return d < 0 ? 0 : (int)d;
        }

        public static IEnumerable<GameEvent> Apply(EffectDef e, Combatant src, Combatant tgt, DuelKind kind, Action<Combatant, int> draw)
        {
            var events = new List<GameEvent>();
            switch (e.Type)
            {
                case EffectType.Damage:
                {
                    int dmg = ComputeDamage(e, src, tgt, kind);
                    int blocked = Math.Min(tgt.Block, dmg);
                    tgt.Block -= blocked;
                    tgt.Hp = Math.Max(0, tgt.Hp - (dmg - blocked));
                    events.Add(new DamageDealt(tgt.IsUser, dmg - blocked, blocked, tgt.Hp));
                    break;
                }
                case EffectType.Block:
                {
                    int amt = e.Amount + ScaledBonus(e, src);
                    src.Block += amt;
                    events.Add(new BlockGained(src.IsUser, amt));
                    break;
                }
                case EffectType.Status:
                {
                    var holder = e.Target == EffectTarget.Self ? src : tgt;
                    var id = e.Status.Value;
                    holder.Statuses[id] = holder.StatusOf(id) + e.Stacks;
                    events.Add(new StatusApplied(holder.IsUser, id, holder.Statuses[id]));
                    break;
                }
                case EffectType.Draw:
                    draw?.Invoke(src, e.Amount);
                    break;
                case EffectType.Energy:
                    src.Energy += e.Amount;
                    events.Add(new EnergyChanged(src.IsUser, src.Energy));
                    break;
                case EffectType.Kondisyon:
                    src.KondisyonSpent += e.Amount;
                    break;
            }
            return events;
        }
    }
}
