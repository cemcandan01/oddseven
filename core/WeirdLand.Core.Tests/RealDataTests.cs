using System.IO;
using System.Linq;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Tests
{
    public class RealDataTests
    {
        static GameData Load() => DataLoader.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../data"));

        [Test]
        public void RealData_LoadsAndValidates()
        {
            Assert.DoesNotThrow(() => Load());
        }

        [Test]
        public void RealData_HasAtLeast40Cards()
        {
            Assert.That(Load().Cards.Count, Is.GreaterThanOrEqualTo(40));
        }

        [Test]
        public void RealData_HasUserTeamAndTiers()
        {
            var gd = Load();
            Assert.That(gd.Team("mavi").Tier, Is.EqualTo(1));
            Assert.That(gd.Teams.ContainsKey("kirmizi"), Is.True);
            foreach (var tier in new[] { 1, 2, 3 })
                Assert.That(gd.Teams.Values.Any(t => t.Id != "mavi" && t.Tier == tier), Is.True, "tier " + tier);
        }

        [Test]
        public void RealData_DeckRoleRules()
        {
            var gd = Load();
            foreach (var team in gd.Teams.Values)
            {
                foreach (var p in team.Players)
                {
                    var deck = p.Deck.Select(id => gd.Card(id)).ToList();
                    string who = team.Id + "/" + p.Id;
                    if (p.Slot == Slot.GK)
                    {
                        Assert.That(deck.Any(c => c.Effects.Any(e => e.Status == StatusId.CoverNear)), Is.True, who + " CoverNear");
                        Assert.That(deck.Any(c => c.Effects.Any(e => e.Status == StatusId.CoverFar)), Is.True, who + " CoverFar");
                        Assert.That(deck.Any(c => c.Effects.Any(e => e.Type == EffectType.Damage)), Is.True, who + " damage");
                        continue;
                    }
                    Assert.That(deck.Count(c => c.Role != CardRole.Defense), Is.GreaterThanOrEqualTo(2), who + " attack/both");
                    Assert.That(deck.Count(c => c.Role != CardRole.Attack), Is.GreaterThanOrEqualTo(2), who + " defense/both");
                    if (p.Slot == Slot.LW || p.Slot == Slot.RW || p.Slot == Slot.ST)
                        Assert.That(deck.Any(c => c.Effects.Any(e => e.Type == EffectType.Damage && e.Corner != Corner.None)), Is.True, who + " corner shot");
                }
            }
        }
    }
}
