using System.Collections.Generic;
using System.Text;

namespace GPL.Core
{
    /// <summary>RFC 4180 style CSV fields: quote a field only when it contains a comma, quote, CR or LF.</summary>
    public static class Csv
    {
        static readonly char[] Special = { ',', '"', '\n', '\r' };

        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.IndexOfAny(Special) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        public static string Join(IEnumerable<string> fields)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var f in fields)
            {
                if (!first) sb.Append(',');
                sb.Append(Escape(f));
                first = false;
            }
            return sb.ToString();
        }

        /// <summary>Splits one CSV line (no embedded line breaks) into unescaped fields.</summary>
        public static List<string> SplitLine(string line)
        {
            var fields = new List<string>();
            if (line == null) return fields;
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else quoted = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields;
        }
    }
}
