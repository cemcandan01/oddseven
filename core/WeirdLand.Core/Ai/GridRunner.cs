using System;
using System.Collections.Generic;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Ai
{
    public class GridResult
    {
        public int UserScore, OppScore, Duels, Moves;
        public int MaxSameDuelStreak;
        public List<GameEvent> Events = new List<GameEvent>();
    }

    public static class GridRunner
    {
        public const int MaxActions = 100000;

        public static GridResult Play(GridMatch m, IGridPolicy userPolicy, IUserPolicy duelPolicy, Rng rng)
        {
            var r = new GridResult();
            int actions = 0;
            string lastPair = null;
            int streak = 0;

            void Add(IReadOnlyList<GameEvent> evs)
            {
                foreach (var e in evs)
                {
                    r.Events.Add(e);
                    if (e is DuelTriggered d)
                    {
                        r.Duels++;
                        string pair = string.CompareOrdinal(d.CarrierId, d.DefenderId) < 0
                            ? d.CarrierId + "|" + d.DefenderId : d.DefenderId + "|" + d.CarrierId;
                        streak = pair == lastPair ? streak + 1 : 1;
                        lastPair = pair;
                        if (streak > r.MaxSameDuelStreak) r.MaxSameDuelStreak = streak;
                    }
                }
            }

            Add(m.Kickoff());
            while (m.Phase != GridPhase.Ended)
            {
                if (++actions > MaxActions)
                    throw new InvalidOperationException("GridRunner exceeded MaxActions.");
                if (m.Phase == GridPhase.InDuel)
                {
                    var duel = m.CurrentDuel;
                    if (duel.Outcome.HasValue) { Add(m.CompleteDuel()); continue; }
                    var action = duelPolicy.Decide(duel, null, rng);
                    switch (action)
                    {
                        case PlayCardAction a: Add(duel.PlayCard(a.HandIndex)); break;
                        case EndTurnAction _: Add(duel.EndTurn()); break;
                        default:
                            throw new InvalidOperationException("GridRunner: unsupported duel action "
                                + (action == null ? "null" : action.GetType().Name) + ".");
                    }
                }
                else if (m.UserHasBall)
                {
                    var action = userPolicy.Decide(m, rng);
                    switch (action)
                    {
                        case GridAdvance a: Add(m.Advance(a.To)); break;
                        case GridPassTo a: Add(m.Pass(a.TargetId)); break;
                        default:
                            throw new InvalidOperationException("GridRunner: unknown action "
                                + (action == null ? "null" : action.GetType().Name) + ".");
                    }
                }
                else Add(m.ResolveOpponentMove());
            }
            r.UserScore = m.UserScore;
            r.OppScore = m.OppScore;
            r.Moves = m.Move;
            return r;
        }
    }
}
