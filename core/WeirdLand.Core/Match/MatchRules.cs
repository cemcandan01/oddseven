using System;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Match
{
    public static class MatchRules
    {
        public const int MomentsMin = 8;
        public const int MomentsRange = 5;
        public const int StartKondisyon = 100;
        public const int KondisyonPerDuel = 10;
        public const int LowKondisyon = 30;
        public const int HypeMax = 3;
        public const int LowHpPercent = 30;
        public const int UstunlukStep = 10;
        public const int UstunlukMax = 100;
        public const int TacticBonus = 10;
        public const int EnemyAlevMomentum = 2;
        public const int DefensiveHpBonus = 3;
        /// <summary>Enemy damage scale at tier 1. The enemy plays one card per turn, the user up to four.</summary>
        public const int EnemyPowerBasePercent = 260;
        public const int TierPowerPercent = 0;
        public const int MatchMinutes = 90;
        public const int GridWidth = 12;
        public const int GridHeight = 8;
        public const int GridMatchMoves = 90;
        public const int StunMoves = 2;
        public const int FastSpeed = 4;
        public const double PassBase = 0.95;
        public const double PassPerDistance = 0.04;
        public const double PassPerStat = 0.03;
        public const double PassPerInterceptor = 0.20;
        public const double PassPerReceiverMarker = 0.10;
        public const double PassMin = 0.05;
        public const double PassMax = 0.95;
        public const double ShiftFactor = 0.5;

        public static int TacticValue(Tactic t) => t == Tactic.Attacking ? TacticBonus : t == Tactic.Defensive ? -TacticBonus : 0;

        public static double Mid(PlayerDef cm) => (cm.Stats.Passing + cm.Stats.Power) / 2.0;

        public static int NextUstunluk(int u, double userMid, double oppMid, int userHype, int oppHype,
            Tactic userTac, Tactic oppTac, double noise01)
        {
            double v = 0.5 * u + 8 * (userMid - oppMid) + 4.0 * (userHype - oppHype) / 3
                + TacticValue(userTac) - TacticValue(oppTac) + (noise01 * 40 - 20);
            return Clamp((int)Math.Round(v, MidpointRounding.AwayFromZero));
        }

        public static int Clamp(int u) => Math.Max(-UstunlukMax, Math.Min(UstunlukMax, u));

        public static double UserAttackChance(int u) => 0.5 + u / 250.0;

        public static int PowerScalePercent(int tier) => EnemyPowerBasePercent + TierPowerPercent * (tier - 1);
    }
}
