using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Match
{
    public class TeamState
    {
        public TeamDef Def;
        public List<PlayerState> Players = new List<PlayerState>();
        public Tactic Tactic;
        public int Score;

        public PlayerState BySlot(Slot s)
        {
            foreach (var p in Players) if (p.Def.Slot == s) return p;
            return null;
        }

        public PlayerState ById(string id)
        {
            foreach (var p in Players) if (p.Def.Id == id) return p;
            return null;
        }

        public int HypeSum
        {
            get
            {
                int n = 0;
                foreach (var p in Players) n += p.Hype;
                return n;
            }
        }
    }
}
