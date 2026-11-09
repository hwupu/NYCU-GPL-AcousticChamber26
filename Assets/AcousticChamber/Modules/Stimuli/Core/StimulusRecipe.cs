using System;
using System.Text;
using GPL.Core;

namespace GPL.Stimuli
{
    public enum StimulusKind { Tone, Noise }

    /// <summary>White = equal power per hertz (flat); pink = equal power per octave (-3 dB/octave).</summary>
    public enum NoiseColor { White, Pink }

    /// <summary>On/off ramp. RaisedCosine = (1 - cos(pi t))/2 (cos², ffmpeg <c>hsin</c> / <c>qsin2</c>); Linear = t (ffmpeg <c>tri</c>).</summary>
    public enum RampShape { RaisedCosine, Linear }

    /// <summary>
    /// What to synthesize: a tone or a noise, its duration, ramps, level and seed. Immutable; the same recipe gives the same samples
    /// (<see cref="Synth.Render"/>). Level = RMS in dBFS of the steady signal before the ramps. <see cref="Note"/> is the log form.
    /// </summary>
    public sealed class StimulusRecipe
    {
        public const int DefaultSampleRate = 48000;
        public const double DefaultLevelDbfs = -26.0;
        public const double DefaultRampS = 0.010;
        public const double PinkLowHz = 20.0, PinkHighHz = 20000.0;

        public StimulusKind Kind { get; }
        public NoiseColor Color { get; }
        /// <summary>Tone frequency (Hz); 0 for noise.</summary>
        public double FrequencyHz { get; }
        public double DurationS { get; }
        /// <summary>Length of each ramp (on and off), seconds; 0 = none (looped clips).</summary>
        public double RampS { get; }
        public RampShape Ramp { get; }
        /// <summary>RMS level of the steady signal before the ramps, dBFS.</summary>
        public double LevelDbfs { get; }
        /// <summary>Noise seed (same seed = the same token); ignored for tones.</summary>
        public int Seed { get; }
        public int SampleRate { get; }
        /// <summary>Pass band of the noise in Hz; both 0 = unfiltered (white only: flat from 0 Hz to Nyquist).</summary>
        public double BandLowHz { get; }
        public double BandHighHz { get; }

        StimulusRecipe(StimulusKind kind, NoiseColor color, double hz, double durationS, double rampS, RampShape ramp,
                       double levelDbfs, int seed, int sampleRate, double bandLowHz, double bandHighHz)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (!(durationS > 0)) throw new ArgumentOutOfRangeException(nameof(durationS), "duration must be > 0");
            if (rampS < 0 || 2 * rampS > durationS + 1e-12) throw new ArgumentOutOfRangeException(nameof(rampS), "two ramps must fit in the duration");
            if (double.IsNaN(levelDbfs) || levelDbfs > 0) throw new ArgumentOutOfRangeException(nameof(levelDbfs), "level must be <= 0 dBFS");
            double nyquist = sampleRate / 2.0;
            if (kind == StimulusKind.Tone && !(hz > 0 && hz < nyquist)) throw new ArgumentOutOfRangeException(nameof(hz), "tone must be between 0 and Nyquist");
            bool banded = bandLowHz > 0 || bandHighHz > 0;
            if (banded && !(bandLowHz >= 0 && bandHighHz > bandLowHz && bandHighHz <= nyquist))
                throw new ArgumentOutOfRangeException(nameof(bandHighHz), "band must satisfy 0 <= low < high <= Nyquist");
            if (kind == StimulusKind.Noise && color == NoiseColor.Pink && !(bandLowHz > 0))
                throw new ArgumentOutOfRangeException(nameof(bandLowHz), "pink noise needs a low band edge > 0 Hz");

            Kind = kind; Color = color; FrequencyHz = hz; DurationS = durationS; RampS = rampS; Ramp = ramp;
            LevelDbfs = levelDbfs; Seed = seed; SampleRate = sampleRate; BandLowHz = bandLowHz; BandHighHz = bandHighHz;
        }

        public static StimulusRecipe Tone(double hz, double durationS, double rampS = DefaultRampS, double levelDbfs = DefaultLevelDbfs,
                                          RampShape ramp = RampShape.RaisedCosine, int sampleRate = DefaultSampleRate) =>
            new StimulusRecipe(StimulusKind.Tone, NoiseColor.White, hz, durationS, rampS, ramp, levelDbfs, 0, sampleRate, 0, 0);

