using System.Collections.Generic;

namespace WeirdLand.Core.Data
{
    public class TeamDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Style { get; set; }
        public int Tier { get; set; }
        public List<PlayerDef> Players { get; set; } = new List<PlayerDef>();

        public PlayerDef BySlot(Slot s)
        {
            foreach (var p in Players) if (p.Slot == s) return p;
            return null;
        }
    }
}
