using UnityEngine;

namespace GPL.Homing
{
    /// <summary>The head relative to the rig: yaw and pitch from the rig's forward (deg), eye height above the rig origin (m).</summary>
    public struct HeadPose
    {
        public float YawDeg, PitchDeg, EyeHeightM;

        /// <summary>Measures the center eye of <paramref name="rig"/>. The rig origin is the floor (floor-level tracking).</summary>
        public static HeadPose Of(OVRCameraRig rig)
        {
            var head = rig.centerEyeAnchor;
            var rt = rig.transform;
            Vector3 f = rt.InverseTransformDirection(head.forward);
            return new HeadPose
            {
                YawDeg = HeadAngles.Yaw(f.x, f.y, f.z),
                PitchDeg = HeadAngles.Pitch(f.x, f.y, f.z),
                EyeHeightM = rt.InverseTransformPoint(head.position).y,
            };
        }

        /// <summary>True while tracking still reports the head near the floor (the first frames after start).</summary>
        public static bool NotTrackedYet(OVRCameraRig rig, float minHeightM = 0.3f) => Of(rig).EyeHeightM < minHeightM;

        /// <summary>The rig must stay at identity (homing uses the system recenter, never the rig transform).</summary>
        public static bool RigAtIdentity(OVRCameraRig rig) =>
            rig.transform.position.sqrMagnitude < 1e-8f && Quaternion.Angle(rig.transform.rotation, Quaternion.identity) < 0.01f;

        public static OVRCameraRig FindRig() => Object.FindAnyObjectByType<OVRCameraRig>();
    }
}
