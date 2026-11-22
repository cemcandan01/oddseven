using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core
{
    /// <summary>Culture-independent text form and fingerprint of an event list.</summary>
    public static class EventLog
    {
        public static string Format(IEnumerable<GameEvent> events)
        {
            var old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                var sb = new StringBuilder();
                var first = true;
                foreach (var e in events)
                {
                    if (!first) sb.Append('\n');
                    first = false;
                    sb.Append(e.ToString());
                }
                return sb.ToString();
            }
            finally { CultureInfo.CurrentCulture = old; }
        }

        /// <summary>FNV-1a 32-bit over the UTF-8 bytes of Format(events), as 8 lowercase hex digits.</summary>
        public static string Fingerprint(IEnumerable<GameEvent> events)
        {
            uint h = 2166136261u;
            foreach (var b in Encoding.UTF8.GetBytes(Format(events)))
            {
                h ^= b;
                h = unchecked(h * 16777619u);
            }
            return h.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>A fixed match whose fingerprint proves the core behaves identically on every runtime.</summary>
    public static class GoldenRun
    {
        public const uint MatchSeed = 20261122, PolicySeed = 7;

        public static string Fingerprint(GameData data)
        {
            var m = new MatchEngine(data, data.Team("mavi"), data.Team("kirmizi"), MatchSeed);
            var result = MatchRunner.Play(m, new GoodPolicy(), new Rng(PolicySeed));
            return EventLog.Fingerprint(result.Events);
        }
    }
}
