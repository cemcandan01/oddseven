using System.Collections.Generic;
using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Ai
{
    /// <summary>Picks uniformly among playable cards, EndTurn and (if allowed) one Pass.</summary>
    public class RandomPolicy : IUserPolicy
    {
        public DuelAction Decide(Duel duel, MatchEngine match, Rng rng)
        {
            var options = new List<DuelAction>();
            for (int i = 0; i < duel.User.Hand.Count; i++)
                if (duel.User.Hand[i].Cost <= duel.User.Energy) options.Add(new PlayCardAction(i));
            options.Add(new EndTurnAction());
            if (duel.CanPass) options.Add(new PassAction(null));
            var pick = options[rng.Next(options.Count)];
            if (pick is PassAction)
                return new PassAction(duel.UserPassTargets[rng.Next(duel.UserPassTargets.Count)].Id);
            return pick;
        }
    }
}
