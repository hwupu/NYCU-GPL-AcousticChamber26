using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Acoustics;
using GPL.Core;
using GPL.DebugBench;
using GPL.HearingLoss;
using GPL.Homing;
using GPL.Input;
using GPL.Logging;
using GPL.Params;
using GPL.Protocol;
using GPL.Responses;
using GPL.Session;
using GPL.SpatialSources;
using GPL.Stimuli;
using GPL.UI;
using UnityEngine;

namespace AcousticChamber.Chambers.HumPerFinal
{
    /// <summary>
    /// HumPerFinal (C05, Docs/Design.md): HW2's ventriloquist task in one 0.7 s room, with 5 hearing conditions × anchors off / on
    /// (10 cells, Williams order by Study ID mod 10), 8 trials per cell (4 position pairs, pairing set by Study ID mod 4 × 2 repeats)
    /// back to back, free 360° adjustment, and a trial restart after the Stop dialog's Keep going or after a resume.
    /// </summary>
    public class HumPerFinalChamber : ProtocolChamber
    {
        public const string Id = "HumPerFinal";
        /// <summary>Session file keys (DataFormat "Files"): <c>custom.order_row</c>, <c>custom.pair_set</c>, <c>custom.condition_order</c>, <c>custom.open_trial</c>.</summary>
        public const string OrderRowKey = "order_row";
        public const string PairSetKey = "pair_set";
        public const string OrderKey = "condition_order";
        public const string OpenTrialKey = "open_trial";

        public static class Stages
        {
            public const string Practice = "practice";
            public const string Experiment = "experiment";
            public const string Thanks = OnboardingStages.Thanks;
        }

        [Header("Scene (Magic > HumPerFinal > Steps 3-4)")]
        [Tooltip("The white-noise target on the circle (pulse train, moved by the triggers).")]
        public PlacedSource stimulus;
        public PulseTrain pulse;
        [Tooltip("The radio (box placeholder): root faces the participant; hidden outside trials.")]
        public Transform picture;
        [Tooltip("Rubber duck (0°) and trumpet (+90°): models and their sound loop.")]
        public AnchorLoop anchors;
        public ReverbTuner reverb;
        public ShoeboxRoom room;
        [Tooltip("Debug: orange sphere on the sound.")]
        public SourceMarker marker;

        /// <summary>The target (Design.md "Stimuli"): Gaussian white noise, 2 s, 20 ms cos² ramps, -26 dBFS RMS, frozen (seed 1).</summary>
        public static readonly StimulusRecipe StimulusRecipe = GPL.Stimuli.StimulusRecipe.WhiteNoise(2.0, seed: 1, rampS: 0.020);

        static ParameterSchema _schema;
        static LogSchema _log;
        AudioClip _stimulusClip;
        OutputHearingLoss _hearing;

        void Awake()
        {
            if (stimulus == null) return;
            _stimulusClip = StimulusClips.CreateAtOutputRate(StimulusRecipe);
            stimulus.GetComponent<AudioSource>().clip = _stimulusClip;
            Debug.Log("[Chamber:" + Id + "] Stimulus " + StimulusRecipe.WithSampleRate(_stimulusClip.frequency).Note());
        }

        void OnDestroy() => StimulusClips.Release(_stimulusClip);

        /// <summary>The preset keys and defaults of Design.md "Parameters" (in this order: it gives the params_hash).</summary>
        public static ParameterSchema CreateSchema()
        {
            var defs = new List<ParamDef>
            {
                ParamDef.Choice(OrderKey, CellOrder.Williams, CellOrder.Modes, "Condition order", "Procedure")
                        .Describe("williams: row = Study ID mod 10; fixed: cells 0-9"),
                ParamDef.Float("iti_s", 0.6f, 0f, 5f, 0.1f, "Inter-trial interval", "s", "Procedure"),
                ParamDef.Int("repeats", 2, 1, 4, "Repeats", "", "Procedure").Describe("per position pair and block"),
                ParamDef.Float("radius_m", 3f, 0.5f, 5f, 0.1f, "Radius", "m", "Stimuli"),
                ParamDef.Float("practice.radius_m", 1.5f, 0.5f, 5f, 0.1f, "Practice radius", "m", "Stimuli"),
                ParamDef.Float("audio_start_deg", 60f, 0f, 90f, 2.5f, "Sound start", "deg", "Stimuli").Describe("−a or +a"),
                ParamDef.Float("practice.audio_start_deg", 30f, -90f, 90f, 2.5f, "Practice sound start", "deg", "Stimuli"),
                ParamDef.Float("practice.visual_deg", 0f, -90f, 90f, 2.5f, "Practice radio", "deg", "Stimuli"),
                ParamDef.Text("visual_pool_deg", "7.5,22.5", "Radio azimuths", "Stimuli").Describe("inner,outer: right side, mirrored left"),
                ParamDef.Float("stim.on_s", 2f, 0.1f, 5f, 0.1f, "Pulse on", "s", "Stimuli"),
                ParamDef.Float("stim.off_s", 0.7f, 0f, 5f, 0.1f, "Pulse off", "s", "Stimuli"),
                ParamDef.Float("anchor.duck_deg", 0f, -180f, 180f, 2.5f, "Duck azimuth", "deg", "Anchors"),
                ParamDef.Float("anchor.trumpet_deg", 90f, -180f, 180f, 2.5f, "Trumpet azimuth", "deg", "Anchors"),
                ParamDef.Float("anchor.radius_m", 3f, 0.5f, 5f, 0.1f, "Anchor radius", "m", "Anchors"),
                ParamDef.Float("anchor.gap_after_duck_s", 1.5f, 0f, 10f, 0.1f, "Pause after duck", "s", "Anchors"),
                ParamDef.Float("anchor.gap_after_trumpet_s", 2.5f, 0f, 10f, 0.1f, "Pause after trumpet", "s", "Anchors"),
                ParamDef.Float("adjust.rate_deg_s", 10f, 1f, 60f, 1f, "Adjust speed", "deg/s", "Response"),
                ParamDef.Float("confirm_ignore_s", 0.3f, 0f, 2f, 0.1f, "Ignore A for", "s", "Response"),
                ParamDef.Float("rt60_s", 0.7f, 0.3f, 6f, 0.05f, "RT60", "s", "Acoustics"),
                ParamDef.Bool("rt60.measure_at_block_start", false, "Measure RT60 at block start", "Acoustics"),
                ParamDef.Choice("resume_from", "next_trial", ResumeNames.All, "Resume from", "Resume").Describe("experiment stage after an interruption"),
                HeadsetVolume.TargetDef(12),
            };
            defs.AddRange(OnboardingStages.SwitchDefs());
            defs.AddRange(HomingParams.GateDefs());
            return new ParameterSchema(defs);
        }

