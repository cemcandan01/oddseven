using System.Linq;
using NUnit.Framework;
using TMPro;
using WeirdLand.Core;
using WeirdLand.Core.Data;

namespace WeirdLand.Unity.Tests
{
    public class BridgeTests
    {
        [Test]
        public void DataLoadsFromRepo()
        {
            var d = GameDataProvider.Load(GameDataProvider.DataDirectory());
            Assert.That(d.Game.Cards.Count, Is.GreaterThanOrEqualTo(40));
            Assert.That(d.Text.Get(UiKeys.DuelEndTurn), Is.EqualTo("Turu bitir"));
        }

        [Test]
        public void Golden_MatchFingerprint_MatchesDotnet()
            => Assert.That(GoldenRun.Fingerprint(GameDataProvider.Load(GameDataProvider.DataDirectory()).Game),
                Is.EqualTo("64f4ec8b"));

        [Test]
        public void Load_MissingDirectory_ThrowsDataExceptionNamingPath()
        {
            var ex = Assert.Throws<DataException>(() => GameDataProvider.Load("Z:/yok/data"));
            Assert.That(ex.Message, Does.Contain("Z:/yok/data").Or.Contain("Z:\\yok\\data"));
        }

        [Test]
        public void TurkishGlyphs_AvailableInDefaultFont()
        {
            var font = TMP_Settings.defaultFontAsset;
            Assert.That(font, Is.Not.Null);
            // TMP 2.0 (com.unity.ugui) exposes the fallback-searching overload only with out uint[].
            Assert.That(font.HasCharacters("ğĞüÜşŞıİöÖçÇ", out uint[] missing, true, true), Is.True,
                "missing: " + new string((missing ?? new uint[0]).Select(c => (char)c).ToArray()));
        }
    }
}
