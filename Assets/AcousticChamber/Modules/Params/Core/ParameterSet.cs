using System;
using System.Collections.Generic;
using GPL.Core;

namespace GPL.Params
{
    /// <summary>
    /// A complete set of parameter values for one schema (every key present, every value valid). Values are canonical strings;
    /// typed getters convert. Setters validate and throw on invalid values, so a set can never hold a value the schema rejects.
    /// </summary>
    public sealed class ParameterSet
    {
        readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public ParameterSchema Schema { get; }

        internal ParameterSet(ParameterSchema schema) => Schema = schema;

        internal void SetRaw(string key, string canonical) => _values[key] = canonical;

        ParamDef Def(string key)
        {
            var d = Schema.Get(key);
            if (d == null) throw new KeyNotFoundException($"Unknown parameter '{key}'.");
            return d;
        }

        public string GetText(string key) { Def(key); return _values[key]; }
        public bool GetBool(string key) { Def(key); return _values[key] == "true"; }
        public int GetInt(string key) { Def(key); Fmt.TryInt(_values[key], out int v); return v; }
        public float GetFloat(string key) { Def(key); Fmt.TryFloat(_values[key], out float v); return v; }

        /// <summary>Sets a value from text (Debug panel, file). Throws ArgumentException if the schema rejects it.</summary>
        public void Set(string key, string raw)
        {
            var d = Def(key);
            if (!d.TryNormalize(raw, out var canonical, out var err)) throw new ArgumentException($"'{key}': {err}");
            _values[key] = canonical;
        }

        public bool TrySet(string key, string raw, out string error)
        {
            error = null;
            var d = Schema.Get(key);
            if (d == null) { error = $"unknown parameter '{key}'"; return false; }
            if (!d.TryNormalize(raw, out var canonical, out error)) return false;
            _values[key] = canonical;
            return true;
        }

        public void Set(string key, float value) => Set(key, Fmt.R(value));
        public void Set(string key, int value) => Set(key, Fmt.I(value));
        public void Set(string key, bool value) => Set(key, Fmt.B(value));

        /// <summary>Pairs in schema order.</summary>
        public IEnumerable<KeyValuePair<string, string>> Pairs()
        {
            foreach (var d in Schema.Defs) yield return new KeyValuePair<string, string>(d.Key, _values[d.Key]);
        }

        /// <summary>The <c>.params</c> file text (schema order).</summary>
        public string Serialize(string headerComment = null) => KeyValueText.Serialize(Pairs(), headerComment);

        /// <summary>8 hex digits identifying the values (not the preset name); logged in every row as <c>params_hash</c>.</summary>
        public string Hash() => Hashing.Short(Serialize());

        public ParameterSet Clone()
        {
            var c = new ParameterSet(Schema);
            foreach (var kv in _values) c._values[kv.Key] = kv.Value;
            return c;
        }

        /// <summary>Keys whose values differ from <paramref name="other"/> (same schema), in schema order.</summary>
        public List<string> DiffKeys(ParameterSet other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (!ReferenceEquals(other.Schema, Schema)) throw new ArgumentException("Parameter sets of different schemas.");
            var keys = new List<string>();
            foreach (var d in Schema.Defs) if (_values[d.Key] != other._values[d.Key]) keys.Add(d.Key);
            return keys;
        }

        public bool ValuesEqual(ParameterSet other) => DiffKeys(other).Count == 0;
    }
}
