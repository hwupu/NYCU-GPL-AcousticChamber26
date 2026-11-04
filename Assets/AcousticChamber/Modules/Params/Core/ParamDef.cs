using System;
using System.Collections.Generic;
using GPL.Core;

namespace GPL.Params
{
    public enum ParamType { Bool, Int, Float, Text, Choice }

    /// <summary>
    /// One experimental parameter: key (used in preset files, session files and logs), type, default, limits and the label/group
    /// the Debug panel shows. Create with the static factories; values are stored as culture-invariant strings.
    /// </summary>
    public sealed class ParamDef
    {
        public string Key { get; private set; }
        public ParamType Type { get; private set; }
        public string Default { get; private set; }
        public double Min { get; private set; } = double.NegativeInfinity;
        public double Max { get; private set; } = double.PositiveInfinity;
        /// <summary>Step of the Debug control (+/- buttons or slider). 0 = free.</summary>
        public double Step { get; private set; }
        public IReadOnlyList<string> Choices { get; private set; } = Array.Empty<string>();
        public string Label { get; private set; }
        public string Group { get; private set; } = "";
        public string Unit { get; private set; } = "";
        public string Description { get; private set; } = "";

        ParamDef() { }

        public static ParamDef Float(string key, float @default, float min, float max, float step, string label, string unit = "", string group = "") =>
            Make(key, ParamType.Float, Fmt.R(@default), min, max, step, label, unit, group);

        public static ParamDef Int(string key, int @default, int min, int max, string label, string unit = "", string group = "", int step = 1) =>
            Make(key, ParamType.Int, Fmt.I(@default), min, max, step, label, unit, group);

        public static ParamDef Bool(string key, bool @default, string label, string group = "") =>
            Make(key, ParamType.Bool, Fmt.B(@default), 0, 1, 0, label, "", group);

        public static ParamDef Text(string key, string @default, string label, string group = "") =>
            Make(key, ParamType.Text, @default ?? "", double.NegativeInfinity, double.PositiveInfinity, 0, label, "", group);

        public static ParamDef Choice(string key, string @default, string[] choices, string label, string group = "")
        {
            if (choices == null || choices.Length == 0) throw new ArgumentException("A choice parameter needs choices.", nameof(choices));
            if (Array.IndexOf(choices, @default) < 0) throw new ArgumentException($"Default '{@default}' is not one of the choices of '{key}'.");
            return Make(key, ParamType.Choice, @default, double.NegativeInfinity, double.PositiveInfinity, 0, label, "", group, (string[])choices.Clone());
        }

        /// <summary>Adds the help text shown in the Debug panel and in the parameter table of the chamber docs.</summary>
        public ParamDef Describe(string description) { Description = description ?? ""; return this; }

        static ParamDef Make(string key, ParamType type, string def, double min, double max, double step, string label, string unit, string group,
                             string[] choices = null)
        {
            if (!IsValidKey(key)) throw new ArgumentException($"Invalid parameter key '{key}': use lowercase letters, digits, '_' and '.'.");
            if (min > max) throw new ArgumentException($"Parameter '{key}': min > max.");
            var d = new ParamDef { Key = key, Type = type, Default = def, Min = min, Max = max, Step = step, Label = string.IsNullOrEmpty(label) ? key : label, Unit = unit ?? "", Group = group ?? "" };
            if (choices != null) d.Choices = choices;
            if (!d.TryNormalize(def, out _, out string err)) throw new ArgumentException($"Parameter '{key}': default is invalid ({err}).");
            return d;
        }

        /// <summary>Keys are snake_case with optional dots (e.g. <c>hl.left.level</c>), so they are safe as CSV column names.</summary>
        public static bool IsValidKey(string key)
        {
            if (string.IsNullOrEmpty(key) || !(key[0] >= 'a' && key[0] <= 'z')) return false;
            foreach (char c in key)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '.')) return false;
            return true;
        }

        /// <summary>
        /// Checks a raw value and returns its canonical text (numbers in invariant format, bools as true/false). Numbers outside
        /// [Min, Max] are rejected, not clamped, so a typo never silently becomes a different experimental value.
        /// </summary>
        public bool TryNormalize(string raw, out string canonical, out string error)
        {
            canonical = null; error = null;
            raw = (raw ?? "").Trim();
            switch (Type)
            {
                case ParamType.Bool:
                    if (Fmt.TryBool(raw, out bool b)) { canonical = Fmt.B(b); return true; }
                    error = $"'{raw}' is not true/false"; return false;
                case ParamType.Int:
                    if (!Fmt.TryInt(raw, out int i)) { error = $"'{raw}' is not an integer"; return false; }
                    if (i < Min || i > Max) { error = $"{i} is outside [{Fmt.F(Min)}, {Fmt.F(Max)}]"; return false; }
                    canonical = Fmt.I(i); return true;
                case ParamType.Float:
                    if (!Fmt.TryDouble(raw, out double f) || double.IsNaN(f) || double.IsInfinity(f)) { error = $"'{raw}' is not a number"; return false; }
                    // compare in float: limits were given as floats (0.3f is 0.30000001 as a double, typed "0.3" must still pass)
                    float fv = (float)f;
                    if (fv < (float)Min || fv > (float)Max) { error = $"{Fmt.F(f)} is outside [{Fmt.F((float)Min)}, {Fmt.F((float)Max)}]"; return false; }
                    canonical = Fmt.R(fv); return true;
                case ParamType.Choice:
                    foreach (var c in Choices) if (c == raw) { canonical = raw; return true; }
                    error = $"'{raw}' is not one of {string.Join("/", Choices)}"; return false;
                default:
                    canonical = raw; return true;
            }
        }
    }
}
