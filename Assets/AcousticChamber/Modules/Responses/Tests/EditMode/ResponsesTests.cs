using NUnit.Framework;

namespace GPL.Responses.Tests
{
    public class ResponsesTests
    {
        static readonly AdjustSettings S = AdjustSettings.HW2;
        const double Dt = 1.0 / 90; // one frame at 90 Hz

        [Test]
        public void Adjust_MovesAtTheRateTowardTheHeldSide()
        {
            var a = new AzimuthAdjust(60, 7.5, S);
            for (int i = 0; i < 90; i++) a.Step(leftHeld: true, rightHeld: false, confirmPressed: false, dt: Dt);   // 1 s left
            Assert.AreEqual(50, a.AzimuthDeg, 1e-9);
            for (int i = 0; i < 45; i++) a.Step(false, true, false, Dt);                                           // 0.5 s right
            Assert.AreEqual(55, a.AzimuthDeg, 1e-9);
            Assert.AreEqual(1, a.Reversals);
        }

        [Test]
        public void Adjust_BothOrNoneHoldsStill()
        {
            var a = new AzimuthAdjust(-60, -22.5, S);
            for (int i = 0; i < 90; i++) a.Step(true, true, false, Dt);
            for (int i = 0; i < 90; i++) a.Step(false, false, false, Dt);
            Assert.AreEqual(-60, a.AzimuthDeg, 1e-12);
            Assert.AreEqual(0, a.Reversals);
        }

        [Test]
        public void Adjust_ClampsAtTheLimit()
        {
            var a = new AzimuthAdjust(85, 37.5, S);
            for (int i = 0; i < 900; i++) a.Step(false, true, false, Dt);
            Assert.AreEqual(90, a.AzimuthDeg, 1e-12);
            Assert.IsTrue(a.Result().AtLimit);
            Assert.AreEqual(-90, new AzimuthAdjust(-120, 0, S).AzimuthDeg, 1e-12); // the start is clamped too
        }

        [Test]
        public void Adjust_IgnoresAnEarlyConfirm()
        {
            var a = new AzimuthAdjust(60, 7.5, S);
            Assert.IsFalse(a.Step(false, false, true, 0.1));    // 0.1 s: ignored
            Assert.IsFalse(a.Done);
            Assert.IsFalse(a.Step(false, false, false, 0.25));  // 0.35 s
            Assert.IsTrue(a.Step(false, false, true, 0.01));    // accepted
            Assert.IsFalse(a.Step(true, false, true, 1.0));     // nothing after done
            Assert.AreEqual(60, a.AzimuthDeg, 1e-12);
        }

        [Test]
        public void Adjust_DurationExcludesPausedFrames()
        {
            var a = new AzimuthAdjust(60, 7.5, S);
            for (int i = 0; i < 90; i++) a.Step(true, false, false, Dt);
            for (int i = 0; i < 500; i++) a.Step(true, false, false, 0);   // Stop dialog: no time, no motion
            a.Step(false, false, true, Dt);
            var r = a.Result();
            Assert.AreEqual(1 + Dt, r.DurationS, 1e-9);
            Assert.AreEqual(50, r.FinalDeg, 1e-9);
        }

        [Test]
        public void Result_SignedErrorAndNote()
        {
            var a = new AzimuthAdjust(60, 7.5, S);
            for (int i = 0; i < 450; i++) a.Step(true, false, false, Dt);   // 5 s → 10 deg
            a.Step(false, false, true, Dt);
            var r = a.Result();
            Assert.AreEqual(10, r.FinalDeg, 1e-9);
            Assert.AreEqual(2.5, r.SignedErrorDeg, 1e-9);                   // ended right of the picture
            StringAssert.StartsWith("start_deg=60 final_deg=10 target_deg=7.5 duration_s=5.011 reversals=0 at_limit=0", r.Note());

            var noTarget = new AzimuthAdjust(0, double.NaN, S).Result();
            Assert.IsTrue(double.IsNaN(noTarget.SignedErrorDeg));
            StringAssert.Contains("target_deg= ", noTarget.Note());
        }
    }
}
