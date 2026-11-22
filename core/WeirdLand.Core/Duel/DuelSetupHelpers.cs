using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    /// <summary>Setup rules shared by the match engine and the scenario builder.</summary>
    internal static class DuelSetupHelpers
    {
        /// <summary>The player's deck as unupgraded card instances.</summary>
        public static List<CardInstance> BuildDeck(GameData data, PlayerDef player)
        {
            var deck = new List<CardInstance>();
            foreach (var id in player.Deck) deck.Add(new CardInstance(data.Card(id), false));
            return deck;
        }

        /// <summary>Attacking team players in team order, excluding the carrier and the keeper.</summary>
        public static void AddPassTargets(List<PlayerDef> targets, IEnumerable<PlayerDef> attackers, PlayerDef carrier)
        {
            foreach (var p in attackers)
                if (p != carrier && p.Slot != Slot.GK) targets.Add(p);
        }
    }
}
