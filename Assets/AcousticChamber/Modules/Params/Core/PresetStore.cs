using System;
using System.Collections.Generic;
using System.IO;
using GPL.Core;

namespace GPL.Params
{
    /// <summary>
    /// Presets of one chamber (ADR-0002). The built-in preset <see cref="DefaultName"/> comes from the chamber's
    /// <c>Config/default.params</c> (in git, read-only here). Saved presets are <c>&lt;name&gt;.params</c> files in
    /// <paramref name="directory"/>; <c>active.txt</c> names the active one. Real sessions always use the active preset;
    /// with no other choice made, that is Default.
    /// </summary>
    public sealed class PresetStore
    {
        public const string DefaultName = "Default";
        public const string Extension = ".params";
        const string ActiveFile = "active.txt";
        public const int MaxNameLength = 24;

        readonly string _dir;
        readonly string _defaultText;

        public ParameterSchema Schema { get; }
        public string Directory => _dir;

        public PresetStore(string directory, ParameterSchema schema, string defaultFileText)
        {
            _dir = directory ?? throw new ArgumentNullException(nameof(directory));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _defaultText = defaultFileText ?? "";
        }

        /// <summary>Letters, digits, '-' and '_', 1..24 characters, not "Default" (any case).</summary>
        public static bool IsValidName(string name) =>
            !string.IsNullOrEmpty(name) && name.Length <= MaxNameLength && Ids.Sanitize(name) == name
            && !string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase);

        string PathFor(string name) => Path.Combine(_dir, name + Extension);

        /// <summary>Default first, then saved presets in alphabetical order.</summary>
        public List<string> List()
        {
            var names = new List<string>();
            if (System.IO.Directory.Exists(_dir))
                foreach (var f in System.IO.Directory.GetFiles(_dir, "*" + Extension))
                {
                    string n = Path.GetFileNameWithoutExtension(f);
                    if (IsValidName(n)) names.Add(n);
                }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            names.Insert(0, DefaultName);
            return names;
        }

        public bool Exists(string name) => name == DefaultName || (IsValidName(name) && AtomicFile.Exists(PathFor(name)));

        /// <summary>The preset's complete set; repairs (older chamber version) are reported in <paramref name="warnings"/>.</summary>
        public ParameterSet Load(string name, List<string> warnings = null)
        {
            if (name == DefaultName) return Schema.Parse(_defaultText, warnings);
            if (!IsValidName(name)) throw new ArgumentException($"Invalid preset name '{name}'.");
            string text = AtomicFile.ReadAllText(PathFor(name));
            if (text == null) throw new FileNotFoundException($"Preset '{name}' not found.", PathFor(name));
            return Schema.Parse(text, warnings);
        }

        /// <summary>Saves (creates or overwrites) a preset. Default cannot be overwritten: change <c>default.params</c> in the project.</summary>
        public void Save(string name, ParameterSet values, string comment = null)
        {
            if (!IsValidName(name)) throw new ArgumentException($"Invalid preset name '{name}' (letters, digits, '-', '_'; not '{DefaultName}').");
            if (values == null || !ReferenceEquals(values.Schema, Schema)) throw new ArgumentException("Values of a different schema.");
            AtomicFile.WriteAllText(PathFor(name), values.Serialize(comment));
        }

        /// <summary>Deletes a saved preset. If it was active, Default becomes active. Returns false for Default or a missing preset.</summary>
        public bool Delete(string name)
        {
            if (!IsValidName(name) || !AtomicFile.Exists(PathFor(name))) return false;
            bool wasActive = ActiveName == name;
            AtomicFile.Delete(PathFor(name));
            if (wasActive) SetActive(DefaultName);
            return true;
        }

        /// <summary>The active preset; Default when none was chosen or the chosen one no longer exists.</summary>
        public string ActiveName
        {
            get
            {
                string text = AtomicFile.ReadAllText(Path.Combine(_dir, ActiveFile));
                string name = text?.Trim();
                return !string.IsNullOrEmpty(name) && Exists(name) ? name : DefaultName;
            }
        }

        public void SetActive(string name)
        {
            if (!Exists(name)) throw new ArgumentException($"Preset '{name}' does not exist.");
            AtomicFile.WriteAllText(Path.Combine(_dir, ActiveFile), name + "\n");
        }

        public ParameterSet LoadActive(List<string> warnings = null) => Load(ActiveName, warnings);

        /// <summary>A free name like "Preset-3" for a quick save from the headset (no keyboard needed).</summary>
        public string NextFreeName(string stem = "Preset")
        {
            for (int i = 1; i < 1000; i++)
            {
                string n = stem + "-" + i;
                if (!Exists(n)) return n;
            }
            throw new InvalidOperationException("No free preset name.");
        }
    }
}
