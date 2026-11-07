using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Core;
using GPL.DebugBench;
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
    /// The Template chamber (C00, Docs/Design.md): onboarding (the stock stages of <see cref="OnboardingStages"/>, each with a
    /// preset switch), practice, blocks of trials (head gate on the fixation dot, then "Press A" with the reaction time),
    /// thanks. The run loop, onboarding, resume check and cleanup come from <see cref="ProtocolChamber"/>; this class holds
    /// the schema, the log columns, the plan and the trial (also the bench's Practice trial), and an example Debug section.
    /// </summary>
    public class TemplateChamber : ProtocolChamber
    {
        public const string Id = "Template";

        public static class Stages
        {
            public const string Handover = OnboardingStages.Handover;
            public const string Language = OnboardingStages.Language;
            public const string Recenter = OnboardingStages.Recenter;
            public const string Instructions = OnboardingStages.Instructions;
            public const string Ready = OnboardingStages.Ready;
            public const string Practice = "practice";
            public const string Experiment = "experiment";
            public const string Thanks = OnboardingStages.Thanks;
        }

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
                HeadsetVolume.TargetDef(8),
            };
            defs.AddRange(OnboardingStages.SwitchDefs());
            defs.AddRange(HomingParams.GateDefs());
            return new ParameterSchema(defs);
        }

        public static LogSchema CreateLogSchema() =>
            new LogSchema("rt_s", "timed_out", "gate_s", "gate_breaks", "stop_dialog", ChamberMenu.VolumeColumn);

        /// <summary>The stage list (Design.md "Flow"), from the session's parameter values.</summary>
        public static ProtocolPlan Plan(ParameterSet v) =>
            OnboardingStages.AddTo(new ProtocolPlan(), v)
                .Blocks(Stages.Practice, 1, v.GetInt("practice_trials"), ResumeFrom.StageStart)
                .Blocks(Stages.Experiment, v.GetInt("blocks"), v.GetInt("trials_per_block"), ResumeNames.Parse(v.GetText("resume_from")))
                .Stage(Stages.Thanks, StageFlags.CompletesSession);

        protected override string ChamberId => Id;
        public override ParameterSchema Schema => _schema ?? (_schema = CreateSchema());
        public override LogSchema LogSchema => _log ?? (_log = CreateLogSchema());
        public override ProtocolPlan CreatePlan(ParameterSet values) => Plan(values);

        /// <summary>
        /// One trial (Design.md "Trial"): the inter-trial interval (before every trial but the first of a block, so a trial's
        /// row is its last action and a kill never leaves a counted trial unlogged), the head gate, then "Press A".
        /// </summary>
        protected override IEnumerator RunTrial(ProtocolStep s)
        {
            var run = CurrentRun;
            int stops = StopDialog.OpenCount;
            float iti = run.Values.GetFloat("iti_s");
            if (s.Trial > 0 && iti > 0f) yield return new WaitForSeconds(iti);

            HomingResult gate = null;
            yield return HeadGate(r => gate = r);

            // go cue: the dot is gone; the reaction time runs from this frame to the frame A is seen
            Screens.Cue(Loc.Get(ProtocolStrings.PressA), 0f);
            double t0 = Time.realtimeSinceStartupAsDouble;
            float timeout = run.Values.GetFloat("response_timeout_s"), waited = 0f;
            double rt = -1;
            while (true)
            {
                yield return null;
                if (Screens.PressedA())
                {
                    rt = Time.realtimeSinceStartupAsDouble - t0;
                    break;
                }
                waited += Time.deltaTime; // scaled: the Stop dialog freezes the timeout
                if (waited >= timeout) break;
            }
            Screens.Cue(null);

            var v = new LogValues()
                .Set("rt_s", rt >= 0 ? Fmt.F(rt, 3) : "")
                .Set("timed_out", rt < 0)
                .Set("gate_s", gate != null ? Fmt.F(gate.DurationSec, 3) : "")
                .Set("gate_breaks", gate != null ? Fmt.I(gate.Breaks) : "")
                .Set("stop_dialog", StopDialog.OpenCount != stops);
            run.Log.Write(LogEvents.Trial, v, "");
        }

        /// <summary>
        /// Example of a chamber's own Debug section (PB-21b): the fixation dot in front of the headset outside a run, to check
        /// its distance and height. No session, no log row.
        /// </summary>
        public override void DebugSections(Column col, DebugBench bench)
        {
            col.Label("Fixation dot");
            col.Buttons("Fixation dot",
                ("Show", UiStyle.Borderless, () =>
                {
                    var rig = HeadPose.FindRig();
                    var dot = cue != null ? cue : FindAnyObjectByType<FixationCue>();
                    if (rig == null || dot == null) { bench.SetStatus("No camera rig or fixation dot in the scene (Magic > Template > Step 2)."); return; }
                    dot.Show(rig);
                    bench.SetStatus("Fixation dot shown " + Fmt.F(dot.distanceM, 1) + " m ahead at head height.");
                }),
                ("Hide", UiStyle.Borderless, () =>
                {
                    var dot = cue != null ? cue : FindAnyObjectByType<FixationCue>();
                    if (dot != null) dot.Hide();
                    bench.SetStatus("Fixation dot hidden.");
                }));
        }
    }
}
