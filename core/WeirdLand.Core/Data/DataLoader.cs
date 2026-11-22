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
        private static readonly Stat[] StatOrder =
            { Stat.Speed, Stat.Power, Stat.Passing, Stat.Dribbling, Stat.Shooting, Stat.Defending, Stat.Aerial, Stat.Near, Stat.Far };

        private class CardsFile
        {
            [JsonProperty(Required = Required.Always)]
            public int SchemaVersion { get; set; }
            [JsonProperty(Required = Required.Always)]
            public List<CardDef> Cards { get; set; }
        }

        private class TeamsFile
        {
            [JsonProperty(Required = Required.Always)]
            public int SchemaVersion { get; set; }
            [JsonProperty(Required = Required.Always)]
            public List<TeamDef> Teams { get; set; }
        }

        private class UiTextFile
        {
            [JsonProperty(Required = Required.Always)]
            public int SchemaVersion { get; set; }
            [JsonProperty(Required = Required.Always)]
            public Dictionary<string, string> Strings { get; set; }
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

        public static UiText LoadUiText(string dataDir)
        {
            string json;
            try { json = File.ReadAllText(Path.Combine(dataDir, "ui_tr.json")); }
            catch (IOException e) { throw new DataException("Cannot read ui_tr.json in '" + dataDir + "': " + e.Message, e); }
            catch (UnauthorizedAccessException e) { throw new DataException("Cannot read ui_tr.json in '" + dataDir + "': " + e.Message, e); }
            return UiTextFromJson(json);
        }

        public static UiText UiTextFromJson(string json)
        {
            var file = Parse<UiTextFile>(json, "ui_tr.json");
            if (file.SchemaVersion != SchemaVersion)
                throw new DataException("ui_tr.json: unsupported schemaVersion " + file.SchemaVersion + " (expected " + SchemaVersion + ")");
            foreach (var kv in file.Strings)
                if (string.IsNullOrEmpty(kv.Value)) throw new DataException("ui_tr.json: empty text for key '" + kv.Key + "'");
            return new UiText(file.Strings);
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
            foreach (var c in cardsFile.Cards)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) throw new DataException("cards.json: card without id");
                if (cards.ContainsKey(c.Id)) throw new DataException("cards.json: duplicate card id '" + c.Id + "'");
                ValidateCard(c);
                cards[c.Id] = c;
            }

            var teams = new Dictionary<string, TeamDef>();
            foreach (var t in teamsFile.Teams)
            {
                if (t == null || string.IsNullOrEmpty(t.Id)) throw new DataException("teams.json: team without id");
                if (teams.ContainsKey(t.Id)) throw new DataException("teams.json: duplicate team id '" + t.Id + "'");
                ValidateTeam(t, cards);
                teams[t.Id] = t;
            }

            return new GameData(cards, teams);
        }

        private static void ValidateCard(CardDef c)
        {
            string who = "cards.json: card '" + c.Id + "'";
            if (c.Cost < 0) throw new DataException(who + ": negative cost " + c.Cost);
            if (c.UpgradedCost.HasValue && c.UpgradedCost.Value < 0)
                throw new DataException(who + ": negative upgradedCost " + c.UpgradedCost.Value);
            ValidateEffects(c.Effects, who + ", effects");
            if (c.UpgradedEffects != null) ValidateEffects(c.UpgradedEffects, who + ", upgradedEffects");
        }

        private static void ValidateEffects(List<EffectDef> effects, string who)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                string at = who + "[" + i + "]";
                if (e == null) throw new DataException(at + ": null effect");
                if (e.Type == EffectType.Status)
                {
                    if (!e.Status.HasValue) throw new DataException(at + ": Status effect without status");
                    if (e.Stacks <= 0) throw new DataException(at + ": Status effect needs stacks > 0, has " + e.Stacks);
                }
                if (e.Scale != 0 && !e.ScaleStat.HasValue)
                    throw new DataException(at + ": scale " + e.Scale + " without scaleStat");
            }
        }

        private static void ValidateTeam(TeamDef team, Dictionary<string, CardDef> cards)
        {
            var seen = new HashSet<Slot>();
            var ids = new HashSet<string>();
            foreach (var p in team.Players)
            {
                if (p == null) throw new DataException("team '" + team.Id + "': null player");
                if (!seen.Add(p.Slot))
                    throw new DataException("team '" + team.Id + "': duplicate slot " + p.Slot + " (player '" + p.Id + "')");
                if (!ids.Add(p.Id))
                    throw new DataException("team '" + team.Id + "': duplicate player id '" + p.Id + "'");
                foreach (Stat st in StatOrder)
                    if (p.Stats.Get(st) < 0)
                        throw new DataException("team '" + team.Id + "', player '" + p.Id + "': negative stat " + st + " " + p.Stats.Get(st));
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
