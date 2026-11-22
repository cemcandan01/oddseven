using System;
using System.Collections.Generic;
using System.Globalization;

namespace WeirdLand.Core.Data
{
    /// <summary>Player-facing Turkish strings, keyed by dotted ids (data/ui_tr.json).</summary>
    public sealed class UiText
    {
        private readonly IReadOnlyDictionary<string, string> _strings;

        public UiText(IReadOnlyDictionary<string, string> strings)
        {
            _strings = strings;
        }

        public bool Has(string key) => _strings.ContainsKey(key);

        public string Get(string key)
        {
            string value;
            if (!_strings.TryGetValue(key, out value))
                throw new DataException("ui_tr.json: missing key '" + key + "'");
            return value;
        }

        public string Format(string key, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, Get(key), args);

        public static string Percent(double p)
            => "%" + ((int)Math.Round(p * 100, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
    }
}
