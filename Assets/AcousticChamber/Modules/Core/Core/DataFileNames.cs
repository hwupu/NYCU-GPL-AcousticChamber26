using System;
using System.Globalization;

namespace GPL.Core
{
    /// <summary>
    /// Names of the per-session file pair in a chamber's data folder:
    /// <c>yyyyMMdd_HHmmss_&lt;ChamberId&gt;[_&lt;StudyId&gt;][_DEBUG]</c> + <c>.csv</c> (events) and + <c>.session</c> (resume state).
    /// The base name is also the session ID, so the pair is always found together.
    /// </summary>
    public static class DataFileNames
    {
        public const string CsvExtension = ".csv";
        public const string SessionExtension = ".session";
        public const string DebugSuffix = "DEBUG";
        const string StampFormat = "yyyyMMdd_HHmmss";

        public static string BaseName(DateTime start, string chamberId, string studyId, bool debug)
        {
            string chamber = Ids.SanitizeToken(chamberId);
            if (chamber.Length == 0) throw new ArgumentException("Chamber ID must contain letters or digits.", nameof(chamberId));
            string name = start.ToString(StampFormat, CultureInfo.InvariantCulture) + "_" + chamber;
            string id = Ids.SanitizeToken(studyId);
            if (id.Length > 0) name += "_" + id;
            if (debug) name += "_" + DebugSuffix;
            return name;
        }

        /// <summary>Parsed parts of a base name; false if the name does not follow the pattern.</summary>
        public static bool TryParse(string baseName, out DateTime start, out string chamberId, out string studyId, out bool debug)
        {
            start = default; chamberId = ""; studyId = ""; debug = false;
            if (string.IsNullOrEmpty(baseName)) return false;
            var parts = baseName.Split('_');
            if (parts.Length < 3) return false;
            if (!DateTime.TryParseExact(parts[0] + "_" + parts[1], StampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
                return false;
            chamberId = parts[2];
            int i = 3;
            if (i < parts.Length && parts[i] != DebugSuffix) studyId = parts[i++];
            if (i < parts.Length && parts[i] == DebugSuffix) { debug = true; i++; }
            return i == parts.Length && chamberId.Length > 0;
        }
    }
}
