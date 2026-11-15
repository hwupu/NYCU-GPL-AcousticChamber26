using System;
using System.Linq;
using NUnit.Framework;

namespace GPL.Acoustics.Tests
{
    /// <summary>ISO 3382-2 octave-band RT60 (PB-26): filters, decay analysis on synthetic responses, report, calibration import.</summary>
    public class Rt60IsoTests
    {
        const double Fs = 48000;

        // ---- octave filters ---------------------------------------------------------------------------------

        [Test]
        public void OctaveBands_ExactBaseTenFrequencies()
        {
            Assert.AreEqual(1000.0, OctaveBands.Exact(1000), 1e-9);
            Assert.AreEqual(125.89, OctaveBands.Exact(125), 0.01);
            Assert.AreEqual(3981.07, OctaveBands.Exact(4000), 0.01);
            Assert.AreEqual(707.95, OctaveBands.Lower(1000), 0.01);
            Assert.AreEqual(1412.54, OctaveBands.Upper(1000), 0.01);
        }

        [Test]
        public void OctaveFilter_ResponseMeetsTheBandShape([Values(125, 250, 500, 1000, 2000, 4000)] int band)
        {
            var f = new OctaveFilter(band, Fs);
            Assert.AreEqual(0.0, f.ResponseDb(f.CenterHz), 0.01, "mid-band gain");
            Assert.AreEqual(-3.01, f.ResponseDb(f.LowerHz), 0.05, "lower edge");
            Assert.AreEqual(-3.01, f.ResponseDb(f.UpperHz), 0.05, "upper edge");
            // IEC 61260-1 class 1 minimum attenuation one and two octaves from the mid-band frequency (17.5 dB, 42.5 dB)
            Assert.Less(f.ResponseDb(f.CenterHz * 2), -17.5, "one octave above");
            Assert.Less(f.ResponseDb(f.CenterHz / 2), -17.5, "one octave below");
            Assert.Less(f.ResponseDb(f.CenterHz * 4), -42.5, "two octaves above");
            Assert.Less(f.ResponseDb(f.CenterHz / 4), -42.5, "two octaves below");
        }

        [Test]
        public void OctaveFilter_PassesAMidBandSineAtUnityGain()
        {
            var f = new OctaveFilter(1000, Fs);
            var x = new float[48000];
            for (int i = 0; i < x.Length; i++) x[i] = (float)Math.Sin(2 * Math.PI * f.CenterHz * i / Fs);
            var y = f.Apply(x);
            double rx = 0, ry = 0;
            for (int i = 24000; i < 48000; i++) { rx += x[i] * x[i]; ry += y[i] * y[i]; }
            Assert.AreEqual(0.0, 10 * Math.Log10(ry / rx), 0.05);
        }

        [Test]
        public void OctaveFilter_RejectsBandsAboveNyquist() =>
            Assert.Throws<ArgumentException>(() => new OctaveFilter(16000, 22050));

        // ---- decay analysis ---------------------------------------------------------------------------------

        static float[] DecayingNoise(double rt60, double seconds, int seed, double floorDb = double.NegativeInfinity)
        {
            var rng = new Random(seed);
            int n = (int)(seconds * Fs);
            var x = new float[n];
            double k = 3.0 * Math.Log(10) / rt60; // amplitude decay: 60 dB energy over rt60
            double floor = double.IsNegativeInfinity(floorDb) ? 0 : Math.Pow(10, floorDb / 20.0);
            for (int i = 0; i < n; i++)
            {
                double g = Gauss(rng);
                x[i] = (float)(0.5 * g * Math.Exp(-k * i / Fs) + floor * Gauss(rng));
            }
            return x;
        }

        static double Gauss(Random r) => Math.Sqrt(-2 * Math.Log(1 - r.NextDouble())) * Math.Cos(2 * Math.PI * r.NextDouble());

