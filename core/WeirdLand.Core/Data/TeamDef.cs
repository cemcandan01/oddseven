using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeirdLand.Core.Data
{
    public class TeamDef
    {
        [JsonProperty(Required = Required.Always)]
        public string Id { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        public string Style { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int Tier { get; set; }
        [JsonProperty(Required = Required.Always)]
        public List<PlayerDef> Players { get; set; } = new List<PlayerDef>();

        public PlayerDef BySlot(Slot s)
        {
            foreach (var p in Players) if (p.Slot == s) return p;
            return null;
        }
    }
}
