using System;

namespace GPL.Homing
{
    /// <summary>
    /// Head direction as yaw and pitch (degrees), Unity axes (+Y up, +Z forward, +X right). Azimuth convention of the
    /// project: 0 = forward, negative = left, positive = right. Pitch: positive = up.
    /// </summary>
    public static class HeadAngles
    {
        const double Rad2Deg = 180.0 / Math.PI;

        /// <summary>Yaw of a direction (deg, -180..180]; 0 = +Z, +90 = +X (right).</summary>
        public static float Yaw(float x, float y, float z) => (float)(Math.Atan2(x, z) * Rad2Deg);

        /// <summary>Pitch of a direction (deg, -90..90); positive = up. The direction need not be normalized.</summary>
        public static float Pitch(float x, float y, float z)
        {
            double len = Math.Sqrt(x * x + y * y + z * z);
            if (len < 1e-9) return 0f;
            return (float)(Math.Asin(Math.Max(-1.0, Math.Min(1.0, y / len))) * Rad2Deg);
        }

        /// <summary>Wraps an angle to (-180, 180].</summary>
        public static float Wrap(float deg)
        {
            double a = deg % 360.0;
            if (a <= -180.0) a += 360.0;
            else if (a > 180.0) a -= 360.0;
            return (float)a;
        }

        /// <summary>Signed yaw of the head relative to a reference yaw (deg, -180..180]; positive = head turned right.</summary>
        public static float YawError(float headYawDeg, float referenceYawDeg) => Wrap(headYawDeg - referenceYawDeg);
    }
}
