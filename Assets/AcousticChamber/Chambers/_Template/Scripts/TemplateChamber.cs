using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Core;
using GPL.Homing;
using GPL.Logging;
using GPL.Params;
using GPL.Protocol;
using GPL.Session;
using GPL.UI;
using UnityEngine;

namespace AcousticChamber.Chambers.Template
{
    /// <summary>
    /// The Template chamber (C00, Docs/Design.md): language, handover (system recenter), practice, blocks of trials (head
    /// gate on the fixation dot, then "Press A" with the reaction time), thanks. The protocol and its resume rules come from
    /// <see cref="ProtocolWalker"/>; this class runs the content of each step and writes the chamber's rows.
    /// </summary>
    public class TemplateChamber : ChamberBehaviour
    {
        public const string Id = "Template";

        public static class Stages
        {
            public const string Language = "language";
            public const string Handover = "handover";
            public const string Practice = "practice";
            public const string Experiment = "experiment";
            public const string Thanks = "thanks";
        }

        [Tooltip("The fixation dot (Magic > Template > Step 2). Empty = the first in the scene.")]
        public FixationCue cue;
        [Tooltip("Logs recenters by the Meta button during a run. Empty = the first in the scene.")]
        public RecenterListener listener;

        static ParameterSchema _schema;
        static LogSchema _log;

        public static ParameterSchema CreateSchema()
        {
            var defs = new List<ParamDef>
            {
                ParamDef.Int("practice_trials", 2, 0, 10, "Practice trials", "", "Procedure"),
                ParamDef.Int("blocks", 2, 1, 10, "Blocks", "", "Procedure"),
                ParamDef.Int("trials_per_block", 4, 1, 50, "Trials per block", "", "Procedure"),
                ParamDef.Float("response_timeout_s", 5f, 1f, 30f, 1f, "Response timeout", "s", "Procedure").Describe("no press = timeout"),
                ParamDef.Float("iti_s", 1f, 0f, 5f, 0.1f, "Inter-trial interval", "s", "Procedure"),
                ParamDef.Choice("resume_from", "block_start", ResumeNames.All, "Resume from", "Resume").Describe("experiment stage after an interruption"),
            };
            defs.AddRange(HomingParams.GateDefs());
            return new ParameterSchema(defs);
        }

        public static LogSchema CreateLogSchema() =>
            new LogSchema("rt_s", "timed_out", "gate_s", "gate_breaks", "stop_dialog", ChamberMenu.VolumeColumn);

        /// <summary>The stage list (Design.md "Flow"), from the session's parameter values.</summary>
        public static ProtocolPlan CreatePlan(ParameterSet v) =>
            new ProtocolPlan()
                .Stage(Stages.Language, StageFlags.Once)
                .Stage(Stages.Handover, StageFlags.RepeatOnResume)
                .Blocks(Stages.Practice, 1, v.GetInt("practice_trials"), ResumeFrom.StageStart)
                .Blocks(Stages.Experiment, v.GetInt("blocks"), v.GetInt("trials_per_block"), ResumeNames.Parse(v.GetText("resume_from")))
                .Stage(Stages.Thanks, StageFlags.CompletesSession);

        public override ParameterSchema Schema => _schema ?? (_schema = CreateSchema());
        public override LogSchema LogSchema => _log ?? (_log = CreateLogSchema());

        public override string Describe(SessionRecord r)
        {
            var plan = CreatePlan(SessionLauncher.Snapshot(r, Schema));
            var c = r.Cursor;
            if (c.Stage >= plan.Stages.Count) return "At the end";
            var stage = plan.Stages[c.Stage];
            if (stage.Name == Stages.Experiment)
                return "Block " + (c.Block + 1) + " of " + stage.Blocks + " · " + c.TrialsDone + (c.TrialsDone == 1 ? " trial" : " trials") + " done";
            return char.ToUpperInvariant(stage.Name[0]) + stage.Name.Substring(1);
        }

        public override string ResumeProblem(SessionRecord r, ParameterSet snapshot)
        {
            string hash = CreatePlan(snapshot).Hash();
            return string.IsNullOrEmpty(r.PlanHash) || r.PlanHash == hash ? null
                : "The Template's protocol changed since this session started (plan " + r.PlanHash + ", now " + hash + "). Start new instead.";
        }

        ChamberRun _run;
        ProtocolScreens _screens;
        HeadGateMethod _gate;
        OVRCameraRig _rig;