        /// <summary>Gaussian white noise; unfiltered unless a band is given.</summary>
        public static StimulusRecipe WhiteNoise(double durationS, int seed, double rampS = DefaultRampS, double levelDbfs = DefaultLevelDbfs,
                                                RampShape ramp = RampShape.RaisedCosine, int sampleRate = DefaultSampleRate,
                                                double bandLowHz = 0, double bandHighHz = 0) =>
            new StimulusRecipe(StimulusKind.Noise, NoiseColor.White, 0, durationS, rampS, ramp, levelDbfs, seed, sampleRate, bandLowHz, bandHighHz);

        /// <summary>Gaussian pink noise (exact -3 dB/octave between the band edges, default 20 Hz-20 kHz, capped at Nyquist).</summary>
        public static StimulusRecipe PinkNoise(double durationS, int seed, double rampS = DefaultRampS, double levelDbfs = DefaultLevelDbfs,
                                               RampShape ramp = RampShape.RaisedCosine, int sampleRate = DefaultSampleRate,
                                               double bandLowHz = PinkLowHz, double bandHighHz = PinkHighHz) =>
            new StimulusRecipe(StimulusKind.Noise, NoiseColor.Pink, 0, durationS, rampS, ramp, levelDbfs, seed, sampleRate,
                               bandLowHz, Math.Min(bandHighHz, sampleRate / 2.0));

        /// <summary>The same recipe at another sample rate (e.g. the device output rate); a pink band is capped at the new Nyquist.</summary>
        public StimulusRecipe WithSampleRate(int sampleRate) =>
            new StimulusRecipe(Kind, Color, FrequencyHz, DurationS, RampS, Ramp, LevelDbfs, Seed, sampleRate, BandLowHz,
                               BandHighHz > 0 ? Math.Min(BandHighHz, sampleRate / 2.0) : 0);

        /// <summary>The same recipe with another seed (a token bank: seeds 1..N).</summary>
        public StimulusRecipe WithSeed(int seed) =>
            new StimulusRecipe(Kind, Color, FrequencyHz, DurationS, RampS, Ramp, LevelDbfs, seed, SampleRate, BandLowHz, BandHighHz);

        public bool Banded => BandLowHz > 0 || BandHighHz > 0;
        public int Samples => Math.Max(1, (int)Math.Round(DurationS * SampleRate));
        public int RampSamples => (int)Math.Round(RampS * SampleRate);

        /// <summary>Short file-like name, e.g. <c>noise_pink_240ms_r20_s3</c>, <c>tone_1000hz_20ms_r10</c>.</summary>
        public string Name
        {
            get
            {
                var sb = new StringBuilder();
                if (Kind == StimulusKind.Tone) sb.Append("tone_").Append(Fmt.F(FrequencyHz)).Append("hz");
                else sb.Append("noise_").Append(Color == NoiseColor.Pink ? "pink" : "white");
                sb.Append('_').Append(Fmt.F(DurationS * 1000.0)).Append("ms_r").Append(Fmt.F(RampS * 1000.0));
                if (Ramp == RampShape.Linear) sb.Append("lin");
                if (Kind == StimulusKind.Noise) sb.Append("_s").Append(Fmt.I(Seed));
                return sb.ToString().Replace('.', 'p');
            }
        }

        /// <summary>
        /// Log form (key=value, invariant numbers), e.g.
        /// <c>stimulus=noise color=pink dur_s=0.24 ramp_s=0.02 ramp=cos2 rms_dbfs=-26 seed=3 band_hz=20-20000 sr=48000</c>.
        /// </summary>
        public string Note()
        {
            var sb = new StringBuilder();
            sb.Append("stimulus=").Append(Kind == StimulusKind.Tone ? "tone" : "noise");
            if (Kind == StimulusKind.Tone) sb.Append(" hz=").Append(N(FrequencyHz));
            else sb.Append(" color=").Append(Color == NoiseColor.Pink ? "pink" : "white");
            sb.Append(" dur_s=").Append(N(DurationS))
              .Append(" ramp_s=").Append(N(RampS))
              .Append(" ramp=").Append(Ramp == RampShape.Linear ? "linear" : "cos2")
              .Append(" rms_dbfs=").Append(N(LevelDbfs));
            if (Kind == StimulusKind.Noise)
            {
                sb.Append(" seed=").Append(Fmt.I(Seed));
                sb.Append(" band_hz=").Append(Banded ? N(BandLowHz) + "-" + N(BandHighHz) : "full");
            }
            sb.Append(" sr=").Append(Fmt.I(SampleRate));
            return sb.ToString();
        }

        public override string ToString() => Note();

        /// <summary>Invariant number with up to 6 decimals (2.5 ms ramps and -23.0103 dB levels stay exact enough to rebuild).</summary>
        static string N(double v) => v.ToString("0.######", Fmt.Inv);
    }
}