        /// <summary>The preset Default: the schema with HW2's head gate (±5°, ±10°, 1 s).</summary>
        public static ParameterSet CreateDefaults()
        {
            var v = CreateSchema().Defaults();
            v.Set("gate.yaw_tol_deg", 5f);
            v.Set("gate.pitch_tol_deg", 10f);
            v.Set("gate.hold_s", 1f);
            return v;
        }

        /// <summary>DataFormat.md "Columns" (chamber columns, in this order).</summary>
        public static LogSchema CreateLogSchema() => new LogSchema(
            "hearing", "anchors", "order_row", "pair_set", "trial_index", "repeat", "visual_az_deg", "audio_start_az_deg", "start_side",
            "audio_final_az_deg", "audio_final_unwrapped_deg", "signed_error_deg", "duration_s", "restarts", "reversals", "radius_m",
            "rt60_target_s", "rt60_measured_s", "stim_level_dbfs", "anchor_level_dbfs", "gate_s", "stop_dialog", ChamberMenu.VolumeColumn);

        /// <summary>Trials per block from the preset (4 position pairs × repeats = 8 by default).</summary>
        public static int TrialsPerBlock(ParameterSet v) => TrialPlanner.TrialsPerBlock(v.GetInt("repeats"));

        /// <summary>
        /// Design.md "Flow": onboarding, practice, 10 cell blocks back to back (no break screens), thanks. The cell order comes from
        /// the Study ID and lives in the session file, so the plan hash is the same for every participant.
        /// </summary>
        public static ProtocolPlan Plan(ParameterSet v) =>
            OnboardingStages.AddTo(new ProtocolPlan(), v)
                .Stage(Stages.Practice, StageFlags.None, ResumeFrom.StageStart)
                .Blocks(Stages.Experiment, Cells.Count, TrialsPerBlock(v), ResumeNames.Parse(v.GetText("resume_from")))
                .Stage(Stages.Thanks, StageFlags.CompletesSession);

        /// <summary>Design.md "Trial": left / right trigger move the sound (A = confirm is standard).</summary>
        public override void DeclareActions(BindingTable table)
        {
            table.DeclareStandard(InputActions.TriggerLeft, "move the sound left");
            table.DeclareStandard(InputActions.TriggerRight, "move the sound right");
        }

        protected override string ChamberId => Id;
        /// <summary>Design.md "Flow" 4: the last instruction slide's button reads "Start Practice" / "開始練習".</summary>
        protected override System.Func<UiLanguage, string> InstructionsLastLabel => l => Loc.Get(l, Strings.StartPractice); // the table is registered in StartRun, before the onboarding
        public override ParameterSchema Schema => _schema ?? (_schema = CreateSchema());
        public override LogSchema LogSchema => _log ?? (_log = CreateLogSchema());
        public override ProtocolPlan CreatePlan(ParameterSet values) => Plan(values);

        /// <summary>The session's cell order (from the session file; set on a new session); null in the bench's practice trial.</summary>
        protected int[] Order { get; private set; }
        protected int OrderRow { get; private set; }
        /// <summary>The session's pairing set (Design.md "Position pairs"), 0-3.</summary>
        protected int PairSetOfRun { get; private set; }

        public override IEnumerator Run(ChamberRun run)
        {
            Order = EnsureOrder(run.Record, run.Values, out int row);
            OrderRow = row;
            PairSetOfRun = EnsurePairSet(run.Record);
            StartRun();
            yield return base.Run(run);
            EndRun();
        }

        public override IEnumerator PracticeTrial(ChamberRun run)
        {
            Order = null;
            OrderRow = 0;
            PairSetOfRun = 0;
            StartRun();
            yield return base.PracticeTrial(run);
            EndRun();
        }

