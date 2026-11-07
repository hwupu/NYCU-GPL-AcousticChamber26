using System.Collections.Generic;
using System.Linq;
using GPL.Protocol;
using GPL.Session;
using NUnit.Framework;

namespace AcousticChamber.Chambers.HumPerHW2.Tests
{
    public class HumPerHW2Tests
    {
        static readonly float[] Pool = { 7.5f, 22.5f, 37.5f };

        // ---- condition order (Design.md "Order", Q29) --------------------------------------------------------

        [Test]
        public void LatinSquare_RowsByStudyId()
        {
            string Row(string id) => string.Join(",", ConditionOrder.For(ConditionOrder.LatinSquare, id));
            Assert.AreEqual("quiet,noisy,reverb", Row("S0001"));
            Assert.AreEqual("noisy,reverb,quiet", Row("S0002"));
            Assert.AreEqual("reverb,quiet,noisy", Row("S0003"));
            Assert.AreEqual("quiet,noisy,reverb", Row("S0004"));
            Assert.AreEqual("reverb,quiet,noisy", Row("S0012"));
            Assert.AreEqual("quiet,noisy,reverb", Row(""));       // Debug run without a Study ID
            Assert.AreEqual("quiet,noisy,reverb", string.Join(",", ConditionOrder.For(ConditionOrder.Fixed, "S0002")));
        }

        [Test]
        public void LatinSquare_EachConditionOnceInEachPosition()
        {
            for (int pos = 0; pos < 3; pos++)
                CollectionAssert.AreEquivalent(Conditions.All, ConditionOrder.Rows.Select(r => r[pos]).ToList(), "position " + pos);
            foreach (var row in ConditionOrder.Rows) CollectionAssert.AreEquivalent(Conditions.All, row.ToList());
        }

        [Test]
        public void Order_FormatAndParse()
        {
            Assert.IsTrue(ConditionOrder.TryParse("noisy,reverb,quiet", out var o));
            Assert.AreEqual("noisy,reverb,quiet", ConditionOrder.Format(o));
            Assert.IsFalse(ConditionOrder.TryParse("noisy,noisy,quiet", out _));
            Assert.IsFalse(ConditionOrder.TryParse("quiet,noisy", out _));
            Assert.IsFalse(ConditionOrder.TryParse("", out _));
            Assert.IsFalse(ConditionOrder.TryParse("quiet,noisy,practice", out _));
        }

        [Test]
        public void EnsureOrder_StoresOnceAndKeepsIt()
        {
            var values = HumPerHW2Chamber.CreateDefaults();
            var record = new SessionRecord { StudyId = "S0002" };
            CollectionAssert.AreEqual(new[] { "noisy", "reverb", "quiet" }, HumPerHW2Chamber.EnsureOrder(record, values));
            Assert.AreEqual("noisy,reverb,quiet", record.Custom[HumPerHW2Chamber.OrderKey]);
            values.Set(HumPerHW2Chamber.OrderKey, ConditionOrder.Fixed);              // a later preset change does not move a session
            CollectionAssert.AreEqual(new[] { "noisy", "reverb", "quiet" }, HumPerHW2Chamber.EnsureOrder(record, values));
        }

        // ---- trial lists (HW2 TrialPlanner) ------------------------------------------------------------------

        [Test]
        public void Trials_FollowTheHW2Sequence([Values("quiet", "noisy", "reverb")] string condition)
        {
            var t = TrialPlanner.Plan(condition, 12345, Pool, 60f, 2f);
            Assert.AreEqual(6, t.Count);
            CollectionAssert.AreEqual(new[] { 60f, -60f, 60f, -60f, 60f, -60f }, t.Select(x => x.AudioStartAzDeg).ToArray());
            CollectionAssert.AreEqual(new[] { false, false, true, true, false, true }, t.Select(x => x.VisualAzDeg > 0).ToArray());
            CollectionAssert.AreEquivalent(new[] { -7.5f, -22.5f, -37.5f }, t.Where(x => x.VisualAzDeg < 0).Select(x => x.VisualAzDeg).ToArray());
            CollectionAssert.AreEquivalent(new[] { 7.5f, 22.5f, 37.5f }, t.Where(x => x.VisualAzDeg > 0).Select(x => x.VisualAzDeg).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, t.Select(x => x.Index).ToArray());
            Assert.IsTrue(t.All(x => x.Condition == condition && x.RadiusM == 2f));
        }

        [Test]
        public void Trials_AreReproducibleFromTheSeed()
        {
            string Visuals(int seed, string c) => string.Join(",", TrialPlanner.Plan(c, seed, Pool, 60f, 2f).Select(x => x.VisualAzDeg));
            Assert.AreEqual(Visuals(777, "noisy"), Visuals(777, "noisy"));
            // condition index offsets the seed: noisy of seed s = quiet of seed s + 1
            Assert.AreEqual(Visuals(778, "quiet"), Visuals(777, "noisy"));
            var distinct = new HashSet<string>();
            for (int s = 0; s < 50; s++) distinct.Add(Visuals(s, "quiet"));
            Assert.Greater(distinct.Count, 10);   // 36 possible lists: the shuffle varies
        }

