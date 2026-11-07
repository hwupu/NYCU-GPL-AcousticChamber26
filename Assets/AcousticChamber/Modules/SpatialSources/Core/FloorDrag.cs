using System;

namespace GPL.SpatialSources
{
    /// <summary>
    /// Pure geometry for dragging an object over a horizontal floor with a ray (PB-10): where the ray meets the floor, and a clamp to
    /// a ring around a centre (the listener) so the object never comes closer than a minimum or farther than a maximum.
    /// </summary>
    public static class FloorDrag
    {
        /// <summary>
        /// Floor point (x, z) where the ray from (ox, oy, oz) along (dx, dy, dz) meets the plane y = <paramref name="floorY"/>, if it
        /// points down and meets it within <paramref name="maxRange"/>. Otherwise the point straight below the ray at that range, so
        /// a ray at or above the horizon still drags the object away (false is returned then).
        /// </summary>
        public static bool FloorPoint(double ox, double oy, double oz, double dx, double dy, double dz, double floorY, double maxRange,
                                      out double x, out double z)
        {
            double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-12) { x = ox; z = oz; return false; }
            dx /= len; dy /= len; dz /= len;
            if (dy < -1e-6)
            {
                double t = (floorY - oy) / dy;
                if (t >= 0 && t <= maxRange) { x = ox + dx * t; z = oz + dz * t; return true; }
            }
            x = ox + dx * maxRange;
            z = oz + dz * maxRange;
            return false;
        }

        /// <summary>Moves (x, z) radially so its distance from (cx, cz) lies in [min, max]. At the centre itself it goes to +z.</summary>
        public static void ClampToRing(ref double x, ref double z, double cx, double cz, double min, double max)
        {
            double vx = x - cx, vz = z - cz;
            double r = Math.Sqrt(vx * vx + vz * vz);
            if (r < 1e-9)
            {
                if (min > 0) { x = cx; z = cz + min; }
                return;
            }
            double clamped = Math.Max(min, Math.Min(max, r));
            if (Math.Abs(clamped - r) < 1e-12) return;
            x = cx + vx / r * clamped;
            z = cz + vz / r * clamped;
        }

        /// <summary>
        /// Ray–sphere test: distance along the (normalized) ray to the first intersection with the sphere, if any lies ahead within
        /// <paramref name="maxDistance"/>. A ray starting inside the sphere hits at 0.
        /// </summary>
        public static bool RaySphere(double ox, double oy, double oz, double dx, double dy, double dz,
                                     double cx, double cy, double cz, double radius, double maxDistance, out double distance)
        {
            distance = 0;
            double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-12) return false;
            dx /= len; dy /= len; dz /= len;
            double lx = cx - ox, ly = cy - oy, lz = cz - oz;
            double tca = lx * dx + ly * dy + lz * dz;
            double d2 = lx * lx + ly * ly + lz * lz - tca * tca;
            double r2 = radius * radius;
            if (d2 > r2) return false;
            double thc = Math.Sqrt(r2 - d2);
            double t0 = tca - thc, t1 = tca + thc;
            if (t1 < 0) return false;
            distance = t0 >= 0 ? t0 : 0;
            return distance <= maxDistance;
        }
    }
}
