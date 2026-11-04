using System;
using System.Collections.Generic;
using GPL.Core;

namespace GPL.Session
{
    /// <summary>
    /// Everything needed to continue an interrupted real session (generalized from HW2's SessionProgress): who, which chamber and
    /// version, which CSV, the seed (identical trial plan), the parameter snapshot (a resumed session never picks up a newer preset),
    /// the cursor and the per-block attempt counters. Saved as a <c>.session</c> key=value file after every checkpoint.
    /// A trial that was running when the app stopped has no result and is not counted: it runs again.
    /// </summary>
    public sealed class SessionRecord
    {
        public const int FormatVersion = 1;

        public string SessionId = "";       // = base name of the CSV and .session pair
        public string ChamberId = "";
        public string ChamberVersion = "";
        public string StudyId = "";
        public SessionStatus Status = SessionStatus.InProgress;
        public int Seed;
        public string Language = "";
        public string PresetName = "";
        public string ParamsHash = "";
        public string PlanHash = "";
        public string AppVersion = "";
        public DateTime Created;
        public DateTime Updated;

        public SessionCursor Cursor;
        /// <summary>Policy of the stage the cursor is in; set by the protocol when it enters a stage.</summary>
        public ResumeFrom ResumePolicy = ResumeFrom.NextTrial;

        /// <summary>Parameter snapshot (key -> canonical value), in schema order.</summary>
        public List<KeyValuePair<string, string>> Params = new List<KeyValuePair<string, string>>();
        /// <summary>Chamber-specific state (e.g. staircase level), key -> value.</summary>
        public Dictionary<string, string> Custom = new Dictionary<string, string>();

        readonly Dictionary<string, int> _attempts = new Dictionary<string, int>();

        static string AttemptKey(int stage, int block) => Fmt.I(stage) + "." + Fmt.I(block);

        /// <summary>Highest attempt started for a block (0 = never begun).</summary>
        public int AttemptsOf(int stage, int block) => _attempts.TryGetValue(AttemptKey(stage, block), out int n) ? n : 0;

        public bool CanResume => Status == SessionStatus.InProgress && ResumePolicy != ResumeFrom.NotResumable;

        // ---- progress (call, then save) -------------------------------------------------------------------------------

        /// <summary>Enter a stage (first block, nothing begun) and remember its resume policy.</summary>
        public void EnterStage(int stage, ResumeFrom policy)
        {
            Cursor = new SessionCursor(stage, 0, 0, 0);
            ResumePolicy = policy;
        }

        /// <summary>Begin (or begin again) the cursor's block: new attempt number, trials from 0. Returns the attempt.</summary>
        public int BeginBlock()
        {
            string key = AttemptKey(Cursor.Stage, Cursor.Block);
            int attempt = (_attempts.TryGetValue(key, out int n) ? n : 0) + 1;
            _attempts[key] = attempt;
            Cursor.TrialsDone = 0;
            Cursor.Attempt = attempt;
            return attempt;
        }

        public void TrialFinished()
        {
            if (!Cursor.BlockBegun) throw new InvalidOperationException("TrialFinished before BeginBlock.");
            Cursor.TrialsDone++;
        }

        /// <summary>Move to the next block of the stage (not begun yet).</summary>
        public void BlockFinished() => Cursor = new SessionCursor(Cursor.Stage, Cursor.Block + 1, 0, 0);

        /// <summary>Move past the current stage; the protocol then calls <see cref="EnterStage"/> for the next one.</summary>
        public void StageFinished() => Cursor = new SessionCursor(Cursor.Stage + 1, 0, 0, 0);

        // ---- resume ---------------------------------------------------------------------------------------------------

        /// <summary>Where to continue under <see cref="ResumePolicy"/>; throws if the session cannot be resumed.</summary>
        public ResumePoint ResolveResume()
        {
            if (!CanResume) throw new InvalidOperationException($"Session {SessionId} cannot be resumed (status {Status}, policy {ResumePolicy}).");
            switch (ResumePolicy)
            {
                case ResumeFrom.NextTrial:
                    return new ResumePoint { Stage = Cursor.Stage, Block = Cursor.Block, NextTrial = Cursor.BlockBegun ? Cursor.TrialsDone : 0, BeginBlock = !Cursor.BlockBegun, Policy = ResumePolicy };
                case ResumeFrom.BlockStart:
                    return new ResumePoint { Stage = Cursor.Stage, Block = Cursor.Block, NextTrial = 0, BeginBlock = true, Policy = ResumePolicy };
                case ResumeFrom.StageStart:
                    return new ResumePoint { Stage = Cursor.Stage, Block = 0, NextTrial = 0, BeginBlock = true, Policy = ResumePolicy };
                case ResumeFrom.SessionStart:
                    return new ResumePoint { Stage = 0, Block = 0, NextTrial = 0, BeginBlock = true, Policy = ResumePolicy };
                default:
                    throw new InvalidOperationException("Unknown policy " + ResumePolicy);
            }
        }