        [Test]
        public void Analysis_RecoversTheDecayInEveryBand([Values(0.3, 0.7, 2.2)] double rt60)
        {
            // A single noise decay scatters (random-noise uncertainty, largest in the 125 Hz band: about +/- 10 % at 0.3 s);
            // the mean over twelve seeds must sit on the true value in every band (no bias from filters or evaluation).
            const int seeds = 12;
            var t30 = new double[7]; var t20 = new double[7]; var edt = new double[7]; double tmid = 0;
            for (int s = 1; s <= seeds; s++)
            {
                var bands = ImpulseResponseAnalysis.Analyze(DecayingNoise(rt60, Rt60Protocol.RecordSeconds(rt60), s), Fs);
                Assert.AreEqual(7, bands.Length);
                Assert.AreEqual(0, bands[0].BandHz);
                for (int i = 0; i < bands.Length; i++)
                {
                    var b = bands[i];
                    Assert.IsTrue(b.ValidT20 && b.ValidT30, b.Label + ": " + b.Note);
                    Assert.Less(b.RT30, b.BandHz == 125 || b.BandHz == 250 ? -0.95 : -0.98, b.Label + " r");
                    t30[i] += b.T30 / seeds; t20[i] += b.T20 / seeds; edt[i] += b.Edt / seeds;
                }
                tmid += Rt60Protocol.Tmid(bands) / seeds;
            }
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(rt60, t30[i], rt60 * 0.05, "band " + i + " T30");
                if (i == 0 || i >= 3) Assert.AreEqual(rt60, t20[i], rt60 * 0.08, "band " + i + " T20"); // T20 at 125 / 250 Hz scatters more (shorter fit range)
                Assert.AreEqual(rt60, edt[i], rt60 * 0.15, "band " + i + " EDT");
            }
            Assert.AreEqual(rt60, tmid, rt60 * 0.03, "T_mid");
        }

