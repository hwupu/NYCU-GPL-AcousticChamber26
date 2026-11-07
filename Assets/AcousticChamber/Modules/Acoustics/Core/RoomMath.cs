using System;

namespace GPL.Acoustics
{
    /// <summary>
    /// Shoebox room formulas (calculated values, not measurements). Meta XR Audio does not say whether its reflection coefficient is a
    /// pressure (amplitude) or an energy coefficient: amplitude means absorption alpha = 1 - R^2, energy alpha = 1 - R; the amplitude
    /// reading is the default. For the RT60 the plugin actually renders use <see cref="Rt60Calibration"/>: Eyring was off by 20-170 %
    /// against measurements of the plugin (HW2, 2026-10-18).
    /// </summary>
    public static class RoomMath
    {
        public static void BoxVolumeSurface(double width, double depth, double height, out double volume, out double surface)
        {
            volume = width * depth * height;
            surface = 2.0 * (width * depth + width * height + depth * height);
        }

        /// <summary>V / S of a box (m), the quantity the reverberation time of a uniform room scales with.</summary>
        public static double VolumeOverSurface(double width, double depth, double height)
        {
            BoxVolumeSurface(width, depth, height, out double v, out double s);
            return s > 0 ? v / s : 0;
        }

        /// <summary>Eyring: uniform reflection coefficient for an RT60 (s), limited to [0, 0.999].</summary>
        public static double EyringReflectionForRt60(double rt60S, double volumeM3, double surfaceM2, bool amplitude = true)
        {
            double k = amplitude ? 2.0 : 1.0;
            double r = Math.Exp(-0.161 * volumeM3 / (k * surfaceM2 * Math.Max(rt60S, 0.01)));
            return Math.Min(0.999, Math.Max(0.0, r));
        }

        /// <summary>Eyring: RT60 (s) of a uniform reflection coefficient.</summary>
        public static double EyringRt60ForReflection(double reflection, double volumeM3, double surfaceM2, bool amplitude = true)
        {
            double k = amplitude ? 2.0 : 1.0;
            double r = Math.Min(0.999, Math.Max(1e-6, reflection));
            return 0.161 * volumeM3 / (-k * surfaceM2 * Math.Log(r));
        }

        /// <summary>Sabine: mean absorption coefficient a room needs for an RT60 (s).</summary>
        public static double SabineAbsorptionForRt60(double rt60S, double volumeM3, double surfaceM2) =>
            rt60S <= 0 || surfaceM2 <= 0 ? 1.0 : Math.Min(1.0, 0.161 * volumeM3 / (surfaceM2 * rt60S));
    }
}
