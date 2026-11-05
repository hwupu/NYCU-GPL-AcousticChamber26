using System;
using System.Collections.Generic;
using System.Globalization;
using GPL.Params;

namespace GPL.DebugBench
{
    /// <summary>
    /// The Debug bench's working copy of a preset (ADR-0002): Debug runs use it; real sessions never do. It remembers the
    /// preset it came from (<see cref="Source"/>), so it can show what was edited, revert, save back (not to Default) or
    /// save as a new preset. Stepping stays inside each parameter's range (no out-of-range values ever reach the set).
    /// Unity-free; the UI is <c>GPL.DebugBench.DebugBench</c>.
    /// </summary>
    public sealed class WorkingCopy
    {
        ParameterSet _source;

        public PresetStore Store { get; }

        /// <summary>The preset the working copy was loaded from.</summary>
        public string Source { get; private set; }

        public ParameterSet Values { get; private set; }

        /// <summary>Repairs reported when the source preset was loaded (older chamber version).</summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>Raised after any change (step, revert, load, save).</summary>
        public event Action Changed;

        /// <summary>A parameter changed by a step: key, old value, new value (log <c>param_change</c>).</summary>
        public event Action<string, string, string> ParamChanged;

        /// <summary>Loads the active preset.</summary>
        public WorkingCopy(PresetStore store)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Load(store.ActiveName);
        }

        public ParameterSchema Schema => Store.Schema;

        /// <summary>Replaces the working copy with the preset <paramref name="name"/> (unsaved edits are lost).</summary>
        public void Load(string name)
        {
            Warnings.Clear();
            _source = Store.Load(name, Warnings);
            Values = _source.Clone();
            Source = name;
            Changed?.Invoke();
        }

        public List<string> EditedKeys => Values.DiffKeys(_source);
        public bool IsEdited => EditedKeys.Count > 0;
        public bool IsKeyEdited(string key) => Values.GetText(key) != _source.GetText(key);

        /// <summary>The value in the source preset.</summary>
        public string SourceValue(string key) => _source.GetText(key);

        /// <summary>Default is read-only (it is <c>Config/default.params</c> in the project): use Save as.</summary>
        public bool SourceIsReadOnly => Source == PresetStore.DefaultName;

        public bool CanSave => !SourceIsReadOnly && IsEdited;

        /// <summary>"Default", or "Pilot-A · 2 edited".</summary>
        public string Label
        {
            get
            {
                int n = EditedKeys.Count;
                return n == 0 ? Source : Source + " · " + n + " edited";
            }
        }

        // ---- stepping ------------------------------------------------------------------------------------------

        /// <summary>True if <paramref name="key"/> can move one step in direction <paramref name="dir"/> (−1 or +1).</summary>
        public bool CanStep(string key, int dir) => Next(key, dir) != null;

        /// <summary>Moves <paramref name="key"/> one step (numbers: by the step, inside min..max; Bool: toggles; Choice: next / previous, wrapping). Returns false at a limit.</summary>
        public bool Step(string key, int dir)
        {
            string next = Next(key, dir);
            if (next == null) return false;
            string old = Values.GetText(key);
            Values.Set(key, next);
            ParamChanged?.Invoke(key, old, Values.GetText(key));
            Changed?.Invoke();
            return true;
        }

        string Next(string key, int dir)
        {
            var d = Schema.Get(key) ?? throw new ArgumentException("Unknown parameter '" + key + "'.");
            string raw = Values.GetText(key);
            switch (d.Type)
            {
                case ParamType.Bool:
                    return raw == "true" ? "false" : "true";
                case ParamType.Choice:
                {
                    int n = d.Choices.Count;
                    if (n < 2) return null;
                    int i = 0;
                    for (int k = 0; k < n; k++) if (d.Choices[k] == raw) i = k;
                    return d.Choices[((i + Math.Sign(dir)) % n + n) % n];
                }
                case ParamType.Int:
                case ParamType.Float:
                {
                    double step = d.Step > 0 ? d.Step : 1;
                    double v = double.Parse(raw, CultureInfo.InvariantCulture);
                    double nv = Math.Round(v / step + Math.Sign(dir)) * step; // snapped to the step grid
                    if (nv < d.Min - 1e-6 || nv > d.Max + 1e-6)
                    {
                        // off-grid value next to a limit: go to the limit itself
                        double limit = dir > 0 ? d.Max : d.Min;
                        if (double.IsInfinity(limit) || Math.Abs(limit - v) < 1e-6) return null;
                        nv = limit;
                    }
                    int dec = Decimals(d);
                    string s = d.Type == ParamType.Int
                        ? ((long)Math.Round(nv)).ToString(CultureInfo.InvariantCulture)
                        : Math.Round(nv, dec).ToString("F" + dec, CultureInfo.InvariantCulture);
                    return d.TryNormalize(s, out string canonical, out _) && canonical != raw ? canonical : null;
                }
                default:
                    return null; // Text: not editable on the headset
            }
        }

        // ---- presets -------------------------------------------------------------------------------------------

        /// <summary>Back to the source preset's values.</summary>
        public void Revert()
        {
            Values = _source.Clone();
            Changed?.Invoke();
        }

        /// <summary>Overwrites the source preset with the working copy (not Default).</summary>
        public void Save()
        {
            if (SourceIsReadOnly) throw new InvalidOperationException("Default is read-only; use Save as.");
            Store.Save(Source, Values);
            _source = Values.Clone();
            Changed?.Invoke();
        }

        /// <summary>The name <see cref="SaveAs"/> would use.</summary>
        public string NextName => Store.NextFreeName();

        /// <summary>Saves the working copy as a new preset (it is not made active) and continues editing that one. Returns its name.</summary>
        public string SaveAs(string name = null)
        {
            name = name ?? Store.NextFreeName();
            Store.Save(name, Values);
            _source = Values.Clone();
            Source = name;
            Changed?.Invoke();
            return name;
        }

        // ---- display -------------------------------------------------------------------------------------------

        /// <summary>A value for display: numbers with the step's decimals and the unit ("0.7 s"), Bool as On / Off.</summary>
        public static string Format(ParamDef d, string raw)
        {
            switch (d.Type)
            {
                case ParamType.Bool:
                    return raw == "true" ? "On" : "Off";
                case ParamType.Float:
                {
                    double v = double.Parse(raw, CultureInfo.InvariantCulture);
                    return v.ToString("F" + Decimals(d), CultureInfo.InvariantCulture) + Unit(d);
                }
                case ParamType.Int:
                    return raw + Unit(d);
                default:
                    return raw;
            }
        }

        static string Unit(ParamDef d) => string.IsNullOrEmpty(d.Unit) ? "" : " " + d.Unit;

        /// <summary>Decimals shown for a number parameter: as many as its step needs (0.1 → 1, 0.05 → 2, 1 → 0).</summary>
        public static int Decimals(ParamDef d)
        {
            if (d.Type != ParamType.Float || d.Step <= 0) return 0;
            for (int dec = 0; dec < 6; dec++)
            {
                double scaled = d.Step * Math.Pow(10, dec);
                if (Math.Abs(scaled - Math.Round(scaled)) < 1e-6) return dec;
            }
            return 6;
        }
    }
}
