using System.Collections.Generic;
using System.Linq;
using GPL.HearingLoss;
using GPL.Protocol;
using GPL.Session;
using NUnit.Framework;

namespace AcousticChamber.Chambers.HumPerFinal.Tests
{
    public class HumPerFinalTests
    {
        static readonly float[] Pool = { 7.5f, 22.5f };

        // ---- cells (Design.md "Conditions and order") ----------------------------------------------------------

        [Test]
        public void Cells_AreTheDesignTable()
        {
            CollectionAssert.AreEqual(new[]
            {
                "normal_none", "normal_on", "left40_none", "left40_on", "right40_none", "right40_on",
                "left80_none", "left80_on", "right80_none", "right80_on",
            }, Cells.All.ToArray());
            Assert.AreEqual("left80", Cells.HearingOf(7));
            Assert.IsTrue(Cells.HasAnchors(7));
            Assert.IsFalse(Cells.HasAnchors(6));
            Assert.AreEqual(7, Cells.Index("left80_on"));
            Assert.AreEqual(-1, Cells.Index("practice"));
        }

        // ---- Williams square ------------------------------------------------------------------------------------

        [Test]
        public void Williams_RowsAreTheDesignTable()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 9, 2, 8, 3, 7, 4, 6, 5 }, CellOrder.Row(0));
            CollectionAssert.AreEqual(new[] { 1, 2, 0, 3, 9, 4, 8, 5, 7, 6 }, CellOrder.Row(1));
            CollectionAssert.AreEqual(new[] { 9, 0, 8, 1, 7, 2, 6, 3, 5, 4 }, CellOrder.Row(9));
        }

        [Test]
        public void Williams_EachCellOnceInEachPosition_AndEachOrderedPairOnce()
        {
            var rows = Enumerable.Range(0, 10).Select(CellOrder.Row).ToList();
            foreach (var r in rows) CollectionAssert.AreEquivalent(Enumerable.Range(0, 10), r);
            for (int pos = 0; pos < 10; pos++)
                CollectionAssert.AreEquivalent(Enumerable.Range(0, 10), rows.Select(r => r[pos]), "position " + pos);
            var pairs = new HashSet<(int, int)>();
            foreach (var r in rows)
                for (int i = 0; i + 1 < r.Length; i++) Assert.IsTrue(pairs.Add((r[i], r[i + 1])), "pair " + r[i] + "→" + r[i + 1] + " twice");
            Assert.AreEqual(90, pairs.Count);
        }

        [Test]
        public void Williams_RowIsStudyIdMod10()
        {
            Assert.AreEqual(2, CellOrder.RowOf("S0012"));
            Assert.AreEqual(1, CellOrder.RowOf("S0021"));
            Assert.AreEqual(0, CellOrder.RowOf("S0020"));
            Assert.AreEqual(2, CellOrder.RowOf("S0022"));
            Assert.AreEqual(0, CellOrder.RowOf(""));
            var rows = Enumerable.Range(12, 10).Select(n => CellOrder.RowOf("S" + n.ToString("D4"))).ToList();
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 10), rows);   // Study IDs 12-21 use every row once
            CollectionAssert.AreEqual(Enumerable.Range(0, 10), CellOrder.For(CellOrder.Fixed, 4));
        }

        [Test]
        public void Order_FormatAndParse()
        {
            var row = CellOrder.Row(3);
            string text = CellOrder.Format(row);
            StringAssert.StartsWith("left40_on,right40_none,left40_none,", text);
            Assert.IsTrue(CellOrder.TryParse(text, out var back));
            CollectionAssert.AreEqual(row, back);
            Assert.IsFalse(CellOrder.TryParse("normal_none,normal_none", out _));
            Assert.IsFalse(CellOrder.TryParse("", out _));
            Assert.IsFalse(CellOrder.TryParse(text.Replace("left40_on", "practice"), out _));
        }

        [Test]
        public void EnsureOrder_StoresOnceAndKeepsIt()
        {
            var values = HumPerFinalChamber.CreateDefaults();
            var record = new SessionRecord { StudyId = "S0013" };
            var order = HumPerFinalChamber.EnsureOrder(record, values, out int row);
            Assert.AreEqual(3, row);
            CollectionAssert.AreEqual(CellOrder.Row(3), order);
            Assert.AreEqual("3", record.Custom[HumPerFinalChamber.OrderRowKey]);
            values.Set(HumPerFinalChamber.OrderKey, CellOrder.Fixed);              // a later preset change does not move a session
            CollectionAssert.AreEqual(CellOrder.Row(3), HumPerFinalChamber.EnsureOrder(record, values, out row));
            Assert.AreEqual(3, row);
        }

        // ---- hearing curves --------------------------------------------------------------------------------

        [Test]
        public void Hearing_Curves()
        {
            CollectionAssert.AreEqual(new[] { 0, 0, -10, -20, -25, -30, -30, -40, -30, -20 }, Hearing.Profile(Hearing.Left40).Left);
            Assert.IsTrue(Hearing.Profile(Hearing.Left40).IsNormal(Ear.Right));
            CollectionAssert.AreEqual(new[] { 0, 0, -20, -40, -50, -60, -60, -80, -60, -40 }, Hearing.Profile(Hearing.Right80).Right);
            Assert.IsTrue(Hearing.Profile(Hearing.Right80).IsNormal(Ear.Left));
            Assert.AreEqual(HearingProfile.Normal, Hearing.Profile(Hearing.Normal));
            Assert.AreEqual("hearing=left80 left_db=-80 right_db=0 curve=severe_x2", Hearing.Note(Hearing.Left80));
            Assert.AreEqual("R −40", Hearing.Label(Hearing.Right40));
        }

        // ---- trial lists ---------------------------------------------------------------------------------------

        /// <summary>Design.md "Position pairs" table: start side (L / R) of −22.5, −7.5, +7.5, +22.5 per set.</summary>
        static readonly string[] SetTable = { "LRLR", "LRRL", "RLLR", "RLRL" };

        [Test]
        public void PairSets_AreTheDesignTable([Values(0, 1, 2, 3)] int set)
        {
            var visuals = new[] { -22.5f, -7.5f, 7.5f, 22.5f };
            string sides = string.Concat(visuals.Select(v => PairSet.StartSign(set, v, System.Math.Abs(v) == 22.5f) < 0 ? "L" : "R"));
            Assert.AreEqual(SetTable[set], sides);
            // each hemisphere: one L and one R
            Assert.AreEqual(1, sides.Substring(0, 2).Count(c => c == 'L'));
            Assert.AreEqual(1, sides.Substring(2, 2).Count(c => c == 'L'));
        }

        [Test]
        public void PairSet_IsStudyIdMod4()
        {
            Assert.AreEqual(0, PairSet.Of("S0012"));
            Assert.AreEqual(1, PairSet.Of("S0013"));
            Assert.AreEqual(3, PairSet.Of("S0015"));
            Assert.AreEqual(1, PairSet.Of("S0021"));
            Assert.AreEqual(0, PairSet.Of(""));
            // Study IDs 12-21: sets used 3, 3, 2, 2 times (Design.md)
            var counts = Enumerable.Range(12, 10).GroupBy(n => PairSet.Of("S" + n.ToString("D4"))).ToDictionary(g => g.Key, g => g.Count());
            CollectionAssert.AreEqual(new[] { 3, 3, 2, 2 }, Enumerable.Range(0, 4).Select(s => counts[s]).ToArray());
        }

        [Test]
        public void EnsurePairSet_StoresOnceAndKeepsIt()
        {
            var record = new SessionRecord { StudyId = "S0015" };
            Assert.AreEqual(3, HumPerFinalChamber.EnsurePairSet(record));
            Assert.AreEqual("3", record.Custom[HumPerFinalChamber.PairSetKey]);
            record.StudyId = "S0016";                                                // the stored set wins on a resume
            Assert.AreEqual(3, HumPerFinalChamber.EnsurePairSet(record));
        }

        [Test]
        public void Trials_EveryPositionPairTwice([Values(0, 5, 9)] int cell, [Values(0, 1, 2, 3)] int set)
        {
            var t = TrialPlanner.Plan(cell, 12345, Pool, 60f, 2, 2f, set);
            Assert.AreEqual(8, t.Count);
            Assert.AreEqual(8, TrialPlanner.TrialsPerBlock(2));
            var visuals = new[] { -22.5f, -7.5f, 7.5f, 22.5f };
            for (int i = 0; i < visuals.Length; i++)
            {
                float v = visuals[i];
                var same = t.Where(x => x.VisualAzDeg == v).ToList();
                Assert.AreEqual(2, same.Count, "visual " + v);
                Assert.IsTrue(same.All(x => x.StartSide == SetTable[set][i].ToString()), "visual " + v + " side");
                Assert.IsTrue(same.All(x => System.Math.Abs(x.AudioStartAzDeg) == 60f));
                CollectionAssert.AreEqual(new[] { 1, 2 }, same.Select(x => x.Repeat).ToArray()); // repeat in order of appearance
            }
            CollectionAssert.AreEqual(Enumerable.Range(1, 8), t.Select(x => x.Index));
            Assert.IsTrue(t.All(x => x.Condition == Cells.Name(cell) && x.RadiusM == 2f));
        }

        [Test]
        public void Trials_PoolOrderDoesNotMatter_AndNeedsTwoValues()
        {
            string Sides(float[] pool) => string.Join(";", TrialPlanner.Plan(0, 1, pool, 60f, 1, 3f, 1).Select(x => x.VisualAzDeg + "/" + x.AudioStartAzDeg));
            Assert.AreEqual(Sides(new[] { 7.5f, 22.5f }), Sides(new[] { 22.5f, 7.5f }));
            Assert.Throws<System.ArgumentException>(() => TrialPlanner.Plan(0, 1, new[] { 7.5f }, 60f, 1, 3f, 0));
            Assert.Throws<System.ArgumentException>(() => TrialPlanner.Plan(0, 1, new[] { 7.5f, 15f, 22.5f }, 60f, 1, 3f, 0));
            Assert.Throws<System.ArgumentException>(() => TrialPlanner.Plan(0, 1, new[] { 7.5f, -7.5f }, 60f, 1, 3f, 0));
        }

        [Test]
        public void Trials_AreReproducible_AndIndependentOfBlockPosition()
        {
            string List(int seed, int cell) => string.Join(";", TrialPlanner.Plan(cell, seed, Pool, 60f, 2, 2f, 2).Select(x => x.VisualAzDeg + "/" + x.AudioStartAzDeg));
            Assert.AreEqual(List(777, 3), List(777, 3));
            Assert.AreEqual(List(780, 0), List(777, 3)); // seed = session seed + cell index
            var distinct = new HashSet<string>();
            for (int s = 0; s < 30; s++) distinct.Add(List(s, 0));
            Assert.Greater(distinct.Count, 20);          // 8 trials with pairs: 8!/2^4 = 2520 distinct orders
        }

        [Test]
        public void List_Parses()
        {
            Assert.IsTrue(TrialPlanner.TryParseList("7.5, 22.5", out var p));
            CollectionAssert.AreEqual(Pool, p);
            Assert.IsFalse(TrialPlanner.TryParseList("7.5,x", out _));
            Assert.IsFalse(TrialPlanner.TryParseList(" ", out _));
        }

        // ---- free 360° and restarts --------------------------------------------------------------------------

        [Test]
        public void Angles_WrapAndSignedDifference()
        {
            Assert.AreEqual(-170.0, Angles.Wrap(190.0), 1e-9);
            Assert.AreEqual(180.0, Angles.Wrap(-180.0), 1e-9);
            Assert.AreEqual(180.0, Angles.Wrap(540.0), 1e-9);
            Assert.AreEqual(10.0, Angles.Wrap(370.0), 1e-9);
            Assert.AreEqual(-10.0, Angles.Wrap(-370.0), 1e-9);
            Assert.AreEqual(5.0, Angles.SignedDiff(12.5, 7.5), 1e-9);
            Assert.AreEqual(5.0, Angles.SignedDiff(372.5, 7.5), 1e-9);    // one full turn more
            Assert.AreEqual(-20.0, Angles.SignedDiff(170.0, -170.0), 1e-9); // across ±180
        }

        [Test]
        public void Adjust_WithoutLimit_GoesRoundTheCircle()
        {
            var a = new GPL.Responses.AzimuthAdjust(60, 7.5, new GPL.Responses.AdjustSettings { RateDegPerS = 10, LimitDeg = double.PositiveInfinity, ConfirmIgnoreS = 0.3 });
            for (int i = 0; i < 400; i++) a.Step(false, true, false, 0.1); // 400 × 1° right
            Assert.AreEqual(460.0, a.AzimuthDeg, 1e-6);
            Assert.IsTrue(a.Step(false, false, true, 0.0));
            Assert.IsFalse(a.Result().AtLimit);
            Assert.AreEqual(100.0, Angles.Wrap(a.Result().FinalDeg), 1e-6);
        }

        [Test]
        public void OpenTrial_RoundTrip()
        {
            var t = new OpenTrial { Block = 4, Trial = 11, Restarts = 2 };
            Assert.AreEqual("4:11:2", t.Format());
            Assert.IsTrue(OpenTrial.TryParse("4:11:2", out var back));
            Assert.IsTrue(back.Is(4, 11));
            Assert.AreEqual(2, back.Restarts);
            Assert.IsFalse(OpenTrial.TryParse("4:11", out _));
            Assert.IsFalse(OpenTrial.TryParse("a:1:0", out _));
            Assert.IsFalse(OpenTrial.TryParse("", out _));
        }

        // ---- preset Default and plan -------------------------------------------------------------------------

        [Test]
        public void Defaults_AreTheDesignValues()
        {
            var v = HumPerFinalChamber.CreateDefaults();
            Assert.AreEqual("williams", v.GetText("condition_order"));
            Assert.AreEqual("next_trial", v.GetText("resume_from"));
            Assert.AreEqual(2, v.GetInt("repeats"));
            Assert.AreEqual("7.5,22.5", v.GetText("visual_pool_deg"));
            Assert.AreEqual(0.7f, v.GetFloat("stim.off_s"));
            Assert.AreEqual(2f, v.GetFloat("stim.on_s"));
            Assert.AreEqual(0.7f, v.GetFloat("rt60_s"));
            Assert.AreEqual(0f, v.GetFloat("anchor.duck_deg"));
            Assert.AreEqual(90f, v.GetFloat("anchor.trumpet_deg"));
            Assert.AreEqual(1.5f, v.GetFloat("anchor.gap_after_duck_s"));
            Assert.AreEqual(2.5f, v.GetFloat("anchor.gap_after_trumpet_s"));
            Assert.AreEqual(5f, v.GetFloat("gate.yaw_tol_deg"));
            Assert.AreEqual(1f, v.GetFloat("gate.hold_s"));
            Assert.AreEqual(12, v.GetInt("volume.target_step"));
            Assert.IsFalse(v.Schema.Contains("adjust.limit_deg"));
        }

        [Test]
        public void Plan_HasPracticeThenTenBlocksOf8()
        {
            var plan = HumPerFinalChamber.Plan(HumPerFinalChamber.CreateDefaults());
            CollectionAssert.AreEqual(new[] { "handover", "language", "recenter", "instructions", "ready", "practice", "experiment", "thanks" },
                                      plan.Stages.Select(s => s.Name).ToList());
            var exp = plan.Stages[plan.IndexOf("experiment")];
            Assert.AreEqual(10, exp.Blocks);
            Assert.IsTrue(exp.TrialsPerBlock.All(n => n == 8));
            Assert.AreEqual(80, exp.TrialsPerBlock.Sum());
            Assert.AreEqual(ResumeFrom.NextTrial, exp.Policy);
            Assert.IsTrue(plan.Stages[plan.IndexOf("thanks")].Has(StageFlags.CompletesSession));
        }

        [Test]
        public void Plan_DoesNotDependOnTheOrder()
        {
            var a = HumPerFinalChamber.CreateDefaults();
            var b = HumPerFinalChamber.CreateDefaults();
            b.Set("condition_order", "fixed");
            Assert.AreEqual(HumPerFinalChamber.Plan(a).Hash(), HumPerFinalChamber.Plan(b).Hash());
        }

        [Test]
        public void LogColumns_AreTheDataFormatColumns()
        {
            CollectionAssert.AreEqual(new[]
            {
                "hearing", "anchors", "order_row", "pair_set", "trial_index", "repeat", "visual_az_deg", "audio_start_az_deg", "start_side",
                "audio_final_az_deg", "audio_final_unwrapped_deg", "signed_error_deg", "duration_s", "restarts", "reversals", "radius_m",
                "rt60_target_s", "rt60_measured_s", "stim_level_dbfs", "anchor_level_dbfs", "gate_s", "stop_dialog", "volume_step",
            }, HumPerFinalChamber.CreateLogSchema().ChamberColumns.ToArray());
        }

        // ---- anchor models on their surfaces (Design.md "Stimuli", PO 2026-11-18) -------------------------------

        [Test]
        public void Ground_PutsTheModelOnTheSurfaceUnderTheSound()
        {
            var anchor = new UnityEngine.GameObject("Anchor");
            var model = new UnityEngine.GameObject("Model");
            try
            {
                model.transform.SetParent(anchor.transform, false);
                anchor.transform.SetPositionAndRotation(new UnityEngine.Vector3(0.2f, 1.2f, 3f), UnityEngine.Quaternion.Euler(0f, 180f, 0f));
                AnchorLoop.Ground(model.transform, 0.778f);
                Assert.AreEqual(0.2f, model.transform.position.x, 1e-5f);
                Assert.AreEqual(0.778f, model.transform.position.y, 1e-5f);
                Assert.AreEqual(3f, model.transform.position.z, 1e-5f);
                Assert.AreEqual(1.2f, anchor.transform.position.y, 1e-5f, "the sound stays at eye height");

                anchor.transform.position = new UnityEngine.Vector3(-3f, 1.1f, 0f); // next block: placed again, grounded again
                AnchorLoop.Ground(model.transform, 0.013f);
                Assert.AreEqual(-3f, model.transform.position.x, 1e-5f);
                Assert.AreEqual(0.013f, model.transform.position.y, 1e-5f);

                AnchorLoop.Ground(model.transform, float.NaN);
                Assert.AreEqual(0.013f, model.transform.position.y, 1e-5f, "NaN keeps the model where it is");
                AnchorLoop.Ground(null, 1f); // no model: no exception
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(anchor);
            }
        }
    }
}
