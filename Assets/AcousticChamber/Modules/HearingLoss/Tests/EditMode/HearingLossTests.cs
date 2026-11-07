using System;
using NUnit.Framework;

namespace GPL.HearingLoss.Tests
{
    public class Ansis344Tests
    {
        // HLSIM_GUIDELINES section 9.1: UseAge, Male, Age, UseEffects, Years, Level, Fractile -> 10 bands. Must match exactly.
        static readonly object[] Vectors =
        {
            new object[] { 1,  true,  true,  55, true,  25, 95,  0.5,  new[] { -5, -5, -5, -8, -15, -19, -34, -42, -38, -43 } },
            new object[] { 2,  true,  false, 55, true,  25, 95,  0.5,  new[] { -5, -5, -5, -8, -14, -18, -29, -34, -31, -35 } },
            new object[] { 3,  false, true,  55, true,  25, 95,  0.5,  new[] { 0, 0, 0, -3, -8, -11, -21, -24, -17, -17 } },
            new object[] { 4,  true,  true,  55, false, 0,  0,   0.5,  new[] { -4, -4, -5, -5, -8, -10, -16, -22, -25, -30 } },
            new object[] { 5,  true,  false, 55, false, 0,  0,   0.5,  new[] { -4, -4, -5, -5, -7, -8, -10, -12, -16, -21 } },
            new object[] { 6,  true,  true,  18, true,  1,  85,  0.5,  new[] { 0, 0, 0, 0, 0, 0, 0, -1, 0, 0 } },
            new object[] { 7,  false, true,  30, true,  5,  100, 0.5,  new[] { -2, -2, -2, -3, -3, 0, -15, -20, -14, -14 } },
            new object[] { 8,  false, true,  30, true,  10, 90,  0.5,  new[] { 0, 0, 0, 0, -1, -2, -8, -11, -7, -7 } },
            new object[] { 9,  false, true,  30, true,  10, 90,  0.1,  new[] { 0, 0, 0, 0, -3, -6, -13, -15, -12, -12 } },
            new object[] { 10, false, true,  30, true,  10, 90,  0.9,  new[] { 0, 0, 0, 0, 2, 0, -4, -7, -3, -3 } },
            new object[] { 11, true,  true,  45, true,  20, 105, 0.25, new[] { -28, -28, -28, -32, -42, -45, -70, -72, -65, -68 } },
            new object[] { 12, true,  true,  45, true,  20, 105, 0.75, new[] { -10, -11, -11, -14, -3, -19, -40, -46, -33, -34 } },
            new object[] { 13, true,  false, 70, true,  50, 130, 0.1,  new[] { -80, -80, -80, -80, -80, -80, -80, -80, -80, -80 } },
            new object[] { 14, true,  true,  70, true,  50, 130, 0.9,  new[] { -80, -80, -80, -80, 12, -80, -80, -80, -80, -80 } },
            new object[] { 15, false, false, 30, true,  3,  70,  0.5,  new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
            new object[] { 16, true,  true,  40, true,  9,  100, 0.5,  new[] { -5, -5, -5, -7, -10, -10, -28, -35, -28, -30 } },
            new object[] { 17, true,  true,  40, true,  10, 100, 0.5,  new[] { -5, -5, -5, -8, -12, -12, -30, -37, -30, -31 } },
            new object[] { 18, false, true,  30, true,  40, 120, 0.5,  new[] { -80, -80, -80, -80, -80, -80, -80, -80, -80, -80 } },
            new object[] { 19, true,  true,  18, false, 0,  0,   0.5,  new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
            new object[] { 20, false, true,  30, false, 0,  0,   0.5,  new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        };

        [TestCaseSource(nameof(Vectors))]
        public void TestVectorMatchesExactly(int id, bool useAge, bool male, int age, bool useEffects, int years, int level,
                                             double fractile, int[] expected)
        {
            var bands = ANSIS344.Apply(useAge, male, age, useEffects, years, level, fractile);
            CollectionAssert.AreEqual(expected, bands, $"vector {id}");
        }

        [Test]
        public void DefaultsGiveVector1()
        {
            var p = new PredictionParameters { Age = 20, Male = false, Years = 3, ExposureLevel = 80, Fractile = 0.9, UseAge = false, UseEffects = false };
            p.ResetToDefaults();
            Assert.IsTrue(p.UseAge && p.Male && p.UseEffects);
            Assert.AreEqual(55, p.Age); Assert.AreEqual(25, p.Years); Assert.AreEqual(95, p.ExposureLevel); Assert.AreEqual(0.5, p.Fractile);
            CollectionAssert.AreEqual(new[] { -5, -5, -5, -8, -15, -19, -34, -42, -38, -43 }, ANSIS344.Apply(p));
        }

        [Test]
        public void GridStaysInRange()
        {
            double[] fractiles = { 0.1, 0.25, 0.5, 0.75, 0.9 };
            int[] yearsSet = { 0, 1, 3, 9, 10, 20, 35, 50 };
            foreach (bool male in new[] { true, false })
            for (int age = 18; age <= 70; age += 4)
            foreach (int years in yearsSet)
            for (int level = 70; level <= 130; level += 10)
            foreach (double f in fractiles)
            for (int combo = 0; combo < 3; combo++)
            {
                bool useAge = combo != 1, useEffects = combo != 2;
                foreach (int b in ANSIS344.Apply(useAge, male, age, useEffects, years, level, f))
                    Assert.That(b, Is.InRange(ANSIS344.MinBandDb, ANSIS344.MaxBandDb));
            }
        }

        [Test]
        public void UseAgeFalseIgnoresAge_UseEffectsFalseIgnoresExposure()
        {
            CollectionAssert.AreEqual(ANSIS344.Apply(false, true, 18, true, 20, 100, 0.5), ANSIS344.Apply(false, true, 70, true, 20, 100, 0.5));
            CollectionAssert.AreEqual(ANSIS344.Apply(true, true, 50, false, 0, 0, 0.5), ANSIS344.Apply(true, true, 50, false, 40, 125, 0.5));
        }

        [Test]
        public void ClampGainToZeroOptionOnlyRemovesPositiveValues()
        {
            var faithful = ANSIS344.Apply(true, true, 70, true, 50, 130, 0.9);
            var clamped = ANSIS344.Apply(true, true, 70, true, 50, 130, 0.9, clampGainToZero: true);
            Assert.AreEqual(12, faithful[4]);
            Assert.AreEqual(0, clamped[4]);
            for (int i = 0; i < 10; i++) if (i != 4) Assert.AreEqual(faithful[i], clamped[i]);
        }
    }

    public class HearingLossStateTests
    {
        [Test]
        public void PresetsMatchGuidelines()
        {
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, -5, -10, -10, -20, -10, 0 }, HearingPresets.Mild);
            CollectionAssert.AreEqual(new[] { 0, 0, -10, -10, -15, -20, -20, -30, -20, -10 }, HearingPresets.Moderate);
            CollectionAssert.AreEqual(new[] { 0, 0, -10, -20, -25, -30, -30, -40, -30, -20 }, HearingPresets.Severe);
            CollectionAssert.AreEqual(new int[10], HearingPresets.None);
        }

        [Test]
        public void PresetsAreCopies()
        {
            HearingPresets.Mild[7] = 5;
            Assert.AreEqual(-20, HearingPresets.Mild[7]);
        }

        [Test]
        public void ManualIsRestoredAfterSwitchingAway()
        {
            var s = new HearingLossState();
            s.Set(HearingLossLevel.Manual);
            s.SetManualBand(3, -17);
            s.SetManualBand(7, -99); // clamped
            s.Set(HearingLossLevel.Severe);
            CollectionAssert.AreEqual(HearingPresets.Severe, s.Bands);
            s.Set(HearingLossLevel.Manual);
            Assert.AreEqual(-17, s.Bands[3]);
            Assert.AreEqual(-80, s.Bands[7]);
            s.ResetManual();
            CollectionAssert.AreEqual(new int[10], s.Bands);
        }

        [Test]
        public void PredictedWithoutParametersThrows()
        {
            var s = new HearingLossState();
            var ex = Assert.Throws<ArgumentException>(() => s.Set(HearingLossLevel.Predicted, null));
            StringAssert.Contains("Prediction parameters cannot be null", ex.Message);
        }

        [Test]
        public void PredictedUsesModel_AndManualEditOutsideManualThrows()
        {
            var s = new HearingLossState();
            int[] seen = null;
            s.Changed += b => seen = b;
            s.Set(HearingLossLevel.Predicted, new PredictionParameters());
            CollectionAssert.AreEqual(new[] { -5, -5, -5, -8, -15, -19, -34, -42, -38, -43 }, seen);
            Assert.Throws<InvalidOperationException>(() => s.SetManualBand(0, -5));
        }
    }

