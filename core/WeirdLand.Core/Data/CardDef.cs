using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeirdLand.Core.Data
{
    public class CardDef
    {
        [JsonProperty(Required = Required.Always)]
        public string Id { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        public string Text { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int Cost { get; set; }
        [JsonProperty(Required = Required.Always)]
        public CardRole Role { get; set; }
        [JsonProperty(Required = Required.Always)]
        public List<Position> Positions { get; set; } = new List<Position>();
        [JsonProperty(Required = Required.Always)]
        public Rarity Rarity { get; set; }
        [JsonProperty(Required = Required.Always)]
        public List<EffectDef> Effects { get; set; } = new List<EffectDef>();
        public int? UpgradedCost { get; set; }
        public List<EffectDef> UpgradedEffects { get; set; }

        public bool UsableWhen(bool attacking)
        {
            return Role == CardRole.Both || (attacking ? Role == CardRole.Attack : Role == CardRole.Defense);
        }
    }
}
