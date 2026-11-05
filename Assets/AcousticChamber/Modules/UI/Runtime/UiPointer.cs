using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// Controller laser for world-space uGUI (from HW2's UiPointer). Meta's OVRInputModule relies on the legacy Input API,
    /// which is disabled (Input System only), so this does its own ray/rect hit test against every active, interactable
    /// <see cref="Selectable"/>. The hand that last pulled its index trigger owns the ray (right hand by default). The laser
    /// is drawn only while some usable UI exists (white). Hit regions are grown to the theme minimum (UiKit.HitRect);
    /// trigger down presses a <see cref="UiButton"/> (pressed look until the trigger is released) and clicks it.
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
            SetLaserColor();
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

            if ((leftDown || rightDown) && hit is Button button)
            {
                ReleasePressed();
                if (button is UiButton ub)
                {
                    ub.Press();
                    _pressed = ub;
                    _pressedHand = leftDown && !rightDown ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                }
                button.onClick.Invoke();
            }
            if (_pressed != null && !OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _pressedHand)) ReleasePressed();
        }

        UiButton _pressed;
        OVRInput.Controller _pressedHand;

        void ReleasePressed()
        {
            if (_pressed != null) _pressed.Release();
            _pressed = null;
        }

        static bool IsUsable(Selectable s) =>
            s != null && s.isActiveAndEnabled && s.interactable && (Modal == null || s.transform.IsChildOf(Modal));

        Selectable Raycast(Ray ray, Selectable[] targets, out Vector3 point) => Pick(ray, targets, maxDistanceM, out point);

        /// <summary>
        /// The usable selectable hit by <paramref name="ray"/>: nearest plane first; on the same plane, a hit inside the
        /// element's own rect beats a hit only in its grown hit region (<see cref="UiKit.HitRect"/>), then the nearer center.
        /// </summary>
        public static Selectable Pick(Ray ray, Selectable[] targets, float maxDistance, out Vector3 point)
        {
            Selectable best = null;
            float bestT = maxDistance, bestScore = float.MaxValue;
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
                if (t <= 0f || t > bestT + 1e-4f) continue;
                Vector3 p = ray.origin + ray.direction * t;
                Vector2 local = rt.InverseTransformPoint(p);
                if (!UiKit.HitRect(rt).Contains(local)) continue;
                float score = (rt.rect.Contains(local) ? 0f : 1e6f) + (local - rt.rect.center).sqrMagnitude;
                bool samePlane = Mathf.Abs(t - bestT) <= 1e-4f;
                if (best != null && samePlane && score >= bestScore) continue;
                best = s; bestT = t; bestScore = score; point = p;
            }
            return best;
        }

        static void SetHover(Selectable s)
        {
            if (Hovered == s) return;
            var old = Hovered;
            Hovered = s;
            if (old != null) old.OnPointerExit(null);
            if (Hovered != null) Hovered.OnPointerEnter(null);
        }

        void SetLaserColor()
        {
            if (_lineMat != null && _lineMat.HasProperty(BaseColorId)) _lineMat.SetColor(BaseColorId, Color.white);
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
