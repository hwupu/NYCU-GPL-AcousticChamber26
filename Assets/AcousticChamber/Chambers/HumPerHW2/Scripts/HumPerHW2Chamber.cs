using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Homing;
using GPL.Logging;
using GPL.Params;
using GPL.Protocol;
using GPL.Session;
using UnityEngine;

namespace AcousticChamber.Chambers.HumPerHW2
{
    /// <summary>
    /// HumPerHW2 (C01, Docs/Design.md): HW2's ventriloquist task on the modules. Onboarding (stock stages), a repeatable
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

        static ParameterSchema _schema;
        static LogSchema _log;

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

        protected override string ChamberId => Id;
        public override ParameterSchema Schema => _schema ?? (_schema = CreateSchema());
        public override LogSchema LogSchema => _log ?? (_log = CreateLogSchema());
        public override ProtocolPlan CreatePlan(ParameterSet values) => Plan(values);

        /// <summary>The session's condition order (from the session file; set on a new session).</summary>
        protected string[] Order { get; private set; }

        public override IEnumerator Run(ChamberRun run)
        {
            Order = EnsureOrder(run.Record, run.Values);
            return base.Run(run);
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

        /// <summary>PB-08d-3 builds the trial; until then a trial logs a warning and ends at once.</summary>
        protected override IEnumerator RunTrial(ProtocolStep s)
        {
            Debug.LogWarning("[Chamber:" + Id + "] trial not built yet (PB-08d-3): " + s.Stage.Name + " block " + (s.Block + 1) + " trial " + (s.Trial + 1));
            yield break;
        }
    }
}
