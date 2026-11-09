using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Acoustics;
using GPL.Core;
using GPL.DebugBench;
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

namespace AcousticChamber.Chambers.HumPerHW2
{
    /// <summary>
    /// HumPerHW2 (C04, Docs/Design.md): HW2's ventriloquist task on the modules. Onboarding (stock stages), a repeatable
    /// practice, three condition blocks of six trials in the session's condition order, thanks.
    /// PB-08d-1: schema, log columns, plan and the condition order; the practice, the trial and the acoustics follow in 08d-3.
    /// </summary>
    public class HumPerHW2Chamber : ProtocolChamber
    {
        public const string Id = "HumPerHW2";
        /// <summary>Session file key of the condition order (<c>custom.condition_order</c>, DataFormat "Files").</summary>
        public const string OrderKey = "condition_order";

        public static class Stages
        {
            public const string Practice = "practice";
            public const string Experiment = "experiment";
            public const string Thanks = OnboardingStages.Thanks;
        }

        [Header("Scene (Magic > HumPerHW2 > Steps 3-4)")]
        [Tooltip("The white-noise source on the circle (pulse train, moved by the triggers).")]
        public PlacedSource stimulus;
        public PulseTrain pulse;
        [Tooltip("The radio picture: root faces the participant; hidden outside trials.")]
        public Transform picture;
        public BackgroundBed background;
        public ReverbTuner reverb;
        public ShoeboxRoom room;
        [Tooltip("Debug: orange sphere on the sound.")]
        public SourceMarker marker;

        /// <summary>The stimulus (Design.md "Object"): Gaussian white noise, 2 s, 20 ms cos² ramps, -26 dBFS RMS, frozen (seed 1); GPL.Stimuli.</summary>
        public static readonly StimulusRecipe StimulusRecipe = GPL.Stimuli.StimulusRecipe.WhiteNoise(2.0, seed: 1, rampS: 0.020);

