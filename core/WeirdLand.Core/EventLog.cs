using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core
{
    /// <summary>Culture-independent text form and fingerprint of an event list.</summary>
    public static class EventLog
    {
        /// <summary>
        /// One line per event: <c>TypeName { Prop = value, ... }</c>, properties in declaration
        /// (metadata token) order. Every value is written explicitly so the text is identical on
        /// every runtime: doubles as their raw IEEE bits, never through double.ToString().
        /// </summary>
        public static string Format(IEnumerable<GameEvent> events)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var e in events)
            {
                if (!first) sb.Append('\n');
                first = false;
                AppendEvent(sb, e);
            }
            return sb.ToString();
        }

        static void AppendEvent(StringBuilder sb, GameEvent e)
        {
            var type = e.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(props, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            sb.Append(type.Name).Append(" {");
            for (int i = 0; i < props.Length; i++)
            {
                sb.Append(i == 0 ? " " : ", ").Append(props[i].Name).Append(" = ");
                sb.Append(FormatValue(type, props[i], props[i].GetValue(e)));
            }
            sb.Append(" }");
        }

        static string FormatValue(Type owner, PropertyInfo prop, object value)
        {
            if (value == null) return "null";
            switch (value)
            {
                case double d: return "0x" + BitConverter.DoubleToInt64Bits(d).ToString("X16", CultureInfo.InvariantCulture);
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case long l: return l.ToString(CultureInfo.InvariantCulture);
                case bool b: return b ? "True" : "False";
                case string s: return s;
                case Enum en: return en.ToString();
                default:
                    throw new InvalidOperationException(
                        "EventLog cannot format " + owner.Name + "." + prop.Name + " of type " + prop.PropertyType.Name);
            }
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
