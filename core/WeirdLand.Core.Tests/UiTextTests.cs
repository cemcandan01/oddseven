using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Core.Tests
{
    public class UiTextTests
    {
        static UiText Real() => DataLoader.LoadUiText(TestKit.RealDataDir());

        [Test] public void RealUiText_HasEveryKey() { var t = Real(); foreach (var k in UiKeys.All()) Assert.That(t.Has(k), k); }
        [Test] public void Get_MissingKey_ThrowsNamingKey()
        { var ex = Assert.Throws<DataException>(() => Real().Get("yok.boyle")); Assert.That(ex.Message, Does.Contain("yok.boyle")); }
        [Test] public void Format_FillsPlaceholders() => Assert.That(Real().Format(UiKeys.DuelTurn, 2, 3), Is.EqualTo("Tur 2/3"));
        [Test] public void TurkishCharactersSurviveLoading() => Assert.That(Real().Get(UiKeys.Role(CardRole.Both)), Is.EqualTo("İkisi de"));
        [Test] public void WrongSchemaVersion_Throws() => Assert.Throws<DataException>(() => DataLoader.UiTextFromJson("{\"schemaVersion\":2,\"strings\":{}}"));
        [Test] public void UnknownRootField_Throws() => Assert.Throws<DataException>(() => DataLoader.UiTextFromJson("{\"schemaVersion\":1,\"strings\":{},\"x\":1}"));
        [Test] public void EmptyValue_ThrowsNamingKey()
        {
            var ex = Assert.Throws<DataException>(() => DataLoader.UiTextFromJson("{\"schemaVersion\":1,\"strings\":{\"a.b\":\"\"}}"));
            Assert.That(ex.Message, Does.Contain("a.b"));
        }
        [TestCase(0.654, "%65")] [TestCase(0.05, "%5")] [TestCase(1.0, "%100")] [TestCase(0.655, "%66")]
        public void Percent_TurkishStyle(double p, string s) => Assert.That(UiText.Percent(p), Is.EqualTo(s));
        [Test] public void HpLabel_Keys()
        {
            Assert.That(UiKeys.HpLabel(DuelKind.Field, true), Is.EqualTo("hp.kontrol"));
            Assert.That(UiKeys.HpLabel(DuelKind.Field, false), Is.EqualTo("hp.denge"));
            Assert.That(UiKeys.HpLabel(DuelKind.Shot, false), Is.EqualTo("hp.kale"));
        }
        [Test] public void Result_PenaltyUsesShotKeys()
            => Assert.That(UiKeys.Result(DuelKind.Penalty, DuelOutcome.AttackerWon, true), Is.EqualTo("result.shot.AttackerWon.attacker"));
    }
}