    public class HearingProfileTests
    {
        [Test]
        public void UnilateralLeavesOtherEarNormal()
        {
            var p = HearingProfile.For(Ear.Left, HearingLossLevel.Mild);
            CollectionAssert.AreEqual(HearingPresets.Mild, p.Left);
            CollectionAssert.AreEqual(new int[10], p.Right);
            Assert.IsFalse(p.IsNormal(Ear.Left));
            Assert.IsTrue(p.IsNormal(Ear.Right));
            var both = HearingProfile.For(Ear.Both, HearingLossLevel.Severe);
            CollectionAssert.AreEqual(both.Left, both.Right);
        }

        [Test]
        public void NoteRoundTrips()
        {
            var p = new HearingProfile(HearingPresets.Moderate, HearingPresets.Mild);
            Assert.AreEqual("L=0,0,-10,-10,-15,-20,-20,-30,-20,-10;R=0,0,0,0,-5,-10,-10,-20,-10,0", p.Note());
            Assert.AreEqual(p, HearingProfile.Parse(p.Note()));
            Assert.AreEqual(HearingProfile.For(Ear.Both, HearingLossLevel.Mild), HearingProfile.Parse("0,0,0,0,-5,-10,-10,-20,-10,0"));
            Assert.AreEqual(HearingProfile.Normal, HearingProfile.Parse(""));
        }

