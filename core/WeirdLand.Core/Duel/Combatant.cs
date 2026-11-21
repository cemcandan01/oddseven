using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public class Combatant
    {
        public PlayerDef Player;
        /// <summary>True when this combatant is the user's side; used for event flags.</summary>
        public bool IsUser;
        public int Hp, MaxHp, Block, Energy, KondisyonSpent;
        public int PowerScalePercent = 100;
        public Dictionary<StatusId, int> Statuses = new Dictionary<StatusId, int>();
        public List<CardInstance> DrawPile = new List<CardInstance>();
        public List<CardInstance> Hand = new List<CardInstance>();
        public List<CardInstance> Discard = new List<CardInstance>();

        public int StatusOf(StatusId s) => Statuses.TryGetValue(s, out var n) ? n : 0;
    }
}
