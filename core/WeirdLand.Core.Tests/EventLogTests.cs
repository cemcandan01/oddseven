using System.Globalization;
using NUnit.Framework;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Tests
{
    public class EventLogTests
    {
        [Test] public void Format_OneLinePerEvent()
            => Assert.That(EventLog.Format(new GameEvent[] { new TurnStarted(1), new EnergyChanged(true, 3) }).Split('\n').Length, Is.EqualTo(2));

        [Test] public void Fingerprint_SameUnderTurkishAndInvariantCulture()
        {
            var ev = new GameEvent[] { new PassAttempted(true, "x", 0.655, true) };
            var old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var tr = EventLog.Fingerprint(ev);
                Assert.That(EventLog.Format(ev), Does.Contain("0.655"));
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.That(EventLog.Fingerprint(ev), Is.EqualTo(tr));
            }
            finally { CultureInfo.CurrentCulture = old; }
        }

        [Test] public void Format_RestoresCulture()
        {
            var old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                EventLog.Format(new GameEvent[] { new TurnStarted(1) });
                Assert.That(CultureInfo.CurrentCulture.Name, Is.EqualTo("tr-TR"));
            }
            finally { CultureInfo.CurrentCulture = old; }
        }

        [Test] public void Fingerprint_IsEightLowerHex() => Assert.That(EventLog.Fingerprint(new GameEvent[0]), Does.Match("^[0-9a-f]{8}$"));

        [Test] public void Golden_MatchFingerprint() => Assert.That(GoldenRun.Fingerprint(TestKit.RealData()), Is.EqualTo("20012847"));
    }
}
