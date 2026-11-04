using System;
using System.Collections.Generic;
using GPL.Core;

namespace GPL.Logging
{
    /// <summary>
    /// Column layout of an event log: the common columns every chamber shares, then the chamber's own columns, then <c>note</c>
    /// (always last). Chamber columns are declared up front; writing an undeclared column throws, so a typo is caught by a unit test
    /// instead of silently producing an empty column in the data.
    /// </summary>
    public sealed class LogSchema
    {
        public const string Event = "event";
        public const string Timestamp = "timestamp_local";
        public const string Elapsed = "elapsed_s";
        public const string SessionId = "session_id";
        public const string Chamber = "chamber";
        public const string ChamberVersion = "chamber_version";
        public const string StudyId = "study_id";
        public const string Mode = "mode";
        public const string Preset = "preset";
        public const string ParamsHash = "params_hash";
        public const string Stage = "stage";
        public const string Block = "block";
        public const string Condition = "condition";
        public const string Trial = "trial";
        public const string Attempt = "attempt";
        public const string Note = "note";

        /// <summary>Common columns in file order (<see cref="Note"/> is appended after the chamber columns).</summary>
        public static readonly IReadOnlyList<string> CommonColumns = new[]
        {
            Event, Timestamp, Elapsed, SessionId, Chamber, ChamberVersion, StudyId, Mode, Preset, ParamsHash,
            Stage, Block, Condition, Trial, Attempt,
        };

        /// <summary>Columns the log fills itself; a row may not set them.</summary>
        internal static readonly HashSet<string> Automatic = new HashSet<string> { Event, Timestamp, Elapsed };

        readonly List<string> _columns = new List<string>();
        readonly Dictionary<string, int> _index = new Dictionary<string, int>();

        public IReadOnlyList<string> Columns => _columns;
        public IReadOnlyList<string> ChamberColumns { get; }
        public string Header { get; }

        public LogSchema(params string[] chamberColumns) : this((IEnumerable<string>)chamberColumns) { }

        public LogSchema(IEnumerable<string> chamberColumns)
        {
            foreach (var c in CommonColumns) AddColumn(c);
            var extra = new List<string>();
            foreach (var c in chamberColumns ?? Array.Empty<string>())
            {
                if (!IsValidColumn(c)) throw new ArgumentException($"Invalid column name '{c}': use lowercase letters, digits and '_'.");
                if (c == Note || _index.ContainsKey(c)) throw new ArgumentException($"Column '{c}' is already defined.");
                AddColumn(c);
                extra.Add(c);
            }
            AddColumn(Note);
            ChamberColumns = extra;
            Header = Csv.Join(_columns);
        }

        void AddColumn(string c) { _index[c] = _columns.Count; _columns.Add(c); }

        public bool Has(string column) => column != null && _index.ContainsKey(column);
        public int IndexOf(string column) => column != null && _index.TryGetValue(column, out int i) ? i : -1;

        public static bool IsValidColumn(string c)
        {
            if (string.IsNullOrEmpty(c) || !(c[0] >= 'a' && c[0] <= 'z')) return false;
            foreach (char ch in c) if (!((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_')) return false;
            return true;
        }
    }
}
