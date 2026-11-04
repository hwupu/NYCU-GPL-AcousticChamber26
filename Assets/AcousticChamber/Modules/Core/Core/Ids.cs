using System.Text;

namespace GPL.Core
{
    /// <summary>Identifier rules shared by file names and logs.</summary>
    public static class Ids
    {
        /// <summary>Keeps ASCII letters, digits, '-' and '_' (safe in file names on Android, Windows and macOS).</summary>
        public static string Sanitize(string s) => Keep(s, allowUnderscore: true);

        /// <summary>
        /// Like <see cref="Sanitize"/> but without '_': chamber IDs are parts of file names that are split on '_'
        /// (<c>yyyyMMdd_HHmmss_Chamber_S0001_DEBUG</c>).
        /// </summary>
        public static string SanitizeToken(string s) => Keep(s, allowUnderscore: false);

        static string Keep(string s, bool allowUnderscore)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || (allowUnderscore && c == '_');
                if (ok) sb.Append(c);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Study IDs are typed as up to four digits on the number pad and stored with a prefix and leading zeros:
    /// "1" becomes "S0001" (PO decision, 2026-11-05).
    /// </summary>
    public static class StudyId
    {
        public const string Prefix = "S";
        public const int MaxDigits = 4;

        /// <summary>True if <paramref name="digits"/> is 1..4 ASCII digits (leading zeros allowed) and not all zeros.</summary>
        public static bool IsValidDigits(string digits)
        {
            if (string.IsNullOrEmpty(digits) || digits.Length > MaxDigits) return false;
            int value = 0;
            foreach (char c in digits)
            {
                if (c < '0' || c > '9') return false;
                value = value * 10 + (c - '0');
            }
            return value > 0;
        }

        /// <summary>"1" -> "S0001", "0042" -> "S0042". Returns "" if the digits are not valid.</summary>
        public static string FromDigits(string digits)
        {
            if (!IsValidDigits(digits)) return "";
            return Prefix + int.Parse(digits, Fmt.Inv).ToString("D" + MaxDigits, Fmt.Inv);
        }

        /// <summary>True for a formatted ID such as "S0001".</summary>
        public static bool IsFormatted(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length != Prefix.Length + MaxDigits || !id.StartsWith(Prefix)) return false;
            return IsValidDigits(id.Substring(Prefix.Length));
        }
    }
}
