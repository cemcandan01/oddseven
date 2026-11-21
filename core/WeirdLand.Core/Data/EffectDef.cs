namespace WeirdLand.Core.Data
{
    public class EffectDef
    {
        public EffectType Type { get; set; }
        public EffectTarget Target { get; set; } = EffectTarget.Opponent;
        public int Amount { get; set; }
        public Stat? ScaleStat { get; set; }
        public double Scale { get; set; }
        public StatusId? Status { get; set; }
        public int Stacks { get; set; }
        public Corner Corner { get; set; }
        public StatusId? BonusIfTargetHas { get; set; }
        public double BonusMultiplier { get; set; } = 1;
    }
}