        /// <summary>
        /// The cell order of a session: read from <c>custom.condition_order</c> / <c>custom.order_row</c>, or (new session) computed
        /// from the preset's <c>condition_order</c> and the Study ID and stored in the record.
        /// </summary>
        public static int[] EnsureOrder(SessionRecord record, ParameterSet values, out int row)
        {
            if (record.Custom.TryGetValue(OrderKey, out var text) && CellOrder.TryParse(text, out var stored))
            {
                row = record.Custom.TryGetValue(OrderRowKey, out var r) && int.TryParse(r, System.Globalization.NumberStyles.Integer, Fmt.Inv, out int n) ? n : 0;
                return stored;
            }
            row = CellOrder.RowOf(record.StudyId);
            var order = CellOrder.For(values.GetText(OrderKey), row);
            record.Custom[OrderRowKey] = Fmt.I(row);
            record.Custom[OrderKey] = CellOrder.Format(order);
            return order;
        }

        /// <summary>
        /// The pairing set of a session: read from <c>custom.pair_set</c>, or (new session) <c>study_id % 4</c>, stored in the record
        /// so a resume keeps it.
        /// </summary>
        public static int EnsurePairSet(SessionRecord record)
        {
            if (record.Custom.TryGetValue(PairSetKey, out var text)
                && int.TryParse(text, System.Globalization.NumberStyles.Integer, Fmt.Inv, out int stored)
                && stored >= 0 && stored < PairSet.Count)
                return stored;
            int set = PairSet.Of(record.StudyId);
            record.Custom[PairSetKey] = Fmt.I(set);
            return set;
        }

        /// <summary>The cell of an experiment block (0-based) in this session (Debug practice trial: cell 0).</summary>
        protected int CellOf(int block) => Order != null && block >= 0 && block < Order.Length ? Order[block] : 0;

        // ---- strings (English / 中文) -----------------------------------------------------------------------

        public static class Strings
        {
            public const string PracticeAsk = "humperfinal.practice_ask";
            public const string PracticeAgain = "humperfinal.practice_again";
            public const string Ready = "humperfinal.ready";
            /// <summary>Next on the last instruction slide ("Ready?"): the practice follows (PO 2026-11-18).</summary>
            public const string StartPractice = "humperfinal.start_practice";

            public static readonly StringTable Table = new StringTable(Id)
                .Add(StartPractice, "Start Practice", "開始練習")
                .Add(PracticeAsk, "Practice again, or start the experiment?", "要再練習一次，還是開始正式實驗？")
                .Add(PracticeAgain, "Practice again", "再練習一次")
                .Add(Ready, "I'm ready", "我準備好了");
        }

        /// <summary>Log events of this chamber (DataFormat.md "Events").</summary>
        public static class Events
        {
            public const string Condition = "condition";
            public const string Anchors = "anchors";
            public const string TrialRestart = "trial_restart";
            public const string PracticeChoice = "practice_choice";
            public const string Rt60Measured = "rt60_measured";
        }

        // ---- run state ---------------------------------------------------------------------------------------

        string _applied;            // cell name (or practice) whose hearing and anchors are on; null = none yet in this run
        bool _roomSet;
        float _rt60Target;
        float _rt60Measured = float.NaN;
        readonly Dictionary<int, List<TrialSpec>> _lists = new Dictionary<int, List<TrialSpec>>();
        bool _inTrial, _restartRequested;
        TrialSpec _spec;

        void StartRun()
        {
            Loc.Register(Strings.Table);
            StopDebugPlayback();
            _applied = null;
            _roomSet = false;
            _rt60Measured = float.NaN;
            _lists.Clear();
            _inTrial = false;
            _hearing = OutputHearingLoss.Ensure();
            if (_hearing != null) _hearing.SetProfile(HearingProfile.Normal);
            else Debug.LogWarning("[Chamber:" + Id + "] no AudioListener: hearing loss cannot be applied.");
            if (listener != null) listener.Recentered += OnRecenter;
            StopDialog.OpenChanged += OnStopDialogChanged;
        }

        void EndRun()
        {
            StopDialog.OpenChanged -= OnStopDialogChanged;
            if (listener != null) listener.Recentered -= OnRecenter;
            StopStimuli();
            if (anchors != null) anchors.End();
            if (_hearing != null) _hearing.SetProfile(HearingProfile.Normal);
            _applied = null;
        }

        protected override void OnAbort() => EndRun();

        /// <summary>Keep going after a Stop dialog that opened during a trial's adjustment restarts the trial (Design.md "Pause and restart").</summary>
        void OnStopDialogChanged(bool open)
        {
            if (!open && _inTrial) _restartRequested = true;
        }

        /// <summary>Block rows carry the block's cell; practice rows carry <c>practice</c>.</summary>
        protected override void BeforeRow(ProtocolStep s)
        {
            if (CurrentRun == null || s.Kind == StepKind.StageBegin || s.Kind == StepKind.StageEnd) return;
            if (s.Stage.Name == Stages.Experiment) CurrentRun.Context.Condition = Cells.Name(CellOf(s.Block));
            else if (s.Stage.Name == Stages.Practice) CurrentRun.Context.Condition = Cells.Practice;
        }

        protected override IEnumerator RunStage(ProtocolStep s)
        {
            if (s.Stage.Name == Stages.Practice) return Practice();
            if (s.Stage.Name == Stages.Thanks) { StopStimuli(); if (anchors != null) anchors.End(); if (_hearing != null) _hearing.SetProfile(HearingProfile.Normal); }
            return base.RunStage(s);
        }

        /// <summary>Design.md "Flow": no break screens between the blocks; the cell changes silently in the next inter-trial interval.</summary>
        protected override IEnumerator BlockEnd(ProtocolStep s) => null;

        // ---- practice -------------------------------------------------------------------------------------------

