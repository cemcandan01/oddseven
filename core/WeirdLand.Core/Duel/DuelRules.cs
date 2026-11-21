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
        /// <summary>Corner (shot) cards against a non-goalkeeper target.</summary>
        public const double CornerVsFieldMult = 0.5;
        public const double EnemyPassThreshold = 0.6;
        public const int PassCost = 1;

        public static double PassChance(Combatant carrier, Combatant defender)
        {
            double p = 0.30 + 0.06 * carrier.Player.Stats.Passing
                + 0.30 * (1.0 - (double)defender.Hp / defender.MaxHp)
                + (defender.StatusOf(StatusId.Unbalanced) > 0 ? 0.15 : 0)
                - 0.25 * (1.0 - (double)carrier.Hp / carrier.MaxHp);
            return System.Math.Min(0.95, System.Math.Max(0.05, p));
        }

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
