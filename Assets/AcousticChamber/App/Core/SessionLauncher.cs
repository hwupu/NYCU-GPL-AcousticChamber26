using System;
using System.Collections.Generic;
using System.IO;
using GPL.Core;
using GPL.Logging;
using GPL.Params;
using GPL.Session;

namespace AcousticChamber.App
{
    /// <summary>
    /// Creates, supersedes and reopens the sessions of a chamber (Architecture section 6): the pure part of the chamber menu's
    /// Start / Resume / Start new and of a Debug run. Real sessions get a <c>.session</c> file next to their CSV; Debug runs
    /// only a <c>_DEBUG</c> CSV.
    /// </summary>
    public static class SessionLauncher
    {
        /// <summary>A new real session for <paramref name="studyId"/> with a snapshot of <paramref name="values"/>; saved.</summary>
        public static SessionRecord NewSession(SessionStore store, string chamberId, string chamberVersion, string appVersion,
                                               string studyId, string presetName, ParameterSet values, int seed, DateTime now)
        {
            if (!StudyId.IsFormatted(studyId)) throw new ArgumentException("Study ID must look like S0001: " + studyId, nameof(studyId));
            var r = new SessionRecord
            {
                SessionId = DataFileNames.BaseName(now, chamberId, studyId, false),
                ChamberId = chamberId,
                ChamberVersion = chamberVersion ?? "",
                StudyId = studyId,
                Seed = seed,
                PresetName = presetName ?? "",
                ParamsHash = values.Hash(),
                AppVersion = appVersion ?? "",
                Created = now,
                Params = new List<KeyValuePair<string, string>>(values.Pairs()),
            };
            store.Save(r, now);
            return r;
        }

        /// <summary>Base name of a Debug run's CSV (no session file).</summary>
        public static string DebugRunId(string chamberId, DateTime now) => DataFileNames.BaseName(now, chamberId, "", true);

        /// <summary>
        /// "Start new" over an unfinished session: marks it <see cref="SessionStatus.Superseded"/> (saved) and appends a
        /// <c>session_superseded</c> row to its CSV when the CSV exists. Never deletes anything.
        /// </summary>
        public static void Supersede(SessionStore store, SessionRecord old, string byId, LogSchema schema, IClock clock)
        {
            old.Status = SessionStatus.Superseded;
            store.Save(old, clock.Now);
            string csv = store.CsvPathFor(old.SessionId);
            if (!File.Exists(csv)) return;
            using (var log = EventLog.Open(csv, clock, schema, ContextOf(old)))
                log.Write(LogEvents.SessionSuperseded, "by " + byId);
        }

        /// <summary>The log context of a real session (position columns empty).</summary>
        public static LogContext ContextOf(SessionRecord r) => new LogContext
        {
            SessionId = r.SessionId,
            Chamber = r.ChamberId,
            ChamberVersion = r.ChamberVersion,
            StudyId = r.StudyId,
            Mode = LogModes.Experiment,
            Preset = r.PresetName,
            ParamsHash = r.ParamsHash,
        };

        /// <summary>The parameter values a session was started with (a resume never uses a newer preset).</summary>
        public static ParameterSet Snapshot(SessionRecord r, ParameterSchema schema, List<string> warnings = null) =>
            schema.FromPairs(r.Params, warnings ?? new List<string>());
    }

    /// <summary>Preset spelling of <see cref="ResumeFrom"/> (a choice parameter such as the Template's <c>resume_from</c>).</summary>
    public static class ResumeNames
    {
        public static readonly string[] All = { "next_trial", "block_start", "stage_start", "session_start", "not_resumable" };

        public static ResumeFrom Parse(string name)
        {
            switch (name)
            {
                case "next_trial": return ResumeFrom.NextTrial;
                case "block_start": return ResumeFrom.BlockStart;
                case "stage_start": return ResumeFrom.StageStart;
                case "session_start": return ResumeFrom.SessionStart;
                case "not_resumable": return ResumeFrom.NotResumable;
                default: throw new ArgumentException("Unknown resume policy '" + name + "'", nameof(name));
            }
        }

        public static string Name(ResumeFrom r) => All[(int)r];
    }
}
