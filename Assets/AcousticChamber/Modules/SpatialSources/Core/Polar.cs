using System;

namespace GPL.SpatialSources
{
    /// <summary>
    /// Azimuth convention of the lab (CLAUDE.md): 0 = forward, negative = left, positive = right; x = right, z = forward in the
    /// frame of the origin's yaw. Position = origin + yaw * (r sin az, 0, r cos az).
    /// </summary>
    public static class Polar
    {
        const double Deg = Math.PI / 180.0;

        public static void ToXZ(double azDeg, double radius, out double x, out double z)
        {
            x = radius * Math.Sin(azDeg * Deg);
            z = radius * Math.Cos(azDeg * Deg);
        }

        /// <summary>Azimuth (deg, in (-180, 180]) of a point in the yaw frame; 0 for the origin itself.</summary>
        public static double AzimuthDeg(double x, double z) =>
            Math.Abs(x) < 1e-12 && Math.Abs(z) < 1e-12 ? 0.0 : Wrap180(Math.Atan2(x, z) / Deg);

        /// <summary>Wrap an angle to (-180, 180].</summary>
        public static double Wrap180(double deg)
        {
            double a = deg % 360.0;
            if (a <= -180.0) a += 360.0;
            else if (a > 180.0) a -= 360.0;
            return a;
        }

        /// <summary>Signed difference a - b wrapped to (-180, 180] (e.g. response - target).</summary>
        public static double DeltaDeg(double a, double b) => Wrap180(a - b);
    }
}
