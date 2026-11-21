using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeirdLand.Core.Data
{
    public class PlayerDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Slot Slot { get; set; }
        public Stats Stats { get; set; } = new Stats();
        public List<string> Deck { get; set; } = new List<string>();

        [JsonIgnore]
        public Position Position => SlotInfo.PositionOf(Slot);
    }
}
