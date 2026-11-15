using System;
using System.Collections.Generic;

namespace GPL.Acoustics
{
    /// <summary>
    /// The RT60 measurement protocol of GPL.Acoustics (README "RT60 measurement protocol", ADR-0010). Values here are the defaults;
    /// a chamber may pass others in a <c>Rt60Plan</c> and the report records what was used.
    /// </summary>
    public static class Rt60Protocol
    {
        public const string Standard = "ISO 3382-2:2008 engineering method; integrated impulse response method (Schroeder backward integration)";
        public const string Excitation = "single-sample impulse (Dirac), amplitude 0.5, through a probe source with HRTF intensity 0 (omni-microphone substitute)";
        public const string Filters = "IEC 61260-1 octave bands (base ten), 6th-order Butterworth band-pass, bilinear with pre-warped edges, time-reversed filtering";
        public const string Evaluation = "onset -20 dB of peak; floor = mean of last 10 %; truncation at floor + 5 dB (10 ms smoothing); EDT 0..-10 dB, T20 -5..-25 dB, T30 -5..-35 dB; peak-to-noise >= 35 dB (T20) / 45 dB (T30)";
        public const string SingleNumber = "T_mid = mean of the T30 values at 500 Hz and 1 kHz (ear mean); the RT60 targets refer to T_mid";

        /// <summary>Source azimuths (deg; 0 = ahead, negative = left) relative to the head yaw at the start of a measurement.</summary>
        public static readonly float[] Azimuths = { 0f, -90f, 90f, 180f };
        public const float DistanceM = 2f;
        public const int Repeats = 3;

        /// <summary>One position for the calibration sweep and the block-start check (0 deg, 2 m).</summary>
        public static readonly float[] SingleAzimuth = { 0f };

        /// <summary>
        /// Uniform reflection coefficients of a calibration sweep: the HW2 table's 18 coefficients (0.50 ... 0.99) plus the extension
        /// points 0.20, 0.30, 0.40 for short times (below <see cref="CoreSweepFloor"/>; see <see cref="Rt60Calibration.FromSweepCsv"/>).
        /// </summary>
        public static readonly double[] SweepReflections =
            { 0.20, 0.30, 0.40, 0.50, 0.60, 0.65, 0.70, 0.75, 0.80, 0.85, 0.88, 0.90, 0.92, 0.94, 0.95, 0.96, 0.97, 0.975, 0.98, 0.985, 0.99 };

        /// <summary>
        /// Coefficients below this are extension points: at import they are dropped (and listed) when they have no valid T_mid or do
        /// not lie below the next point; from this coefficient up the times must rise or the import fails.
        /// </summary>
        public const double CoreSweepFloor = 0.50;

        /// <summary>Default shoebox for a chamber without walls (open space): 12 x 12 x 3.5 m (PO, 2026-11-15).</summary>
        public const float OpenSpaceWidth = 12f, OpenSpaceDepth = 12f, OpenSpaceHeight = 3.5f;

        /// <summary>Silence before the impulse: the previous sound must have decayed (s).</summary>
        public static double SettleSeconds(double expectedRt60) => Math.Max(1.0, 1.2 * expectedRt60);

        /// <summary>Recording after the impulse: well past 60 dB of decay, plus a tail for the noise floor (s).</summary>
        public static double RecordSeconds(double expectedRt60) => Math.Max(3.0, 1.6 * expectedRt60 + 0.5);

        /// <summary>T_mid of one set of band results (ear mean): mean of 500 Hz and 1 kHz; NaN unless both are valid.</summary>
        public static double Tmid(IReadOnlyList<BandDecay> bands, bool t30 = true)
        {
            double a = double.NaN, b = double.NaN;
            foreach (var d in bands)
            {
                double v = t30 ? (d.ValidT30 ? d.T30 : double.NaN) : (d.ValidT20 ? d.T20 : double.NaN);
                if (d.BandHz == 500) a = v;
                else if (d.BandHz == 1000) b = v;
            }
            return double.IsNaN(a) || double.IsNaN(b) ? double.NaN : (a + b) / 2.0;
        }

        /// <summary>Room text for reports: <c>12x12x3.5</c>.</summary>
        public static string RoomText(double w, double d, double h) =>
            GPL.Core.Fmt.F(w) + "x" + GPL.Core.Fmt.F(d) + "x" + GPL.Core.Fmt.F(h);

        public static bool TryParseRoom(string text, out double w, out double d, out double h)
        {
            w = d = h = 0;
            var p = (text ?? "").Split('x');
            return p.Length == 3 && GPL.Core.Fmt.TryDouble(p[0], out w) && GPL.Core.Fmt.TryDouble(p[1], out d) && GPL.Core.Fmt.TryDouble(p[2], out h);
        }
    }

    /// <summary>Analysis of one recorded impulse response (one channel): broadband plus each octave band.</summary>
    public static class ImpulseResponseAnalysis
    {
        /// <summary>Broadband first, then <paramref name="bands"/> (default ISO 3382-2 engineering, 125 Hz to 4 kHz).</summary>
        public static BandDecay[] Analyze(float[] channel, double sampleRate, int[] bands = null)
        {
            bands = bands ?? OctaveBands.Engineering;
            var result = new BandDecay[bands.Length + 1];
            result[0] = DecayAnalysis.AnalyzeBroadband(channel, sampleRate);
            for (int i = 0; i < bands.Length; i++) result[i + 1] = AnalyzeBand(channel, sampleRate, bands[i]);
            return result;
        }

        public static BandDecay AnalyzeBand(float[] channel, double sampleRate, int bandHz)
        {
            var filter = new OctaveFilter(bandHz, sampleRate);
            return DecayAnalysis.Analyze(filter.ApplyTimeReversed(channel), sampleRate, bandHz);
        }

        /// <summary>
        /// Ear mean of two band results: mean times when both ears are valid, else NaN; the smaller ratios and depths; flagged when
        /// either ear is flagged (notes of both ears).
        /// </summary>
        public static BandDecay Mean(BandDecay l, BandDecay r)
        {
            var m = new BandDecay
            {
                BandHz = l.BandHz,
                Edt = Avg(l.Edt, r.Edt), REdt = double.NaN, RT20 = double.NaN, RT30 = double.NaN,
                ValidT20 = l.ValidT20 && r.ValidT20, ValidT30 = l.ValidT30 && r.ValidT30, Flagged = l.Flagged || r.Flagged,
                PeakToNoiseDb = Math.Min(l.PeakToNoiseDb, r.PeakToNoiseDb), EdcDepthDb = Math.Min(l.EdcDepthDb, r.EdcDepthDb),
                Note = l.Note == r.Note ? l.Note : (l.Note + (l.Note != "" && r.Note != "" ? "; " : "") + r.Note)
            };
            m.T20 = m.ValidT20 ? (l.T20 + r.T20) / 2.0 : double.NaN;
            m.T30 = m.ValidT30 ? (l.T30 + r.T30) / 2.0 : double.NaN;
            m.CurvaturePct = m.ValidT20 && m.ValidT30 ? 100.0 * (m.T30 / m.T20 - 1.0) : double.NaN;
            return m;
        }

        static double Avg(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? double.NaN : (a + b) / 2.0;
    }
}