        static ParameterSchema _schema;
        static LogSchema _log;
        AudioClip _stimulusClip;

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
                ParamDef.Choice(OrderKey, ConditionOrder.LatinSquare, ConditionOrder.Modes, "Condition order", "Procedure")
                        .Describe("latin_square: row (Study ID − 1) mod 3; fixed: quiet, noisy, reverb"),
                ParamDef.Float("iti_s", 0.6f, 0f, 5f, 0.1f, "Inter-trial interval", "s", "Procedure"),
                ParamDef.Float("radius_m", 2f, 0.5f, 5f, 0.1f, "Radius", "m", "Stimuli"),
                ParamDef.Float("practice.radius_m", 1.2f, 0.5f, 5f, 0.1f, "Practice radius", "m", "Stimuli"),
                ParamDef.Float("audio_start_deg", 60f, 0f, 90f, 2.5f, "Sound start", "deg", "Stimuli").Describe("± alternating"),
                ParamDef.Float("practice.audio_start_deg", 30f, -90f, 90f, 2.5f, "Practice sound start", "deg", "Stimuli"),
                ParamDef.Float("practice.visual_deg", 0f, -90f, 90f, 2.5f, "Practice picture", "deg", "Stimuli"),
                ParamDef.Text("visual_pool_deg", "7.5,22.5,37.5", "Picture azimuths", "Stimuli").Describe("right side, mirrored left"),
                ParamDef.Float("stim.on_s", 2f, 0.1f, 5f, 0.1f, "Pulse on", "s", "Stimuli"),
                ParamDef.Float("stim.off_s", 1f, 0f, 5f, 0.1f, "Pulse off", "s", "Stimuli"),
                ParamDef.Float("adjust.rate_deg_s", 10f, 1f, 60f, 1f, "Adjust speed", "deg/s", "Response"),
                ParamDef.Float("adjust.limit_deg", 90f, 30f, 180f, 5f, "Adjust limit", "deg", "Response"),
                ParamDef.Float("confirm_ignore_s", 0.3f, 0f, 2f, 0.1f, "Ignore A for", "s", "Response"),
                ParamDef.Float("rt60.quiet_s", 0.3f, 0.3f, 6f, 0.05f, "RT60 quiet", "s", "Acoustics"),
                ParamDef.Float("rt60.noisy_s", 0.7f, 0.3f, 6f, 0.05f, "RT60 noisy", "s", "Acoustics"),
                ParamDef.Float("rt60.reverb_s", 2.2f, 0.3f, 6f, 0.05f, "RT60 reverb", "s", "Acoustics"),
                ParamDef.Float("bg.radius_m", 2f, 0.5f, 5f, 0.1f, "Background radius", "m", "Acoustics"),
                ParamDef.Text("bg.azimuths_deg", "-90,90,180", "Background azimuths", "Acoustics"),
                ParamDef.Bool("rt60.measure_at_block_start", false, "Measure RT60 at block start", "Acoustics"),
                ParamDef.Choice("resume_from", "next_trial", ResumeNames.All, "Resume from", "Resume").Describe("experiment stage after an interruption"),
                HeadsetVolume.TargetDef(8),
            };
            defs.AddRange(OnboardingStages.SwitchDefs());
            defs.AddRange(HomingParams.GateDefs());
            var schema = new ParameterSchema(defs);
            return schema;
        }

        /// <summary>The preset Default: the schema with the HW2 head gate (Design.md, Q24: ±5°, ±10°, 1 s).</summary>
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
            "visual_az_deg", "audio_start_az_deg", "audio_final_az_deg", "signed_error_deg", "duration_s", "radius_m",
            "rt60_target_s", "rt60_measured_s", "background", "stim_level_dbfs", "bg_level_dbfs", "gate_s", "stop_dialog",
            ChamberMenu.VolumeColumn);

        /// <summary>
        /// Design.md "Flow": onboarding, practice (a plain stage: the participant repeats it as often as wanted), three condition
        /// blocks of six trials, thanks. The condition order is not part of the plan (it comes from the Study ID and is stored in
        /// the session file), so the plan hash is the same for every participant.
        /// </summary>
        public static ProtocolPlan Plan(ParameterSet v) =>
            OnboardingStages.AddTo(new ProtocolPlan(), v)
                .Stage(Stages.Practice, StageFlags.None, ResumeFrom.StageStart)
                .Blocks(Stages.Experiment, Conditions.All.Count, TrialPlanner.TrialsPerCondition, ResumeNames.Parse(v.GetText("resume_from")))
                .Stage(Stages.Thanks, StageFlags.CompletesSession);

        /// <summary>Design.md "Trial": left / right trigger move the sound (A = confirm is standard).</summary>
        public override void DeclareActions(BindingTable table)
        {
            table.DeclareStandard(InputActions.TriggerLeft, "move the sound left");
            table.DeclareStandard(InputActions.TriggerRight, "move the sound right");
        }

        protected override string ChamberId => Id;
        public override ParameterSchema Schema => _schema ?? (_schema = CreateSchema());
        public override LogSchema LogSchema => _log ?? (_log = CreateLogSchema());
        public override ProtocolPlan CreatePlan(ParameterSet values) => Plan(values);

        /// <summary>The session's condition order (from the session file; set on a new session).</summary>
        protected string[] Order { get; private set; }

        public override IEnumerator Run(ChamberRun run)
        {
            Order = EnsureOrder(run.Record, run.Values);
            StartRun();
            yield return base.Run(run);
            EndRun();
        }

        public override IEnumerator PracticeTrial(ChamberRun run)
        {
            Order = null;
            StartRun();
            yield return base.PracticeTrial(run);
            EndRun();
        }

        /// <summary>
        /// The condition order of a session: read from <c>custom.condition_order</c>, or (new session, or a record without it)
        /// computed from the preset's <c>condition_order</c> and the Study ID and stored in the record.
        /// </summary>
        public static string[] EnsureOrder(SessionRecord record, ParameterSet values)
        {
            if (record.Custom.TryGetValue(OrderKey, out var text) && ConditionOrder.TryParse(text, out var stored)) return stored;
            var order = ConditionOrder.For(values.GetText(OrderKey), record.StudyId);
            record.Custom[OrderKey] = ConditionOrder.Format(order);
            return order;
        }

        /// <summary>The condition of an experiment block (0-based) in this session.</summary>
        protected string ConditionOf(int block) => Order != null && block >= 0 && block < Order.Length ? Order[block] : Conditions.Quiet;

        // ---- strings (English / 中文) -----------------------------------------------------------------------

        public static class Strings
        {
            public const string Hint = "humper.hint";
            public const string PracticeAsk = "humper.practice_ask";
            public const string PracticeAgain = "humper.practice_again";
            public const string Ready = "humper.ready";
            public const string ConditionDone = "humper.condition_done";

            public static readonly StringTable Table = new StringTable(Id)
                .Add(Hint, "Left / right trigger: move the sound\nA: confirm", "左／右扳機：移動聲音\nA：確認")
                .Add(PracticeAsk, "Practice again, or start the experiment?", "要再練習一次，還是開始正式實驗？")
                .Add(PracticeAgain, "Practice again", "再練習一次")
                .Add(Ready, "I'm ready", "我準備好了")
                .Add(ConditionDone, "Condition {0} of {1} done.", "第 {0} 個情境（共 {1} 個）完成。");
        }

        /// <summary>Log events of this chamber (DataFormat.md "Events").</summary>
        public static class Events
        {
            public const string Condition = "condition";
            public const string Background = "background";
            public const string PracticeChoice = "practice_choice";
            public const string Rt60Measured = "rt60_measured";
        }

        // ---- run state ---------------------------------------------------------------------------------------

        string _applied;            // condition whose acoustics are on (null = none yet in this run)
        float _rt60Target;
        float _rt60Measured = float.NaN; // T20 of the current block when measured at its start (Q46), else NaN
        readonly Dictionary<string, List<TrialSpec>> _lists = new Dictionary<string, List<TrialSpec>>();
        bool _inTrial;
        TrialSpec _spec;

        void StartRun()
        {
            Loc.Register(Strings.Table);
            StopDebugPlayback(); // a Debug section may have left a test sound or the background on
            _applied = null;
            _rt60Measured = float.NaN;
            _lists.Clear();
            _inTrial = false;
            if (listener != null) listener.Recentered += OnRecenter;
        }

        void EndRun()
        {
            if (listener != null) listener.Recentered -= OnRecenter;
            StopStimuli();
            if (background != null) background.End();
            _applied = null;
        }

        protected override void OnAbort() => EndRun();

        /// <summary>Block rows carry the block's condition; practice rows carry <c>practice</c>.</summary>
        protected override void BeforeRow(ProtocolStep s)
        {
            if (CurrentRun == null || s.Kind == StepKind.StageBegin || s.Kind == StepKind.StageEnd) return;
            if (s.Stage.Name == Stages.Experiment) CurrentRun.Context.Condition = ConditionOf(s.Block);
            else if (s.Stage.Name == Stages.Practice) CurrentRun.Context.Condition = Conditions.Practice;
        }

        protected override IEnumerator RunStage(ProtocolStep s)
        {
            if (s.Stage.Name == Stages.Practice) return Practice();
            if (s.Stage.Name == Stages.Thanks) { StopStimuli(); if (background != null) background.End(); }
            return base.RunStage(s);
        }

        /// <summary>Between condition blocks: "Condition n of 3 done." + Continue; the background keeps playing (Design.md).</summary>
        protected override IEnumerator BlockEnd(ProtocolStep s) => s.LastBlock ? null : ConditionBreak(s);

        IEnumerator ConditionBreak(ProtocolStep s)
        {
            yield return Screens.Message(Loc.Get(Strings.ConditionDone, s.Block + 1, s.Blocks), UiStrings.Continue);
            Screens.Hide();
        }

        // ---- practice (Design.md "Flow" 6, Q20) ---------------------------------------------------------------

        /// <summary>
        /// Practice trials with the hint, each followed by "Practice again" / "I'm ready" (<c>practice_choice</c> row); quiet
        /// acoustics, closer circle, picture straight ahead. Rows: stage <c>practice</c>, block 1, trial 1, 2, ...
        /// </summary>
        IEnumerator Practice()
        {
            var run = CurrentRun;
            var c = run.Context;
            c.Condition = Conditions.Practice;
            c.Block = "1";
            c.Attempt = 1;
            EnsureCondition(Conditions.Practice);
            for (int n = 1; ; n++)
            {
                c.Trial = n;
                yield return DoTrial(PracticeSpec(n), hints: true, iti: n > 1);
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

        /// <summary>
        /// A trial of a condition block (its condition from the session's order, its spec from the HW2 list of that condition),
        /// or the Debug bench's practice trial (stage <c>practice</c>).
        /// </summary>
        protected override IEnumerator RunTrial(ProtocolStep s)
        {
            if (s.Stage.Name == Stages.Practice)
            {
                EnsureCondition(Conditions.Practice);
                yield return DoTrial(PracticeSpec(s.Trial + 1), hints: true, iti: false);
                yield break;
            }
            string condition = ConditionOf(s.Block);
            bool switched = EnsureCondition(condition);
            if (switched && CurrentRun.Values.GetBool("rt60.measure_at_block_start")) yield return MeasureAtBlockStart();
            var list = ListOf(condition);
            yield return DoTrial(list[Mathf.Clamp(s.Trial, 0, list.Count - 1)], hints: false, iti: s.Trial > 0);
        }

        /// <summary>
        /// Q46: the rendered RT60 before the block's first trial (also after a resume): about 5 s of silence with the background
        /// paused, one <c>rt60_measured</c> row; its T20 goes into <c>rt60_measured_s</c> of the block's trial rows.
        /// </summary>
        IEnumerator MeasureAtBlockStart()
        {
            Rt60Result result = default;
            yield return Measure(_rt60Target, r => result = r);
            _rt60Measured = result.Valid ? (float)result.T20 : float.NaN;
            CurrentRun.Log.Write(Events.Rt60Measured, new LogValues()
                .Set("rt60_target_s", _rt60Target)
                .Set("rt60_measured_s", result.Valid ? Fmt.F(result.T20, 3) : ""),
                "target=" + Fmt.F(_rt60Target) + " " + result.Note() + " source=block_start");
        }

        /// <summary>One RT60 measurement through the stimulus source (GPL.Acoustics <see cref="Rt60Meter"/> on the AudioListener, added at run time).</summary>
        IEnumerator Measure(float expectedRt60, System.Action<Rt60Result> done)
        {
            var meter = Meter();
            if (meter == null || stimulus == null)
            {
                done(Rt60Result.Invalid(meter == null ? "no AudioListener" : "no stimulus source"));
                yield break;
            }
            if (pulse != null) pulse.Stop();
            yield return meter.Measure(stimulus.Source, Mathf.Max(0.3f, expectedRt60),
                                       silence => { if (background != null) background.Pause(silence); }, done);
        }

        Rt60Meter Meter()
        {
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener == null) return null;
            var meter = listener.GetComponent<Rt60Meter>();
            return meter != null ? meter : listener.gameObject.AddComponent<Rt60Meter>();
        }

        List<TrialSpec> ListOf(string condition)
        {
            if (_lists.TryGetValue(condition, out var list)) return list;
            var v = CurrentRun.Values;
            if (!TrialPlanner.TryParsePool(v.GetText("visual_pool_deg"), out var pool))
            {
                Debug.LogError("[Chamber:" + Id + "] visual_pool_deg '" + v.GetText("visual_pool_deg") + "' is not a list of numbers; using 7.5,22.5,37.5");
                pool = new List<float> { 7.5f, 22.5f, 37.5f };
            }
            list = TrialPlanner.Plan(condition, CurrentRun.Record.Seed, pool, v.GetFloat("audio_start_deg"), v.GetFloat("radius_m"));
            _lists[condition] = list;
            return list;
        }

        /// <summary>
        /// Design.md "Trial": inter-trial interval, head gate, picture and sound on the circle around the head, pulse train, the
        /// triggers move the sound until A, then the <c>trial</c> row (the trial's last action).
        /// </summary>
        IEnumerator DoTrial(TrialSpec spec, bool hints, bool iti)
        {
            var run = CurrentRun;
            var v = run.Values;
            int stops = StopDialog.OpenCount;
            float itiS = v.GetFloat("iti_s");
            if (iti && itiS > 0f) yield return new WaitForSeconds(itiS);

            HomingResult gate = null;
            yield return HeadGate(r => gate = r);

            _spec = spec;
            Place(HeadOrigin(), Yaw());
            _inTrial = true;
            if (pulse != null) pulse.Play(v.GetFloat("stim.on_s"), v.GetFloat("stim.off_s"));
            if (hints) Screens.Cue(Loc.Get(Strings.Hint));
            bool debug = run.IsDebug;
            if (debug && SphereAlwaysVisible && marker != null) marker.Show(true); // Q45: Debug runs only

            var settings = new AdjustSettings
            {
                RateDegPerS = v.GetFloat("adjust.rate_deg_s"),
                LimitDeg = v.GetFloat("adjust.limit_deg"),
                ConfirmIgnoreS = v.GetFloat("confirm_ignore_s"),
            };
            AdjustResult result = default;
            var adjust = new TriggerAdjust(run.Actions.Reader, () => !ProtocolScreens.InputOpen);
            yield return adjust.Run(spec.AudioStartAzDeg, spec.VisualAzDeg, settings,
                                    az => { if (stimulus != null) stimulus.SetAzimuth(az); }, r => result = r);

            _inTrial = false;
            Vector3 answer = stimulus != null ? stimulus.transform.position : Vector3.zero;
            StopStimuli();
            Screens.Cue(null);

            bool bg = background != null && background.Playing;
            var values = new LogValues()
                .Set("visual_az_deg", spec.VisualAzDeg)
                .Set("audio_start_az_deg", spec.AudioStartAzDeg)
                .Set("audio_final_az_deg", result.FinalDeg)
                .Set("signed_error_deg", result.SignedErrorDeg)
                .Set("duration_s", result.DurationS)
                .Set("radius_m", spec.RadiusM)
                .Set("rt60_target_s", _rt60Target)
                .Set("rt60_measured_s", spec.Condition != Conditions.Practice && !float.IsNaN(_rt60Measured) ? Fmt.F(_rt60Measured, 3) : "")
                .Set("background", bg)
                .Set("stim_level_dbfs", stimulus != null ? Fmt.F(stimulus.LevelDbfs, 2) : "")
                .Set("bg_level_dbfs", bg ? Fmt.F(background.LevelDbfs, 2) : "")
                .Set("gate_s", gate != null ? Fmt.F(gate.DurationSec, 3) : "")
                .Set("stop_dialog", StopDialog.OpenCount != stops);
            run.Log.Write(LogEvents.Trial, values, result.Note());

            // Q45, Debug runs only (after the row, so a real session's trial still ends with its row): the sphere at the answer
            // and the signed difference until A
            if (debug)
            {
                if (marker != null) marker.ShowAt(answer);
                double diff = result.SignedErrorDeg;
                Screens.Cue("Sound " + (diff >= 0 ? "+" : "−") + Fmt.F(System.Math.Abs(diff), 1) + "° from the picture   (A: continue)");
                yield return null; // the confirming press is not the continue press
                while (!Screens.PressedA()) yield return null;
                Screens.Cue(null);
                if (marker != null) marker.Show(false);
            }
        }

        /// <summary>Q45: Debug display switch (not a preset key, not logged): the sphere on the sound during Debug trials, or only after the answer.</summary>
        public static bool SphereAlwaysVisible { get; set; }

        /// <summary>Picture and sound of the current trial on the circle around <paramref name="origin"/> (at the head's height).</summary>
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

        /// <summary>A Meta-button recenter during a trial rebuilds the circle around the new head pose (HW2); azimuths are kept.</summary>
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

        /// <summary>Hidden while the Stop dialog is open: the fixation dot, the picture and the Debug marker (the sounds pause with the freeze).</summary>
        protected override IEnumerable<GameObject> ViewBlockers()
        {
            foreach (var go in base.ViewBlockers()) yield return go;
            if (picture != null) yield return picture.gameObject;
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

        /// <summary>The run's rig, or (Debug sections, outside a run) the scene's.</summary>
        OVRCameraRig AnyRig => Rig != null ? Rig : _rigOutsideRun != null ? _rigOutsideRun : (_rigOutsideRun = HeadPose.FindRig());

        Vector3 HeadOrigin() => AnyRig != null && AnyRig.centerEyeAnchor != null ? AnyRig.centerEyeAnchor.position : new Vector3(0f, 1.2f, 0f);
        Quaternion Yaw() => AnyRig != null ? Ring.YawOf(AnyRig.transform.forward) : Quaternion.identity;

        // ---- Debug sections (Design.md "Debug", PB-08e, Q44-Q47) -----------------------------------------------

        DebugBench _bench;
        bool _measuring;
        const string PresetMode = "preset";
        string _debugRoom;          // quiet / noisy / reverb / preset set from the Debug page; null = not set there
        UiButton[] _roomButtons, _measureButtons, _playButtons, _sphereButtons;

        /// <summary>
        /// Acoustics, playback, RT60 measurement and the sound sphere, on the Playground values. Nothing here writes a log row.
        /// The buttons show their state (UiButton.Selected): the room in use (one of four), the playing sounds, a running
        /// measurement and the sphere mode (one of two).
        /// </summary>
        public override void DebugSections(UiColumn col, DebugBench bench)
        {
            _bench = bench;
            col.Label("Acoustics");
            var conditions = col.Buttons("Conditions",
                ("0.3 s Quiet", UiStyle.Borderless, () => DebugCondition(Conditions.Quiet)),
                ("0.7 s Noisy", UiStyle.Borderless, () => DebugCondition(Conditions.Noisy)),
                ("2.2 s Reverb", UiStyle.Borderless, () => DebugCondition(Conditions.Reverb)));
            var room = col.Buttons("Room",
                ("Meta preset", UiStyle.Borderless, DebugPreset),
                ("Measure RT60", UiStyle.Borderless, DebugMeasure));
            _roomButtons = new[] { conditions[0], conditions[1], conditions[2], room[0] };
            _measureButtons = new[] { room[1] };
            col.Label("Playback");
            _playButtons = col.Buttons("Playback",
                ("Play stimulus", UiStyle.Borderless, DebugToggleStimulus),
                ("Background", UiStyle.Borderless, DebugToggleBackground));
            col.Label("Sound sphere");
            _sphereButtons = col.Buttons("Sound sphere",
                ("After confirm", UiStyle.Borderless, () => DebugSphere(false)),
                ("Always visible", UiStyle.Borderless, () => DebugSphere(true)));
            RefreshDebugButtons();
        }

        /// <summary>Selected = on: the room in use, stimulus / background playing (a paused background is off), measuring, the sphere mode.</summary>
        void RefreshDebugButtons()
        {
            string[] rooms = { Conditions.Quiet, Conditions.Noisy, Conditions.Reverb, PresetMode };
            UiKit.Select(_roomButtons, i => _debugRoom == rooms[i]);
            UiKit.Select(_measureButtons, _ => _measuring);
            UiKit.Select(_playButtons, i => i == 0 ? pulse != null && pulse.Playing : background != null && background.Playing && !background.Paused);
            UiKit.Select(_sphereButtons, i => (i == 1) == SphereAlwaysVisible);
        }

        /// <summary>Q47: back to the chamber menu stops the test sound and the background and returns the room to Quiet.</summary>
        public override void DebugLeft()
        {
            if (CurrentRun != null) return;
            StopDebugPlayback();
            var values = _bench != null ? _bench.Work.Values : CreateDefaults();
            if (reverb != null && !_measuring)
            {
                reverb.SetRt60(values.GetFloat(Conditions.Rt60Key(Conditions.Quiet)));
                _debugRoom = Conditions.Quiet;
            }
            RefreshDebugButtons();
        }

        void StopDebugPlayback()
        {
            StopStimuli();
            if (background != null) background.End();
            _debugRoom = null; // a run sets its own acoustics
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
            if (_measuring) { Status("Measuring RT60, wait about 5 s."); return true; }
            return false;
        }

        void DebugCondition(string condition)
        {
            if (DebugBusy()) return;
            var v = DebugValues;
            float rt = v.GetFloat(Conditions.Rt60Key(condition));
            if (reverb != null) reverb.SetRt60(rt);
            if (background != null)
            {
                if (Conditions.HasBackground(condition))
                {
                    if (!background.Playing) background.Begin(HeadOrigin(), Yaw(), v.GetFloat("bg.radius_m"), ParseAzimuths(v.GetText("bg.azimuths_deg")));
                    else background.Pause(false);
                }
                else background.End();
            }
            _debugRoom = condition;
            RefreshDebugButtons();
            Status(char.ToUpperInvariant(condition[0]) + condition.Substring(1) + ": " + (reverb != null ? reverb.Describe() : "no reverb tuner") +
                   (background != null && background.Playing ? ", background on (" + Fmt.F(background.LevelDbfs, 1) + " dBFS)" : ", no background"));
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
            Status("Stimulus ahead at " + Fmt.F(_spec.RadiusM, 1) + " m with the picture (" + (reverb != null ? reverb.Describe() : "no reverb") + "). Press again to stop.");
        }

        void DebugToggleBackground()
        {
            if (DebugBusy()) return;
            if (background == null) { Status("No background (Step 4)."); return; }
            var v = DebugValues;
            if (!background.Playing)
            {
                background.Begin(HeadOrigin(), Yaw(), v.GetFloat("bg.radius_m"), ParseAzimuths(v.GetText("bg.azimuths_deg")));
                Status("Background playing (" + Fmt.F(background.LevelDbfs, 1) + " dBFS), fixed in the room.");
            }
            else
            {
                background.Pause(!background.Paused);
                Status(background.Paused ? "Background paused." : "Background playing.");
            }
            RefreshDebugButtons();
        }

        void DebugSphere(bool always)
        {
            SphereAlwaysVisible = always;
            if (marker != null && pulse != null && pulse.Playing) marker.Show(always);
            RefreshDebugButtons();
            Status(always ? "Sound sphere: always visible in Debug trials." : "Sound sphere: shown after confirm in Debug trials.");
        }

        void DebugMeasure()
        {
            if (DebugBusy()) return;
            if (reverb == null) { Status("No reverb tuner (Step 4)."); return; }
            float expected = reverb.Custom ? reverb.ExpectedRt60() : reverb.PresetRt60();
            StopStimuli();
            StartCoroutine(DebugMeasureRun(expected));
        }

        IEnumerator DebugMeasureRun(float expected)
        {
            _measuring = true;
            RefreshDebugButtons();
            Status("Measuring RT60 (" + reverb.Describe() + "): about 5 s of silence, then an impulse…");
            Rt60Result result = default;
            yield return Measure(expected, r => result = r);
            _measuring = false;
            RefreshDebugButtons();
            // Q44: status line and console only, no log row
            Status(result.Valid
                ? "RT60 T20 " + Fmt.F(result.T20, 2) + " s, T30 " + (double.IsNaN(result.T30) ? "n/a" : Fmt.F(result.T30, 2) + " s") +
                  ", depth " + Fmt.F(result.DepthDb, 0) + " dB (expected " + Fmt.F(expected, 2) + " s)"
                : "RT60 measurement failed: " + result.Reason);
        }

        // ---- acoustics per condition (Design.md "Conditions") -----------------------------------------------

        /// <summary>
        /// Switches the acoustics to <paramref name="condition"/> when they differ: the RT60 target (practice = quiet), the
        /// background on (placed around the head, then fixed in the room) or off. Writes a <c>condition</c> row and, when the
        /// background starts or stops, a <c>background</c> row. Called before a block's first trial and again after a resume.
        /// </summary>
        bool EnsureCondition(string condition)
        {
            if (_applied == condition) return false;
            _applied = condition;
            _rt60Measured = float.NaN;
            var run = CurrentRun;
            var v = run.Values;
            _rt60Target = v.GetFloat(Conditions.Rt60Key(condition));
            if (reverb != null) reverb.SetRt60(_rt60Target);
            else Debug.LogWarning("[Chamber:" + Id + "] no ReverbTuner: run Magic > HumPerHW2 > Step 4.");

            bool wanted = Conditions.HasBackground(condition);
            if (background != null)
            {
                if (wanted && !background.Playing)
                {
                    background.Begin(HeadOrigin(), Yaw(), v.GetFloat("bg.radius_m"), ParseAzimuths(v.GetText("bg.azimuths_deg")));
                    run.Log.Write(Events.Background, "on " + background.Note(Rig != null ? Rig.centerEyeAnchor : null));
                }
                else if (!wanted && background.Playing)
                {
                    background.End();
                    run.Log.Write(Events.Background, "off");
                }
            }
            bool bg = background != null && background.Playing;
            run.Log.Write(Events.Condition, new LogValues()
                .Set("rt60_target_s", _rt60Target)
                .Set("background", bg)
                .Set("bg_level_dbfs", bg ? Fmt.F(background.LevelDbfs, 2) : ""),
                (reverb != null ? reverb.Note() : "reverb=none") + " " + (reverb != null ? reverb.RoomModelingStatus : ""));
            return true;
        }

        static float[] ParseAzimuths(string text)
        {
            if (TrialPlanner.TryParsePool(text, out var list)) return list.ToArray();
            Debug.LogError("[Chamber:" + Id + "] bg.azimuths_deg '" + text + "' is not a list of numbers; using -90,90,180");
            return new[] { -90f, 90f, 180f };
        }
    }
}
