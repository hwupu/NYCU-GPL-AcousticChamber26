using System;
using NUnit.Framework;

namespace GPL.Acoustics.Tests
{
    public class AcousticsTests
    {
        const double HW2Vs = 457.8 / 427.64; // V/S of the 8.72 x 15 x 3.5 m room

        // ---- RoomMath -------------------------------------------------------------------------------------

        [Test]
        public void BoxVolumeSurface_MatchesTheHW2Room()
        {
            RoomMath.BoxVolumeSurface(8.72, 15, 3.5, out double v, out double s);
            Assert.AreEqual(457.8, v, 1e-9);
            Assert.AreEqual(427.64, s, 1e-9);
        }

        [Test]
        public void Eyring_RoundTrips()
        {
            RoomMath.BoxVolumeSurface(8.72, 15, 3.5, out double v, out double s);
            foreach (double rt in new[] { 0.5, 0.7, 2.2 })
            {
                double r = RoomMath.EyringReflectionForRt60(rt, v, s);
                Assert.AreEqual(rt, RoomMath.EyringRt60ForReflection(r, v, s), 1e-6);
            }
        }

        [Test]
        public void Sabine_AbsorptionForRt60()
        {
            // 0.161 * 457.8 / (427.64 * 0.7) = 0.2462
            Assert.AreEqual(0.2462, RoomMath.SabineAbsorptionForRt60(0.7, 457.8, 427.64), 1e-4);
        }

        // ---- AudioLevelMath -------------------------------------------------------------------------------

        [Test]
        public void Rms_OfFullScaleSquareIsZeroDb_SilenceIsSilent()
        {
            Assert.AreEqual(0f, AudioLevelMath.RmsDbfs(new[] { 1f, -1f, 1f, -1f }), 1e-5);
            Assert.AreEqual(AudioLevelMath.Silent, AudioLevelMath.RmsDbfs(new float[8]));
            Assert.AreEqual(AudioLevelMath.Silent, AudioLevelMath.RmsDbfs(null));
        }

        [Test]
        public void PowerSum_ThreeEqualClipsAdd4_8Db_SilentIgnored()
        {
            Assert.AreEqual(-23f + 4.771f, AudioLevelMath.PowerSumDb(-23f, -23f, -23f), 1e-3);
            Assert.AreEqual(-23f, AudioLevelMath.PowerSumDb(-23f, AudioLevelMath.Silent), 1e-4);
            Assert.AreEqual(AudioLevelMath.Silent, AudioLevelMath.PowerSumDb());
        }

        // ---- Rt60Calibration ------------------------------------------------------------------------------

        [Test]
        public void Calibration_TableEntriesAreExactInTheCalibrationRoom()
        {
            var c = Rt60Calibration.MetaXrAudioShoebox;
            Assert.AreEqual(0.85, c.ReflectionForRt60(0.89, HW2Vs), 1e-9);
            Assert.AreEqual(0.95, c.ReflectionForRt60(2.11, HW2Vs), 1e-9);
            Assert.AreEqual(1.30, c.Rt60ForReflection(0.90, HW2Vs), 1e-9);
        }

        [Test]
        public void Calibration_HW2ConditionsRoundTrip()
        {
            var c = Rt60Calibration.MetaXrAudioShoebox;
            foreach (double rt in new[] { 0.3, 0.7, 2.2 })
                Assert.AreEqual(rt, c.Rt60ForReflection(c.ReflectionForRt60(rt, HW2Vs), HW2Vs), 1e-9);
            double r07 = c.ReflectionForRt60(0.7, HW2Vs);
            Assert.That(r07, Is.InRange(0.80, 0.85)); // between the 0.65 s and 0.89 s entries
        }

        [Test]
        public void Calibration_ClampsOutsideTheTable()
        {
            var c = Rt60Calibration.MetaXrAudioShoebox;
            Assert.AreEqual(0.50, c.ReflectionForRt60(0.2, HW2Vs), 1e-12);  // 0.2 s cannot be rendered
            Assert.AreEqual(0.99, c.ReflectionForRt60(9.0, HW2Vs), 1e-12);
            Assert.AreEqual(0.30, c.LowestRt60, 1e-12);
            Assert.AreEqual(0.30, c.Rt60ForReflection(0.1, HW2Vs), 1e-12);
        }

