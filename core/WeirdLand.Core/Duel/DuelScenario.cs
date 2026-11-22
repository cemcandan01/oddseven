using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Duels
{
    public enum ScenarioKind { FieldAttack, FieldDefense, ShotAsShooter, ShotAsKeeper }

    public sealed record DuelScenario(string UserTeamId, string OpponentTeamId, ScenarioKind Kind, Slot UserSlot, Slot EnemySlot);

    /// <summary>Builds a single duel without the match engine (scenario picker).</summary>
    public static class DuelScenarios
    {
        private static readonly Slot[] FieldSlots =
            { Slot.LB, Slot.RB, Slot.CM, Slot.LW, Slot.RW, Slot.ST };
        private static readonly Slot[] KeeperOnly = { Slot.GK };

        public static IReadOnlyList<Slot> UserSlots(ScenarioKind k)
            => k == ScenarioKind.ShotAsKeeper ? KeeperOnly : FieldSlots;

        public static IReadOnlyList<Slot> EnemySlots(ScenarioKind k)
            => k == ScenarioKind.ShotAsShooter ? KeeperOnly : FieldSlots;

        public static Slot DefaultEnemySlot(ScenarioKind k, Slot userSlot)
        {
            var lane = SlotInfo.LaneOf(userSlot);
            switch (k)
            {
                case ScenarioKind.FieldAttack:
                    return lane == Lane.Left ? Slot.RB : lane == Lane.Right ? Slot.LB : Slot.CM;
                case ScenarioKind.FieldDefense:
                    return lane == Lane.Left ? Slot.RW : lane == Lane.Right ? Slot.LW : Slot.CM;
                case ScenarioKind.ShotAsShooter:
                    return Slot.GK;
                default:
                    return Slot.ST;
            }
        }

        public static DuelSetup BuildSetup(GameData data, DuelScenario s)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (!data.Teams.TryGetValue(s.UserTeamId ?? "", out var userTeam))
                throw new ArgumentException("Unknown team id: '" + s.UserTeamId + "'");
            if (!data.Teams.TryGetValue(s.OpponentTeamId ?? "", out var oppTeam))
                throw new ArgumentException("Unknown team id: '" + s.OpponentTeamId + "'");
            if (!Contains(UserSlots(s.Kind), s.UserSlot))
                throw new ArgumentException("Slot " + s.UserSlot + " is not allowed for user in " + s.Kind);
            if (!Contains(EnemySlots(s.Kind), s.EnemySlot))
                throw new ArgumentException("Slot " + s.EnemySlot + " is not allowed for enemy in " + s.Kind);
            var userP = userTeam.BySlot(s.UserSlot)
                ?? throw new ArgumentException("Team '" + userTeam.Id + "' has no slot " + s.UserSlot);
            var enemyP = oppTeam.BySlot(s.EnemySlot)
                ?? throw new ArgumentException("Team '" + oppTeam.Id + "' has no slot " + s.EnemySlot);

            bool userAttacks = s.Kind == ScenarioKind.FieldAttack || s.Kind == ScenarioKind.ShotAsShooter;
            bool shot = s.Kind == ScenarioKind.ShotAsShooter || s.Kind == ScenarioKind.ShotAsKeeper;
            var setup = new DuelSetup
            {
                Kind = shot ? DuelKind.Shot : DuelKind.Field,
                UserIsAttacker = userAttacks,
                EnemyPowerScalePercent = MatchRules.PowerScalePercent(oppTeam.Tier),
                UserPlayer = userP,
                EnemyPlayer = enemyP,
            };
            setup.UserDeck = DuelSetupHelpers.BuildDeck(data, userP);
            setup.EnemyDeck = DuelSetupHelpers.BuildDeck(data, enemyP);

            if (!shot)
            {
                var atkTeam = userAttacks ? userTeam : oppTeam;
                var carrier = userAttacks ? userP : enemyP;
                var targets = userAttacks ? setup.UserPassTargets : setup.EnemyPassTargets;
                DuelSetupHelpers.AddPassTargets(targets, atkTeam.Players, carrier);
            }
            return setup;
        }

        public static Duel Create(GameData data, DuelScenario s, uint seed)
            => new Duel(BuildSetup(data, s), new Rng(seed));

        private static bool Contains(IReadOnlyList<Slot> list, Slot s)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == s) return true;
            return false;
        }
    }
}
