using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public class CardInstance
    {
        public CardDef Def { get; }
        public bool Upgraded { get; }
        public int Cost { get; }
        public IReadOnlyList<EffectDef> Effects { get; }

        public CardInstance(CardDef def, bool upgraded)
        {
            Def = def;
            Upgraded = upgraded;
            Cost = upgraded && def.UpgradedCost.HasValue ? def.UpgradedCost.Value : def.Cost;
            Effects = upgraded && def.UpgradedEffects != null ? def.UpgradedEffects : def.Effects;
        }
    }
}