        [Test]
        public void RejectsOutOfRangeOrWrongLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HearingProfile(new[] { -81, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new int[10]));
            Assert.Throws<ArgumentException>(() => new HearingProfile(new int[9], new int[10]));
            Assert.Throws<FormatException>(() => HearingProfile.Parse("L=1,2"));
        }
    }

    public class FilterTests
    {
        const double Fs = 48000;

        // Steady-state level change (dB) of a sine at f through the bank: 2 s settle, 1 s measure.
        static double SineGainDb(EarFilterBank bank, double f)
        {
            int settle = (int)(Fs * 2), measure = (int)Fs;
            var buf = new float[1024];
            double inSum = 0, outSum = 0; long n = 0;
            bank.Reset();
            for (long done = 0; done < settle + measure; done += buf.Length)
            {
                for (int i = 0; i < buf.Length; i++) buf[i] = (float)(0.5 * Math.Sin(2 * Math.PI * f * (done + i) / Fs));
                if (done >= settle) foreach (var x in buf) inSum += x * x;
                bank.Process(buf, 1, 0);
                if (done >= settle) { foreach (var y in buf) outSum += y * y; n += buf.Length; }
            }
            return 10 * Math.Log10(outSum / inSum);
        }

        [Test]
        public void QTableIsEffectiveOriginal()
        {
            var bank = new EarFilterBank(Fs);
            Assert.AreEqual(24, bank.Q(0));
            for (int i = 1; i < 10; i++) Assert.AreEqual(12, bank.Q(i));
        }

        [Test]
        public void SingleBandGainAtItsCenter([Values(0, 3, 7, 9)] int band, [Values(-20, -40, 12)] int gain)
        {
            var g = new int[10];
            g[band] = gain;
            var bank = new EarFilterBank(Fs);
            bank.SetGains(g);
            Assert.AreEqual(gain, bank.Band(band).ResponseDb(ANSIS344.BandHz[band], Fs), 1e-6, "analytic");
            Assert.AreEqual(gain, SineGainDb(bank, ANSIS344.BandHz[band]), 0.1, "simulated");
        }

