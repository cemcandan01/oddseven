using System.Collections.Generic;

namespace WeirdLand.Core.Data
{
    public class GameData
    {
        public IReadOnlyDictionary<string, CardDef> Cards { get; }
        public IReadOnlyDictionary<string, TeamDef> Teams { get; }

        public GameData(IReadOnlyDictionary<string, CardDef> cards, IReadOnlyDictionary<string, TeamDef> teams)
        {
            Cards = cards;
            Teams = teams;
        }

        public CardDef Card(string id) => Cards[id];
        public TeamDef Team(string id) => Teams[id];
    }
}
