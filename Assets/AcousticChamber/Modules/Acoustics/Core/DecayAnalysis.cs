using System;

namespace GPL.Acoustics
{
    /// <summary>
    /// Decay measures of one impulse response in one band (ISO 3382-2, integrated impulse response method). Times in seconds;
    /// NaN when not evaluable. <c>R*</c> = correlation coefficient of the line fit (closer to -1 = straighter decay).
    /// </summary>
    public struct BandDecay
    {
        /// <summary>Nominal band (Hz); 0 = broadband (unfiltered).</summary>
        public int BandHz;
        public double Edt, T20, T30;
        public double REdt, RT20, RT30;
        /// <summary>Curvature C = 100 (T30 / T20 - 1) %; NaN unless both are valid.</summary>
        public double CurvaturePct;
        /// <summary>Peak of the squared response above the noise floor (dB); 200 = no measurable floor.</summary>
        public double PeakToNoiseDb;
        /// <summary>Depth the Schroeder curve reaches before the truncation point (dB).</summary>
        public double EdcDepthDb;
        public bool ValidT20, ValidT30;
        /// <summary>
        /// Quality flag (values stay valid and are reported): curved decay, non-linear fit, or a decay too short for the octave
        /// filter. The reasons are in <see cref="Note"/>. See <see cref="DecayAnalysis.MaxCurvaturePct"/>.
        /// </summary>
        public bool Flagged;
        public string Note;

        public string Label => BandHz == 0 ? "broadband" : BandHz.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Schroeder backward integration and line fits (ISO 3382-2). Steps: onset = first sample within 20 dB of the peak; noise floor =
    /// mean energy of the last 10 % of the recording; truncation = where the 10 ms smoothed energy first reaches the floor + 5 dB (no
    /// truncation when the floor is below -150 dB of the peak); the floor is subtracted before integrating. Fits by least squares:
    /// EDT 0 to -10 dB, T20 -5 to -25 dB, T30 -5 to -35 dB, each extrapolated to 60 dB. T20 needs a peak-to-noise ratio of at least
    /// 35 dB and T30 of 45 dB (ISO 3382-2: the floor at least 10 dB below the lowest evaluation level).
    /// </summary>
    public static class DecayAnalysis
    {
        public const double MinPeakToNoiseT20 = 35.0;
        public const double MinPeakToNoiseT30 = 45.0;

        /// <summary>|C| above this (%) = curved decay: T20 and T30 disagree, the decay is not one exponential (ISO 3382-2 annex).</summary>
        public const double MaxCurvaturePct = 10.0;
        /// <summary>A T20 / T30 line fit with a correlation coefficient above this (closer to 0) is flagged as non-linear.</summary>
        public const double MaxFitCorrelation = -0.98;
        /// <summary>
        /// Shortest decay an octave filter can measure: B * T &gt; 4 with time-reversed filtering (B = band width in Hz; forward
        /// filtering needs B * T &gt; 16). Shorter values are the filter's ring-down, not the room.
        /// </summary>
        public const double MinBandwidthTimeProduct = 4.0;

        /// <summary>Shortest T (s) the octave band can measure; 0 for broadband.</summary>
        public static double FilterLimitSeconds(int bandHz) =>
            bandHz <= 0 ? 0.0 : MinBandwidthTimeProduct / (OctaveBands.Upper(bandHz) - OctaveBands.Lower(bandHz));

        /// <summary>Sets <see cref="BandDecay.Flagged"/> and appends the reasons to the note (curvature, fit, filter limit).</summary>
        public static void Flag(ref BandDecay r, bool checkFits = true)
        {
            var reasons = new System.Collections.Generic.List<string>();
            if (!double.IsNaN(r.CurvaturePct) && Math.Abs(r.CurvaturePct) > MaxCurvaturePct)
                reasons.Add("curved decay (C " + GPL.Core.Fmt.F(r.CurvaturePct, 1) + " %)");
            if (checkFits && ((r.ValidT20 && r.RT20 > MaxFitCorrelation) || (r.ValidT30 && r.RT30 > MaxFitCorrelation)))
                reasons.Add("non-linear fit (r > " + GPL.Core.Fmt.F(MaxFitCorrelation, 2) + ")");
            double limit = FilterLimitSeconds(r.BandHz);
            if (limit > 0 && ((r.ValidT20 && r.T20 < limit) || (r.ValidT30 && r.T30 < limit)))
                reasons.Add("below the filter limit (T < " + GPL.Core.Fmt.F(limit, 3) + " s): no measurable reverberation");
            if (reasons.Count == 0) return;
            r.Flagged = true;
            string text = string.Join("; ", reasons);
            r.Note = string.IsNullOrEmpty(r.Note) ? text : r.Note + "; " + text;
        }

