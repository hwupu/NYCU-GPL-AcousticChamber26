using System.Collections.Generic;
using GPL.Params;

namespace GPL.Protocol
{
    /// <summary>
    /// The stock onboarding stages (Architecture section 8, PB-20b): names, the preset switch of each stage
    /// (<c>onboarding.&lt;stage&gt;</c>, group Onboarding) and the plan entries with their resume flags. Pure; the screens
    /// are run by <c>Onboarding</c> (Runtime). A chamber adds <see cref="SwitchDefs"/> to its schema and
    /// <see cref="AddTo"/> to its plan, before its practice and trials.
    /// </summary>
    public static class OnboardingStages
    {
        public const string Handover = "handover";
        public const string Language = "language";
        public const string Recenter = "recenter";
        public const string Instructions = "instructions";
        public const string Ready = "ready";
        /// <summary>Not an onboarding stage: the stock closing stage (<c>CompletesSession</c>); its screen is run by the chamber base.</summary>
        public const string Thanks = "thanks";

        /// <summary>The onboarding stages in their fixed order.</summary>
        public static readonly IReadOnlyList<string> All = new[] { Handover, Language, Recenter, Instructions, Ready };

        /// <summary>CSV column of the headset volume step (session start / resume rows and the <c>volume_check</c> row).</summary>
        public const string VolumeColumn = "volume_step";

        /// <summary>Preset key of a stage's switch.</summary>
        public static string SwitchKey(string stage) => "onboarding." + stage;

        public static bool IsOnboarding(string stage)
        {
            foreach (var s in All) if (s == stage) return true;
            return false;
        }

        /// <summary>True when the stage is switched on; a schema without the switch counts as on.</summary>
        public static bool On(ParameterSet values, string stage)
        {
            string key = SwitchKey(stage);
            return values == null || !values.Schema.Contains(key) || values.GetBool(key);
        }

        /// <summary>The five switches (all on by default), in stage order.</summary>
        public static IEnumerable<ParamDef> SwitchDefs()
        {
            yield return Switch(Handover, "Handover", "experimenter: set the IPD, hand over");
            yield return Switch(Language, "Language", "participant picks English / 中文");
            yield return Switch(Recenter, "Recenter (Meta button)", "participant holds the Meta button");
            yield return Switch(Instructions, "Instruction slides", "shown only if the slide set has slides");
            yield return Switch(Ready, "Final check at the dot", "head gate + volume_check row before the practice");
        }

        static ParamDef Switch(string stage, string label, string description) =>
            ParamDef.Bool(SwitchKey(stage), true, label, "Onboarding").Describe(description);

        /// <summary>
        /// Adds the switched-on stages: handover, recenter and ready run again after a resume, the language is asked once,
        /// the instruction slides resume at the stored slide.
        /// </summary>
        public static ProtocolPlan AddTo(ProtocolPlan plan, ParameterSet values) =>
            plan.StageIf(On(values, Handover), Handover, StageFlags.RepeatOnResume)
                .StageIf(On(values, Language), Language, StageFlags.Once)
                .StageIf(On(values, Recenter), Recenter, StageFlags.RepeatOnResume)
                .StageIf(On(values, Instructions), Instructions)
                .StageIf(On(values, Ready), Ready, StageFlags.RepeatOnResume);
    }
}
