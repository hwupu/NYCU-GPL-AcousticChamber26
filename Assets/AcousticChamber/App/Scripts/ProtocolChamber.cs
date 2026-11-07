using System;
using System.Collections;
using GPL.Homing;
using GPL.Params;
using GPL.Protocol;
using GPL.Session;
using GPL.UI;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Base of a chamber that runs a <see cref="ProtocolPlan"/> (PB-21): owns the run loop (walker, structure rows), the stock
    /// onboarding stages (<see cref="Onboarding"/>), the stock thanks screen, the "block done" break, the head gate before a
    /// trial, Meta-button recenters during the run (<c>recenter</c> rows), the plan hash check on resume, the unfinished-session
    /// line and the cleanup after Stop. A chamber supplies its schema, log columns, <see cref="CreatePlan"/> and
    /// <see cref="RunTrial"/>, and optionally its own stages (<see cref="RunStage"/>).
    /// </summary>
    public abstract class ProtocolChamber : ChamberBehaviour
    {
        [Tooltip("The fixation dot (the chamber's homing setup step). Empty = the first in the scene.")]
        public FixationCue cue;
        [Tooltip("Logs recenters by the Meta button during a run. Empty = the first in the scene.")]
        public RecenterListener listener;
        [Tooltip("Instruction slides after the recenter slide. Empty or no slides = the stage shows nothing.")]
        public SlideSet instructions;
        [Tooltip("The recenter slide's title and illustration (2000 x 936 px). No image = the stock text on the gray area.")]
        public SlideSet.Slide recenterSlide = new SlideSet.Slide { id = "recenter", titleEnglish = "", titleChinese = "" };

        /// <summary>The chamber id for console lines and messages (<c>[Chamber:&lt;Id&gt;]</c>).</summary>
        protected abstract string ChamberId { get; }

        /// <summary>The stage with the counted blocks, named in the unfinished-session line ("Block 2 of 4 · 3 trials done").</summary>
        protected virtual string MainStage => "experiment";

        /// <summary>The stage list from the session's parameter values (the same values give the same plan and hash).</summary>
        public abstract ProtocolPlan CreatePlan(ParameterSet values);

        /// <summary>One trial; its trial row must be its last action (a kill then never leaves a counted trial unlogged).</summary>
        protected abstract IEnumerator RunTrial(ProtocolStep step);

        /// <summary>
        /// A stage of the chamber's own (not onboarding). The default runs the stock thanks screen for
        /// <see cref="OnboardingStages.Thanks"/> and nothing for other stages; override and call the base for the rest.
        /// </summary>
        protected virtual IEnumerator RunStage(ProtocolStep step) =>
            step.Stage.Name == OnboardingStages.Thanks ? Thanks() : null;

        /// <summary>After a block: the default shows "Block n of N done." + Continue between the blocks of a stage.</summary>
        protected virtual IEnumerator BlockEnd(ProtocolStep step) => step.LastBlock ? null : BlockBreak(step);

        /// <summary>Stops the chamber's own things after the Stop dialog ended the run (sounds, stimuli). Called before the base cleanup.</summary>
        protected virtual void OnAbort() { }

        // ---- run state, for the chamber's code ----------------------------------------------------------------

        protected ChamberRun CurrentRun { get; private set; }
        protected ProtocolScreens Screens { get; private set; }
        protected Onboarding Onboarding { get; private set; }
        protected OVRCameraRig Rig { get; private set; }
        /// <summary>The head gate settings of the session (preset keys <c>gate.*</c>).</summary>
        protected GateSettings Gate { get; private set; }

        HeadGateMethod _gate;

        public override string Describe(SessionRecord r)
        {
            var plan = CreatePlan(SessionLauncher.Snapshot(r, Schema));
            var c = r.Cursor;
            if (c.Stage >= plan.Stages.Count) return "At the end";
            var stage = plan.Stages[c.Stage];
            if (stage.Name == MainStage && stage.HasBlocks)
                return "Block " + (c.Block + 1) + " of " + stage.Blocks + " · " + c.TrialsDone + (c.TrialsDone == 1 ? " trial" : " trials") + " done";
            return char.ToUpperInvariant(stage.Name[0]) + stage.Name.Substring(1);
        }

        public override string ResumeProblem(SessionRecord r, ParameterSet snapshot)
        {
            string hash = CreatePlan(snapshot).Hash();
            return string.IsNullOrEmpty(r.PlanHash) || r.PlanHash == hash ? null
                : "The " + ChamberId + "'s protocol changed since this session started (plan " + r.PlanHash + ", now " + hash + "). Start new instead.";
        }

        public override IEnumerator Run(ChamberRun run)
        {
            CurrentRun = run;
            Screens = new ProtocolScreens(run.Canvas, run.Actions);
            Rig = HeadPose.FindRig();
            if (cue == null) cue = FindAnyObjectByType<FixationCue>();
            if (listener == null) listener = FindAnyObjectByType<RecenterListener>();
            if (cue == null) Debug.LogWarning("[Chamber:" + ChamberId + "] no FixationCue: run the chamber's homing setup step.");
            if (listener != null) listener.Recentered += OnUserRecenter;

            var rec = run.Record;
            var plan = CreatePlan(run.Values);
            if (!run.IsResume) rec.PlanHash = plan.Hash();
            if (Loc.TryParseCode(rec.Language, out var lang)) Loc.Set(lang);
            else Loc.Set(UiLanguage.English);
            Gate = HomingParams.ReadGate(run.Values);
            Onboarding = new Onboarding(Screens, rec, run.Log, run.Values)
            {
                Save = run.Save, Rig = Rig, Cue = cue, Listener = listener, Instructions = instructions, RecenterSlide = recenterSlide,
                ReadVolume = HeadsetVolume.ReadOrUnknown,
                VolumeTarget = run.Values.Schema.Contains(HeadsetVolume.TargetKey) ? run.Values.GetInt(HeadsetVolume.TargetKey) : -1,
            };

            var walker = new ProtocolWalker(plan, rec, run.IsResume, _ => run.Save());
            while (walker.Next())
            {
                var s = walker.Current;
                ProtocolLog.Write(run.Log, s);
                IEnumerator body = null;
                switch (s.Kind)
                {
                    case StepKind.StageBegin: body = Onboarding.TryRun(s) ?? RunStage(s); break;
                    case StepKind.Trial: body = RunTrial(s); break;
                    case StepKind.BlockEnd: body = BlockEnd(s); break;
                }
                if (body != null) yield return body;
            }
            Cleanup();
        }

        /// <summary>
        /// The head gate before a trial (HW2): the fixation dot, "Look at the dot" after the hint time, the result to
        /// <paramref name="done"/> and a <c>homing</c> row. Without a rig nothing happens and the result is null.
        /// </summary>
        protected IEnumerator HeadGate(Action<HomingResult> done)
        {
            if (Rig == null) { done?.Invoke(null); yield break; }
            HomingResult result = null;
            _gate = new HeadGateMethod(Rig, cue, Gate);
            _gate.HintChanged += on => Screens.Cue(on ? Loc.Get(ProtocolStrings.LookAtDot) : null);
            yield return _gate.Run(r => result = r);
            _gate = null;
            CurrentRun.Log.Write(GPL.Logging.LogEvents.Homing, result.Note());
            done?.Invoke(result);
        }

        IEnumerator BlockBreak(ProtocolStep s)
        {
            yield return Screens.Message(Loc.Get(ProtocolStrings.BlockDone, s.Block + 1, s.Blocks), UiStrings.Continue);
            Screens.Hide();
        }

        IEnumerator Thanks()
        {
            yield return Screens.Message(Loc.Get(ProtocolStrings.Thanks), ProtocolStrings.Finish);
            Screens.Hide();
        }

        void OnUserRecenter(HomingResult r)
        {
            if (CurrentRun != null && !CurrentRun.Stopped && (Onboarding == null || !Onboarding.InRecenterStage))
                CurrentRun.Log.Write(GPL.Logging.LogEvents.Recenter, r.Note());
        }

        public override void Abort(ChamberRun run)
        {
            if (_gate != null) _gate.Cancel();
            if (cue != null) cue.Hide();
            OnAbort();
            Cleanup();
        }

        void Cleanup()
        {
            if (listener != null) listener.Recentered -= OnUserRecenter;
            if (Screens != null) Screens.Clear();
            Screens = null;
            Onboarding = null;
            _gate = null;
            CurrentRun = null;
        }
    }
}
