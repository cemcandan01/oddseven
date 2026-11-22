namespace WeirdLand.Core.Ai
{
    public abstract record DuelAction;
    public record PlayCardAction(int HandIndex) : DuelAction;
    public record PassAction(string TargetId) : DuelAction;
    public record EndTurnAction : DuelAction;
}
