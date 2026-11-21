using System.Linq;
using Newtonsoft.Json;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Tests
{
    /// <summary>Shared builders for tests.</summary>
    public static class TestKit
    {
        public static CardDef Card(string id, int cost, CardRole role, params EffectDef[] fx)
        {
            var c = new CardDef
            {
                Id = id,
                Name = id,
                Text = id,
                Cost = cost,
                Role = role,
            };
            c.Positions.AddRange(System.Enum.GetValues(typeof(Position)).Cast<Position>());
            c.Effects.AddRange(fx);
            return c;
        }

        public static EffectDef Dmg(int amount, Stat? stat = null, double scale = 0)
            => new EffectDef { Type = EffectType.Damage, Amount = amount, ScaleStat = stat, Scale = scale };

        public static EffectDef Blk(int amount)
            => new EffectDef { Type = EffectType.Block, Target = EffectTarget.Self, Amount = amount };

        public static EffectDef Sts(StatusId s, int stacks, EffectTarget t = EffectTarget.Opponent)
            => new EffectDef { Type = EffectType.Status, Status = s, Stacks = stacks, Target = t };

        public static EffectDef AtCorner(EffectDef e, Corner c)
        {
            e.Corner = c;
            return e;
        }

        public static PlayerDef Player(string id, Slot slot, Stats? stats = null, params string[] deck)
        {
            var p = new PlayerDef
            {
                Id = id,
                Name = id,
                Slot = slot,
                Stats = stats ?? new Stats
                {
                    Speed = 3, Power = 3, Passing = 3, Dribbling = 3, Shooting = 3,
                    Defending = 3, Aerial = 3, Near = 3, Far = 3,
                },
            };
            p.Deck.AddRange(deck);
            return p;
        }

        public static string CardsJson(params object[] cards)
            => JsonConvert.SerializeObject(new { schemaVersion = 1, cards });

        public static string TeamsJson(params object[] teams)
            => JsonConvert.SerializeObject(new { schemaVersion = 1, teams });
    }
}
