using System;

namespace AcousticChamber.Chambers.MovingSource
{
    /// <summary>Where a grabbed source may be: 0.5-10 m from the head, at least 0.1 m above the floor (Docs/Design.md "Movement").</summary>
    public static class SourceLimits
    {
        public const double MinDistanceM = 0.5, MaxDistanceM = 10.0, MinHeightM = 0.1;

        /// <summary>Pulls (x, y, z) into the limits seen from the head at (hx, hy, hz): the distance radially, the height upward.</summary>
        public static void Clamp(ref double x, ref double y, ref double z, double hx, double hy, double hz)
        {
            if (y < MinHeightM) y = MinHeightM;
            double dx = x - hx, dy = y - hy, dz = z - hz;
            double r = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (r < 1e-9) { x = hx; y = hy; z = hz + MinDistanceM; return; }
            double c = Math.Max(MinDistanceM, Math.Min(MaxDistanceM, r));
            if (Math.Abs(c - r) < 1e-12) return;
            x = hx + dx / r * c;
            y = hy + dy / r * c;
            z = hz + dz / r * c;
            if (y < MinHeightM) y = MinHeightM; // pulling toward a head near the floor must not sink the source
        }
    }
}
