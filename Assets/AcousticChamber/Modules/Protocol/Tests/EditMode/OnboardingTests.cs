using System.Collections.Generic;
using System.Linq;
using GPL.Params;
using NUnit.Framework;

namespace GPL.Protocol.Tests
{
    public class OnboardingTests
    {
        static string[] Names(ProtocolPlan p) => p.Stages.Select(s => s.Name).ToArray();

        [Test]
        public void AddTo_AllOn_FixedOrderAndFlags()
        {
            var v = new ParameterSchema(OnboardingStages.SwitchDefs()).Defaults();
            var plan = OnboardingStages.AddTo(new ProtocolPlan(), v);
            CollectionAssert.AreEqual(OnboardingStages.All.ToArray(), Names(plan));
            Assert.IsTrue(plan.Stages[0].Has(StageFlags.RepeatOnResume));
            Assert.IsTrue(plan.Stages[1].Has(StageFlags.Once));
            Assert.IsTrue(plan.Stages[2].Has(StageFlags.RepeatOnResume));
            Assert.AreEqual(StageFlags.None, plan.Stages[3].Flags);
            Assert.IsTrue(plan.Stages[4].Has(StageFlags.RepeatOnResume));
        }

        [Test]
        public void AddTo_SwitchedOffStagesLeftOut()
        {
            var v = new ParameterSchema(OnboardingStages.SwitchDefs()).Defaults();
            v.Set(OnboardingStages.SwitchKey(OnboardingStages.Language), false);
            v.Set(OnboardingStages.SwitchKey(OnboardingStages.Instructions), false);
            CollectionAssert.AreEqual(new[] { "handover", "recenter", "ready" }, Names(OnboardingStages.AddTo(new ProtocolPlan(), v)));
        }

        [Test]
        public void AddTo_SchemaWithoutSwitches_AllOn()
        {
            var v = new ParameterSchema(new List<ParamDef> { ParamDef.Int("blocks", 2, 1, 10, "Blocks", "", "Procedure") }).Defaults();
            Assert.AreEqual(5, OnboardingStages.AddTo(new ProtocolPlan(), v).Stages.Count);
            Assert.IsTrue(OnboardingStages.On(null, OnboardingStages.Ready));
        }

        [Test]
        public void SwitchKeys_AndIsOnboarding()
        {
            CollectionAssert.AreEqual(OnboardingStages.All.Select(OnboardingStages.SwitchKey).ToArray(),
                                      OnboardingStages.SwitchDefs().Select(d => d.Key).ToArray());
            Assert.AreEqual("onboarding.ready", OnboardingStages.SwitchKey("ready"));
            Assert.IsTrue(OnboardingStages.IsOnboarding("recenter"));
            Assert.IsFalse(OnboardingStages.IsOnboarding("thanks"));
        }
    }
}
