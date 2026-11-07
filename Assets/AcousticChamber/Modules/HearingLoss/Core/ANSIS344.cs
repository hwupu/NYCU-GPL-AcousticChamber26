using System;

namespace GPL.HearingLoss
{
    /// <summary>Hearing-loss modes of the NIOSH HLSim tool (values as in the original).</summary>
    public enum HearingLossLevel
    {
        None = 0, Mild = 1, Moderate = 2, Severe = 3, Predicted = 4, Manual = 5
    }

    /// <summary>Inputs of the ANSI S3.44 prediction. Defaults = the HLSim defaults.</summary>
    [Serializable]
    public sealed class PredictionParameters
    {
        public bool UseAge = true;
        public bool Male = true;
        public int Age = 55;            // 18..70
        public bool UseEffects = true;
        public int Years = 25;          // 0..50
        public int ExposureLevel = 95;  // 70..130 dB(A) (TWA)
        public double Fractile = 0.5;   // 0.1, 0.25, 0.5, 0.75, 0.9

        public void ResetToDefaults()
        {
            UseAge = true; Male = true; Age = 55;
            UseEffects = true; Years = 25; ExposureLevel = 95; Fractile = 0.5;
        }
    }

    /// <summary>
    /// ANSI S3.44 hearing-threshold prediction (age + noise exposure), float32-faithful port of HLSimWeb.Logic.ANSIS344.ApplyANSI
    /// (NIOSH HLSim v4.0.29.0), from docs/references/HLSIM_GUIDELINES.md section 5. Unofficial; not endorsed by NIOSH/CDC.
    /// The float casts mirror the original's narrowing points: do not "simplify" them, the 20 test vectors must match exactly.
    /// Known quirk kept on purpose (PO decision, open): Xi at 1500 Hz = 0.070 (section 5.4).
    /// </summary>
    public static class ANSIS344
    {
        public const int BandCount = 10;
        public const int MinBandDb = -80;
        public const int MaxBandDb = 12;

        public static readonly int[] BandHz = { 125, 250, 500, 1000, 1500, 2000, 3000, 4000, 6000, 8000 };

        const double K10 = 1.282;
        const double K25 = 0.675;

        // ---- noise-induced loss constants (float32 in the original) ----
        static readonly float[] L0 = { 93f, 93f, 93f, 89f, 84f, 80f, 77f, 75f, 77f, 77f };
        static readonly float[] U  = { -0.033f, -0.033f, -0.033f, -0.02f, -0.0325f, -0.045f, 0.012f, 0.025f, 0.019f, 0.019f };
        static readonly float[] V  = { 0.11f, 0.11f, 0.11f, 0.07f, 0.068f, 0.066f, 0.037f, 0.025f, 0.024f, 0.024f };
        static readonly float[] XU = { 0.044f, 0.044f, 0.044f, 0.022f, 0.0265f, 0.031f, 0.007f, 0.005f, 0.013f, 0.013f };
        static readonly float[] YU = { 0.016f, 0.016f, 0.016f, 0.016f, 0.007f, -0.002f, 0.016f, 0.009f, 0.008f, 0.008f };
        static readonly float[] XI = { 0.033f, 0.033f, 0.033f, 0.02f, 0.07f, 0.016f, 0.029f, 0.016f, 0.028f, 0.028f };
        static readonly float[] YI = { 0.002f, 0.002f, 0.002f, 0.0f, 0.0f, 0.0f, -0.01f, -0.002f, -0.007f, -0.007f };

        // ---- age-related loss constants: [band, sex]  sex 0 = male, 1 = female ----
        static readonly float[,] A = {
            {0.003f, 0.003f},   {0.003f, 0.003f},   {0.0035f, 0.0035f}, {0.004f, 0.004f},  {0.0055f, 0.005f},
            {0.007f, 0.006f},   {0.0115f, 0.0075f}, {0.016f, 0.009f},   {0.018f, 0.012f},  {0.022f, 0.015f} };
        static readonly float[,] BU = {
            {7.23f, 6.67f},  {6.67f, 6.12f},  {6.12f, 6.12f},  {6.12f, 6.12f},  {6.67f, 6.67f},
            {7.23f, 6.67f},  {7.78f, 7.23f},  {8.34f, 7.78f},  {9.45f, 8.9f},   {10.56f, 10.56f} };
        static readonly float[,] BI = {
            {5.78f, 5.34f},  {5.34f, 4.89f},  {4.89f, 4.89f},  {4.89f, 4.89f},  {5.34f, 5.34f},
            {5.78f, 5.34f},  {6.23f, 5.78f},  {6.67f, 6.23f},  {7.56f, 7.12f},  {8.45f, 8.45f} };

        public static int[] Apply(PredictionParameters p, bool clampGainToZero = false)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            return Apply(p.UseAge, p.Male, p.Age, p.UseEffects, p.Years, p.ExposureLevel, p.Fractile, clampGainToZero);
        }

