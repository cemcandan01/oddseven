using System.Collections.Generic;
using System.Linq;
using WeirdLand.Core;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity.Tests
{
    /// <summary>Tiny hand-built duel setups for EditMode tests.</summary>
    public static class TestDecks
    {
        public static CardDef Card(string id, int cost, CardRole role, params EffectDef[] fx)
        {
            var c = new CardDef { Id = id, Name = id, Text = id, Cost = cost, Role = role };
            c.Positions.AddRange(System.Enum.GetValues(typeof(Position)).Cast<Position>());
            c.Effects.AddRange(fx);
            return c;
        }

        public static PlayerDef Player(string id, Slot slot)
        {
            return new PlayerDef
            {
                Id = id, Name = id, Slot = slot,
                Stats = new Stats { Speed = 3, Power = 3, Passing = 3, Dribbling = 3, Shooting = 3, Defending = 3, Aerial = 3, Near = 3, Far = 3 },
            };
        }

        private static List<CardInstance> Deck(int n, int cost)
        {
            var effect = new EffectDef { Type = EffectType.Damage, Amount = 1 };
            return Enumerable.Range(0, n)
                .Select(i => new CardInstance(Card("c" + i, cost, CardRole.Both, effect), false)).ToList();
        }

        /// <summary>Field duel; when the user attacks it has one pass target (id "mate").</summary>
        public static Duel FieldDuel(uint seed = 1, int cardCost = 1, bool userAttacks = true)
        {
            var s = new DuelSetup
            {
                Kind = DuelKind.Field,
                UserIsAttacker = userAttacks,
                UserPlayer = Player("u", Slot.ST),
                EnemyPlayer = Player("e", Slot.CM),
                UserDeck = Deck(6, cardCost),
                EnemyDeck = Deck(6, 1),
            };
            if (userAttacks) s.UserPassTargets.Add(Player("mate", Slot.CM));
            else s.EnemyPassTargets.Add(Player("emate", Slot.CM));
            return new Duel(s, new Rng(seed));
        }
    }
}
