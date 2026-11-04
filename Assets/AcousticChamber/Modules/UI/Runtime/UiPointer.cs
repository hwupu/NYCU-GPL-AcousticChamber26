using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// Controller laser for world-space uGUI (from HW2's UiPointer). Meta's OVRInputModule relies on the legacy Input API,
    /// which is disabled (Input System only), so this does its own ray/rect hit test against every active, interactable
    /// <see cref="Selectable"/>. The hand that last pulled its index trigger owns the ray (right hand by default). The laser
    /// is drawn only while some usable UI exists; it is white and turns accent while it hits a button.
    /// </summary>
    public class UiPointer : MonoBehaviour
    {
        /// <summary>While set (a modal dialog), only selectables under this transform can be pointed at.</summary>
        public static Transform Modal;

        /// <summary>The selectable under the ray, or null.</summary>
        public static Selectable Hovered { get; private set; }

        public float maxDistanceM = 6f;
        public float idleLaserLengthM = 2f;
        public float laserWidthM = 0.004f;
        [Tooltip("Must support single-pass stereo (URP Unlit material asset). Sprites/Default renders in one eye only.")]
        public Material laserMaterial;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        LineRenderer _line;
        Material _lineMat;
        bool _useLeft;
        bool _lastHit;
        Transform _leftRay, _rightRay;
        float _nextSearchTime;
        OVRCameraRig _rig;

        void Awake()
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = _line.endWidth = laserWidthM;
            _line.useWorldSpace = true;
            _lineMat = laserMaterial != null ? new Material(laserMaterial) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _line.sharedMaterial = _lineMat;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.enabled = false;
            SetLaserColor(false);
        }

        void OnDestroy()
        {
            if (_lineMat != null) Destroy(_lineMat);
            if (Hovered != null) SetHover(null);
        }

        void Update()
        {
            var targets = Selectable.allSelectablesArray;
            bool any = false;
            for (int i = 0; i < targets.Length && !any; i++) any = IsUsable(targets[i]);
            if (!any)
            {
                _line.enabled = false;
                SetHover(null);
                return;
            }

            bool leftDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            bool rightDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            if (leftDown && !rightDown) _useLeft = true;
            else if (rightDown) _useLeft = false;

            Transform origin = _useLeft ? Resolve(OVRInput.Controller.LTouch, ref _leftRay) : Resolve(OVRInput.Controller.RTouch, ref _rightRay);
            if (origin == null)
            {
                _line.enabled = false;
                SetHover(null);
                return;
            }

            var ray = new Ray(origin.position, origin.forward);
            Selectable hit = Raycast(ray, targets, out Vector3 hitPoint);
            SetHover(hit);

            _line.enabled = true;
            _line.SetPosition(0, ray.origin);
            _line.SetPosition(1, hit != null ? hitPoint : ray.origin + ray.direction * idleLaserLengthM);
            if (_lastHit != (hit != null)) SetLaserColor(hit != null);

            if ((leftDown || rightDown) && hit is Button button) button.onClick.Invoke();
        }

        static bool IsUsable(Selectable s) =>
            s != null && s.isActiveAndEnabled && s.interactable && (Modal == null || s.transform.IsChildOf(Modal));

        Selectable Raycast(Ray ray, Selectable[] targets, out Vector3 point)
        {
            Selectable best = null;
            float bestT = maxDistanceM;
            point = Vector3.zero;
            foreach (var s in targets)
            {
                if (!IsUsable(s)) continue;
                var rt = s.transform as RectTransform;
                if (rt == null) continue;
                Vector3 n = rt.forward;
                float denom = Vector3.Dot(ray.direction, n);
                if (Mathf.Abs(denom) < 1e-5f) continue;
                float t = Vector3.Dot(rt.position - ray.origin, n) / denom;
                if (t <= 0f || t >= bestT) continue;
                Vector3 p = ray.origin + ray.direction * t;
                if (rt.rect.Contains(rt.InverseTransformPoint(p))) { best = s; bestT = t; point = p; }
            }
            return best;
        }

        static void SetHover(Selectable s)
        {
            if (Hovered == s) return;
            if (Hovered != null)
            {
                Hovered.OnPointerExit(null);
                var h = Hovered.GetComponent<HoverHighlight>();
                if (h != null) h.SetHover(false);
            }
            Hovered = s;
            if (Hovered != null)
            {
                Hovered.OnPointerEnter(null);
                var h = Hovered.GetComponent<HoverHighlight>();
                if (h != null) h.SetHover(true);
            }
        }

        void SetLaserColor(bool onTarget)
        {
            _lastHit = onTarget;
            if (_lineMat != null && _lineMat.HasProperty(BaseColorId))
                _lineMat.SetColor(BaseColorId, onTarget ? UiKit.Theme.accent : Color.white);
        }

        /// <summary>
        /// Ray origin = the active controller model's "laser_begin" transform (Controller Tracking building block,
        /// OVRControllerHelper), else the helper's pointer transform, else the camera rig's controller anchor.
        /// </summary>
        Transform Resolve(OVRInput.Controller hand, ref Transform cache)
        {
            if (cache != null && cache.gameObject.activeInHierarchy) return cache;
            if (Time.unscaledTime < _nextSearchTime) return null;
            _nextSearchTime = Time.unscaledTime + 0.5f;

            cache = null;
            foreach (var helper in FindObjectsByType<OVRControllerHelper>(FindObjectsInactive.Exclude))
            {
                if (helper.m_controller != hand) continue;
                foreach (var t in helper.GetComponentsInChildren<Transform>(false))
                    if (t.name.EndsWith("laser_begin")) { cache = t; break; }
                if (cache == null) cache = helper.GetPointerRayTransform();
                break;
            }
            if (cache == null)
            {
                if (_rig == null) _rig = FindAnyObjectByType<OVRCameraRig>();
                if (_rig != null) cache = hand == OVRInput.Controller.LTouch ? _rig.leftControllerAnchor : _rig.rightControllerAnchor;
            }
            return cache;
        }
    }
}
