using System;
using System.Collections;
using UnityEngine;

namespace GPL.Homing
{
    /// <summary>
    /// Handover / experimenter recenter (PO 2026-11-06): calls the Meta system recenter, so the headset's own tracking frame
    /// gets 0 deg = where the participant faces now and the logged values and raw poses share one frame. The rig stays at
    /// identity. The system does not report the correction, so the head yaw is measured right before (= the correction) and
    /// two frames after the recenter event (= the residual, about 0). Unavailable without a headset (Editor without Link).
    /// </summary>
    public sealed class SystemRecenterMethod : IHomingMethod
    {
        /// <summary>Frames to wait for the recenter event before measuring anyway.</summary>
        public const int MaxWaitFrames = 60;

        readonly OVRCameraRig _rig;
        bool _cancel;

        public SystemRecenterMethod(OVRCameraRig rig) { _rig = rig != null ? rig : throw new ArgumentNullException(nameof(rig)); }

        public string Name => HomingMethods.SystemRecenter;
        public bool Running { get; private set; }
        public void Cancel() => _cancel = true;

        public IEnumerator Run(Action<HomingResult> done)
        {
            if (Running) throw new InvalidOperationException("The recenter is already running.");
            Running = true;
            _cancel = false;
            float t0 = Time.time;
            var before = HeadPose.Of(_rig);
            var r = new HomingResult { Method = Name, YawBeforeDeg = before.YawDeg, PitchBeforeDeg = before.PitchDeg };

            var display = OVRManager.display;
            if (display == null || !OVRManager.isHmdPresent)
            {
                r.Outcome = HomingOutcome.Unavailable;
                r.Detail = "no headset";
            }
            else
            {
                if (!HeadPose.RigAtIdentity(_rig))
                {
                    r.Detail = "rig not at identity";
                    Debug.LogWarning("[GPL] Homing: the OVRCameraRig is not at identity; logged angles are relative to the rig.");
                }
                bool fired = false;
                Action onRecentered = () => fired = true;
                display.RecenteredPose += onRecentered;
                RecenterListener.ExpectAppRecenter();
                try
                {
                    display.RecenterPose();
                    for (int i = 0; i < MaxWaitFrames && !fired && !_cancel; i++) yield return null;
                    if (!fired && !_cancel) r.Detail = Join(r.Detail, "no recenter event");
                    // the poses settle on the next frames (HW2 and OverlayPanel wait two)
                    yield return null;
                    yield return null;
                }
                finally { display.RecenteredPose -= onRecentered; }
                r.Outcome = _cancel ? HomingOutcome.Cancelled : HomingOutcome.Done;
            }

            var after = HeadPose.Of(_rig);
            r.YawAfterDeg = after.YawDeg;
            r.PitchAfterDeg = after.PitchDeg;
            r.EyeHeightM = after.EyeHeightM;
            r.DurationSec = Time.time - t0;
            Running = false;
            Debug.Log("[GPL] Homing: " + r.Note());
            done?.Invoke(r);
        }

        static string Join(string a, string b) => string.IsNullOrEmpty(a) ? b : a + "; " + b;
    }
}
