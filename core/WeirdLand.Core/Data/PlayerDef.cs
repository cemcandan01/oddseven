using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeirdLand.Core.Data
{
    public class PlayerDef
    {
        [JsonProperty(Required = Required.Always)]
        public string Id { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Slot Slot { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Stats Stats { get; set; } = new Stats();
        [JsonProperty(Required = Required.Always)]
        public List<string> Deck { get; set; } = new List<string>();

        [JsonIgnore]
        public Position Position => SlotInfo.PositionOf(Slot);
    }
}