        public override IEnumerator Run(ChamberRun run)
        {
            _run = run;
            _screens = new ProtocolScreens(run.Canvas, run.Actions);
            _rig = HeadPose.FindRig();
            if (cue == null) cue = FindAnyObjectByType<FixationCue>();
            if (listener == null) listener = FindAnyObjectByType<RecenterListener>();
            if (cue == null) Debug.LogWarning("[Chamber:" + Id + "] no FixationCue: run Magic > Template > Step 2.");
            if (listener != null) listener.Recentered += OnUserRecenter;

            var rec = run.Record;
            var plan = CreatePlan(run.Values);
            if (!run.IsResume) rec.PlanHash = plan.Hash();
            if (Loc.TryParseCode(rec.Language, out var lang)) Loc.Set(lang);
            else Loc.Set(UiLanguage.English);
            var gate = HomingParams.ReadGate(run.Values);

            var walker = new ProtocolWalker(plan, rec, run.IsResume, _ => run.Save());
            while (walker.Next())
            {
                var s = walker.Current;
                ProtocolLog.Write(run.Log, s);
                switch (s.Kind)
                {
                    case StepKind.StageBegin:
                        yield return RunStage(s);
                        break;
                    case StepKind.Trial:
                        yield return RunTrial(s, gate);
                        break;
                    case StepKind.BlockEnd:
                        if (s.Stage.Name == Stages.Experiment && !s.LastBlock)
                        {
                            yield return _screens.Message(Loc.Get(ProtocolStrings.BlockDone, s.Block + 1, s.Blocks), true);
                            _screens.Hide();
                        }
                        break;
                }
            }
            Cleanup();
        }

        IEnumerator RunStage(ProtocolStep s)
        {
            var run = _run;
            switch (s.Stage.Name)
            {
                case Stages.Language:
                    yield return _screens.Language(l =>
                    {
                        run.Record.Language = Loc.CodeOf(l);
                        run.Save();
                        run.Log.Write(LogEvents.Language, Loc.CodeOf(l));
                    });
                    break;
                case Stages.Handover:
                    yield return _screens.Handover(_rig, r =>
                    {
                        run.Log.Write(LogEvents.Handover, "participant pressed A");
                        run.Log.Write(LogEvents.Homing, r.Note());
                    });
                    break;
                case Stages.Thanks:
                    yield return _screens.Message(Loc.Get(ProtocolStrings.Thanks), false, ProtocolStrings.Finish);
                    _screens.Hide();
                    break;
            }
        }

        /// <summary>
        /// One trial (Design.md "Trial"): the inter-trial interval (before every trial but the first of a block, so a trial's
        /// row is its last action and a kill never leaves a counted trial unlogged), the head gate, then "Press A".
        /// </summary>
        IEnumerator RunTrial(ProtocolStep s, GateSettings settings)
        {
            var run = _run;
            int stops = StopDialog.OpenCount;
            float iti = run.Values.GetFloat("iti_s");
            if (s.Trial > 0 && iti > 0f) yield return new WaitForSeconds(iti);

            HomingResult gate = null;
            if (_rig != null)
            {
                _gate = new HeadGateMethod(_rig, cue, settings);
                _gate.HintChanged += on => _screens.Cue(on ? Loc.Get(ProtocolStrings.LookAtDot) : null);
                yield return _gate.Run(r => gate = r);
                _gate = null;
                run.Log.Write(LogEvents.Homing, gate.Note());
            }

            // go cue: the dot is gone; the reaction time runs from this frame to the frame A is seen
            _screens.Cue(Loc.Get(ProtocolStrings.PressA), 0f);
            double t0 = Time.realtimeSinceStartupAsDouble;
            float timeout = run.Values.GetFloat("response_timeout_s"), waited = 0f;
            double rt = -1;
            while (true)
            {
                yield return null;
                if (_screens.PressedA())
                {
                    rt = Time.realtimeSinceStartupAsDouble - t0;
                    break;
                }
                waited += Time.deltaTime; // scaled: the Stop dialog freezes the timeout
                if (waited >= timeout) break;
            }
            _screens.Cue(null);

            var v = new LogValues()
                .Set("rt_s", rt >= 0 ? Fmt.F(rt, 3) : "")
                .Set("timed_out", rt < 0)
                .Set("gate_s", gate != null ? Fmt.F(gate.DurationSec, 3) : "")
                .Set("gate_breaks", gate != null ? Fmt.I(gate.Breaks) : "")
                .Set("stop_dialog", StopDialog.OpenCount != stops);
            run.Log.Write(LogEvents.Trial, v, "");
        }

        void OnUserRecenter(HomingResult r)
        {
            if (_run != null && !_run.Stopped) _run.Log.Write(LogEvents.Recenter, r.Note());
        }

        public override void Abort(ChamberRun run)
        {
            if (_gate != null) _gate.Cancel();
            if (cue != null) cue.Hide();
            Cleanup();
        }

        void Cleanup()
        {
            if (listener != null) listener.Recentered -= OnUserRecenter;
            if (_screens != null) _screens.Clear();
            _screens = null;
            _gate = null;
            _run = null;
        }
    }
}