        /// <summary>As HW2: practice trials (normal hearing, no anchors), each followed by "Practice again" / "I'm ready".</summary>
        IEnumerator Practice()
        {
            var run = CurrentRun;
            var c = run.Context;
            c.Condition = Cells.Practice;
            c.Block = "1";
            c.Attempt = 1;
            for (int n = 1; ; n++)
            {
                c.Trial = n;
                yield return DoTrial(PracticeSpec(n), n > 1, () => EnsureCell(-1), null);
                int pick = -1;
                yield return Screens.Choice(Loc.Get(Strings.PracticeAsk), Strings.PracticeAgain, Strings.Ready, p => pick = p);
                run.Log.Write(Events.PracticeChoice, pick == 0 ? "again" : "ready");
                if (pick != 0) break;
            }
            c.Block = "";
            c.Attempt = null;
            c.Trial = null;
        }

        TrialSpec PracticeSpec(int n)
        {
            var v = CurrentRun.Values;
            return TrialPlanner.Practice(n, v.GetFloat("practice.visual_deg"), v.GetFloat("practice.audio_start_deg"), v.GetFloat("practice.radius_m"));
        }

        // ---- trials --------------------------------------------------------------------------------------------

        /// <summary>A trial of a cell block (the cell from the session's order, the spec from the cell's list), or the bench's practice trial.</summary>
        protected override IEnumerator RunTrial(ProtocolStep s)
        {
            if (s.Stage.Name == Stages.Practice)
            {
                yield return DoTrial(PracticeSpec(s.Trial + 1), false, () => EnsureCell(-1), null);
                yield break;
            }
            int cell = CellOf(s.Block);
            var list = ListOf(cell);
            var spec = list[Mathf.Clamp(s.Trial, 0, list.Count - 1)];

            // a resume after the app closed during this trial: log the lost try, keep counting (DataFormat "trial_restart")
            int restarts = 0;
            var record = CurrentRun.Record;
            if (record.Custom.TryGetValue(OpenTrialKey, out var text) && OpenTrial.TryParse(text, out var open) && open.Is(s.Block, s.Trial))
            {
                restarts = open.Restarts + 1;
                CurrentRun.Context.Condition = spec.Condition;
                CurrentRun.Log.Write(Events.TrialRestart, TryValues(spec, null, restarts - 1), "reason=app_closed");
            }
            var slot = new OpenSlot { Block = s.Block, Trial = s.Trial, Restarts = restarts };
            yield return DoTrial(spec, s.Trial > 0 && !s.Resumed, () => EnsureCell(cell), slot);
        }

        /// <summary>The experiment trial being run (null in practice): written to the session file at stimulus onset.</summary>
        sealed class OpenSlot
        {
            public int Block, Trial, Restarts;
            public OpenTrial ToOpenTrial() => new OpenTrial { Block = Block, Trial = Trial, Restarts = Restarts };
        }

        /// <summary>
        /// Q46 (as HW2): the rendered RT60 before a block's first trial, with the anchors paused; one <c>rt60_measured</c> row.
        /// </summary>
        IEnumerator MeasureAtBlockStart()
        {
            string condition = _applied ?? "";
            var plan = Rt60PlanFor(Rt60Report.KindBlockStart);
            plan.Azimuths = Rt60Protocol.SingleAzimuth;
            plan.Repeats = 1;
            plan.Conditions.Add(new Rt60Condition { Name = "room", TargetRt60 = _rt60Target });
            plan.Meta.Add(new KeyValuePair<string, string>("block", CurrentRun.Context.Block + " (" + condition + ")"));
            Rt60RunResult result = null;
            var runner = Rt60Runner.Begin(plan, null, r => result = r);
            yield return runner.Wait();
            var stats = result != null && result.Report != null ? result.Report.TmidStats("room") : default;
            _rt60Measured = stats.N > 0 ? (float)stats.Mean : float.NaN;
            CurrentRun.Log.Write(Events.Rt60Measured, new LogValues()
                .Set("rt60_target_s", _rt60Target)
                .Set("rt60_measured_s", stats.N > 0 ? Fmt.F(stats.Mean, 3) : ""),
                "target=" + Fmt.F(_rt60Target) + " tmid_t30=" + (stats.N > 0 ? Fmt.F(stats.Mean, 3) : "") +
                " completed=" + (result != null && result.Completed ? "1" : "0") +
                " report=" + (result != null && result.Completed && result.Report != null ? result.Report.ReportId : "") +
                " calibrated=" + (reverb != null && reverb.Calibrated ? "1" : "0") + " source=block_start");
        }

        /// <summary>A GPL.Acoustics RT60 plan for this chamber: its reverb, the target's mixer group, the anchors paused.</summary>
        Rt60Plan Rt60PlanFor(string kind)
        {
            StopStimuli();
            return new Rt60Plan
            {
                Kind = kind,
                Chamber = Id,
                Reverb = reverb,
                Output = stimulus != null && stimulus.Source != null ? stimulus.Source.outputAudioMixerGroup : null,
                SilenceOthers = silence => { if (anchors != null && anchors.Playing) anchors.Pause(silence); },
            };
        }

