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
        /// <summary>The panel (and its move handle) stays at least this far above the floor (m); lifted when needed.</summary>
        public const float MinClearanceM = 0.15f;

        public float distanceM = 2f;
        [Tooltip("Panel center below eye level (deg). Negative = above.")]
        public float pitchDownDeg = 8f;
        [Tooltip("Extra height (m) after the pitch, e.g. a HUD strip above the scene.")]
        public float heightOffsetM = 0f;
        [Tooltip("Panel center to the side of straight ahead (deg). Negative = left.")]
        public float yawOffsetDeg = 0f;
        public bool followRecenter = true;
        [Tooltip("Head must be this far above the rig origin before placing (m).")]
        public float minHeadHeightM = 0.3f;
        public float maxWaitSec = 5f;

        /// <summary>
        /// True = the participant can move the panel with the handle below it (<see cref="PanelMover"/>). Set at run time by
        /// the chamber menus and experience panels; the chosen yaw / pitch stay for the visit (also after a recenter).
        /// </summary>
        public bool Movable
        {
            get => GetComponent<PanelMover>() != null;
            set
            {
                var mover = GetComponent<PanelMover>();
                if (value && mover == null) gameObject.AddComponent<PanelMover>();
                else if (!value && mover != null) Destroy(mover);
            }
        }

        /// <summary>Raised after every placement (start, recenter, manual).</summary>
        public event Action Placed;

        OVRCameraRig _rig;
        bool _subscribed;

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
            var head = _rig.centerEyeAnchor.position;
            var yaw = Quaternion.Euler(0f, _rig.transform.eulerAngles.y, 0f);
            var rot = yaw * Quaternion.Euler(0f, yawOffsetDeg, 0f) * Quaternion.Euler(pitchDownDeg, 0f, 0f); // +X pitch tilts forward downward
            float scale = ScalePerMeter * distanceM;
            Vector3 pos = head + rot * Vector3.forward * distanceM + Vector3.up * heightOffsetM;
            // a tall panel (or its move handle below it) must stay above the floor: lift it when its lowest point would go under
            float halfHeight = ((RectTransform)transform).rect.height / 2f + (Movable ? PanelMover.EdgeGap + PanelMover.PillLength / 8f + 40f : 0f);
            float lowest = pos.y + (rot * Vector3.down).y * halfHeight * scale;
            float floor = _rig.transform.position.y + MinClearanceM;
            if (lowest < floor) pos.y += floor - lowest;
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = Vector3.one * scale;
            Placed?.Invoke();
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
