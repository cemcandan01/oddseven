using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public static class EnemyAi
    {
        public static EnemyIntent ChooseIntent(Duel d, Rng rng)
        {
            var enemy = d.Enemy;
            var user = d.User;
            Duel.DrawCards(enemy, 2, rng);
            var drawn = new List<CardInstance>(enemy.Hand);
            enemy.Hand.Clear();

            if (d.Kind == DuelKind.Field && !d.UserIsAttacker && d.EnemyPassTargets.Count > 0)
            {
                double chance = DuelRules.PassChance(enemy, user);
                if (chance >= DuelRules.EnemyPassThreshold)
                {
                    PlayerDef target = null;
                    int bestSum = int.MinValue;
                    foreach (var t in d.EnemyPassTargets)
                    {
                        int sum = t.Stats.Dribbling + t.Stats.Shooting;
                        if (sum > bestSum) { bestSum = sum; target = t; }
                    }
                    enemy.Discard.AddRange(drawn);
                    return new EnemyIntent { PassTarget = target, PassChance = chance };
                }
            }
            if (drawn.Count == 0) return new EnemyIntent();

            CardInstance best = null;
            double bestScore = double.NegativeInfinity;
            foreach (var c in drawn)
            {
                if (Damage(c, enemy, user, d.Kind) >= user.Hp + user.Block) { best = c; break; }
                double s = Score(c, enemy, user, d.Kind);
                if (s > bestScore) { bestScore = s; best = c; }
            }
            foreach (var c in drawn) if (!ReferenceEquals(c, best)) enemy.Discard.Add(c);
            return new EnemyIntent { Card = best };
        }

        internal static int Damage(CardInstance c, Combatant src, Combatant tgt, DuelKind kind)
        {
            int total = 0;
            foreach (var e in c.Effects)
                if (e.Type == EffectType.Damage) total += EffectResolver.ComputeDamage(e, src, tgt, kind);
            return total;
        }

        private static double Score(CardInstance c, Combatant src, Combatant tgt, DuelKind kind)
        {
            double score = Damage(c, src, tgt, kind);
            foreach (var e in c.Effects)
            {
                if (e.Type == EffectType.Block)
                {
                    int amt = e.Amount + (e.ScaleStat.HasValue ? (int)Math.Floor(e.Scale * src.Player.Stats.Get(e.ScaleStat.Value)) : 0);
                    score += 0.8 * amt;
                }
                else if (e.Type == EffectType.Status && e.Status.HasValue)
                {
                    var s = e.Status.Value;
                    bool onOpponent = e.Target == EffectTarget.Opponent;
                    if (onOpponent && (s == StatusId.Unbalanced || s == StatusId.Tired)) score += 3;
                    else if (!onOpponent && (s == StatusId.Momentum || s == StatusId.CoverNear || s == StatusId.CoverFar)) score += 2;
                }
            }
            return score;
        }
    }
}