        [Test]
        public void Analysis_SeparatesBandsWithDifferentDecays()
        {
            int n = (int)(3 * Fs);
            var x = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = i / Fs;
                x[i] = (float)(0.4 * Math.Sin(2 * Math.PI * 1000 * t) * Math.Exp(-6.9078 * t / 1.0) +
                               0.4 * Math.Sin(2 * Math.PI * 250 * t) * Math.Exp(-6.9078 * t / 0.5));
            }
            Assert.AreEqual(1.0, ImpulseResponseAnalysis.AnalyzeBand(x, Fs, 1000).T30, 0.03);
            Assert.AreEqual(0.5, ImpulseResponseAnalysis.AnalyzeBand(x, Fs, 250).T30, 0.03);
        }

        [Test]
        public void Analysis_FlagsTooLittleDynamicRange()
        {
            var x = DecayingNoise(0.7, 3.0, 3, floorDb: -46); // about 40 dB below the peak energy
            var b = DecayAnalysis.AnalyzeBroadband(x, Fs);
            Assert.IsTrue(b.ValidT20, b.Note);
            Assert.IsFalse(b.ValidT30);
            Assert.Less(b.PeakToNoiseDb, DecayAnalysis.MinPeakToNoiseT30);
            Assert.IsNaN(b.CurvaturePct);
        }

        [Test]
        public void Analysis_HandlesSilenceAndShortInput()
        {
            Assert.IsFalse(DecayAnalysis.AnalyzeBroadband(new float[48000], Fs).ValidT20);
            Assert.IsFalse(DecayAnalysis.AnalyzeBroadband(new float[10], Fs).ValidT20);
            Assert.IsFalse(DecayAnalysis.AnalyzeBroadband(null, Fs).ValidT20);
        }

        [Test]
        public void EarMean_NeedsBothEars()
        {
            var l = new BandDecay { BandHz = 500, T20 = 1.0, T30 = 1.1, ValidT20 = true, ValidT30 = true, Edt = 1, Note = "" };
            var r = new BandDecay { BandHz = 500, T20 = 1.2, T30 = 1.3, ValidT20 = true, ValidT30 = false, Edt = 1, Note = "x" };
            var m = ImpulseResponseAnalysis.Mean(l, r);
            Assert.AreEqual(1.1, m.T20, 1e-12);
            Assert.IsNaN(m.T30);
            Assert.IsFalse(m.ValidT30);
        }

        // ---- report and calibration import ------------------------------------------------------------------

        static BandDecay[] Flat(double t) =>
            new[] { 0, 125, 250, 500, 1000, 2000, 4000 }.Select(b => new BandDecay
            {
                BandHz = b, Edt = t, T20 = t, T30 = t, REdt = -1, RT20 = -1, RT30 = -1, ValidT20 = true, ValidT30 = true,
                CurvaturePct = 0, PeakToNoiseDb = 200, EdcDepthDb = 80, Note = ""
            }).ToArray();

        [Test]
        public void Report_CsvHasOneLinePerEarAndBand_SummaryHasTmid()
        {
            var rep = new Rt60Report("id1", Rt60Report.KindMeasurement, "HiddenSource", "Android", 12, 12, 3.5, openSpace: true);
            rep.AddMeta("unity", "6000.6.6f1");
            rep.Add("Room", 0.7, 0.69, 1, 0, 2, 1, Flat(0.70), Flat(0.72));
            rep.Add("Room", 0.7, 0.69, 2, -90, 2, 1, Flat(0.68), Flat(0.70));
            var lines = rep.Csv().TrimEnd('\n').Split('\n');
            Assert.AreEqual(1 + 2 * 7 * 3, lines.Length);
            Assert.AreEqual(Rt60Report.Columns.Length, GPL.Core.Csv.SplitLine(lines[1]).Count);
            StringAssert.Contains("open_space_default", lines[1]);
            StringAssert.Contains("12x12x3.5", lines[1]);
            var stats = rep.TmidStats("Room");
            Assert.AreEqual(2, stats.N);
            Assert.AreEqual(0.70, stats.Mean, 1e-9);
            var text = rep.Text();
            StringAssert.Contains("ISO 3382-2", text);
            StringAssert.Contains("open_space_default: the chamber has no walls", text);
            StringAssert.Contains("unity", text);
            StringAssert.Contains("T_mid (T30)  0.7 +/-", text);
        }

        [Test]
        public void Report_FileStem()
        {
            Assert.AreEqual("RT60_Hidden_Source_20261115-142301",
                Rt60Report.FileStem("Hidden Source", new DateTime(2026, 11, 15, 14, 23, 1)));
        }

        static string Sweep(params (double refl, double t)[] points)
        {
            var rep = new Rt60Report("sw", Rt60Report.KindSweep, "MovingSource", "Android", 12, 12, 3.5, openSpace: true);
            foreach (var p in points)
                for (int rep_ = 1; rep_ <= 2; rep_++)
                    rep.Add("r=" + p.refl, double.NaN, p.refl, 1, 0, 2, rep_, Flat(p.t), Flat(p.t + 0.02));
            return rep.Csv();
        }

        [Test]
        public void Calibration_FromSweepCsv_BuildsAnIsoTable()
        {
            var cal = Rt60Calibration.FromSweepCsv(Sweep((0.5, 0.25), (0.8, 0.6), (0.95, 2.0)), "test.csv");
            Assert.AreEqual(3, cal.Count);
            Assert.AreEqual(0.26, cal.Rt60At(0), 1e-9); // ear mean of 0.25 and 0.27
            Assert.AreEqual(Rt60Calibration.ProtocolIso, cal.Protocol);
            Assert.IsTrue(cal.CalibratedFor("Android", 12, 12, 3.5));
            Assert.IsFalse(cal.CalibratedFor("Windows", 12, 12, 3.5));
            Assert.IsFalse(cal.CalibratedFor("Android", 8.72, 15, 3.5));
            double vs = RoomMath.VolumeOverSurface(12, 12, 3.5);
            Assert.AreEqual(0.8, cal.ReflectionForRt60(0.61, vs), 1e-9); // table point: ear mean of 0.60 and 0.62
        }

        [Test]
        public void Calibration_FromSweepCsv_RejectsNonMonotonicOrWrongKind()
        {
            Assert.Throws<FormatException>(() => Rt60Calibration.FromSweepCsv(Sweep((0.5, 0.5), (0.8, 0.4)), "x"));
            var m = new Rt60Report("m", Rt60Report.KindMeasurement, "C", "Android", 12, 12, 3.5, true);
            m.Add("Room", 0.7, 0.7, 1, 0, 2, 1, Flat(0.7), Flat(0.7));
            Assert.Throws<FormatException>(() => Rt60Calibration.FromSweepCsv(m.Csv(), "x"));
        }

        [Test]
        public void Calibration_FromSweepCsv_DropsOnlyBadExtensionPoints()
        {
            // ear mean = t + 0.01: 0.2 -> 0.25 (not below 0.3's 0.21: dropped), 0.3 -> 0.21, 0.4 -> 0.23, 0.5 -> 0.26
            var cal = Rt60Calibration.FromSweepCsv(Sweep((0.2, 0.24), (0.3, 0.20), (0.4, 0.22), (0.5, 0.25), (0.8, 0.6)), "x");
            Assert.AreEqual(4, cal.Count);
            Assert.AreEqual(0.3, cal.ReflectionAt(0), 1e-12);
            Assert.AreEqual(1, cal.DroppedPoints.Count);
            StringAssert.StartsWith("0.2 ", cal.DroppedPoints[0]);
            // a fault in the core range (>= 0.5) still fails the import
            Assert.Throws<FormatException>(() => Rt60Calibration.FromSweepCsv(Sweep((0.3, 0.2), (0.5, 0.5), (0.8, 0.4)), "x"));
            Assert.AreEqual(21, Rt60Protocol.SweepReflections.Length);
            Assert.AreEqual(0.2, Rt60Protocol.SweepReflections[0], 1e-12);
        }

        // ---- quality flags ------------------------------------------------------------------------------------

        [Test]
        public void Flag_CurvatureFitAndFilterLimit()
        {
            var clean = new BandDecay { BandHz = 500, T20 = 1.0, T30 = 1.05, RT20 = -0.999, RT30 = -0.999, ValidT20 = true, ValidT30 = true, CurvaturePct = 5, Note = "" };
            DecayAnalysis.Flag(ref clean);
            Assert.IsFalse(clean.Flagged, clean.Note);

            var curved = clean; curved.T30 = 0.8; curved.CurvaturePct = -20;
            DecayAnalysis.Flag(ref curved);
            Assert.IsTrue(curved.Flagged);
            StringAssert.Contains("curved decay", curved.Note);

            var bent = clean; bent.RT30 = -0.95;
            DecayAnalysis.Flag(ref bent);
            StringAssert.Contains("non-linear fit", bent.Note);

            // 125 Hz octave: B = 88.4 Hz, limit 4 / B = 0.045 s
            Assert.AreEqual(4.0 / (OctaveBands.Upper(125) - OctaveBands.Lower(125)), DecayAnalysis.FilterLimitSeconds(125), 1e-12);
            Assert.AreEqual(0.0, DecayAnalysis.FilterLimitSeconds(0));
            var dry = new BandDecay { BandHz = 125, T20 = 0.02, T30 = 0.02, RT20 = -0.999, RT30 = -0.999, ValidT20 = true, ValidT30 = true, CurvaturePct = 0, Note = "" };
            DecayAnalysis.Flag(ref dry);
            StringAssert.Contains("filter limit", dry.Note);
        }

        [Test]
        public void Flag_CleanExponentialDecayIsNotFlagged()
        {
            var b = DecayAnalysis.AnalyzeBroadband(DecayingNoise(0.7, 3.0, 1), Fs);
            Assert.IsFalse(b.Flagged, b.Note);
        }

        [Test]
        public void Report_ListsFlaggedConditionsAndDryRooms()
        {
            BandDecay[] Curved(double t) => Flat(t).Select(b => { b.T30 = t * 0.7; b.CurvaturePct = -30; DecayAnalysis.Flag(ref b); return b; }).ToArray();
            var rep = new Rt60Report("q", Rt60Report.KindMeasurement, "C", "Android", 12, 12, 3.5, true);
            rep.Add("quiet", 0.3, 0.5, 1, 0, 2, 1, Curved(0.4), Curved(0.4));
            rep.Add("quiet", 0.3, 0.5, 2, 90, 2, 1, Flat(0.3), Flat(0.3));
            rep.Add("Dry", double.NaN, 0.0, 1, 0, 2, 1, Flat(0.02), Flat(0.02));
            rep.Add("Room", 0.7, 0.78, 1, 0, 2, 1, Flat(0.7), Flat(0.7));
            Assert.AreEqual(1, rep.TmidFlaggedCount("quiet", out int n));
            Assert.AreEqual(2, n);
            var problems = rep.QualityProblems();
            Assert.AreEqual(2, problems.Count);
            StringAssert.StartsWith("quiet: T_mid flagged in 1 of 2", problems[0]);
            StringAssert.StartsWith("Dry: reflection 0", problems[1]);
            var text = rep.Text();
            StringAssert.Contains("PROBLEMS", text);
            StringAssert.Contains("flagged      1 of 2 recordings", text);
            Assert.AreEqual(0, rep.TmidFlaggedCount("Room", out _));
        }

        [Test]
        public void Calibration_Hw2TableIsNotCalibrated()
        {
            var hw2 = Rt60Calibration.MetaXrAudioShoebox;
            Assert.AreEqual(Rt60Calibration.ProtocolHw2, hw2.Protocol);
            Assert.IsFalse(hw2.CalibratedFor("Windows", 8.72, 15, 3.5));
        }

        [Test]
        public void Protocol_RecordingCoversTheDecay()
        {
            foreach (double t in new[] { 0.3, 0.7, 2.2, 5.74 })
                Assert.GreaterOrEqual(Rt60Protocol.RecordSeconds(t), 1.5 * t);
            Assert.IsTrue(Rt60Protocol.TryParseRoom("12x12x3.5", out double w, out double d, out double h));
            Assert.AreEqual(3.5, h, 1e-12);
        }
    }
}
