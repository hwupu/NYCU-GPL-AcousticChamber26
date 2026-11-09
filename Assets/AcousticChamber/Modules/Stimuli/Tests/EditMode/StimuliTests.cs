using System;
using System.Collections.Generic;
using GPL.Core;
using NUnit.Framework;

namespace GPL.Stimuli.Tests
{
    public class FftTests
    {
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(5)] [TestCase(8)] [TestCase(12)] [TestCase(17)] [TestCase(64)] [TestCase(100)]
        public void Forward_MatchesNaiveDft(int n)
        {
            var rng = new Rng(n);
            var re = new double[n]; var im = new double[n];
            for (int i = 0; i < n; i++) { re[i] = rng.NextDouble() - 0.5; im[i] = rng.NextDouble() - 0.5; }
            var er = new double[n]; var ei = new double[n];
            for (int k = 0; k < n; k++)
                for (int t = 0; t < n; t++)
                {
                    double a = -2.0 * Math.PI * k * t / n;
                    er[k] += re[t] * Math.Cos(a) - im[t] * Math.Sin(a);
                    ei[k] += re[t] * Math.Sin(a) + im[t] * Math.Cos(a);
                }
            Fft.Forward(re, im);
            for (int k = 0; k < n; k++)
            {
                Assert.AreEqual(er[k], re[k], 1e-9, "re " + k);
                Assert.AreEqual(ei[k], im[k], 1e-9, "im " + k);
            }
        }