        /// <summary>
        /// Moves the cursor to the resolved point. If <see cref="ResumePoint.BeginBlock"/>, the block is not begun yet (attempt 0):
        /// the protocol calls <see cref="BeginBlock"/>, which gives it the next attempt number. Earlier rows stay in the CSV.
        /// </summary>
        public void ApplyResume(ResumePoint p)
        {
            if (p.BeginBlock) Cursor = new SessionCursor(p.Stage, p.Block, 0, 0);
            else Cursor = new SessionCursor(p.Stage, p.Block, p.NextTrial, Cursor.Attempt);
        }

        /// <summary>Short text for the chamber menu, e.g. "stage 2, block 1, 3 trials done". Chambers usually pass their own describer.</summary>
        public string Describe(Func<SessionRecord, string> describer = null) =>
            describer != null ? describer(this) : $"stage {Cursor.Stage + 1}, block {Cursor.Block + 1}, {Cursor.TrialsDone} trials done";

        // ---- file format ----------------------------------------------------------------------------------------------

        public string Serialize()
        {
            var p = new List<KeyValuePair<string, string>>();
            void Put(string k, string v) => p.Add(new KeyValuePair<string, string>(k, v ?? ""));
            Put("format", Fmt.I(FormatVersion));
            Put("session_id", SessionId);
            Put("chamber", ChamberId);
            Put("chamber_version", ChamberVersion);
            Put("study_id", StudyId);
            Put("status", Status.ToString());
            Put("seed", Fmt.I(Seed));
            Put("language", Language);
            Put("preset", PresetName);
            Put("params_hash", ParamsHash);
            Put("plan_hash", PlanHash);
            Put("app_version", AppVersion);
            Put("created", Fmt.Timestamp(Created));
            Put("updated", Fmt.Timestamp(Updated));
            Put("cursor.stage", Fmt.I(Cursor.Stage));
            Put("cursor.block", Fmt.I(Cursor.Block));
            Put("cursor.trials_done", Fmt.I(Cursor.TrialsDone));
            Put("cursor.attempt", Fmt.I(Cursor.Attempt));
            Put("resume_from", ResumePolicy.ToString());
            var keys = new List<string>(_attempts.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var k in keys) Put("attempt." + k, Fmt.I(_attempts[k]));
            foreach (var kv in Params) Put("param." + kv.Key, kv.Value);
            var custom = new List<string>(Custom.Keys);
            custom.Sort(StringComparer.Ordinal);
            foreach (var k in custom) Put("custom." + k, Custom[k]);
            return KeyValueText.Serialize(p, "AcousticChamber session state. Written by the app; do not edit while a session runs.");
        }

        public static SessionRecord Parse(string text)
        {
            var r = new SessionRecord();
            var pairs = KeyValueText.Parse(text);
            var map = new Dictionary<string, string>();
            foreach (var kv in pairs) map[kv.Key] = kv.Value;
            string Get(string k) => map.TryGetValue(k, out var v) ? v : "";
            int Int(string k) => Fmt.TryInt(Get(k), out int n) ? n : 0;
            DateTime Time(string k) => Fmt.TryParseTimestamp(Get(k), out var t) ? t : default;

            r.SessionId = Get("session_id");
            r.ChamberId = Get("chamber");
            r.ChamberVersion = Get("chamber_version");
            r.StudyId = Get("study_id");
            r.Status = Enum.TryParse(Get("status"), out SessionStatus st) ? st : SessionStatus.InProgress;
            r.Seed = Int("seed");
            r.Language = Get("language");
            r.PresetName = Get("preset");
            r.ParamsHash = Get("params_hash");
            r.PlanHash = Get("plan_hash");
            r.AppVersion = Get("app_version");
            r.Created = Time("created");
            r.Updated = Time("updated");
            r.Cursor = new SessionCursor(Int("cursor.stage"), Int("cursor.block"), Int("cursor.trials_done"), Int("cursor.attempt"));
            r.ResumePolicy = Enum.TryParse(Get("resume_from"), out ResumeFrom rf) ? rf : ResumeFrom.NextTrial;
            foreach (var kv in pairs)
            {
                if (kv.Key.StartsWith("attempt.", StringComparison.Ordinal) && Fmt.TryInt(kv.Value, out int a))
                    r._attempts[kv.Key.Substring("attempt.".Length)] = a;
                else if (kv.Key.StartsWith("param.", StringComparison.Ordinal))
                    r.Params.Add(new KeyValuePair<string, string>(kv.Key.Substring("param.".Length), kv.Value));
                else if (kv.Key.StartsWith("custom.", StringComparison.Ordinal))
                    r.Custom[kv.Key.Substring("custom.".Length)] = kv.Value;
            }
            return r;
        }
    }
}
