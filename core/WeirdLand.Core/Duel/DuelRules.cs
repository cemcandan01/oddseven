using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public enum DuelKind { Field, Shot, Penalty }

    public static class DuelRules
    {
        public const int EnergyPerTurn = 3;
        public const int HandSize = 4;
        public const double UnbalancedMult = 1.5;
        public const double TiredMult = 0.75;
        public const double NonCornerShotMult = 0.5;
        public const double UncoveredCornerMult = 2;

        public static int MaxTurns(DuelKind kind)
        {
            switch (kind)
            {
                case DuelKind.Field: return 3;
                case DuelKind.Shot: return 2;
                default: return 1;
            }
        }

        public static int KontrolMax(Stats s) => 10 + s.Power + s.Dribbling;
        public static int DengeMax(Stats s) => 10 + s.Defending + s.Power;
        public static int ShooterKontrolMax(Stats s) => 8 + s.Power;
        public static int KaleMax(Stats gk) => 8 + gk.Near + gk.Far;
    }
}
