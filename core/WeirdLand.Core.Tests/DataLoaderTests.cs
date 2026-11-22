using Newtonsoft.Json;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Tests
{
    public class DataLoaderTests
    {
        private static readonly string[] AllPos = { "GK", "DEF", "MID", "WING", "ST" };
        private static readonly string[] Slots = { "GK", "LB", "RB", "CM", "LW", "RW", "ST" };

        private static object Fx(string type = "Damage") => new
        {
            type,
            amount = 4,
            scaleStat = "Dribbling",
            scale = 1,
            bonusIfTargetHas = "Unbalanced",
            bonusMultiplier = 2,
        };

        private static object CardObj(string id, string[]? positions = null, string effectType = "Damage") => new
        {
            id,
            name = "Çalım",
            text = "Açıklama",
            cost = 2,
            role = "Attack",
            positions = positions ?? AllPos,
            rarity = "Common",
            effects = new[] { Fx(effectType) },
        };

        private static string[] Deck(int n, string id = "calim")
        {
            var d = new string[n];
            for (int i = 0; i < n; i++) d[i] = id;
            return d;
        }

        private static object PlayerObj(string id, string slot, string[] deck) => new
        {
            id,
            name = id,
            slot,
            stats = new { speed = 3, power = 3, passing = 3, dribbling = 3, shooting = 3, defending = 3, aerial = 3, near = 3, far = 3 },
            deck,
        };

        private static object[] Squad(string[]? slots = null, string[]? deck = null, string? gkId = null, string[]? gkDeck = null)
        {
            slots ??= Slots;
            deck ??= Deck(6);
            var list = new object[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                list[i] = PlayerObj(i == 0 && gkId != null ? gkId : "p" + i, slots[i], i == 0 && gkDeck != null ? gkDeck : deck);
            return list;
        }

        private static object TeamObj(object[] players, string id = "t1") => new
        {
            id,
            name = "Takım",
            style = "dengeli",
            tier = 1,
            players,
        };

        private static string ValidCards => TestKit.CardsJson(CardObj("calim"));
        private static string ValidTeams => TestKit.TeamsJson(TeamObj(Squad()));

        [Test]
        public void ValidData_Loads()
        {
            var gd = DataLoader.FromJson(ValidCards, ValidTeams);
            Assert.That(gd.Cards["calim"].Effects[0].ScaleStat, Is.EqualTo(Stat.Dribbling));
            Assert.That(gd.Team("t1").BySlot(Slot.LW)!.Position, Is.EqualTo(Position.WING));
            Assert.That(gd.Cards["calim"].Effects[0].BonusMultiplier, Is.EqualTo(2));
            Assert.That(gd.Cards["calim"].Name, Is.EqualTo("Çalım"));
        }

        [Test]
        public void WrongSchemaVersion_Throws()
        {
            var bad = JsonConvert.SerializeObject(new { schemaVersion = 2, cards = new[] { CardObj("calim") } });
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(bad, ValidTeams));
            Assert.That(ex!.Message, Does.Contain("schemaVersion"));
        }

        [Test]
        public void UnknownField_Throws()
        {
            var card = new { id = "calim", name = "x", text = "x", cost = 1, role = "Attack", positions = AllPos, rarity = "Common", effects = new object[0], foo = 1 };
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(TestKit.CardsJson(card), ValidTeams));
            Assert.That(ex!.Message, Does.Contain("foo"));
        }

        [Test]
        public void UnknownEnum_Throws()
        {
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(TestKit.CardsJson(CardObj("calim", null, "Explode")), ValidTeams));
            Assert.That(ex!.Message, Does.Contain("effects"));
        }

        [Test]
        public void IntegerEnum_Throws()
        {
            var card = new { id = "calim", name = "x", text = "x", cost = 1, role = 0, positions = AllPos, rarity = "Common", effects = new object[0] };
            Assert.Throws<DataException>(() => DataLoader.FromJson(TestKit.CardsJson(card), ValidTeams));
        }

        [Test]
        public void UnknownCardInDeck_Throws()
        {
            var teams = TestKit.TeamsJson(TeamObj(Squad(gkId: "kaleci1", gkDeck: Deck(6, "yok"))));
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(ValidCards, teams));
            Assert.That(ex!.Message, Does.Contain("yok").And.Contain("kaleci1"));
        }

        [Test]
        public void CardNotAllowedForPosition_Throws()
        {
            var cards = TestKit.CardsJson(CardObj("calim"), CardObj("golcu", new[] { "ST" }));
            var deck = new[] { "calim", "calim", "calim", "calim", "calim", "golcu" };
            var teams = TestKit.TeamsJson(TeamObj(Squad(gkId: "kaleci1", gkDeck: deck)));
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(cards, teams));
            Assert.That(ex!.Message, Does.Contain("golcu").And.Contain("kaleci1"));
        }

        [TestCase(5)]
        [TestCase(11)]
        public void DeckSizeOutOfRange_Throws(int size)
        {
            var teams = TestKit.TeamsJson(TeamObj(Squad(deck: Deck(size))));
            Assert.Throws<DataException>(() => DataLoader.FromJson(ValidCards, teams));
        }

        [Test]
        public void MissingOrDuplicateSlot_Throws()
        {
            var slots = new[] { "GK", "LB", "RB", "CM", "LW", "LW", "RW" };
            var teams = TestKit.TeamsJson(TeamObj(Squad(slots), "takim9"));
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(ValidCards, teams));
            Assert.That(ex!.Message, Does.Contain("takim9"));
        }

        [Test]
        public void DuplicateCardId_Throws()
        {
            var cards = TestKit.CardsJson(CardObj("calim"), CardObj("calim"));
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(cards, ValidTeams));
            Assert.That(ex!.Message, Does.Contain("calim"));
        }

        // ---- Strict loading: required members, no null lists, semantic checks ----

        private static Dictionary<string, object?> CardMap(string id, params object[] effects) => new()
        {
            ["id"] = id,
            ["name"] = "Bozuk",
            ["text"] = "x",
            ["cost"] = 1,
            ["role"] = "Attack",
            ["positions"] = AllPos,
            ["rarity"] = "Common",
            ["effects"] = effects.Length == 0 ? new[] { Fx() } : effects,
        };

        private static Dictionary<string, object?> StatsMap() => new()
        {
            ["speed"] = 3, ["power"] = 3, ["passing"] = 3, ["dribbling"] = 3, ["shooting"] = 3,
            ["defending"] = 3, ["aerial"] = 3, ["near"] = 3, ["far"] = 3,
        };

        private static Dictionary<string, object?> PlayerMap(string id, string slot) => new()
        {
            ["id"] = id,
            ["name"] = id,
            ["slot"] = slot,
            ["stats"] = StatsMap(),
            ["deck"] = Deck(6),
        };

        private static object[] SquadMaps(Action<Dictionary<string, object?>[]> tweak)
        {
            var list = new Dictionary<string, object?>[Slots.Length];
            for (int i = 0; i < Slots.Length; i++) list[i] = PlayerMap("p" + i, Slots[i]);
            tweak(list);
            return list;
        }

        private static DataException BadCard(Dictionary<string, object?> card)
            => Assert.Throws<DataException>(() => DataLoader.FromJson(TestKit.CardsJson(CardObj("calim"), card), ValidTeams))!;

        private static DataException BadTeams(object team)
            => Assert.Throws<DataException>(() => DataLoader.FromJson(ValidCards, TestKit.TeamsJson(team)))!;

        [Test]
        public void NullPlayers_Throws()
        {
            var ex = BadTeams(TeamObj(null!));
            Assert.That(ex.Message, Does.Contain("players"));
        }

        [Test]
        public void NullDeck_Throws()
        {
            var ex = BadTeams(TeamObj(SquadMaps(p => p[2]["deck"] = null)));
            Assert.That(ex.Message, Does.Contain("deck"));
        }

        [Test]
        public void NullPositions_Throws()
        {
            var card = CardMap("bozuk");
            card["positions"] = null;
            Assert.That(BadCard(card).Message, Does.Contain("positions"));
        }

        [Test]
        public void MissingRole_Throws()
        {
            var card = CardMap("bozuk");
            card.Remove("role");
            Assert.That(BadCard(card).Message, Does.Contain("role"));
        }

        [Test]
        public void MissingEffectType_Throws()
        {
            var card = CardMap("bozuk", new { amount = 3 });
            Assert.That(BadCard(card).Message, Does.Contain("type").And.Contain("effects"));
        }

        [Test]
        public void MissingStats_Throws()
        {
            var ex = BadTeams(TeamObj(SquadMaps(p => p[3].Remove("stats"))));
            Assert.That(ex.Message, Does.Contain("stats"));
        }

        [Test]
        public void MissingSchemaVersion_Throws()
        {
            var cards = JsonConvert.SerializeObject(new { cards = new[] { CardObj("calim") } });
            var ex = Assert.Throws<DataException>(() => DataLoader.FromJson(cards, ValidTeams));
            Assert.That(ex!.Message, Does.Contain("schemaVersion"));
        }

        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void StatusEffect_NeedsStatusAndPositiveStacks(bool withStatus, int stacks)
        {
            object fx = withStatus
                ? new { type = "Status", status = "Tired", stacks }
                : (object)new { type = "Status", stacks };
            var ex = BadCard(CardMap("bozuk", fx));
            Assert.That(ex.Message, Does.Contain("bozuk").And.Contain("status").IgnoreCase);
        }

        [Test]
        public void ScaleWithoutScaleStat_Throws()
        {
            var ex = BadCard(CardMap("bozuk", new { type = "Damage", amount = 4, scale = 1 }));
            Assert.That(ex.Message, Does.Contain("bozuk").And.Contain("scaleStat"));
        }

        [Test]
        public void NegativeCost_Throws()
        {
            var card = CardMap("bozuk");
            card["cost"] = -1;
            Assert.That(BadCard(card).Message, Does.Contain("bozuk").And.Contain("cost"));
        }

        [Test]
        public void NegativeStat_Throws()
        {
            var ex = BadTeams(TeamObj(SquadMaps(p => ((Dictionary<string, object?>)p[4]["stats"]!)["power"] = -1), "takim5"));
            Assert.That(ex.Message, Does.Contain("takim5").And.Contain("p4").And.Contain("Power"));
        }

        [Test]
        public void DuplicatePlayerId_Throws()
        {
            var ex = BadTeams(TeamObj(SquadMaps(p => p[5]["id"] = "p1"), "takim6"));
            Assert.That(ex.Message, Does.Contain("takim6").And.Contain("p1"));
        }

        [Test]
        public void UsableWhen_FollowsRole()
        {
            var atk = TestKit.Card("a", 1, CardRole.Attack);
            var def = TestKit.Card("d", 1, CardRole.Defense);
            var both = TestKit.Card("b", 1, CardRole.Both);
            Assert.That(atk.UsableWhen(true), Is.True);
            Assert.That(atk.UsableWhen(false), Is.False);
            Assert.That(def.UsableWhen(true), Is.False);
            Assert.That(def.UsableWhen(false), Is.True);
            Assert.That(both.UsableWhen(true) && both.UsableWhen(false), Is.True);
        }

        [Test]
        public void SlotInfo_MapsPositionAndLane()
        {
            Assert.That(SlotInfo.PositionOf(Slot.RB), Is.EqualTo(Position.DEF));
            Assert.That(SlotInfo.LaneOf(Slot.LW), Is.EqualTo(Lane.Left));
            Assert.That(SlotInfo.LaneOf(Slot.RB), Is.EqualTo(Lane.Right));
            Assert.That(SlotInfo.LaneOf(Slot.GK), Is.EqualTo(Lane.Center));
        }
    }
}