        [Test]
        public void Calibration_ScalesWithVolumeOverSurface()
        {
            var c = Rt60Calibration.MetaXrAudioShoebox;
            double halfVs = HW2Vs / 2;
            // the same coefficient gives half the time in a room with half the V/S
            Assert.AreEqual(1.30 / 2, c.Rt60ForReflection(0.90, halfVs), 1e-9);
            Assert.AreEqual(0.90, c.ReflectionForRt60(0.65, halfVs), 1e-9);
            Assert.AreEqual(0.15, c.LowestRt60In(halfVs), 1e-9);
        }

        [Test]
        public void Calibration_RejectsBadTables()
        {
            Assert.Throws<ArgumentException>(() => new Rt60Calibration(new[] { 0.5, 0.4 }, new[] { 0.3, 0.4 }, 1, 1, 1, ""));
            Assert.Throws<ArgumentException>(() => new Rt60Calibration(new[] { 0.5, 0.6 }, new[] { 0.3 }, 1, 1, 1, ""));
            Assert.Throws<ArgumentException>(() => new Rt60Calibration(new[] { 0.5, 0.6 }, new[] { 0.4, 0.4 }, 1, 1, 1, ""));
        }

        // ---- Rt60Estimator --------------------------------------------------------------------------------

        /// <summary>Energy of an exponentially decaying impulse response with a given RT60 (60 dB of energy in rt60 seconds).</summary>
        static float[] Decay(double rt60, double sampleRate, double seconds, double noiseFloor = 0)
        {
            int n = (int)(sampleRate * seconds);
            var e = new float[n];
            double perSample = Math.Pow(10.0, -6.0 / (rt60 * sampleRate)); // energy factor per sample
            double v = 1.0;
            for (int i = 0; i < n; i++) { e[i] = (float)(v + noiseFloor); v *= perSample; }
            return e;
        }

        [Test]
        public void Estimator_RecoversTheDecayRate([Values(0.3, 0.7, 2.2)] double rt60)
        {
            const double fs = 48000;
            var e = Decay(rt60, fs, Math.Max(1.5, rt60 * 1.6));
            var r = Rt60Estimator.Estimate(e, 0, e.Length, fs);
            Assert.IsTrue(r.Valid, r.Reason);
            Assert.AreEqual(rt60, r.T20, rt60 * 0.03);
            Assert.AreEqual(rt60, r.T30, rt60 * 0.03);
            Assert.That(r.DepthDb, Is.GreaterThan(35));
        }

        [Test]
        public void Estimator_RejectsARecordingThatEndsTooEarly()
        {
            const double fs = 48000;
            var e = Decay(2.2, fs, 0.5); // only about 14 dB of decay
            var r = Rt60Estimator.Estimate(e, 0, e.Length, fs);
            Assert.IsFalse(r.Valid);
            StringAssert.Contains("valid=0", r.Note());
        }

        [Test]
        public void Estimator_HandlesSilenceAndBadArguments()
        {
            Assert.IsFalse(Rt60Estimator.Estimate(new float[48000], 0, 48000, 48000).Valid);
            Assert.IsFalse(Rt60Estimator.Estimate(null, 0, 100, 48000).Valid);
            Assert.IsFalse(Rt60Estimator.Estimate(new float[10], 0, 10, 48000).Valid);
        }

        [Test]
        public void Estimator_NoteIsKeyValue()
        {
            const double fs = 48000;
            var e = Decay(0.7, fs, 1.5);
            string note = Rt60Estimator.Estimate(e, 0, e.Length, fs).Note();
            StringAssert.StartsWith("valid=1 t20=0.", note);
            StringAssert.Contains(" t30=", note);
            StringAssert.Contains(" depth_db=", note);
        }
    }
}
