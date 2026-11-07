using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Params;
using GPL.Protocol;
using GPL.Session;
using NUnit.Framework;

namespace AcousticChamber.Chambers.Template.Tests
{
    /// <summary>PB-21: the refactor onto ProtocolChamber keeps the Template's parameters and plan (so old sessions still resume).</summary>
    public class TemplateTests
    {
        /// <summary>Config/default.params before PB-21, in schema order (keys, defaults and order give the params_hash).</summary>
        static readonly string[] DefaultsBefore =
        {
            "practice_trials=2", "blocks=2", "trials_per_block=4", "response_timeout_s=5", "iti_s=1", "resume_from=block_start",
            "volume.target_step=8", "onboarding.handover=true", "onboarding.language=true", "onboarding.recenter=true",
            "onboarding.instructions=true", "onboarding.ready=true", "gate.yaw_tol_deg=10", "gate.pitch_tol_deg=15", "gate.hold_s=0.5",
            "gate.hint_after_s=4",
        };

        [Test]
        public void Schema_DefaultsAndOrderUnchanged()
        {
            var got = new List<string>();
            foreach (var p in TemplateChamber.CreateSchema().Defaults().Pairs()) got.Add(p.Key + "=" + p.Value);
            CollectionAssert.AreEqual(DefaultsBefore, got);
        }

        /// <summary>The plan as TemplateChamber built it before PB-21 (verbatim).</summary>
        static ProtocolPlan PlanBefore(ParameterSet v)
        {
            bool On(string stage) => v.GetBool("onboarding." + stage);
            return new ProtocolPlan()
                .StageIf(On("handover"), "handover", StageFlags.RepeatOnResume)
                .StageIf(On("language"), "language", StageFlags.Once)
                .StageIf(On("recenter"), "recenter", StageFlags.RepeatOnResume)
                .StageIf(On("instructions"), "instructions")
                .StageIf(On("ready"), "ready", StageFlags.RepeatOnResume)
                .Blocks("practice", 1, v.GetInt("practice_trials"), ResumeFrom.StageStart)
                .Blocks("experiment", v.GetInt("blocks"), v.GetInt("trials_per_block"), ResumeNames.Parse(v.GetText("resume_from")))
                .Stage("thanks", StageFlags.CompletesSession);
        }

        [Test]
        public void Plan_SameAsBefore_ForEverySwitchCombination()
        {
            var stages = OnboardingStages.All;
            for (int mask = 0; mask < 1 << stages.Count; mask++)
            {
                var v = TemplateChamber.CreateSchema().Defaults();
                for (int i = 0; i < stages.Count; i++) v.Set(OnboardingStages.SwitchKey(stages[i]), (mask & (1 << i)) != 0);
                if (mask == 5) { v.Set("practice_trials", 0); v.Set("resume_from", "next_trial"); }
                Assert.AreEqual(PlanBefore(v).Describe(), TemplateChamber.Plan(v).Describe(), "mask " + mask);
                Assert.AreEqual(PlanBefore(v).Hash(), TemplateChamber.Plan(v).Hash(), "mask " + mask);
            }
        }
    }
}
