using System;
using System.Collections.Generic;
using System.IO;
using GPL.Core;

namespace GPL.Session
{
    /// <summary>
    /// The <c>.session</c> files of one chamber, stored next to their CSV in the chamber's data folder
    /// (<c>persistentDataPath/&lt;ChamberId&gt;/</c>). Several unfinished sessions may exist (one per Study ID).
    /// </summary>
    public sealed class SessionStore
    {
        readonly string _dir;

        public SessionStore(string chamberDataDirectory) => _dir = chamberDataDirectory ?? throw new ArgumentNullException(nameof(chamberDataDirectory));

        public string Directory => _dir;

        public string PathFor(string sessionId) => Path.Combine(_dir, sessionId + DataFileNames.SessionExtension);
        public string CsvPathFor(string sessionId) => Path.Combine(_dir, sessionId + DataFileNames.CsvExtension);

        /// <summary>Writes the record atomically; sets <see cref="SessionRecord.Updated"/>.</summary>
        public void Save(SessionRecord record, DateTime now)
        {
            if (string.IsNullOrEmpty(record.SessionId)) throw new ArgumentException("Session ID required.");
            record.Updated = now;
            AtomicFile.WriteAllText(PathFor(record.SessionId), record.Serialize());
        }

        public SessionRecord Load(string sessionId)
        {
            string text = AtomicFile.ReadAllText(PathFor(sessionId));
            return text == null ? null : SessionRecord.Parse(text);
        }

        /// <summary>All sessions of the folder, newest first. Unreadable files are skipped.</summary>
        public List<SessionRecord> ListAll()
        {
            var list = new List<SessionRecord>();
            if (!System.IO.Directory.Exists(_dir)) return list;
            var ids = new HashSet<string>();
            foreach (var f in System.IO.Directory.GetFiles(_dir, "*" + DataFileNames.SessionExtension + "*"))
            {
                string name = Path.GetFileName(f);
                if (name.EndsWith(AtomicFile.TempSuffix, StringComparison.Ordinal)) name = name.Substring(0, name.Length - AtomicFile.TempSuffix.Length);
                if (!name.EndsWith(DataFileNames.SessionExtension, StringComparison.Ordinal)) continue;
                ids.Add(name.Substring(0, name.Length - DataFileNames.SessionExtension.Length));
            }
            foreach (var id in ids)
            {
                try { var r = Load(id); if (r != null) list.Add(r); }
                catch (IOException) { }
            }
            list.Sort((a, b) => b.Created.CompareTo(a.Created));
            return list;
        }

        /// <summary>Resumable sessions (in progress, policy allows it), newest first.</summary>
        public List<SessionRecord> ListUnfinished() => ListAll().FindAll(r => r.CanResume);

        /// <summary>The newest resumable session of a Study ID, or null.</summary>
        public SessionRecord FindUnfinished(string studyId)
        {
            foreach (var r in ListUnfinished()) if (r.StudyId == studyId) return r;
            return null;
        }
    }
}
