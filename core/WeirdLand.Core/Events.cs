using WeirdLand.Core.Data;

namespace WeirdLand.Core
{
    public enum DuelOutcome { AttackerWon, DefenderWon, PassSucceeded, PassFailed }

    public abstract record GameEvent;
    public record CardPlayed(bool ByUser, string CardId) : GameEvent;
    public record DamageDealt(bool ToUser, int Amount, int Blocked, int HpAfter) : GameEvent;
    public record BlockGained(bool User, int Amount) : GameEvent;
    public record StatusApplied(bool ToUser, StatusId Status, int Stacks) : GameEvent;
    /// <summary>A timed status ticked down. Stacks 0 means it was removed.</summary>
    public record StatusChanged(bool ToUser, StatusId Status, int Stacks) : GameEvent;
    /// <summary>A side's block was cleared (only emitted when it was above 0).</summary>
    public record BlockReset(bool User) : GameEvent;
    /// <summary>A user turn starts, before energy refill and draw.</summary>
    public record TurnStarted(int Turn) : GameEvent;
    public record CardsDrawn(bool User, int Count) : GameEvent;
    public record EnergyChanged(bool User, int Energy) : GameEvent;
    public record IntentRevealed(string CardId, string PassTargetId) : GameEvent;
    public record PassAttempted(bool ByUser, string TargetId, double Chance, bool Success) : GameEvent;
    public record DuelEnded(DuelOutcome Outcome) : GameEvent;
    public record MomentStarted(int Index, bool UserAttacks, Lane Lane) : GameEvent;
    public record GoalScored(bool ByUser, string ScorerId) : GameEvent;
    public record HypeChanged(string PlayerId, int Hype, bool Alev) : GameEvent;
    public record KondisyonChanged(string PlayerId, int Value) : GameEvent;
    public record HalfTime() : GameEvent;
    public record MatchEnded(int UserScore, int OppScore) : GameEvent;
}
