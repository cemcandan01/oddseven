using System.Collections.Generic;

namespace WeirdLand.Core.Data
{
    public class CardDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int Cost { get; set; }
        public CardRole Role { get; set; }
        public List<Position> Positions { get; set; } = new List<Position>();
        public Rarity Rarity { get; set; }
        public List<EffectDef> Effects { get; set; } = new List<EffectDef>();
        public int? UpgradedCost { get; set; }
        public List<EffectDef> UpgradedEffects { get; set; }

        public bool UsableWhen(bool attacking)
        {
            return Role == CardRole.Both || (attacking ? Role == CardRole.Attack : Role == CardRole.Defense);
        }
    }
}