        List<TrialSpec> ListOf(int cell)
        {
            if (_lists.TryGetValue(cell, out var list)) return list;
            var v = CurrentRun.Values;
            if (!TrialPlanner.TryParseList(v.GetText("visual_pool_deg"), out var pool) || pool.Count != 2
                || Mathf.Approximately(Mathf.Abs(pool[0]), Mathf.Abs(pool[1])))
            {
                Debug.LogError("[Chamber:" + Id + "] visual_pool_deg '" + v.GetText("visual_pool_deg") + "' is not two different numbers (inner,outer); using 7.5,22.5");
                pool = new List<float> { 7.5f, 22.5f };
            }
            list = TrialPlanner.Plan(cell, CurrentRun.Record.Seed, pool, v.GetFloat("audio_start_deg"), v.GetInt("repeats"), v.GetFloat("radius_m"), PairSetOfRun);
            _lists[cell] = list;
            return list;
        }

        /// <summary>
        /// Design.md "Trial": inter-trial interval, the cell (hearing, anchors; only when it changes), head gate, radio and target on
        /// the circle around the head, pulse loop, the triggers move the sound with no limit until A, then the <c>trial</c> row (the
        /// trial's last action). Keep going after a Stop dialog during the adjustment writes a <c>trial_restart</c> row and runs the
        /// same trial again from the head gate. No helper text in practice or experiment trials.
        /// </summary>
        IEnumerator DoTrial(TrialSpec spec, bool iti, System.Func<bool> applyCell, OpenSlot slot)
        {
            var run = CurrentRun;
            var v = run.Values;
            float itiS = v.GetFloat("iti_s");
            if (iti && itiS > 0f) yield return new WaitForSeconds(itiS);
            bool switched = applyCell();
            if (switched && slot != null && v.GetBool("rt60.measure_at_block_start")) yield return MeasureAtBlockStart();

            int restarts = slot != null ? slot.Restarts : 0;
            while (true)
            {
                int stops = StopDialog.OpenCount;
                HomingResult gate = null;
                yield return HeadGate(r => gate = r);

                _spec = spec;
                Place(HeadOrigin(), Yaw());
                if (slot != null)
                {
                    slot.Restarts = restarts;
                    run.Record.Custom[OpenTrialKey] = slot.ToOpenTrial().Format();
                    run.Save();
                }
                _restartRequested = false;
                _inTrial = true;
                if (pulse != null) pulse.Play(v.GetFloat("stim.on_s"), v.GetFloat("stim.off_s"));
                bool debug = run.IsDebug;
                if (debug && SphereAlwaysVisible && marker != null) marker.Show(true); // Q45: Debug runs only

                var settings = new AdjustSettings
                {
                    RateDegPerS = v.GetFloat("adjust.rate_deg_s"),
                    LimitDeg = double.PositiveInfinity, // Design.md "Trial" 4: free 360°, any number of turns
                    ConfirmIgnoreS = v.GetFloat("confirm_ignore_s"),
                };
                bool done = false;
                AdjustResult result = default;
                var adjust = new TriggerAdjust(run.Actions.Reader, () => !ProtocolScreens.InputOpen);
                var steps = adjust.Run(spec.AudioStartAzDeg, spec.VisualAzDeg, settings,
                                       az => { if (stimulus != null) stimulus.SetAzimuth(az); }, r => { result = r; done = true; });
                while (!done && !_restartRequested && steps.MoveNext()) yield return steps.Current;

                _inTrial = false;
                if (!done)
                {
                    // Keep going after the Stop dialog: log the interrupted try, run the same trial again from the head gate
                    var partial = adjust.Current != null ? adjust.Current.Result() : default;
                    StopStimuli();
                    run.Log.Write(Events.TrialRestart, TryValues(spec, partial, restarts), "reason=stop_dialog " + partial.Note());
                    restarts++;
                    continue;
                }

                Vector3 answer = stimulus != null ? stimulus.transform.position : Vector3.zero;
                StopStimuli();
                Screens.Cue(null);

                var values = TryValues(spec, result, restarts)
                    .Set("signed_error_deg", Fmt.F(Angles.SignedDiff(result.FinalDeg, spec.VisualAzDeg), 3))
                    .Set("restarts", restarts)
                    .Set("radius_m", spec.RadiusM)
                    .Set("rt60_target_s", _rt60Target)
                    .Set("rt60_measured_s", slot != null && !float.IsNaN(_rt60Measured) ? Fmt.F(_rt60Measured, 3) : "")
                    .Set("stim_level_dbfs", stimulus != null ? Fmt.F(stimulus.LevelDbfs, 2) : "")
                    .Set("anchor_level_dbfs", AnchorLevel())
                    .Set("gate_s", gate != null ? Fmt.F(gate.DurationSec, 3) : "")
                    .Set("stop_dialog", StopDialog.OpenCount != stops);
                if (slot != null) run.Record.Custom.Remove(OpenTrialKey); // saved with the cursor after this step
                run.Log.Write(LogEvents.Trial, values, result.Note());

                // Q45, Debug runs only (after the row): the sphere at the answer and the signed difference until A
                if (debug)
                {
                    if (marker != null) marker.ShowAt(answer);
                    double diff = Angles.SignedDiff(result.FinalDeg, spec.VisualAzDeg);
                    Screens.Cue("Sound " + (diff >= 0 ? "+" : "−") + Fmt.F(System.Math.Abs(diff), 1) + "° from the radio   (A or X: continue)");
                    yield return null;
                    while (!Screens.PressedA()) yield return null;
                    Screens.Cue(null);
                    if (marker != null) marker.Show(false);
                }
                yield break;
            }
        }