        /// <returns>int[10] of dB HL band values (negative = loss), clamped to [-80, +12]. <paramref name="clampGainToZero"/>
        /// (default false = original) clamps positive values to 0 (an option for the 1500 Hz quirk; not a decision).</returns>
        public static int[] Apply(bool useAge, bool male, int age, bool useEffects,
                                  int years, int level, double fractile, bool clampGainToZero = false)
        {
            int sex = male ? 0 : 1;
            if (!useAge) age = 18;
            if (!useEffects) { years = 0; level = 85; }

            var n50 = new float[BandCount]; var n10 = new float[BandCount]; var n25 = new float[BandCount];
            var n75 = new float[BandCount]; var n90 = new float[BandCount];
            var z50 = new float[BandCount]; var z10 = new float[BandCount]; var z25 = new float[BandCount];
            var z75 = new float[BandCount]; var z90 = new float[BandCount];

            for (int i = 0; i < BandCount; i++)
            {
                // ---- age-related ("normal") ----
                double a = A[i, sex], bu = BU[i, sex], bi = BI[i, sex];
                double dAge = age - 18;
                n50[i] = (float)(a * Math.Pow(dAge, 2.0));
                n10[i] = (float)(n50[i] + K10 * (bu + 0.445 * n50[i]));
                n25[i] = (float)(n50[i] + K25 * (bu + 0.445 * n50[i]));
                n75[i] = (float)(n50[i] - K25 * (bi + 0.356 * n50[i]));
                n90[i] = (float)(n50[i] - K10 * (bi + 0.356 * n50[i]));

                // ---- noise-induced ----
                if (years > 0)
                {
                    float l1 = (float)Math.Log10(years);
                    float l2 = (float)(Math.Log10(years + 1) / Math.Log10(11.0));
                    float l0 = (float)level < L0[i] ? 0f : (float)level - L0[i];
                    double p = Math.Pow(l0, 2.0);

                    if (years <= 9) { float t = l2 * (U[i] + l1 * V[i]); z50[i] = (float)(t * p); }
                    else            { float t = U[i] + l1 * V[i];        z50[i] = (float)(t * p); }

                    float tu = XU[i] + YU[i] * l1;
                    float ti = XI[i] + YI[i] * l1;
                    float du = (float)(tu * p);
                    float di = (float)(ti * p);

                    z10[i] = (float)(z50[i] + K10 * du);
                    z25[i] = (float)(z50[i] + K25 * du);
                    z75[i] = (float)(z50[i] - K25 * di);
                    z90[i] = (float)(z50[i] - K10 * di);
                }
            }

            float[] nl, nz;
            if (Near(fractile, 0.5))       { nl = n50; nz = z50; }
            else if (Near(fractile, 0.1))  { nl = n10; nz = z10; }
            else if (Near(fractile, 0.9))  { nl = n90; nz = z90; }
            else if (Near(fractile, 0.25)) { nl = n25; nz = z25; }
            else                           { nl = n75; nz = z75; }   // 0.75

            var bands = new int[BandCount];
            for (int i = 0; i < BandCount; i++)
            {
                float loss;
                if (useAge && useEffects) loss = (nl[i] + nz[i]) - (nl[i] * nz[i]) / 120f;
                else if (useEffects)      loss = nz[i];
                else if (useAge)          loss = nl[i];
                else                      loss = 0f;

                float step = -1 * (int)Math.Round((double)loss);      // banker's rounding
                if (step < MinBandDb) step = MinBandDb;
                if (step > MaxBandDb) step = MaxBandDb;
                if (clampGainToZero && step > 0) step = 0;
                bands[i] = (int)Math.Round((double)step);
            }
            return bands;
        }

        static bool Near(double x, double y) => Math.Abs(x - y) < 1e-9;
    }

    /// <summary>Fixed band presets of Mild / Moderate / Severe (dB HL per band): a noise notch with its maximum at 4 kHz.</summary>
    public static class HearingPresets
    {
        //                                    125  250  500  1k   1.5k  2k   3k   4k   6k   8k
        static readonly int[] _none     = {  0,   0,   0,   0,    0,   0,   0,   0,   0,   0 };
        static readonly int[] _mild     = {  0,   0,   0,   0,   -5, -10, -10, -20, -10,   0 };
        static readonly int[] _moderate = {  0,   0, -10, -10,  -15, -20, -20, -30, -20, -10 };
        static readonly int[] _severe   = {  0,   0, -10, -20,  -25, -30, -30, -40, -30, -20 };

        public static int[] None => (int[])_none.Clone();
        public static int[] Mild => (int[])_mild.Clone();
        public static int[] Moderate => (int[])_moderate.Clone();
        public static int[] Severe => (int[])_severe.Clone();

        /// <summary>Copy of the preset bands of None / Mild / Moderate / Severe; other levels throw.</summary>
        public static int[] Of(HearingLossLevel level)
        {
            switch (level)
            {
                case HearingLossLevel.None: return None;
                case HearingLossLevel.Mild: return Mild;
                case HearingLossLevel.Moderate: return Moderate;
                case HearingLossLevel.Severe: return Severe;
                default: throw new ArgumentException($"{level} is not a fixed preset.", nameof(level));
            }
        }
    }
}
