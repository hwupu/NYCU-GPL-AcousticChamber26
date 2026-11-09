using System;
using GPL.Core;

namespace GPL.Stimuli
{
    /// <summary>
    /// Renders a <see cref="StimulusRecipe"/> to mono samples: generate, scale the steady signal to the RMS level, apply the ramps,
    /// check for clipping. Noise is Gaussian (Box-Muller on the GPL.Core <see cref="Rng"/>); pink and band-limited noise are shaped in
    /// the frequency domain (one inverse DFT of the clip's own length), so the result is periodic: a looped clip has no seam.
    /// Pure and deterministic for a given runtime (transcendental functions may differ in the last bit between CPUs).
    /// </summary>
    public static class Synth
    {
        /// <summary>Lowest RMS that counts as a signal (dBFS); <see cref="RmsDbfs"/> returns this for silence.</summary>
        public const double Silent = -200.0;

        public static float[] Render(StimulusRecipe r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            int n = r.Samples;
            double[] x;
            if (r.Kind == StimulusKind.Tone) x = Sine(n, r.FrequencyHz, r.SampleRate);
            else
            {
                var rng = new Rng(r.Seed);
                if (r.Color == NoiseColor.Pink) x = ShapedNoise(n, rng, r.SampleRate, r.BandLowHz, r.BandHighHz, 1.0);
                else if (r.Banded) x = ShapedNoise(n, rng, r.SampleRate, r.BandLowHz, r.BandHighHz, 0.0);
                else x = Gaussian(n, rng);
            }
            ScaleToRms(x, r.LevelDbfs);
            ApplyRamps(x, r.RampSamples, r.Ramp);

            var y = new float[n];
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(x[i]) > 1.0)
                    throw new InvalidOperationException("Stimulus clips at sample " + i + " (" + r.Note() + "): lower the level.");
                y[i] = (float)x[i];
            }
            return y;
        }

        /// <summary>Sine of amplitude 1, phase 0 at sample 0. Integer frequencies use exact integer phase (no drift over long clips).</summary>
        public static double[] Sine(int n, double hz, int sampleRate)
        {
            var x = new double[n];
            bool integral = hz == Math.Floor(hz) && hz < int.MaxValue;
            long h = (long)hz;
            for (int i = 0; i < n; i++)
            {
                double cycles = integral ? (double)(h * i % sampleRate) / sampleRate : hz * i / sampleRate;
                cycles -= Math.Floor(cycles);
                x[i] = Math.Sin(2.0 * Math.PI * cycles);
            }
            return x;
        }

        /// <summary>Independent standard normal samples (Box-Muller; both outputs used).</summary>
        public static double[] Gaussian(int n, Rng rng)
        {
            var x = new double[n];
            for (int i = 0; i < n; i += 2)
            {
                double u1 = 1.0 - rng.NextDouble(); // (0, 1], so the log is finite
                double u2 = rng.NextDouble();
                double radius = Math.Sqrt(-2.0 * Math.Log(u1));
                x[i] = radius * Math.Cos(2.0 * Math.PI * u2);
                if (i + 1 < n) x[i + 1] = radius * Math.Sin(2.0 * Math.PI * u2);
            }
            return x;
        }

        /// <summary>
        /// Gaussian noise with power spectral density proportional to f^(-<paramref name="exponent"/>) between the band edges and
        /// none outside (0 = white, 1 = pink). Each frequency bin gets an independent complex Gaussian scaled by f^(-exponent/2);
        /// the inverse DFT of length <paramref name="n"/> makes a real signal that repeats seamlessly every n samples.
        /// The band includes both edges; DC is always 0.
        /// </summary>
        public static double[] ShapedNoise(int n, Rng rng, int sampleRate, double lowHz, double highHz, double exponent)
        {
            var re = new double[n];
            var im = new double[n];
            int half = n / 2;
            double binHz = (double)sampleRate / n;
            for (int k = 1; k <= half; k++)
            {
                // draw for every bin, in order, so a changed band does not shift the random stream of the other bins
                double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
                double radius = Math.Sqrt(-2.0 * Math.Log(u1));
                double gr = radius * Math.Cos(2.0 * Math.PI * u2), gi = radius * Math.Sin(2.0 * Math.PI * u2);
                double f = k * binHz;
                if (f < lowHz || f > highHz) continue;
                double w = exponent == 0 ? 1.0 : Math.Pow(f, -exponent / 2.0);
                bool nyquistBin = n % 2 == 0 && k == half;
                re[k] = gr * w;
                im[k] = nyquistBin ? 0.0 : gi * w; // the Nyquist bin of an even length must be real
                if (!nyquistBin) { re[n - k] = re[k]; im[n - k] = -im[k]; }
            }
            Fft.Inverse(re, im);
            return re;
        }

        /// <summary>RMS in dBFS (full-scale sine = -3.01); <see cref="Silent"/> for silence or an empty buffer.</summary>
        public static double RmsDbfs(double[] x, int start = 0, int count = -1)
        {
            if (count < 0) count = x.Length - start;
            if (count <= 0) return Silent;
            double sum = 0;
            for (int i = start; i < start + count; i++) sum += x[i] * x[i];
            double ms = sum / count;
            return ms <= 0 ? Silent : Math.Max(Silent, 10.0 * Math.Log10(ms));
        }

        public static double RmsDbfs(float[] x, int start = 0, int count = -1)
        {
            if (count < 0) count = x.Length - start;
            if (count <= 0) return Silent;
            double sum = 0;
            for (int i = start; i < start + count; i++) sum += (double)x[i] * x[i];
            double ms = sum / count;
            return ms <= 0 ? Silent : Math.Max(Silent, 10.0 * Math.Log10(ms));
        }

        /// <summary>Scales the whole buffer so its RMS is <paramref name="dbfs"/>.</summary>
        public static void ScaleToRms(double[] x, double dbfs)
        {
            double current = RmsDbfs(x);
            if (current <= Silent) return;
            double g = Math.Pow(10.0, (dbfs - current) / 20.0);
            for (int i = 0; i < x.Length; i++) x[i] *= g;
        }

        /// <summary>Averages interleaved channels to mono (a mono buffer is copied).</summary>
        public static float[] MixToMono(float[] interleaved, int channels)
        {
            if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
            int frames = interleaved.Length / channels;
            var y = new float[frames];
            for (int f = 0; f < frames; f++)
            {
                double sum = 0;
                for (int c = 0; c < channels; c++) sum += interleaved[f * channels + c];
                y[f] = (float)(sum / channels);
            }
            return y;
        }

        /// <summary>
        /// Scales a recording in place so its RMS over the whole buffer is <paramref name="dbfs"/>; returns the gain in dB.
        /// Throws (and leaves the buffer unchanged) if the result would clip. Whole-buffer RMS counts pauses, so speech with long
        /// pauses comes out louder while talking than a steady noise at the same value.
        /// </summary>
        public static double LevelTo(float[] x, double dbfs)
        {
            double current = RmsDbfs(x);
            if (current <= Silent) throw new InvalidOperationException("The recording is silent.");
            double gainDb = dbfs - current, g = Math.Pow(10.0, gainDb / 20.0), peak = 0;
            foreach (var v in x) peak = Math.Max(peak, Math.Abs(v));
            if (peak * g > 1.0)
                throw new InvalidOperationException("Leveling to " + dbfs + " dBFS RMS would clip (peak " +
                                                    (20 * Math.Log10(peak * g)).ToString("0.0", Fmt.Inv) + " dBFS): choose a lower level.");
            for (int i = 0; i < x.Length; i++) x[i] = (float)(x[i] * g);
            return gainDb;
        }

        /// <summary>Ramp gain at fraction <paramref name="t"/> (0..1) of the ramp.</summary>
        public static double RampGain(double t, RampShape shape)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            return shape == RampShape.Linear ? t : (1.0 - Math.Cos(Math.PI * t)) / 2.0;
        }

        /// <summary>Fades in over the first and out over the last <paramref name="rampSamples"/> samples (first and last sample are 0).</summary>
        public static void ApplyRamps(double[] x, int rampSamples, RampShape shape)
        {
            if (rampSamples <= 0) return;
            int r = Math.Min(rampSamples, x.Length / 2);
            for (int i = 0; i < r; i++)
            {
                double g = RampGain((double)i / r, shape);
                x[i] *= g;
                x[x.Length - 1 - i] *= g;
            }
        }
    }
}
