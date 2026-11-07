using System;

namespace GPL.HearingLoss
{
    /// <summary>
    /// Peaking EQ biquad (Audio EQ Cookbook = Web Audio BiquadFilterNode "peaking"; Q linear). Transposed Direct Form II, one channel,
    /// double precision. Note: with the cookbook definition a cut of g dB has an effective bandwidth of A·Q in the denominator
    /// (A = 10^(g/40)), so deep cuts are much wider than "Q = 12" suggests (see README, Limits).
    /// </summary>
    public sealed class PeakingBiquad
    {
        double _b0 = 1, _b1, _b2, _a1, _a2;
        double _z1, _z2;

        public bool IsIdentity { get; private set; } = true;

        public void Set(double sampleRate, double f0, double q, double gainDb)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (gainDb == 0) { _b0 = 1; _b1 = _b2 = _a1 = _a2 = 0; IsIdentity = true; return; }
            double A = Math.Pow(10.0, gainDb / 40.0);
            double w0 = 2.0 * Math.PI * f0 / sampleRate;
            double cos = Math.Cos(w0);
            double alpha = Math.Sin(w0) / (2.0 * q);
            double a0 = 1.0 + alpha / A;
            _b0 = (1.0 + alpha * A) / a0;
            _b1 = (-2.0 * cos) / a0;
            _b2 = (1.0 - alpha * A) / a0;
            _a1 = (-2.0 * cos) / a0;
            _a2 = (1.0 - alpha / A) / a0;
            if (IsIdentity) Reset();   // state was not updated while bypassed
            IsIdentity = false;
        }

        public void Reset() { _z1 = _z2 = 0; }

        public double Process(double x)
        {
            double y = _b0 * x + _z1;
            _z1 = _b1 * x - _a1 * y + _z2;
            _z2 = _b2 * x - _a2 * y;
            return y;
        }

        /// <summary>Magnitude response in dB at <paramref name="f"/> Hz (analytic, for tests and plots).</summary>
        public double ResponseDb(double f, double sampleRate)
        {
            double w = 2.0 * Math.PI * f / sampleRate;
            double c1 = Math.Cos(w), s1 = -Math.Sin(w), c2 = Math.Cos(2 * w), s2 = -Math.Sin(2 * w);
            double nr = _b0 + _b1 * c1 + _b2 * c2, ni = _b1 * s1 + _b2 * s2;
            double dr = 1 + _a1 * c1 + _a2 * c2, di = _a1 * s1 + _a2 * s2;
            return 10.0 * Math.Log10((nr * nr + ni * ni) / (dr * dr + di * di));
        }
    }

    /// <summary>
    /// Ten peaking filters in series for one channel (index 0 = 125 Hz ... 9 = 8 kHz). Allocation-free after construction.
    /// A bank whose gains are all 0 passes the signal through unchanged (bit-exact).
    /// </summary>
    public sealed class EarFilterBank
    {
        /// <summary>Effective Q of the original app: 24 at 125 Hz, 12 elsewhere. The original intended 24 at 8 kHz as well but the
        /// assignment had no effect (HLSIM_GUIDELINES 7.2); PO decision 2026-11: copy the effective values, may revisit.</summary>
        public static readonly double[] DefaultQ = { 24, 12, 12, 12, 12, 12, 12, 12, 12, 12 };

        readonly PeakingBiquad[] _bands = new PeakingBiquad[ANSIS344.BandCount];
        readonly double[] _q = new double[ANSIS344.BandCount];
        readonly int[] _gainDb = new int[ANSIS344.BandCount];
        double _sampleRate;
        bool _identity = true;

        public EarFilterBank(double sampleRate, double[] q = null)
        {
            q = q ?? DefaultQ;
            if (q.Length != ANSIS344.BandCount) throw new ArgumentException($"Need {ANSIS344.BandCount} Q values.", nameof(q));
            Array.Copy(q, _q, _q.Length);
            for (int i = 0; i < _bands.Length; i++) _bands[i] = new PeakingBiquad();
            _sampleRate = sampleRate;
            Recompute();
        }

        public double SampleRate => _sampleRate;
        public bool IsIdentity => _identity;
        public double Q(int band) => _q[band];
        public int GainDb(int band) => _gainDb[band];
        public PeakingBiquad Band(int band) => _bands[band];

        /// <summary>New band gains (10 values, dB). Filter state is kept (no reset), like the original's instant update.</summary>
        public void SetGains(int[] gainDb)
        {
            if (gainDb == null || gainDb.Length != ANSIS344.BandCount) throw new ArgumentException($"Need {ANSIS344.BandCount} gains.", nameof(gainDb));
            Array.Copy(gainDb, _gainDb, _gainDb.Length);
            Recompute();
        }

        public void SetSampleRate(double sampleRate)
        {
            _sampleRate = sampleRate;
            Recompute();
            Reset();
        }

        public void Reset() { foreach (var b in _bands) b.Reset(); }

        /// <summary>Filters one channel of an interleaved buffer in place.</summary>
        public void Process(float[] data, int channels, int channel)
        {
            if (_identity) return;
            for (int n = channel; n < data.Length; n += channels)
            {
                double x = data[n];
                for (int b = 0; b < _bands.Length; b++)
                    if (!_bands[b].IsIdentity) x = _bands[b].Process(x);
                data[n] = (float)x;
            }
        }

        /// <summary>Analytic magnitude response of the whole bank in dB at <paramref name="f"/> Hz.</summary>
        public double ResponseDb(double f)
        {
            double sum = 0;
            foreach (var b in _bands) sum += b.ResponseDb(f, _sampleRate);
            return sum;
        }

        void Recompute()
        {
            bool identity = true;
            for (int i = 0; i < _bands.Length; i++)
            {
                _bands[i].Set(_sampleRate, ANSIS344.BandHz[i], _q[i], _gainDb[i]);
                if (_gainDb[i] != 0) identity = false;
            }
            if (identity && !_identity) Reset();
            _identity = identity;
        }
    }
}
