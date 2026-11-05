using System;
using System.Linq;
using GPL.Params;
using NUnit.Framework;

namespace GPL.Homing.Tests
{
    public class HomingTests
    {
        const float Dt = 0.125f; // exact in binary: no rounding in the sums

        [Test]
        public void AnglesFollowTheProjectConvention()
        {
            Assert.AreEqual(0f, HeadAngles.Yaw(0f, 0f, 1f), 1e-4f, "forward");
            Assert.AreEqual(90f, HeadAngles.Yaw(1f, 0f, 0f), 1e-4f, "right is positive");
            Assert.AreEqual(-90f, HeadAngles.Yaw(-1f, 0f, 0f), 1e-4f, "left is negative");
            Assert.AreEqual(180f, Math.Abs(HeadAngles.Yaw(0f, 0f, -1f)), 1e-4f, "behind");
            Assert.AreEqual(45f, HeadAngles.Pitch(0f, 1f, 1f), 1e-4f, "up is positive");
            Assert.AreEqual(-30f, HeadAngles.Pitch(0f, -0.5f, (float)Math.Sqrt(0.75)), 1e-3f);
            Assert.AreEqual(0f, HeadAngles.Pitch(0f, 0f, 0f), "zero vector");
            Assert.AreEqual(-170f, HeadAngles.Wrap(190f), 1e-4f);
            Assert.AreEqual(180f, HeadAngles.Wrap(-180f), 1e-4f);
            Assert.AreEqual(-20f, HeadAngles.YawError(170f, -170f), 1e-4f, "across the seam");
        }

        [Test]
        public void GatePassesAfterAnUnbrokenHold()
        {
            var gate = new HeadGate(); // defaults: yaw 10, pitch 15, hold 0.5, hint 4 (PO 2026-11-06)
            Assert.AreEqual(10f, gate.Settings.YawToleranceDeg);
            Assert.AreEqual(15f, gate.Settings.PitchToleranceDeg);
            Assert.AreEqual(0.5f, gate.Settings.HoldSec);
            Assert.AreEqual(4f, gate.Settings.HintAfterSec);

            Assert.IsFalse(gate.Update(30f, 0f, Dt), "outside");
            Assert.AreEqual(30f, gate.YawAtStart);
            Assert.IsFalse(gate.Update(9f, -14f, Dt), "the entering frame starts the hold at 0");
            Assert.AreEqual(0f, gate.Held);
            for (int i = 0; i < 3; i++) Assert.IsFalse(gate.Update(5f, 0f, Dt));
            Assert.IsTrue(gate.Update(-2f, 1f, Dt), "0.5 s held");
            Assert.IsTrue(gate.Passed);
            Assert.AreEqual(-2f, gate.YawAtPass);
            Assert.AreEqual(1f, gate.PitchAtPass);
            Assert.AreEqual(0, gate.Breaks);
            Assert.IsTrue(gate.Update(90f, 0f, Dt), "stays passed");
        }

        [Test]
        public void LeavingTheToleranceRestartsTheHoldAndCountsABreak()
        {
            var gate = new HeadGate(new GateSettings { HoldSec = 0.3f, HintAfterSec = 0f });
            gate.Update(0f, 0f, Dt);
            gate.Update(0f, 0f, Dt);
            gate.Update(0f, 0f, Dt);
            Assert.AreEqual(0.25f, gate.Held, 1e-5f);
            gate.Update(0f, 16f, Dt); // pitch out
            Assert.AreEqual(0f, gate.Held);
            Assert.AreEqual(1, gate.Breaks);
            gate.Update(10.5f, 0f, Dt); // still out: no second break
            Assert.AreEqual(1, gate.Breaks);
            Assert.IsTrue(gate.Inside(10f, -15f), "the edges are inside");
            Assert.IsFalse(gate.ShowHint, "hint 0 = never");
        }

        [Test]
        public void HintShowsOnceAfterTheDelayAndClearsOnPass()
        {
            var gate = new HeadGate(new GateSettings { HoldSec = 0.2f, HintAfterSec = 1f });
            for (int i = 0; i < 7; i++) gate.Update(40f, 0f, Dt);
            Assert.IsFalse(gate.ShowHint);
            gate.Update(40f, 0f, Dt);
            Assert.IsTrue(gate.ShowHint);
            Assert.AreEqual(1f, gate.HintAt, 1e-4f);
            gate.Update(0f, 0f, Dt);
            gate.Update(0f, 0f, Dt);
            Assert.IsTrue(gate.Update(0f, 0f, Dt));
            Assert.IsFalse(gate.ShowHint);
            Assert.AreEqual(1f, gate.HintAt, 1e-4f, "kept for the log");

            gate.Reset();
            Assert.IsFalse(gate.Passed);
            Assert.AreEqual(-1f, gate.HintAt);
            Assert.AreEqual(0f, gate.Elapsed);
        }

        [Test]
        public void SettingsRejectNegativeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeadGate(new GateSettings { HoldSec = -1f }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GateSettings { YawToleranceDeg = float.NaN }.Validated());
        }

        [Test]
        public void ResultNoteIsKeyValueInvariant()
        {
            var r = new HomingResult
            {
                Method = HomingMethods.HeadGate, DurationSec = 0.7342f, YawBeforeDeg = -12.5f, PitchBeforeDeg = 3f,
                YawAfterDeg = 1.25f, PitchAfterDeg = -0.5f, EyeHeightM = 1.2346f, Breaks = 2, HintAtSec = -1f,
            };
            Assert.AreEqual("method=head_gate outcome=done duration_s=0.734 yaw_before_deg=-12.5 pitch_before_deg=3 " +
                            "yaw_after_deg=1.25 pitch_after_deg=-0.5 eye_height_m=1.235 breaks=2 hint_at_s=", r.Note());

            var u = new HomingResult { Method = HomingMethods.SystemRecenter, Outcome = HomingOutcome.Unavailable, Detail = "no headset" };
            StringAssert.StartsWith("method=system_recenter outcome=unavailable ", u.Note());
            StringAssert.EndsWith(" detail=\"no headset\"", u.Note());
            StringAssert.DoesNotContain("breaks=", u.Note(), "gate fields only for the gate");
            Assert.IsFalse(u.Ok);
        }

        [Test]
        public void GateParamsRoundTripThroughAPreset()
        {
            var schema = new ParameterSchema(HomingParams.GateDefs());
            Assert.AreEqual(4, schema.Count);
            Assert.IsTrue(schema.Defs.All(d => d.Group == "Head gate"));
            var set = schema.Defaults();
            var s = HomingParams.ReadGate(set);
            Assert.AreEqual(10f, s.YawToleranceDeg);
            Assert.AreEqual(0.5f, s.HoldSec);

            set.Set(HomingParams.YawTolerance, 5f);
            set.Set(HomingParams.Hold, 1f);
            s = HomingParams.ReadGate(set);
            Assert.AreEqual(5f, s.YawToleranceDeg);
            Assert.AreEqual(1f, s.HoldSec);
            Assert.AreEqual(15f, s.PitchToleranceDeg);

            var other = new ParameterSchema(ParamDef.Int("trials", 6, 1, 10, "Trials")).Defaults();
            Assert.AreEqual(10f, HomingParams.ReadGate(other).YawToleranceDeg, "missing keys keep the defaults");
        }

        [Test]
        public void FixationDotIsOneAndAHalfDegrees()
        {
            Assert.AreEqual(0.05236f, FixationCue.DiameterM(1.5f, 2f), 1e-4f);
        }
    }
}
