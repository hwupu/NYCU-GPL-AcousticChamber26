using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>Placement on a horizontal circle around an origin (azimuth convention: see <see cref="Polar"/>).</summary>
    public static class Ring
    {
        /// <summary>origin + yaw * (r sin az, 0, r cos az): at the origin's height.</summary>
        public static Vector3 Position(Vector3 origin, Quaternion yaw, float radius, float azDeg)
        {
            Polar.ToXZ(azDeg, radius, out double x, out double z);
            return origin + yaw * new Vector3((float)x, 0f, (float)z);
        }

        /// <summary>Rotation that makes an object at <paramref name="position"/> face the origin (upright).</summary>
        public static Quaternion FacingOrigin(Vector3 position, Vector3 origin)
        {
            var toOrigin = new Vector3(origin.x - position.x, 0f, origin.z - position.z);
            return toOrigin.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(toOrigin, Vector3.up) : Quaternion.identity;
        }

        /// <summary>Yaw-only rotation of a forward direction (for a yaw reference from a head or rig).</summary>
        public static Quaternion YawOf(Vector3 forward)
        {
            forward.y = 0f;
            return forward.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(forward.normalized, Vector3.up) : Quaternion.identity;
        }

        /// <summary>Azimuth (deg) of a world point seen from the origin in the yaw frame.</summary>
        public static float AzimuthOf(Vector3 point, Vector3 origin, Quaternion yaw)
        {
            var local = Quaternion.Inverse(yaw) * (point - origin);
            return (float)Polar.AzimuthDeg(local.x, local.z);
        }
    }
}
