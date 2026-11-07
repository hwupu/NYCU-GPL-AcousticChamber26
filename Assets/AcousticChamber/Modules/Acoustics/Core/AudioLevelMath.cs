using System;

namespace GPL.Acoustics
{
    /// <summary>Software levels in dBFS (0 dBFS = RMS of 1.0). These are not sound pressure: that depends on the headset volume.</summary>
    public static class AudioLevelMath
    {
        /// <summary>Level used for silence or "no source".</summary>
        public const float Silent = -200f;

        public static float RmsDbfs(float[] samples)
        {
            if (samples == null || samples.Length == 0) return Silent;
            double sum = 0;
            for (int i = 0; i < samples.Length; i++) sum += (double)samples[i] * samples[i];
            double rms = Math.Sqrt(sum / samples.Length);
            return rms <= 1e-10 ? Silent : (float)(20.0 * Math.Log10(rms));
        }

        /// <summary>Combined level of uncorrelated sources: 10 log10(sum 10^(L/10)). Silent sources are ignored.</summary>
        public static float PowerSumDb(params float[] levelsDb)
        {
            double sum = 0;
            if (levelsDb != null)
                foreach (float l in levelsDb) if (l > Silent + 1f) sum += Math.Pow(10.0, l / 10.0);
            return sum <= 0 ? Silent : (float)(10.0 * Math.Log10(sum));
        }

        public static float GainToDb(float gain) => gain <= 1e-10f ? Silent : (float)(20.0 * Math.Log10(gain));
    }
}