        /// <summary>Analyzes a (band-filtered) pressure signal.</summary>
        public static BandDecay Analyze(double[] signal, double sampleRate, int bandHz)
        {
            var r = new BandDecay
            {
                BandHz = bandHz, Edt = double.NaN, T20 = double.NaN, T30 = double.NaN, REdt = double.NaN, RT20 = double.NaN,
                RT30 = double.NaN, CurvaturePct = double.NaN, PeakToNoiseDb = double.NaN, EdcDepthDb = double.NaN, Note = ""
            };
            if (signal == null || signal.Length < 1000 || sampleRate <= 0) { r.Note = "no data"; return r; }

            int len = signal.Length;
            var e = new double[len];
            double peak = 0; int peakAt = 0;
            for (int i = 0; i < len; i++)
            {
                e[i] = signal[i] * signal[i];
                if (e[i] > peak) { peak = e[i]; peakAt = i; }
            }
            if (peak <= 0) { r.Note = "silent"; return r; }

            int onset = peakAt;
            for (int i = 0; i <= peakAt; i++) if (e[i] >= peak * 0.01) { onset = i; break; }

            int tail = Math.Max(10, len / 10);
            double floor = 0;
            for (int i = len - tail; i < len; i++) floor += e[i];
            floor /= tail;

            // 10 ms moving average for the peak-to-noise ratio and the truncation point
            int win = Math.Max(1, (int)(0.01 * sampleRate));
            double smoothPeak = 0, acc = 0;
            var smooth = new double[len];
            for (int i = 0; i < len; i++)
            {
                acc += e[i];
                if (i >= win) acc -= e[i - win];
                smooth[i] = acc / Math.Min(i + 1, win);
                if (smooth[i] > smoothPeak) smoothPeak = smooth[i];
            }
            r.PeakToNoiseDb = floor <= smoothPeak * 1e-20 ? 200.0 : 10.0 * Math.Log10(smoothPeak / floor);

            int end = len;
            if (r.PeakToNoiseDb < 150.0)
            {
                double limit = floor * Math.Pow(10.0, 0.5); // floor + 5 dB
                int smoothPeakAt = onset;
                for (int i = onset; i < len; i++) if (smooth[i] >= smoothPeak) { smoothPeakAt = i; break; }
                for (int i = smoothPeakAt; i < len; i++) if (smooth[i] <= limit) { end = i; break; }
            }
            int count = end - onset;
            if (count < 100) { r.Note = "decay too short"; return r; }

            var db = new double[count];
            double sum = 0;
            var edc = new double[count];
            for (int i = count - 1; i >= 0; i--)
            {
                sum += Math.Max(0.0, e[onset + i] - floor);
                edc[i] = sum;
            }
            if (edc[0] <= 0) { r.Note = "silent after the floor"; return r; }
            double minDb = 0;
            for (int i = 0; i < count; i++)
            {
                db[i] = edc[i] <= 0 ? -400.0 : 10.0 * Math.Log10(edc[i] / edc[0]);
                if (edc[i] > 0 && db[i] < minDb) minDb = db[i];
            }
            r.EdcDepthDb = -minDb;

            Fit(db, 0.0, -10.0, sampleRate, out r.Edt, out r.REdt);
            Fit(db, -5.0, -25.0, sampleRate, out r.T20, out r.RT20);
            Fit(db, -5.0, -35.0, sampleRate, out r.T30, out r.RT30);

            r.ValidT20 = !double.IsNaN(r.T20) && r.PeakToNoiseDb >= MinPeakToNoiseT20;
            r.ValidT30 = !double.IsNaN(r.T30) && r.PeakToNoiseDb >= MinPeakToNoiseT30;
            if (!r.ValidT20) r.Note = double.IsNaN(r.T20) ? "decay does not reach -25 dB" : "peak-to-noise below 35 dB";
            else if (!r.ValidT30) r.Note = double.IsNaN(r.T30) ? "decay does not reach -35 dB" : "peak-to-noise below 45 dB";
            if (r.ValidT20 && r.ValidT30) r.CurvaturePct = 100.0 * (r.T30 / r.T20 - 1.0);
            Flag(ref r);
            return r;
        }

        /// <summary>Broadband (unfiltered) analysis of a recorded channel.</summary>
        public static BandDecay AnalyzeBroadband(float[] signal, double sampleRate)
        {
            if (signal == null) return Analyze(null, sampleRate, 0);
            var d = new double[signal.Length];
            for (int i = 0; i < d.Length; i++) d[i] = signal[i];
            return Analyze(d, sampleRate, 0);
        }

        /// <summary>Line from the first sample at or below <paramref name="fromDb"/> to the last above <paramref name="toDb"/>.</summary>
        static void Fit(double[] db, double fromDb, double toDb, double sampleRate, out double t60, out double corr)
        {
            t60 = double.NaN; corr = double.NaN;
            int a = -1, b = -1;
            for (int i = 0; i < db.Length; i++) if (db[i] <= fromDb) { a = i; break; }
            for (int i = 0; i < db.Length; i++) if (db[i] <= toDb) { b = i; break; }
            if (a < 0 || b < 0 || b <= a + 2) return;
            double n = b - a + 1, sx = 0, sy = 0, sxx = 0, sxy = 0, syy = 0;
            for (int i = a; i <= b; i++)
            {
                double t = i / sampleRate, y = db[i];
                sx += t; sy += y; sxx += t * t; sxy += t * y; syy += y * y;
            }
            double vx = n * sxx - sx * sx, vy = n * syy - sy * sy;
            if (vx <= 0) return;
            double slope = (n * sxy - sx * sy) / vx;
            if (slope >= 0) return;
            t60 = -60.0 / slope;
            corr = vy > 0 ? (n * sxy - sx * sy) / Math.Sqrt(vx * vy) : -1.0;
        }
    }
}
