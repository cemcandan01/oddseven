using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using WeirdLand.Core.Ai;
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
                Assert.That(EventLog.Format(new GameEvent[] { new PassAttempted(true, "x", 0.5, true) }),
                    Does.Contain("Chance = 0x3FE0000000000000"));
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

        [Test] public void Format_WritesDoubleAsRawBits()
            => Assert.That(EventLog.Format(new GameEvent[] { new PassAttempted(true, "x", 0.5, true) }),
                Is.EqualTo("PassAttempted { ByUser = True, TargetId = x, Chance = 0x3FE0000000000000, Success = True }"));

        [Test] public void Format_WritesEnumIntAndNull()
            => Assert.That(EventLog.Format(new GameEvent[] { new DuelEnded(DuelOutcome.PassFailed), new IntentRevealed("c", null), new HalfTime() }),
                Is.EqualTo(string.Join("\n",
                    "DuelEnded { Outcome = PassFailed }",
                    "IntentRevealed { CardId = c, PassTargetId = null }",
                    "HalfTime { }")));

        [Test] public void Format_UnsupportedPropertyType_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => EventLog.Format(new GameEvent[] { new ListEvent(new List<int> { 1 }) }));
            Assert.That(ex.Message, Does.Contain("ListEvent").And.Contain("Values"));
        }

        private record ListEvent(List<int> Values) : GameEvent;

        [Test] public void Fingerprint_IsEightLowerHex() => Assert.That(EventLog.Fingerprint(new GameEvent[0]), Does.Match("^[0-9a-f]{8}$"));

        [Test] public void Golden_MatchFingerprint() => Assert.That(GoldenRun.Fingerprint(TestKit.RealData()), Is.EqualTo("3b407571"));

        private static string GridFingerprint()
        {
            var m = TestKit.NewGridMatch(1);
            var r = GridRunner.Play(m, new GreedyGridPolicy(), new GoodPolicy(), new Rng(1));
            return EventLog.Fingerprint(r.Events);
        }

        [Test] public void Golden_GridMatchFingerprint() => Assert.That(GridFingerprint(), Is.EqualTo("7d8ace35"));

        [Test] public void GridMatchFingerprint_SameSeedTwice_Equal() => Assert.That(GridFingerprint(), Is.EqualTo(GridFingerprint()));
    }
}
