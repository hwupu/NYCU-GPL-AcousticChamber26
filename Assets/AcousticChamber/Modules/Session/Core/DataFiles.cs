using System;
using System.Collections.Generic;
using System.IO;
using GPL.Core;

namespace GPL.Session
{
    /// <summary>One session's files as the data manager shows them: the CSV and its <c>.session</c> file (Debug runs: CSV only).</summary>
    public sealed class DataEntry
    {
        public string BaseName = "";
        public string CsvPath;          // null if missing
        public string SessionPath;      // null if missing (Debug runs, or a lost file)
        public DateTime Start;
        public string StudyId = "";
        public bool IsDebug;
        /// <summary>From the session file; null for Debug runs or when there is no session file.</summary>
        public SessionStatus? Status;
        public long Bytes;
        /// <summary>Number of rows whose event is <c>trial</c>.</summary>
        public int TrialRows;
    }

    /// <summary>
    /// Lists and deletes the data files of ONE chamber folder (Architecture 10.1). Only top-level <c>.csv</c> / <c>.session</c> files
    /// whose names follow <see cref="DataFileNames"/> are touched; sub-folders (e.g. <c>presets/</c>) and other files are ignored.
    /// The CSV and the session file are always deleted together.
    /// </summary>
    public sealed class DataFiles
    {
        readonly string _dir;
        readonly string _chamberId;

        public DataFiles(string chamberDataDirectory, string chamberId)
        {
            _dir = chamberDataDirectory ?? throw new ArgumentNullException(nameof(chamberDataDirectory));
            _chamberId = Ids.SanitizeToken(chamberId);
        }

        /// <summary>The chamber data folder this instance lists.</summary>
        public string Folder => _dir;

        /// <summary>Entries of this chamber, newest first.</summary>
        public List<DataEntry> List()
        {
            var byBase = new Dictionary<string, DataEntry>();
            if (!Directory.Exists(_dir)) return new List<DataEntry>();
            foreach (var f in Directory.GetFiles(_dir))
            {
                string file = Path.GetFileName(f);
                string baseName; bool isCsv;
                if (file.EndsWith(DataFileNames.CsvExtension, StringComparison.Ordinal)) { isCsv = true; baseName = file.Substring(0, file.Length - DataFileNames.CsvExtension.Length); }
                else if (file.EndsWith(DataFileNames.SessionExtension, StringComparison.Ordinal)) { isCsv = false; baseName = file.Substring(0, file.Length - DataFileNames.SessionExtension.Length); }
                else continue;
                if (!DataFileNames.TryParse(baseName, out var start, out var chamber, out var study, out var debug) || chamber != _chamberId) continue;

                if (!byBase.TryGetValue(baseName, out var e))
                    byBase[baseName] = e = new DataEntry { BaseName = baseName, Start = start, StudyId = study, IsDebug = debug };
                e.Bytes += new FileInfo(f).Length;
                if (isCsv) { e.CsvPath = f; e.TrialRows = CountTrialRows(f); }
                else
                {
                    e.SessionPath = f;
                    string text = AtomicFile.ReadAllText(f);
                    if (text != null) e.Status = SessionRecord.Parse(text).Status;
                }
            }
            var list = new List<DataEntry>(byBase.Values);
            list.Sort((a, b) => b.Start != a.Start ? b.Start.CompareTo(a.Start) : string.CompareOrdinal(b.BaseName, a.BaseName));
            return list;
        }

        static int CountTrialRows(string csvPath)
        {
            int n = 0;
            using (var r = new StreamReader(new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                    if (line.StartsWith("trial,", StringComparison.Ordinal)) n++;
            }
            return n;
        }

        /// <summary>Deletes both files of the entry (and a leftover temp session file). Returns the number of files removed.</summary>
        public int Delete(DataEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            // re-derive paths from the base name inside our folder: never delete a path handed in from elsewhere
            if (!DataFileNames.TryParse(entry.BaseName, out _, out var chamber, out _, out _) || chamber != _chamberId)
                throw new ArgumentException($"'{entry.BaseName}' is not a data file of chamber {_chamberId}.");
            int n = 0;
            string csv = Path.Combine(_dir, entry.BaseName + DataFileNames.CsvExtension);
            if (File.Exists(csv)) { File.Delete(csv); n++; }
            n += AtomicFile.Delete(Path.Combine(_dir, entry.BaseName + DataFileNames.SessionExtension));
            return n;
        }

        /// <summary>Deletes every entry of this chamber. Returns the number of entries deleted.</summary>
        public int DeleteAll()
        {
            var entries = List();
            foreach (var e in entries) Delete(e);
            return entries.Count;
        }
    }
}
