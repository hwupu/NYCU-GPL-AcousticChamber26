using System.Collections.Generic;
using System.Text;

namespace GPL.Core
{
    /// <summary>
    /// The text format of presets (<c>.params</c>) and session files (<c>.session</c>): one <c>key=value</c> per line, '#' starts a
    /// comment line, blank lines ignored, keys and values trimmed. Values escape backslash, CR and LF as <c>\\</c>, <c>\r</c>, <c>\n</c>.
    /// Human-readable and diff-friendly (HW2's progress file used the same idea), and needs no JSON library in pure C#.
    /// </summary>
    public static class KeyValueText
    {
        public static string Serialize(IEnumerable<KeyValuePair<string, string>> pairs, string headerComment = null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(headerComment))
                foreach (var line in headerComment.Split('\n')) sb.Append("# ").Append(line.TrimEnd('\r')).Append('\n');
            foreach (var kv in pairs) sb.Append(kv.Key).Append('=').Append(Escape(kv.Value)).Append('\n');
            return sb.ToString();
        }

        /// <summary>Ordered pairs as they appear; a repeated key keeps its last value at its first position.</summary>
        public static List<KeyValuePair<string, string>> Parse(string text)
        {
            var list = new List<KeyValuePair<string, string>>();
            var index = new Dictionary<string, int>();
            foreach (var raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = Unescape(line.Substring(eq + 1).Trim());
                if (index.TryGetValue(key, out int i)) list[i] = new KeyValuePair<string, string>(key, value);
                else { index[key] = list.Count; list.Add(new KeyValuePair<string, string>(key, value)); }
            }
            return list;
        }

        public static Dictionary<string, string> ParseToDictionary(string text)
        {
            var map = new Dictionary<string, string>();
            foreach (var kv in Parse(text)) map[kv.Key] = kv.Value;
            return map;
        }

        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        public static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('\\') < 0) return s ?? "";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    sb.Append(n == 'n' ? '\n' : n == 'r' ? '\r' : n);
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