        [TestCase(1000)] [TestCase(1024)] [TestCase(11520)]
        public void Inverse_UndoesForward(int n)
        {
            var rng = new Rng(7);
            var re = new double[n]; var im = new double[n];
            for (int i = 0; i < n; i++) re[i] = rng.NextDouble() - 0.5;
            var orig = (double[])re.Clone();
            Fft.Forward(re, im);
            Fft.Inverse(re, im);
            for (int i = 0; i < n; i++) { Assert.AreEqual(orig[i], re[i], 1e-10); Assert.AreEqual(0.0, im[i], 1e-10); }
        }
    }

    public class SynthTests
    {
        const int Sr = 48000;

        static double ExcessKurtosis(float[] x)
        {
            double mean = 0; foreach (var v in x) mean += v; mean /= x.Length;
            double m2 = 0, m4 = 0;
            foreach (var v in x) { double d = v - mean; m2 += d * d; m4 += d * d * d * d; }
            m2 /= x.Length; m4 /= x.Length;
            return m4 / (m2 * m2) - 3.0;
        }

        /// <summary>Power of each DFT bin 0..n/2 of the whole buffer.</summary>
        static double[] Power(float[] x)
        {
            int n = x.Length;
            var re = new double[n]; var im = new double[n];
            for (int i = 0; i < n; i++) re[i] = x[i];
            Fft.Forward(re, im);
            var p = new double[n / 2 + 1];
            for (int k = 0; k < p.Length; k++) p[k] = re[k] * re[k] + im[k] * im[k];
            return p;
        }

        /// <summary>Power per hertz (dB) in [lo, hi), from <see cref="Power"/> of an n-sample buffer.</summary>
        static double BandDensityDb(double[] power, int n, int sr, double lo, double hi)
        {
            double binHz = (double)sr / n, sum = 0; int count = 0;
            for (int k = 1; k < power.Length; k++)
            {
                double f = k * binHz;
                if (f < lo || f >= hi) continue;
                sum += power[k]; count++;
            }
            return 10.0 * Math.Log10(sum / count);
        }

        /// <summary>Least-squares slope of density (dB) against octave number, over octave bands 125 Hz .. 16 kHz.</summary>
        static double SlopeDbPerOctave(double[] power, int n, int sr)
        {
            var xs = new List<double>(); var ys = new List<double>();
            for (double lo = 125; lo < 16000; lo *= 2) { xs.Add(Math.Log(lo, 2)); ys.Add(BandDensityDb(power, n, sr, lo, lo * 2)); }
            double mx = 0, my = 0; for (int i = 0; i < xs.Count; i++) { mx += xs[i]; my += ys[i]; }
            mx /= xs.Count; my /= ys.Count;
            double num = 0, den = 0;
            for (int i = 0; i < xs.Count; i++) { num += (xs[i] - mx) * (ys[i] - my); den += (xs[i] - mx) * (xs[i] - mx); }
            return num / den;
        }

        [Test]
        public void WhiteNoise_IsGaussianFlatAndOnLevel()
        {
            var x = Synth.Render(StimulusRecipe.WhiteNoise(4.0, seed: 1, rampS: 0));
            Assert.AreEqual(-26.0, Synth.RmsDbfs(x), 1e-4, "RMS");
            Assert.AreEqual(0.0, ExcessKurtosis(x), 0.05, "Gaussian: excess kurtosis near 0 (uniform would be -1.2)");
            Assert.AreEqual(0.0, SlopeDbPerOctave(Power(x), x.Length, Sr), 0.15, "flat spectrum");
        }

        [Test]
        public void PinkNoise_SlopesMinus3DbPerOctave_IsGaussian_AndIsBandLimited()
        {
            var x = Synth.Render(StimulusRecipe.PinkNoise(4.0, seed: 3, rampS: 0));
            Assert.AreEqual(-26.0, Synth.RmsDbfs(x), 1e-4, "RMS");
            var p = Power(x);
            Assert.AreEqual(-3.01, SlopeDbPerOctave(p, x.Length, Sr), 0.15, "pink slope");
            Assert.AreEqual(0.0, ExcessKurtosis(x), 0.05, "Gaussian");
            double inBand = BandDensityDb(p, x.Length, Sr, 20, 40), below = BandDensityDb(p, x.Length, Sr, 1, 19.9), above = BandDensityDb(p, x.Length, Sr, 20001, 24000);
            Assert.Less(below, inBand - 100, "nothing below 20 Hz (float rounding only)");
            Assert.Less(above, inBand - 100, "nothing above 20 kHz (float rounding only)");
        }

        [Test]
        public void ShapedNoise_LoopsWithoutASeam()
        {
            var x = Synth.Render(StimulusRecipe.PinkNoise(1.0, seed: 5, rampS: 0));
            double maxStep = 0;
            for (int i = 1; i < x.Length; i++) maxStep = Math.Max(maxStep, Math.Abs(x[i] - x[i - 1]));
            Assert.LessOrEqual(Math.Abs(x[0] - x[x.Length - 1]), maxStep, "the wrap-around step is an ordinary step");
        }

        [Test]
        public void SameSeed_SameSamples_OtherSeed_Different()
        {
            var a = Synth.Render(StimulusRecipe.WhiteNoise(0.2, seed: 9));
            var b = Synth.Render(StimulusRecipe.WhiteNoise(0.2, seed: 9));
            var c = Synth.Render(StimulusRecipe.WhiteNoise(0.2, seed: 10));
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a, c);
            CollectionAssert.AreEqual(Synth.Render(StimulusRecipe.PinkNoise(0.24, 3)), Synth.Render(StimulusRecipe.PinkNoise(0.24, 3)));
        }

        [Test]
        public void Tone_HasItsFrequencyAndLevel()
        {
            var x = Synth.Render(StimulusRecipe.Tone(1000, 0.020, rampS: 0));
            Assert.AreEqual(960, x.Length);
            Assert.AreEqual(-26.0, Synth.RmsDbfs(x), 1e-6);
            Assert.AreEqual(Math.Pow(10, -26.0 / 20) * Math.Sqrt(2), Max(x), 1e-6, "peak = RMS * sqrt 2 (whole cycles)");
            var re = new double[x.Length]; var im = new double[x.Length];
            for (int i = 0; i < x.Length; i++) re[i] = x[i];
            Fft.Forward(re, im);
            int peak = 1; double best = 0;
            for (int k = 1; k <= x.Length / 2; k++) { double p = re[k] * re[k] + im[k] * im[k]; if (p > best) { best = p; peak = k; } }
            Assert.AreEqual(20, peak, "1 kHz = bin 20 of a 20 ms buffer");
        }

        [Test]
        public void Tone_ExactPhaseOverLongClips()
        {
            var x = Synth.Sine(Sr * 10, 1000, Sr);
            Assert.AreEqual(0.0, x[Sr * 10 - 48], 1e-12, "a whole number of cycles later the phase is exactly 0 again");
        }

        static double Max(float[] x) { double m = 0; foreach (var v in x) m = Math.Max(m, Math.Abs(v)); return m; }

        [Test]
        public void RaisedCosineRamp_IsCosSquared_FirstAndLastSampleZero()
        {
            var x = new double[100];
            for (int i = 0; i < x.Length; i++) x[i] = 1;
            Synth.ApplyRamps(x, 10, RampShape.RaisedCosine);
            for (int i = 0; i < 10; i++)
            {
                double expected = Math.Pow(Math.Sin(Math.PI * i / 20.0), 2); // sin²(pi t/2) = (1 - cos pi t)/2
                Assert.AreEqual(expected, x[i], 1e-12);
                Assert.AreEqual(expected, x[99 - i], 1e-12);
            }
            Assert.AreEqual(0.0, x[0]); Assert.AreEqual(0.0, x[99]);
            Assert.AreEqual(1.0, x[50]);
        }

        [Test]
        public void LinearRamp_IsLinear()
        {
            var x = new double[40];
            for (int i = 0; i < x.Length; i++) x[i] = 1;
            Synth.ApplyRamps(x, 4, RampShape.Linear);
            CollectionAssert.AreEqual(new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }, new[] { x[0], x[1], x[2], x[3], x[4] });
        }

        [Test]
        public void LevelIsMeasuredBeforeTheRamps()
        {
            var ramped = Synth.Render(StimulusRecipe.WhiteNoise(1.0, seed: 2, rampS: 0.1));
            var flat = Synth.Render(StimulusRecipe.WhiteNoise(1.0, seed: 2, rampS: 0));
            Assert.AreEqual(flat[24000], ramped[24000], 1e-7, "the steady part is the same");
            Assert.Less(Synth.RmsDbfs(ramped), -26.0, "the ramps lower the overall RMS");
        }

        [Test]
        public void Render_ThrowsInsteadOfClipping()
        {
            Assert.Throws<InvalidOperationException>(() => Synth.Render(StimulusRecipe.WhiteNoise(1.0, seed: 1, levelDbfs: -3)));
        }
    }

    public class RecipeTests
    {
        [Test]
        public void Note_And_Name()
        {
            var r = StimulusRecipe.PinkNoise(0.240, 3, rampS: 0.020);
            Assert.AreEqual("stimulus=noise color=pink dur_s=0.24 ramp_s=0.02 ramp=cos2 rms_dbfs=-26 seed=3 band_hz=20-20000 sr=48000", r.Note());
            Assert.AreEqual("noise_pink_240ms_r20_s3", r.Name);
            Assert.AreEqual("stimulus=tone hz=1000 dur_s=0.2 ramp_s=0.004 ramp=linear rms_dbfs=-26 sr=48000",
                            StimulusRecipe.Tone(1000, 0.2, 0.004, ramp: RampShape.Linear).Note());
            var w = StimulusRecipe.WhiteNoise(2, 1, 0.02);
            Assert.AreEqual("noise_white_2000ms_r20_s1", w.Name);
            Assert.AreEqual("stimulus=noise color=white dur_s=2 ramp_s=0.02 ramp=cos2 rms_dbfs=-26 seed=1 band_hz=full sr=48000", w.Note());
        }

        [Test]
        public void Invalid_Recipes_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StimulusRecipe.Tone(30000, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StimulusRecipe.Tone(1000, 0.01, rampS: 0.006));
            Assert.Throws<ArgumentOutOfRangeException>(() => StimulusRecipe.WhiteNoise(1, 1, levelDbfs: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StimulusRecipe.WhiteNoise(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StimulusRecipe.PinkNoise(1, 1, bandLowHz: 0));
        }

        [Test]
        public void StandardRecipes_AllRender_WithoutClipping()
        {
            foreach (var f in typeof(StandardRecipes).GetFields())
            {
                var r = (StimulusRecipe)f.GetValue(null);
                var t = System.Diagnostics.Stopwatch.StartNew();
                var x = Synth.Render(r);
                TestContext.WriteLine(f.Name + ": " + r.Note() + "  rendered in " + t.ElapsedMilliseconds + " ms (Editor)");
                Assert.AreEqual(r.Samples, x.Length, f.Name);
            }
        }

        [Test]
        public void WithSampleRate_CapsThePinkBand()
        {
            var r = StimulusRecipe.PinkNoise(1, 1).WithSampleRate(32000);
            Assert.AreEqual(16000, r.BandHighHz);
            Assert.AreEqual(32000, r.Samples);
        }
    }
}
