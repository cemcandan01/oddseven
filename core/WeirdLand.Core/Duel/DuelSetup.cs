using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public class DuelSetup
    {
        public DuelKind Kind;
        public bool UserIsAttacker;
        public PlayerDef UserPlayer, EnemyPlayer;
        public List<CardInstance> UserDeck = new List<CardInstance>();
        public List<CardInstance> EnemyDeck = new List<CardInstance>();
        public int UserEnergyBonusTurn1;
        public int UserEnergyPenalty;
        public int UserMomentum, EnemyMomentum;
        public int EnemyPowerScalePercent = 100;
        public int DefenderMaxHpBonus;
        public List<PlayerDef> UserPassTargets = new List<PlayerDef>();
        public List<PlayerDef> EnemyPassTargets = new List<PlayerDef>();
    }

    /// <summary>What the enemy will do on its action. Both null means "wait" (no action).</summary>
    public class EnemyIntent
    {
        public CardInstance Card { get; internal set; }
        public PlayerDef PassTarget { get; internal set; }
        public double PassChance { get; internal set; }
    }
}