        /// <summary>Columns shared by <c>trial</c> and <c>trial_restart</c> rows (<paramref name="result"/> null = no answer, app closed).</summary>
        LogValues TryValues(TrialSpec spec, AdjustResult? result, int restarts)
        {
            bool practice = spec.Condition == Cells.Practice;
            int cell = practice ? -1 : Cells.Index(spec.Condition);
            var v = new LogValues()
                .Set("hearing", practice ? Hearing.Normal : Cells.HearingOf(cell))
                .Set("anchors", practice ? Anchors.None : Cells.AnchorsOf(cell))
                .Set("order_row", practice && Order == null ? "" : Fmt.I(OrderRow))
                .Set("pair_set", practice && Order == null ? "" : Fmt.I(PairSetOfRun))
                .Set("trial_index", spec.Index)
                .Set("repeat", spec.Repeat)
                .Set("visual_az_deg", spec.VisualAzDeg)
                .Set("audio_start_az_deg", spec.AudioStartAzDeg)
                .Set("start_side", spec.StartSide)
                .Set("restarts", restarts);
            if (result.HasValue)
            {
                var r = result.Value;
                v.Set("audio_final_az_deg", Fmt.F(Angles.Wrap(r.FinalDeg), 3))
                 .Set("audio_final_unwrapped_deg", Fmt.F(r.FinalDeg, 3))
                 .Set("duration_s", Fmt.F(r.DurationS, 3))
                 .Set("reversals", r.Reversals);
            }
            return v;
        }

        string AnchorLevel() => anchors != null && anchors.Playing ? Fmt.F(anchors.LevelOfDuck, 2) : "";

        /// <summary>Q45: Debug display switch (not a preset key, not logged).</summary>
        public static bool SphereAlwaysVisible { get; set; }

        /// <summary>Radio and target of the current trial on the circle around <paramref name="origin"/> (at the head's height).</summary>
        void Place(Vector3 origin, Quaternion yaw)
        {
            if (picture != null)
            {
                var p = Ring.Position(origin, yaw, _spec.RadiusM, _spec.VisualAzDeg);
                picture.SetPositionAndRotation(p, Ring.FacingOrigin(p, origin));
                picture.gameObject.SetActive(true);
            }
            if (stimulus != null) stimulus.Place(origin, yaw, _spec.RadiusM, _spec.AudioStartAzDeg);
        }

        /// <summary>A recenter during a trial rebuilds the circle around the new head pose (as HW2); the anchors stay fixed in the room.</summary>
        void OnRecenter(HomingResult r)
        {
            if (!_inTrial) return;
            var origin = HeadOrigin();
            var yaw = Yaw();
            if (picture != null)
            {
                var p = Ring.Position(origin, yaw, _spec.RadiusM, _spec.VisualAzDeg);
                picture.SetPositionAndRotation(p, Ring.FacingOrigin(p, origin));
            }
            if (stimulus != null) stimulus.Replace(origin, yaw);
        }

        /// <summary>Hidden while the Stop dialog is open: the fixation dot, the radio, the anchor models and the Debug marker (sounds pause with the freeze).</summary>
        protected override IEnumerable<GameObject> ViewBlockers()
        {
            foreach (var go in base.ViewBlockers()) yield return go;
            if (picture != null) yield return picture.gameObject;
            if (anchors != null) { yield return anchors.DuckModel; yield return anchors.TrumpetModel; }
            if (marker != null) yield return marker.gameObject;
        }

        void StopStimuli()
        {
            _inTrial = false;
            if (pulse != null) pulse.Stop();
            if (picture != null) picture.gameObject.SetActive(false);
            if (marker != null) marker.Show(false);
        }

        OVRCameraRig _rigOutsideRun;
        OVRCameraRig AnyRig => Rig != null ? Rig : _rigOutsideRun != null ? _rigOutsideRun : (_rigOutsideRun = HeadPose.FindRig());
        Vector3 HeadOrigin() => AnyRig != null && AnyRig.centerEyeAnchor != null ? AnyRig.centerEyeAnchor.position : new Vector3(0f, 1.2f, 0f);
        Quaternion Yaw() => AnyRig != null ? Ring.YawOf(AnyRig.transform.forward) : Quaternion.identity;

        // ---- cells: room, hearing, anchors (Design.md "Hearing loss", "Stimuli") ----------------------------

