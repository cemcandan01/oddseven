using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Ai
{
    /// <summary>Heuristic user model with a fixed priority order.</summary>
    public class GoodPolicy : IUserPolicy
    {
        public DuelAction Decide(Duel duel, MatchEngine match, Rng rng)
        {
            var user = duel.User;
            var enemy = duel.Enemy;
            var playable = new List<int>();
            for (int i = 0; i < user.Hand.Count; i++)
                if (user.Hand[i].Cost <= user.Energy) playable.Add(i);

            // 1. high-chance pass
            if (duel.CanPass && duel.PassChance() >= 0.75) return Pass(duel, match);

            // 2. lethal
            foreach (var i in playable)
                if (TotalDamage(user.Hand[i], duel) >= enemy.Hp + enemy.Block) return new PlayCardAction(i);

            // 3. block against incoming lethal
            if (duel.IncomingDamage() >= user.Hp + user.Block)
            {
                int best = -1, bestBlock = 0;
                foreach (var i in playable)
                {
                    int b = BlockAmount(user.Hand[i], user);
                    if (b > bestBlock) { bestBlock = b; best = i; }
                }
                if (best >= 0) return new PlayCardAction(best);
            }

            // 4. status / cover / momentum / damage per energy
            foreach (var i in playable)
                if (AppliesStatus(user.Hand[i], EffectTarget.Opponent, StatusId.Unbalanced, StatusId.Tired))
                    return new PlayCardAction(i);

            if ((duel.Kind == DuelKind.Shot || duel.Kind == DuelKind.Penalty) && !duel.UserIsAttacker
                && duel.Intent != null && duel.Intent.Card != null)
            {
                var corner = IntentCorner(duel.Intent.Card);
                if (corner == Corner.Near || corner == Corner.Far)
                {
                    var cover = corner == Corner.Near ? StatusId.CoverNear : StatusId.CoverFar;
                    foreach (var i in playable)
                        if (AppliesStatus(user.Hand[i], EffectTarget.Self, cover)) return new PlayCardAction(i);
                }
            }

            foreach (var i in playable)
                if (AppliesStatus(user.Hand[i], EffectTarget.Self, StatusId.Momentum)) return new PlayCardAction(i);

            int bestIdx = -1;
            double bestRatio = 0;
            foreach (var i in playable)
            {
                var c = user.Hand[i];
                double ratio = TotalDamage(c, duel) / (double)Math.Max(1, c.Cost);
                if (ratio > bestRatio) { bestRatio = ratio; bestIdx = i; }
            }
            if (bestIdx >= 0) return new PlayCardAction(bestIdx);

            // 5. last-turn gamble
            if (duel.Turn >= duel.MaxTurns && duel.UserIsAttacker && duel.Kind == DuelKind.Field
                && duel.CanPass && duel.PassChance() >= 0.4)
                return Pass(duel, match);

            return new EndTurnAction();
        }

        private static DuelAction Pass(Duel duel, MatchEngine match)
        {
            bool final = match != null && match.Stage == ChainStage.FinalThird;
            PlayerDef best = null;
            int bestVal = int.MinValue;
            foreach (var t in duel.UserPassTargets)
            {
                int v = final ? t.Stats.Shooting : t.Stats.Dribbling;
                if (v > bestVal) { bestVal = v; best = t; }
            }
            return new PassAction(best.Id);
        }

        private static int TotalDamage(CardInstance c, Duel duel)
        {
            int total = 0;
            foreach (var e in c.Effects)
                if (e.Type == EffectType.Damage)
                    total += EffectResolver.ComputeDamage(e, duel.User, duel.Enemy, duel.Kind);
            return total;
        }

        private static int BlockAmount(CardInstance c, Combatant user)
        {
            int total = 0;
            foreach (var e in c.Effects)
                if (e.Type == EffectType.Block)
                    total += e.Amount + (e.ScaleStat.HasValue ? (int)Math.Floor(e.Scale * user.Player.Stats.Get(e.ScaleStat.Value)) : 0);
            return total;
        }

        private static bool AppliesStatus(CardInstance c, EffectTarget target, params StatusId[] ids)
        {
            foreach (var e in c.Effects)
            {
                if (e.Type != EffectType.Status || !e.Status.HasValue || e.Target != target) continue;
                foreach (var id in ids) if (e.Status.Value == id) return true;
            }
            return false;
        }

        private static Corner IntentCorner(CardInstance c)
        {
            foreach (var e in c.Effects)
                if (e.Type == EffectType.Damage && (e.Corner == Corner.Near || e.Corner == Corner.Far)) return e.Corner;
            return Corner.None;
        }
    }
}
