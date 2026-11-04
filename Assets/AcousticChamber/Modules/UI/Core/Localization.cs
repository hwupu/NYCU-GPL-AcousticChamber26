using System;
using System.Collections.Generic;
using System.Globalization;

namespace GPL.UI
{
    /// <summary>Languages of participant-facing text. Experimenter screens stay English.</summary>
    public enum UiLanguage { English, Chinese }

    /// <summary>A set of localized strings (key -> English, Chinese). Modules and chambers each register their own table.</summary>
    public sealed class StringTable
    {
        readonly Dictionary<string, string[]> _entries = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public string Name { get; }

        public StringTable(string name) { Name = name ?? ""; }

        /// <summary>Adds or replaces an entry. <paramref name="zh"/> = Traditional Chinese (Taiwan).</summary>
        public StringTable Add(string key, string en, string zh)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Empty key.", nameof(key));
            _entries[key] = new[] { en ?? "", zh ?? en ?? "" };
            return this;
        }

        public bool TryGet(string key, UiLanguage language, out string text)
        {
            text = null;
            if (key == null || !_entries.TryGetValue(key, out var e)) return false;
            text = e[(int)language];
            return true;
        }

        public int Count => _entries.Count;
    }

    /// <summary>
    /// Current language and string lookup. Tables registered later win over earlier ones (a chamber can override a module
    /// string). A missing key returns "[key]" so it is visible in the headset. Generalized from HW2's Localization.
    /// </summary>
    public static class Loc
    {
        static readonly List<StringTable> Tables = new List<StringTable>();

        public static UiLanguage Current { get; private set; } = UiLanguage.English;

        /// <summary>Raised after <see cref="Set"/> changed the language.</summary>
        public static event Action Changed;

        /// <summary>Button label of a language; exactly "English" and "中文" (PO decision).</summary>
        public static string LabelOf(UiLanguage language) => language == UiLanguage.Chinese ? "中文" : "English";

        /// <summary>Short code for logs and session files ("en", "zh-TW").</summary>
        public static string CodeOf(UiLanguage language) => language == UiLanguage.Chinese ? "zh-TW" : "en";

        public static bool TryParseCode(string code, out UiLanguage language)
        {
            language = UiLanguage.English;
            if (code == "en") return true;
            if (code == "zh-TW") { language = UiLanguage.Chinese; return true; }
            return false;
        }

        public static void Set(UiLanguage language)
        {
            if (language == Current) return;
            Current = language;
            Changed?.Invoke();
        }

        /// <summary>Registers a table once (registering the same instance again is ignored).</summary>
        public static void Register(StringTable table)
        {
            if (table == null || Tables.Contains(table)) return;
            Tables.Add(table);
        }

        public static bool Has(string key)
        {
            for (int i = Tables.Count - 1; i >= 0; i--)
                if (Tables[i].TryGet(key, Current, out _)) return true;
            return false;
        }

        public static string Get(string key, params object[] args) => Get(Current, key, args);

        public static string Get(UiLanguage language, string key, params object[] args)
        {
            for (int i = Tables.Count - 1; i >= 0; i--)
            {
                if (!Tables[i].TryGet(key, language, out var s)) continue;
                return args != null && args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, s, args) : s;
            }
            return "[" + key + "]";
        }

        /// <summary>Unit tests only: forget tables and go back to English without raising events.</summary>
        public static void ResetForTests()
        {
            Tables.Clear();
            Current = UiLanguage.English;
            Changed = null;
        }
    }
}
