using System;
using System.Collections.Generic;

namespace GPL.Params
{
    /// <summary>The ordered list of a chamber's parameters. The order is the order of preset files, the Debug panel and the docs.</summary>
    public sealed class ParameterSchema
    {
        readonly List<ParamDef> _defs = new List<ParamDef>();
        readonly Dictionary<string, ParamDef> _byKey = new Dictionary<string, ParamDef>();

        public ParameterSchema(IEnumerable<ParamDef> defs)
        {
            foreach (var d in defs) Add(d);
        }

        public ParameterSchema(params ParamDef[] defs) : this((IEnumerable<ParamDef>)defs) { }

        void Add(ParamDef d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            if (_byKey.ContainsKey(d.Key)) throw new ArgumentException($"Duplicate parameter key '{d.Key}'.");
            _defs.Add(d);
            _byKey[d.Key] = d;
        }

        public IReadOnlyList<ParamDef> Defs => _defs;
        public int Count => _defs.Count;
        public bool Contains(string key) => key != null && _byKey.ContainsKey(key);
        public ParamDef Get(string key) => key != null && _byKey.TryGetValue(key, out var d) ? d : null;

        /// <summary>All parameters at their defaults.</summary>
        public ParameterSet Defaults()
        {
            var set = new ParameterSet(this);
            foreach (var d in _defs) set.SetRaw(d.Key, d.Default);
            return set;
        }

        /// <summary>
        /// Builds a complete, valid set from stored pairs (a preset or a session snapshot): known valid values are kept; missing keys get
        /// their default; unknown keys and invalid values are dropped. Every repair is reported in <paramref name="warnings"/> so the
        /// experimenter sees when a preset was made with an older version of the chamber.
        /// </summary>
        public ParameterSet FromPairs(IEnumerable<KeyValuePair<string, string>> pairs, List<string> warnings)
        {
            var given = new Dictionary<string, string>();
            foreach (var kv in pairs)
            {
                if (!Contains(kv.Key)) { warnings?.Add($"unknown parameter '{kv.Key}' ignored"); continue; }
                given[kv.Key] = kv.Value;
            }
            var set = new ParameterSet(this);
            foreach (var d in _defs)
            {
                if (!given.TryGetValue(d.Key, out var raw)) { warnings?.Add($"'{d.Key}' missing, default {d.Default} used"); set.SetRaw(d.Key, d.Default); continue; }
                if (d.TryNormalize(raw, out var canonical, out var err)) set.SetRaw(d.Key, canonical);
                else { warnings?.Add($"'{d.Key}': {err}; default {d.Default} used"); set.SetRaw(d.Key, d.Default); }
            }
            return set;
        }

        /// <summary>A preset/default file's text into a complete set (see <see cref="FromPairs"/>).</summary>
        public ParameterSet Parse(string text, List<string> warnings) => FromPairs(GPL.Core.KeyValueText.Parse(text), warnings);
    }
}
