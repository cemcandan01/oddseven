using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Ai
{
    public interface IUserPolicy
    {
        DuelAction Decide(Duel duel, MatchEngine match, Rng rng);
    }
}