        [Test]
        public void Pool_Parses()
        {
            Assert.IsTrue(TrialPlanner.TryParsePool("7.5, 22.5,37.5", out var p));
            CollectionAssert.AreEqual(Pool, p);
            Assert.IsFalse(TrialPlanner.TryParsePool("7.5,x", out _));
            Assert.IsFalse(TrialPlanner.TryParsePool(" ", out _));
        }

        [Test]
        public void Conditions_Acoustics()
        {
            Assert.IsFalse(Conditions.HasBackground(Conditions.Quiet));
            Assert.IsTrue(Conditions.HasBackground(Conditions.Noisy));
            Assert.IsTrue(Conditions.HasBackground(Conditions.Reverb));
            Assert.IsFalse(Conditions.HasBackground(Conditions.Practice));
            Assert.AreEqual("rt60.quiet_s", Conditions.Rt60Key(Conditions.Practice));
            Assert.AreEqual("rt60.reverb_s", Conditions.Rt60Key(Conditions.Reverb));
        }

        // ---- preset Default and plan -------------------------------------------------------------------------

        [Test]
        public void Defaults_AreTheDesignValues()
        {
            var v = HumPerHW2Chamber.CreateDefaults();
            Assert.AreEqual("latin_square", v.GetText("condition_order"));
            Assert.AreEqual("next_trial", v.GetText("resume_from"));
            Assert.AreEqual(5f, v.GetFloat("gate.yaw_tol_deg"));
            Assert.AreEqual(10f, v.GetFloat("gate.pitch_tol_deg"));
            Assert.AreEqual(1f, v.GetFloat("gate.hold_s"));
            Assert.AreEqual(4f, v.GetFloat("gate.hint_after_s"));
            Assert.AreEqual(0.6f, v.GetFloat("iti_s"));
            Assert.AreEqual(2.2f, v.GetFloat("rt60.reverb_s"));
            Assert.IsFalse(v.GetBool("rt60.measure_at_block_start"));
            Assert.AreEqual("-90,90,180", v.GetText("bg.azimuths_deg"));
            Assert.AreEqual(8, v.GetInt("volume.target_step"));
            foreach (var key in Conditions.All.Select(Conditions.Rt60Key)) Assert.IsTrue(v.Schema.Contains(key), key);
        }

        [Test]
        public void Plan_HasPracticeThenThreeConditionBlocks()
        {
            var plan = HumPerHW2Chamber.Plan(HumPerHW2Chamber.CreateDefaults());
            var names = plan.Stages.Select(s => s.Name).ToList();
            CollectionAssert.AreEqual(new[] { "handover", "language", "recenter", "instructions", "ready", "practice", "experiment", "thanks" }, names);
            var practice = plan.Stages[plan.IndexOf("practice")];
            Assert.IsFalse(practice.HasBlocks);
            Assert.AreEqual(ResumeFrom.StageStart, practice.Policy);
            var exp = plan.Stages[plan.IndexOf("experiment")];
            Assert.AreEqual(3, exp.Blocks);
            CollectionAssert.AreEqual(new[] { 6, 6, 6 }, exp.TrialsPerBlock.ToArray());
            Assert.AreEqual(ResumeFrom.NextTrial, exp.Policy);
            Assert.IsTrue(plan.Stages[plan.IndexOf("thanks")].Has(StageFlags.CompletesSession));
        }

        [Test]
        public void Plan_DoesNotDependOnTheConditionOrder()
        {
            var a = HumPerHW2Chamber.CreateDefaults();
            var b = HumPerHW2Chamber.CreateDefaults();
            b.Set("condition_order", "fixed");
            Assert.AreEqual(HumPerHW2Chamber.Plan(a).Hash(), HumPerHW2Chamber.Plan(b).Hash());
        }

        [Test]
        public void Actions_DeclareBothTriggers()
        {
            var go = new UnityEngine.GameObject("chamber");
            try
            {
                var table = new GPL.Input.BindingTable();
                table.DeclareStandard(GPL.Input.InputActions.Confirm);
                go.AddComponent<HumPerHW2Chamber>().DeclareActions(table);
                Assert.IsTrue(table.Has(GPL.Input.InputActions.TriggerLeft));
                Assert.IsTrue(table.Has(GPL.Input.InputActions.TriggerRight));
                Assert.IsTrue(table.Has(GPL.Input.InputActions.Confirm));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void LogColumns_AreTheDataFormatColumns()
        {
            CollectionAssert.AreEqual(new[]
            {
                "visual_az_deg", "audio_start_az_deg", "audio_final_az_deg", "signed_error_deg", "duration_s", "radius_m",
                "rt60_target_s", "rt60_measured_s", "background", "stim_level_dbfs", "bg_level_dbfs", "gate_s", "stop_dialog", "volume_step",
            }, HumPerHW2Chamber.CreateLogSchema().ChamberColumns.ToArray());
        }
    }
}
