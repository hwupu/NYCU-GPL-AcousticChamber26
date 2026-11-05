using System;
using System.Collections;
using UnityEngine;

namespace GPL.Homing
{
    /// <summary>
    /// Always on (Architecture section 8): reacts to a recenter the participant or experimenter does with a long press of the
    /// Meta button. Two frames after the event it raises <see cref="Recentered"/> (method <c>system_recenter_user</c>; yaw
    /// before = the head yaw of the frame before the event, i.e. the correction; yaw after = the residual) so the chamber can
    /// re-place head-relative stimuli and log a <c>recenter</c> row. Overlay panels re-place themselves (GPL.UI).
    /// A recenter requested by <see cref="SystemRecenterMethod"/> is not reported here (it logs its own <c>homing</c> row),
    /// and the second event Quest raises for the same recenter is ignored.
    /// </summary>
    public class RecenterListener : MonoBehaviour
    {
        /// <summary>Events within this time (unscaled s) after a reported or app recenter belong to it.</summary>
        public const float DebounceSec = 0.5f;

        static float _appRecenterUntil = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _appRecenterUntil = -1f; // Enter Play Mode without a domain reload

        /// <summary>Called by <see cref="SystemRecenterMethod"/> before it recenters.</summary>
        public static void ExpectAppRecenter() => _appRecenterUntil = Time.unscaledTime + DebounceSec + 1f;

        /// <summary>A user recenter, measured; <see cref="HomingResult.Method"/> = <c>system_recenter_user</c>.</summary>
        public event Action<HomingResult> Recentered;

        public int Count { get; private set; }
        public HomingResult LastResult { get; private set; }

        OVRCameraRig _rig;
        OVRDisplay _display;
        HeadPose _lastFrame;
        bool _pending;
        float _ignoreUntil = -1f;

        IEnumerator Start()
        {
            _rig = HeadPose.FindRig();
            if (_rig == null)
            {
                Debug.LogError("[GPL] RecenterListener: no OVRCameraRig in the scene.");
                yield break;
            }
            while (OVRManager.display == null) yield return null; // created by OVRManager after its Awake
            _display = OVRManager.display;
            _display.RecenteredPose += OnRecentered;
        }

        void OnDestroy()
        {
            if (_display != null) _display.RecenteredPose -= OnRecentered;
        }

        void LateUpdate()
        {
            if (_rig != null && !_pending) _lastFrame = HeadPose.Of(_rig);
        }

        void OnRecentered()
        {
            float now = Time.unscaledTime;
            if (_pending || now < _ignoreUntil) return;
            if (now < _appRecenterUntil) { _ignoreUntil = _appRecenterUntil; return; }
            if (!isActiveAndEnabled || _rig == null) return;
            _pending = true;
            StartCoroutine(Report(_lastFrame, now));
        }

        IEnumerator Report(HeadPose before, float t0)
        {
            yield return null;
            yield return null;
            var after = HeadPose.Of(_rig);
            var r = new HomingResult
            {
                Method = HomingMethods.SystemRecenterByUser,
                YawBeforeDeg = before.YawDeg,
                PitchBeforeDeg = before.PitchDeg,
                YawAfterDeg = after.YawDeg,
                PitchAfterDeg = after.PitchDeg,
                EyeHeightM = after.EyeHeightM,
                DurationSec = Time.unscaledTime - t0,
                Detail = HeadPose.RigAtIdentity(_rig) ? "" : "rig not at identity",
            };
            _pending = false;
            _ignoreUntil = Time.unscaledTime + DebounceSec;
            Count++;
            LastResult = r;
            Debug.Log("[GPL] Recenter by user: " + r.Note());
            Recentered?.Invoke(r);
        }
    }
}
