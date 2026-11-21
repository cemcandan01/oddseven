using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace WeirdLand.Core.Data
{
    public static class DataLoader
    {
        private const int SchemaVersion = 1;
        private const int MinDeck = 6;
        private const int MaxDeck = 10;
        private const int SquadSize = 7;

        private class CardsFile
        {
            public int SchemaVersion { get; set; }
            public List<CardDef> Cards { get; set; }
        }

        private class TeamsFile
        {
            public int SchemaVersion { get; set; }
            public List<TeamDef> Teams { get; set; }
        }

        private static JsonSerializerSettings Settings()
        {
            return new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter(new DefaultNamingStrategy(), false) },
            };
        }

        public static GameData Load(string dataDir)
        {
            string cards, teams;
            try
            {
                cards = File.ReadAllText(Path.Combine(dataDir, "cards.json"));
                teams = File.ReadAllText(Path.Combine(dataDir, "teams.json"));
            }
            catch (IOException e) { throw new DataException("Cannot read data files in '" + dataDir + "': " + e.Message, e); }
            catch (UnauthorizedAccessException e) { throw new DataException("Cannot read data files in '" + dataDir + "': " + e.Message, e); }
            return FromJson(cards, teams);
        }

        public static GameData FromJson(string cardsJson, string teamsJson)
        {
            var cardsFile = Parse<CardsFile>(cardsJson, "cards.json");
            var teamsFile = Parse<TeamsFile>(teamsJson, "teams.json");

            if (cardsFile.SchemaVersion != SchemaVersion)
                throw new DataException("cards.json: unsupported schemaVersion " + cardsFile.SchemaVersion + " (expected " + SchemaVersion + ")");
            if (teamsFile.SchemaVersion != SchemaVersion)
                throw new DataException("teams.json: unsupported schemaVersion " + teamsFile.SchemaVersion + " (expected " + SchemaVersion + ")");

            var cards = new Dictionary<string, CardDef>();
            foreach (var c in cardsFile.Cards ?? new List<CardDef>())
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) throw new DataException("cards.json: card without id");
                if (cards.ContainsKey(c.Id)) throw new DataException("cards.json: duplicate card id '" + c.Id + "'");
                cards[c.Id] = c;
            }

            var teams = new Dictionary<string, TeamDef>();
            foreach (var t in teamsFile.Teams ?? new List<TeamDef>())
            {
                if (t == null || string.IsNullOrEmpty(t.Id)) throw new DataException("teams.json: team without id");
                if (teams.ContainsKey(t.Id)) throw new DataException("teams.json: duplicate team id '" + t.Id + "'");
                ValidateTeam(t, cards);
                teams[t.Id] = t;
            }

            return new GameData(cards, teams);
        }

        private static void ValidateTeam(TeamDef team, Dictionary<string, CardDef> cards)
        {
            var seen = new HashSet<Slot>();
            foreach (var p in team.Players)
            {
                if (p == null) throw new DataException("team '" + team.Id + "': null player");
                if (!seen.Add(p.Slot))
                    throw new DataException("team '" + team.Id + "': duplicate slot " + p.Slot + " (player '" + p.Id + "')");
            }
            if (team.Players.Count != SquadSize)
                throw new DataException("team '" + team.Id + "': needs " + SquadSize + " players with unique slots, has " + team.Players.Count);

            foreach (var p in team.Players)
            {
                if (p.Deck.Count < MinDeck || p.Deck.Count > MaxDeck)
                    throw new DataException("team '" + team.Id + "', player '" + p.Id + "': deck size " + p.Deck.Count + " outside " + MinDeck + "-" + MaxDeck);
                foreach (var cardId in p.Deck)
                {
                    CardDef card;
                    if (cardId == null || !cards.TryGetValue(cardId, out card))
                        throw new DataException("team '" + team.Id + "', player '" + p.Id + "': unknown card '" + cardId + "' in deck");
                    if (!card.Positions.Contains(p.Position))
                        throw new DataException("team '" + team.Id + "', player '" + p.Id + "': card '" + cardId + "' not allowed for position " + p.Position);
                }
            }
        }

        private static T Parse<T>(string json, string fileName) where T : class
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(json, Settings());
                if (result == null) throw new DataException(fileName + ": empty document");
                return result;
            }
            catch (JsonSerializationException e) { throw Wrap(fileName, e, e.Path); }
            catch (JsonReaderException e) { throw Wrap(fileName, e, e.Path); }
        }

        private static DataException Wrap(string fileName, Exception e, string path)
        {
            return new DataException(fileName + ": " + e.Message + " (path '" + path + "')", e);
        }
    }
}
