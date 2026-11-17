using System;
using System.Collections;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// Places a world-space canvas (shown through an OVROverlayCanvas) in front of the participant: <see cref="distanceM"/>
    /// along the rig's forward direction, <see cref="pitchDownDeg"/> below eye level, tilted to face the eye. The canvas
    /// scale follows the distance (1 unit = 0.001 * distance m), so sizes stay the same visual angle: 1 unit = 0.057 deg.
    /// Waits for valid head tracking first (the first frames report the head at the floor) and re-places itself two
    /// frames after a system recenter. Keeps the hidden canvas layers out of the rig's eye cameras.
    /// From HW2's GuiPanelPlacer.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class OverlayPanel : MonoBehaviour
    {
        /// <summary>Layer of the source uGUI of an OVROverlayCanvas; the eye cameras must not render it.</summary>
        public const int HiddenCanvasLayer = 30;
        /// <summary>OVROverlayCanvasSettings.CanvasRenderLayer (used while the overlay camera renders).</summary>
        public const int OverlayRenderLayer = 31;
        /// <summary>Canvas scale per meter of distance (0.002 at 2 m, the HW2 scale).</summary>
        public const float ScalePerMeter = 0.001f;
        /// <summary>Default of <see cref="minBottomM"/>: the panel (and its move handle) stays at least this far above the floor (m).</summary>
        public const float MinClearanceM = 0.15f;

        public float distanceM = 2f;
        [Tooltip("Panel center below eye level (deg). Negative = above.")]
        public float pitchDownDeg = 8f;
        [Tooltip("Extra height (m) after the pitch, e.g. a HUD strip above the scene.")]
        public float heightOffsetM = 0f;
        [Tooltip("Panel center to the side of straight ahead (deg). Negative = left.")]
        public float yawOffsetDeg = 0f;
        [Tooltip("Lowest point of the panel (and its move handle) above the rig floor (m). When the panel would reach lower, it " +
                 "is tilted up around the eye (still facing it) until it clears: e.g. a table in front of a seated participant.")]
        public float minBottomM = MinClearanceM;
        [Tooltip("Upright (0° tilt) and standing on minBottomM, e.g. on a table top, instead of pitchDownDeg below eye level. " +
                 "The move handle then only turns it left / right.")]
        public bool standOnBottom = false;
        public bool followRecenter = true;
        [Tooltip("Head must be this far above the rig origin before placing (m).")]
        public float minHeadHeightM = 0.3f;
        public float maxWaitSec = 5f;

        /// <summary>
        /// True = the participant can move the panel with the handle below it (<see cref="UiGrabber"/>). Set at run time by
        /// the chamber menus and experience panels; the chosen yaw / pitch stay for the visit (also after a recenter).
        /// </summary>
        public bool Movable
        {
            get => GetComponent<UiGrabber>() != null;
            set
            {
                var mover = GetComponent<UiGrabber>();
                if (value && mover == null) gameObject.AddComponent<UiGrabber>();
                else if (!value && mover != null)
                {
                    Destroy(mover); // at the end of the frame: Place must not count its handle any more
                    _handleRemoved = true;
                    if (standOnBottom && _placed) Place(); // standing: the panel itself goes down onto the surface (the grabber re-places on add)
                }
                if (value) _handleRemoved = false;
            }
        }

        /// <summary>Raised after every placement (start, recenter, manual).</summary>
        public event Action Placed;

        OVRCameraRig _rig;
        bool _subscribed;
        bool _placed;
        bool _handleRemoved;
        bool HasHandle => Movable && !_handleRemoved;

        IEnumerator Start()
        {
            _rig = FindAnyObjectByType<OVRCameraRig>();
            if (_rig == null)
            {
                Debug.LogError("[GPL] OverlayPanel: no OVRCameraRig in the scene (add the Camera Rig building block).");
                yield break;
            }
            HideCanvasLayersFromCameras(_rig);

            var head = _rig.centerEyeAnchor;
            float waited = 0f;
            while (head.position.y - _rig.transform.position.y < minHeadHeightM && waited < maxWaitSec)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Place();

            if (followRecenter && OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose += OnRecentered;
                _subscribed = true;
            }
        }

        void OnDestroy()
        {
            if (_subscribed && OVRManager.display != null) OVRManager.display.RecenteredPose -= OnRecentered;
        }

        void OnRecentered()
        {
            if (isActiveAndEnabled) StartCoroutine(PlaceAfterFrames(2));
        }

        IEnumerator PlaceAfterFrames(int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
            Place();
            Debug.Log("[GPL] OverlayPanel '" + name + "' re-placed after recenter.");
        }

        /// <summary>Places (or re-places) the panel in front of the participant now.</summary>
        [ContextMenu("Place now")]
        public void Place()
        {
            if (_rig == null) _rig = FindAnyObjectByType<OVRCameraRig>();
            if (_rig == null) return;
            _placed = true;
            var head = _rig.centerEyeAnchor.position;
            var yaw = Quaternion.Euler(0f, _rig.transform.eulerAngles.y, 0f);
            float scale = ScalePerMeter * distanceM;
            // the panel's lowest point (or its move handle's) must stay above minBottomM: tilt it up around the eye when needed
            float halfHeightM = (((RectTransform)transform).rect.height / 2f + (HasHandle ? UiGrabber.Reserve : 0f)) * scale;
            if (standOnBottom)
            {
                // upright, its lowest point (the handle's, when movable) on minBottomM, distanceM ahead on the level
                var level = yaw * Quaternion.Euler(0f, yawOffsetDeg, 0f);
                Vector3 at = head + level * Vector3.forward * distanceM;
                at.y = _rig.transform.position.y + minBottomM + halfHeightM;
                transform.SetPositionAndRotation(at, level);
                transform.localScale = Vector3.one * scale;
                Placed?.Invoke();
                return;
            }
            float room = head.y + heightOffsetM - (_rig.transform.position.y + minBottomM);
            float pitch = ClearancePitch(pitchDownDeg, distanceM, halfHeightM, room);
            var rot = yaw * Quaternion.Euler(0f, yawOffsetDeg, 0f) * Quaternion.Euler(pitch, 0f, 0f); // +X pitch tilts forward downward
            Vector3 pos = head + rot * Vector3.forward * distanceM + Vector3.up * heightOffsetM;
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = Vector3.one * scale;
            Placed?.Invoke();
        }

        /// <summary>
        /// The pitch (deg, down positive) to use: <paramref name="pitchDownDeg"/>, or less (tilted up) when the panel's lowest point
        /// would come within less than <paramref name="roomBelowEyeM"/> below the eye. A panel at distance d, pitch p, half height H
        /// (tilted to face the eye) reaches down to d·sin p + H·cos p = R·sin(p + φ) below the eye, R = √(d² + H²), φ = atan(H / d).
        /// </summary>
        public static float ClearancePitch(float pitchDownDeg, float distanceM, float halfHeightM, float roomBelowEyeM)
        {
            float r = Mathf.Sqrt(distanceM * distanceM + halfHeightM * halfHeightM);
            float phi = Mathf.Atan2(halfHeightM, distanceM);
            float drop = r * Mathf.Sin(pitchDownDeg * Mathf.Deg2Rad + phi);
            if (drop <= roomBelowEyeM || r <= 0f) return pitchDownDeg;
            float max = (Mathf.Asin(Mathf.Clamp(roomBelowEyeM / r, -1f, 1f)) - phi) * Mathf.Rad2Deg;
            return Mathf.Min(pitchDownDeg, max);
        }

        /// <summary>Removes the hidden canvas layers from every camera under the rig (eye cameras).</summary>
        public static void HideCanvasLayersFromCameras(OVRCameraRig rig)
        {
            if (rig == null) return;
            foreach (var cam in rig.GetComponentsInChildren<Camera>(true))
                cam.cullingMask &= ~((1 << HiddenCanvasLayer) | (1 << OverlayRenderLayer));
        }
    }
}