        /// <summary>
        /// Applies a cell (or the practice, <paramref name="cell"/> −1: normal hearing, no anchors) when it differs from the one on:
        /// the room's RT60 (once per run), the hearing filter, the anchors started (placed around the head) or stopped. Writes a
        /// <c>condition</c> row and, when the anchors start or stop, an <c>anchors</c> row. Returns true when it switched.
        /// </summary>
        bool EnsureCell(int cell)
        {
            string name = cell < 0 ? Cells.Practice : Cells.Name(cell);
            if (_applied == name) return false;
            _applied = name;
            var run = CurrentRun;
            var v = run.Values;
            if (!_roomSet)
            {
                _roomSet = true;
                _rt60Measured = float.NaN;
                _rt60Target = v.GetFloat("rt60_s");
                if (reverb != null) reverb.SetRt60(_rt60Target);
                else Debug.LogWarning("[Chamber:" + Id + "] no ReverbTuner: run Magic > HumPerFinal > Step 4.");
            }
            string hearing = cell < 0 ? Hearing.Normal : Cells.HearingOf(cell);
            if (_hearing != null) _hearing.SetProfile(Hearing.Profile(hearing));

            bool wanted = cell >= 0 && Cells.HasAnchors(cell);
            if (anchors != null)
            {
                if (wanted && !anchors.Playing)
                {
                    anchors.Begin(HeadOrigin(), Yaw(), v.GetFloat("anchor.radius_m"), v.GetFloat("anchor.duck_deg"), v.GetFloat("anchor.trumpet_deg"),
                                  v.GetFloat("anchor.gap_after_duck_s"), v.GetFloat("anchor.gap_after_trumpet_s"));
                    run.Log.Write(Events.Anchors, "on " + anchors.Note(Rig != null ? Rig.centerEyeAnchor : null));
                }
                else if (!wanted && anchors.Playing)
                {
                    anchors.End();
                    run.Log.Write(Events.Anchors, "off");
                }
            }
            run.Context.Condition = name;
            run.Log.Write(Events.Condition, new LogValues()
                .Set("hearing", hearing)
                .Set("anchors", wanted ? Anchors.On : Anchors.None)
                .Set("order_row", Order != null ? Fmt.I(OrderRow) : "")
                .Set("pair_set", Order != null ? Fmt.I(PairSetOfRun) : "")
                .Set("rt60_target_s", _rt60Target)
                .Set("anchor_level_dbfs", AnchorLevel()),
                Hearing.Note(hearing) + " anchors=" + (wanted ? Anchors.On : Anchors.None) + " rt60_target_s=" + Fmt.F(_rt60Target) + " " +
                (reverb != null ? reverb.Note() + " calibrated=" + (reverb.Calibrated ? "1" : "0") : "reverb=none") +
                (_hearing != null ? " " + _hearing.Note() : " hl=none"));
            return true;
        }

        // ---- Debug sections (Design.md "Config page"; Storybook Screens/C05 HumPerFinal Config) -------------

        DebugBench _bench;
        bool _measuring;
        const string RoomMode = "room", PresetMode = "preset";
        string _debugRoom;
        string _debugHearing = Hearing.Normal;
        UiButton[] _roomButtons, _measureButtons, _hearingButtons, _anchorButtons, _playButtons, _sphereButtons;
        Rt60RunDialog _rt60Run;

        /// <summary>
        /// Acoustics (Room 0.7 s / Meta preset), RT60 (Measure RT60, its own group), Hearing (5), Anchors (Off / On), Playback (Play
        /// stimulus), Sound sphere, on the Playground values. Nothing here writes a log row. Selected = the state in use.
        /// </summary>
        public override void DebugSections(UiColumn col, DebugBench bench)
        {
            _bench = bench;
            col.Label("Acoustics");
            _roomButtons = col.Buttons("Acoustics",
                ("Room " + Fmt.F(DebugValues.GetFloat("rt60_s"), 1) + " s", UiStyle.Borderless, DebugRoom),
                ("Meta preset", UiStyle.Borderless, DebugPreset));
            col.Label("RT60");
            _measureButtons = col.Buttons("RT60", ("Measure RT60", UiStyle.Borderless, DebugMeasure));
            col.Label("Hearing");
            var hearing = new (string, UiStyle, System.Action)[Hearing.All.Count];
            for (int i = 0; i < hearing.Length; i++)
            {
                string h = Hearing.All[i];
                hearing[i] = (Hearing.Label(h), UiStyle.Borderless, () => DebugHearing(h));
            }
            _hearingButtons = col.Buttons("Hearing", hearing);
            col.Label("Anchors");
            _anchorButtons = col.Buttons("Anchors",
                ("Off", UiStyle.Borderless, () => DebugAnchors(false)),
                ("On", UiStyle.Borderless, () => DebugAnchors(true)));
            col.Label("Playback");
            _playButtons = col.Buttons("Playback", ("Play stimulus", UiStyle.Borderless, DebugToggleStimulus));
            col.Label("Sound sphere");
            _sphereButtons = col.Buttons("Sound sphere",
                ("After confirm", UiStyle.Borderless, () => DebugSphere(false)),
                ("Always visible", UiStyle.Borderless, () => DebugSphere(true)));
            if (_debugRoom == null && reverb != null && CurrentRun == null) { reverb.SetRt60(DebugValues.GetFloat("rt60_s")); _debugRoom = RoomMode; }
            RefreshDebugButtons();
            if (reverb != null && reverb.PanelNote(out _) != null) Status(reverb.PanelNote(out _));
        }

        void RefreshDebugButtons()
        {
            UiKit.Select(_roomButtons, i => _debugRoom == (i == 0 ? RoomMode : PresetMode));
            UiKit.Select(_measureButtons, i => _measuring);
            UiKit.Select(_hearingButtons, i => Hearing.All[i] == _debugHearing);
            UiKit.Select(_anchorButtons, i => (i == 1) == (anchors != null && anchors.Playing));
            UiKit.Select(_playButtons, i => pulse != null && pulse.Playing);
            UiKit.Select(_sphereButtons, i => (i == 1) == SphereAlwaysVisible);
        }

        /// <summary>Leaving Config: sounds off, anchors hidden, normal hearing, the room back to its RT60.</summary>
        public override void DebugLeft()
        {
            if (CurrentRun != null) return;
            StopDebugPlayback();
            if (_rt60Run != null) { _rt60Run.Stop(); _rt60Run = null; _measuring = false; }
            if (reverb != null && !_measuring) { reverb.SetRt60(DebugValues.GetFloat("rt60_s")); _debugRoom = RoomMode; }
            RefreshDebugButtons();
        }

        void StopDebugPlayback()
        {
            StopStimuli();
            if (anchors != null) anchors.End();
            var hl = _hearing != null ? _hearing : OutputHearingLoss.Ensure();
            if (hl != null) hl.SetProfile(HearingProfile.Normal);
            _debugHearing = Hearing.Normal;
            _debugRoom = null;
            RefreshDebugButtons();
        }

