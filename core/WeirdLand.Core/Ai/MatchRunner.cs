using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Ai
{
    public class MatchResult
    {
        public int UserScore, OppScore, Duels, Turns, Passes, Moments;
        public List<GameEvent> Events = new List<GameEvent>();
    }

    public static class MatchRunner
    {
        public const int MaxActions = 10000;

        public static MatchResult Play(MatchEngine m, IUserPolicy p, Rng rng, Func<MatchEngine, Tactic> halfTimeTactic = null)
        {
            var r = new MatchResult();
            int actions = 0;

            void Add(IReadOnlyList<GameEvent> evs)
            {
                foreach (var e in evs)
                {
                    r.Events.Add(e);
                    if (e is PassAttempted) r.Passes++;
                }
            }

            while (m.Phase != MatchPhase.Ended)
            {
                if (m.Phase == MatchPhase.HalfTime && halfTimeTactic != null)
                    m.SetTactic(true, halfTimeTactic(m));
                Add(m.StartNextDuel());
                var duel = m.CurrentDuel;
                while (!duel.Outcome.HasValue)
                {
                    if (++actions > MaxActions)
                        throw new InvalidOperationException("MatchRunner exceeded MaxActions.");
                    switch (p.Decide(duel, m, rng))
                    {
                        case PlayCardAction a: Add(duel.PlayCard(a.HandIndex)); break;
                        case PassAction a: Add(duel.Pass(a.TargetId)); break;
                        default: Add(duel.EndTurn()); break;
                    }
                }
                r.Turns += duel.Turn;
                r.Duels++;
                Add(m.CompleteDuel());
            }
            r.UserScore = m.User.Score;
            r.OppScore = m.Opponent.Score;
            r.Moments = m.MomentsPlayed;
            return r;
        }
    }
}