        [Test]
        public void SimulatedSineMatchesAnalyticResponse_SeverePreset([Values(1000, 1250, 4000, 5000, 7000)] int hz)
        {
            var bank = new EarFilterBank(Fs);
            bank.SetGains(HearingPresets.Severe);
            Assert.AreEqual(bank.ResponseDb(hz), SineGainDb(bank, hz), 0.1);
        }

        [Test]
        public void MidpointIsMuchLessAffectedThanCenter_ShallowCut()
        {
            // Q = 12 narrowness holds for shallow cuts; deep cuts widen (cookbook A·Q), see README Limits.
            var g = new int[10]; g[3] = -6; g[4] = -6;  // 1 kHz and 1.5 kHz
            var bank = new EarFilterBank(Fs);
            bank.SetGains(g);
            Assert.That(bank.ResponseDb(1250), Is.GreaterThan(-1.5));
            Assert.AreEqual(-6, bank.ResponseDb(1000), 0.3);
        }

        [Test]
        public void ZeroGainsAreBitExactPassThrough()
        {
            var bank = new EarFilterBank(Fs);
            bank.SetGains(HearingPresets.Mild);
            bank.SetGains(HearingPresets.None);
            var buf = new float[256];
            var rnd = new Random(1);
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)(rnd.NextDouble() * 2 - 1);
            var copy = (float[])buf.Clone();
            bank.Process(buf, 1, 0);
            CollectionAssert.AreEqual(copy, buf);
            Assert.IsTrue(bank.IsIdentity);
        }

        [Test]
        public void BankFiltersOnlyItsChannel()
        {
            var left = new EarFilterBank(Fs);
            left.SetGains(HearingPresets.Severe);
            var buf = new float[2048];
            for (int f = 0; f < 1024; f++) { float s = (float)Math.Sin(2 * Math.PI * 4000 * f / Fs); buf[2 * f] = s; buf[2 * f + 1] = s; }
            var before = (float[])buf.Clone();
            left.Process(buf, 2, 0);
            for (int f = 0; f < 1024; f++) Assert.AreEqual(before[2 * f + 1], buf[2 * f + 1]);
            double l = 0, r = 0;
            for (int f = 512; f < 1024; f++) { l += buf[2 * f] * buf[2 * f]; r += buf[2 * f + 1] * buf[2 * f + 1]; }
            Assert.Less(l, r * 0.1);
        }

        [Test]
        public void SampleRateChangeKeepsCenterGain()
        {
            var g = new int[10]; g[9] = -30;
            var bank = new EarFilterBank(44100);
            bank.SetGains(g);
            bank.SetSampleRate(Fs);
            Assert.AreEqual(-30, bank.Band(9).ResponseDb(8000, Fs), 1e-6);
        }
    }

    public class ToneGeneratorTests
    {
        [Test]
        public void LeftToneOnlyOnLeftChannel_FadesAndStops()
        {
            var t = new ToneGenerator();
            t.Start(Ear.Left, 1000, -20, 0.05, 48000);
            var buf = new float[48000 / 10 * 2];  // 0.1 s stereo
            t.Mix(buf, 2);
            double peak = 0;
            for (int f = 0; f < buf.Length / 2; f++)
            {
                Assert.AreEqual(0f, buf[2 * f + 1]);
                peak = Math.Max(peak, Math.Abs(buf[2 * f]));
            }
            Assert.AreEqual(0.1, peak, 0.002);
            Assert.AreEqual(0f, buf[0]);
            Assert.AreEqual(0f, buf[2 * 2400]); // after 0.05 s
            Assert.IsFalse(t.Playing);
        }

        [Test]
        public void RightToneOnRightChannel()
        {
            var t = new ToneGenerator();
            t.Start(Ear.Right, 500, -6, 1, 48000);
            var buf = new float[2000];
            t.Mix(buf, 2);
            double l = 0, r = 0;
            for (int f = 0; f < 1000; f++) { l += Math.Abs(buf[2 * f]); r += Math.Abs(buf[2 * f + 1]); }
            Assert.AreEqual(0, l);
            Assert.Greater(r, 10);
            Assert.IsTrue(t.Playing);
        }
    }
}