        ParameterSet DebugValues => _bench != null ? _bench.Work.Values : CreateDefaults();

        void Status(string text)
        {
            if (_bench != null) _bench.SetStatus(text);
            else Debug.Log("[Chamber:" + Id + "] " + text);
        }

        bool DebugBusy()
        {
            if (CurrentRun != null) { Status("A run is active."); return true; }
            if (_measuring) { Status("RT60 measurement running; Back to the chamber menu cancels it."); return true; }
            return false;
        }

        string DebugSummary() =>
            "Hearing " + Hearing.Label(_debugHearing) + (_debugHearing == Hearing.Normal ? "" : " (" + (Hearing.LabelDb(_debugHearing) == -40 ? "Severe" : "Severe × 2") + ")") +
            ", anchors " + (anchors != null && anchors.Playing ? "on" : "off") + ", " + (reverb != null ? reverb.Describe() : "no reverb tuner");

        void DebugRoom()
        {
            if (DebugBusy()) return;
            if (reverb == null) { Status("No reverb tuner (Step 4)."); return; }
            reverb.SetRt60(DebugValues.GetFloat("rt60_s"));
            _debugRoom = RoomMode;
            RefreshDebugButtons();
            Status(DebugSummary());
        }

        void DebugPreset()
        {
            if (DebugBusy()) return;
            if (reverb == null) { Status("No reverb tuner (Step 4)."); return; }
            reverb.UsePreset();
            _debugRoom = PresetMode;
            RefreshDebugButtons();
            Status(reverb.Describe());
        }

        void DebugHearing(string h)
        {
            if (DebugBusy()) return;
            var hl = _hearing != null ? _hearing : (_hearing = OutputHearingLoss.Ensure());
            if (hl == null) { Status("No AudioListener: hearing loss cannot be applied."); return; }
            hl.SetProfile(Hearing.Profile(h));
            _debugHearing = h;
            RefreshDebugButtons();
            Status(DebugSummary());
        }

        void DebugAnchors(bool on)
        {
            if (DebugBusy()) return;
            if (anchors == null) { Status("No anchors (Step 4)."); return; }
            var v = DebugValues;
            if (on && !anchors.Playing)
                anchors.Begin(HeadOrigin(), Yaw(), v.GetFloat("anchor.radius_m"), v.GetFloat("anchor.duck_deg"), v.GetFloat("anchor.trumpet_deg"),
                              v.GetFloat("anchor.gap_after_duck_s"), v.GetFloat("anchor.gap_after_trumpet_s"));
            else if (!on) anchors.End();
            RefreshDebugButtons();
            Status(DebugSummary());
        }

        void DebugToggleStimulus()
        {
            if (DebugBusy()) return;
            if (pulse != null && pulse.Playing)
            {
                StopStimuli();
                RefreshDebugButtons();
                Status("Stimulus stopped.");
                return;
            }
            var v = DebugValues;
            _spec = new TrialSpec { Condition = "debug", Index = 0, VisualAzDeg = 0f, AudioStartAzDeg = 0f, RadiusM = v.GetFloat("radius_m") };
            Place(HeadOrigin(), Yaw());
            if (pulse != null) pulse.Play(v.GetFloat("stim.on_s"), v.GetFloat("stim.off_s"));
            if (SphereAlwaysVisible && marker != null) marker.Show(true);
            RefreshDebugButtons();
            Status("Stimulus ahead at " + Fmt.F(_spec.RadiusM, 1) + " m with the radio. " + DebugSummary() + ". Press again to stop.");
        }

        void DebugSphere(bool always)
        {
            SphereAlwaysVisible = always;
            if (marker != null && pulse != null && pulse.Playing) marker.Show(always);
            RefreshDebugButtons();
            Status(always ? "Sound sphere: always visible in Debug trials." : "Sound sphere: shown after confirm in Debug trials.");
        }

        /// <summary>Measure RT60 (as HW2): the room at its Playground RT60 with the GPL.Acoustics protocol, in the RT60 run dialog. No log row.</summary>
        void DebugMeasure()
        {
            if (DebugBusy()) return;
            if (reverb == null) { Status("No reverb tuner (Step 4)."); return; }
            if (_bench == null || _bench.Canvas == null) { Status("No Config page canvas."); return; }
            var plan = Rt60PlanFor(Rt60Report.KindMeasurement);
            plan.Conditions.Add(Rt60Condition.Rt60("room", reverb, DebugValues.GetFloat("rt60_s")));
            plan.Restore = RestoreDebugRoom;
            plan.Meta.Add(new KeyValuePair<string, string>("values", "Config page (Playground) RT60 value"));
            _measuring = true;
            RefreshDebugButtons();
            _rt60Run = Rt60RunDialog.Run(_bench.Canvas, plan, r =>
            {
                _rt60Run = null;
                _measuring = false;
                RefreshDebugButtons();
                string n = reverb != null ? reverb.PanelNote(out _) : null;
                Status((r != null && r.Completed ? r.Summary() : "RT60 measurement stopped; no report.") + (n != null ? "\n" + n : ""));
            });
        }

        void RestoreDebugRoom()
        {
            if (reverb == null) return;
            if (_debugRoom == PresetMode) reverb.UsePreset();
            else reverb.SetRt60(DebugValues.GetFloat("rt60_s"));
        }
    }
}
