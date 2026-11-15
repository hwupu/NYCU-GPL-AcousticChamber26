using System;

namespace GPL.Acoustics
{
    /// <summary>
    /// Octave bands of ISO 3382-2 (engineering method: 125 Hz to 4 kHz). Exact mid-band frequencies are the base-ten series of
    /// IEC 61260-1: fm = 1000 * 10^(3x/10) Hz; band edges fm * G^(-1/2) and fm * G^(1/2), G = 10^(3/10).
    /// </summary>
    public static class OctaveBands
    {
        /// <summary>Nominal mid-band frequencies (Hz) for the reports.</summary>
        public static readonly int[] Engineering = { 125, 250, 500, 1000, 2000, 4000 };

        public static readonly double G = Math.Pow(10.0, 0.3);

        /// <summary>Exact mid-band frequency (Hz) of a nominal octave (63 ... 16000).</summary>
        public static double Exact(int nominalHz)
        {
            int x = (int)Math.Round(10.0 * Math.Log10(nominalHz / 1000.0) / 3.0);
            return 1000.0 * Math.Pow(10.0, 3.0 * x / 10.0);
        }

        public static double Lower(int nominalHz) => Exact(nominalHz) / Math.Sqrt(G);
        public static double Upper(int nominalHz) => Exact(nominalHz) * Math.Sqrt(G);
    }

    /// <summary>
    /// Octave band-pass filter: 6th-order Butterworth (3rd-order prototype, band-pass transformed, bilinear transform with the band
    /// edges pre-warped), three biquad sections, unity gain at the exact mid-band frequency. Double precision, direct form II
    /// transposed. <see cref="ApplyTimeReversed"/> filters the reversed signal and reverses the result, so the filter's own ringing
    /// lies before the impulse instead of on the decay (needed for short reverberation times in the low bands).
    /// </summary>
    public sealed class OctaveFilter
    {
        public const int Order = 6;

        readonly double[] _b0, _b2, _a1, _a2;
        readonly double _gain;

        public int NominalHz { get; }
        public double SampleRate { get; }
        public double CenterHz { get; }
        public double LowerHz { get; }
        public double UpperHz { get; }

        public OctaveFilter(int nominalHz, double sampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentException("sample rate must be positive");
            NominalHz = nominalHz;
            SampleRate = sampleRate;
            CenterHz = OctaveBands.Exact(nominalHz);
            LowerHz = OctaveBands.Lower(nominalHz);
            UpperHz = OctaveBands.Upper(nominalHz);
            if (UpperHz >= sampleRate / 2) throw new ArgumentException("band " + nominalHz + " Hz is above the Nyquist frequency");

            double fs2 = 2.0 * sampleRate;
            double w1 = fs2 * Math.Tan(Math.PI * LowerHz / sampleRate);
            double w2 = fs2 * Math.Tan(Math.PI * UpperHz / sampleRate);
            double w0sq = w1 * w2, bw = w2 - w1;

            _b0 = new double[3]; _b2 = new double[3]; _a1 = new double[3]; _a2 = new double[3];
            int section = 0;
            const int n = 3;
            for (int k = 0; k < n; k++)
            {
                // left-half-plane Butterworth prototype pole
                double angle = Math.PI * (2.0 * k + n + 1) / (2.0 * n);
                var p = new C(Math.Cos(angle), Math.Sin(angle));
                // band-pass transform: s^2 - p*bw*s + w0^2 = 0
                var pb = p * bw;
                var root = C.Sqrt(pb * pb - new C(4.0 * w0sq, 0));
                foreach (var s in new[] { (pb + root) * 0.5, (pb - root) * 0.5 })
                {
                    if (s.Im <= 0) continue; // one of each conjugate pair; the section holds both
                    var z = (new C(1, 0) + s / fs2) / (new C(1, 0) - s / fs2);
                    if (section >= 3) throw new InvalidOperationException("filter design: unexpected pole count");
                    _b0[section] = 1.0;           // zeros at z = 1 and z = -1: (1 - z^-2)
                    _b2[section] = -1.0;
                    _a1[section] = -2.0 * z.Re;
                    _a2[section] = z.Re * z.Re + z.Im * z.Im;
                    section++;
                }
            }
            if (section != 3) throw new InvalidOperationException("filter design: expected 3 sections, got " + section);
            _gain = 1.0;
            _gain = 1.0 / Magnitude(CenterHz);
        }

        /// <summary>Magnitude response |H(f)| (linear).</summary>
        public double Magnitude(double hz)
        {
            double w = 2.0 * Math.PI * hz / SampleRate;
            var z1 = new C(Math.Cos(-w), Math.Sin(-w));
            var z2 = z1 * z1;
            var h = new C(_gain, 0);
            for (int i = 0; i < 3; i++)
            {
                var num = new C(_b0[i], 0) + z2 * _b2[i];
                var den = new C(1, 0) + z1 * _a1[i] + z2 * _a2[i];
                h = h * (num / den);
            }
            return h.Abs;
        }

        /// <summary>Response in dB at <paramref name="hz"/>.</summary>
        public double ResponseDb(double hz) => 20.0 * Math.Log10(Math.Max(1e-300, Magnitude(hz)));

        /// <summary>Filters <paramref name="input"/> forward in time into a new array.</summary>
        public double[] Apply(float[] input) => Run(input, false);

        /// <summary>Filters the time-reversed signal and reverses the result (zero filter ringing on the decay).</summary>
        public double[] ApplyTimeReversed(float[] input) => Run(input, true);

        double[] Run(float[] input, bool reversed)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            int len = input.Length;
            var y = new double[len];
            for (int i = 0; i < len; i++) y[i] = input[reversed ? len - 1 - i : i] * _gain;
            for (int s = 0; s < 3; s++)
            {
                double b0 = _b0[s], b2 = _b2[s], a1 = _a1[s], a2 = _a2[s], z1 = 0, z2 = 0;
                for (int i = 0; i < len; i++)
                {
                    double x = y[i];
                    double o = b0 * x + z1;
                    z1 = -a1 * o + z2;          // b1 = 0
                    z2 = b2 * x - a2 * o;
                    y[i] = o;
                }
            }
            if (reversed) Array.Reverse(y);
            return y;
        }

        /// <summary>Minimal complex number (keeps this assembly free of System.Numerics).</summary>
        readonly struct C
        {
            public readonly double Re, Im;
            public C(double re, double im) { Re = re; Im = im; }
            public double Abs => Math.Sqrt(Re * Re + Im * Im);
            public static C operator +(C a, C b) => new C(a.Re + b.Re, a.Im + b.Im);
            public static C operator -(C a, C b) => new C(a.Re - b.Re, a.Im - b.Im);
            public static C operator *(C a, C b) => new C(a.Re * b.Re - a.Im * b.Im, a.Re * b.Im + a.Im * b.Re);
            public static C operator *(C a, double b) => new C(a.Re * b, a.Im * b);
            public static C operator /(C a, double b) => new C(a.Re / b, a.Im / b);
            public static C operator /(C a, C b)
            {
                double d = b.Re * b.Re + b.Im * b.Im;
                return new C((a.Re * b.Re + a.Im * b.Im) / d, (a.Im * b.Re - a.Re * b.Im) / d);
            }
            public static C Sqrt(C a)
            {
                double r = Math.Sqrt(a.Abs);
                double t = Math.Atan2(a.Im, a.Re) / 2.0;
                return new C(r * Math.Cos(t), r * Math.Sin(t));
            }
        }
    }
}
