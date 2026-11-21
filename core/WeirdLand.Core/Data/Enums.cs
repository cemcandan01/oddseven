namespace WeirdLand.Core.Data
{
    public enum Slot { GK, LB, RB, CM, LW, RW, ST }
    public enum Position { GK, DEF, MID, WING, ST }
    public enum Lane { Left, Center, Right }
    public enum CardRole { Attack, Defense, Both }
    public enum Rarity { Common, Rare, Legendary }
    public enum Stat { Speed, Power, Passing, Dribbling, Shooting, Defending, Aerial, Near, Far }
    public enum StatusId { Unbalanced, Momentum, Tired, CoverNear, CoverFar }
    public enum EffectType { Damage, Block, Status, Draw, Energy, Kondisyon }
    public enum EffectTarget { Self, Opponent }
    public enum Corner { None, Near, Far, Top }

    public static class SlotInfo
    {
        public static Position PositionOf(Slot s)
        {
            switch (s)
            {
                case Slot.GK: return Position.GK;
                case Slot.LB:
                case Slot.RB: return Position.DEF;
                case Slot.CM: return Position.MID;
                case Slot.LW:
                case Slot.RW: return Position.WING;
                default: return Position.ST;
            }
        }

        public static Lane LaneOf(Slot s)
        {
            switch (s)
            {
                case Slot.LB:
                case Slot.LW: return Lane.Left;
                case Slot.RB:
                case Slot.RW: return Lane.Right;
                default: return Lane.Center;
            }
        }
    }
}
