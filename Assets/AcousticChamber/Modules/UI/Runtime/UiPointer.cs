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
    /// Inside a <see cref="ScrollList"/> the click waits for the release, because the press becomes a drag after 12 u;
    /// the right thumbstick scrolls the list under the ray.
    /// </summary>
    public class UiPointer : MonoBehaviour
    {
        /// <summary>While set (the top <see cref="Sheet"/>), only selectables under this transform can be pointed at.</summary>
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

        /// <summary>The ray of the active controller (the hand that last pulled its trigger; right by default), valid this frame.</summary>
        public static Ray ActiveRay { get; private set; }

        /// <summary>False when there is no pointer, no controller yet, or the pointer is idle (no UI and no ray target in the scene).</summary>
        public static bool HasActiveRay { get; private set; }

        void Update()
        {
            HasActiveRay = false;
            var targets = Selectable.allSelectablesArray;
            if (_held != null && Modal != null) ReleaseHeld(); // a sheet opened while dragging
            bool any = _held != null || (Modal == null && RayTargets.All.Count > 0);
            for (int i = 0; i < targets.Length && !any; i++) any = IsUsable(targets[i]);
            if (!any)
            {
                _line.enabled = false;
                SetHover(null);
                SetHover3D(null);
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
                SetHover3D(null);
                ReleaseHeld();
                return;
            }

            var ray = new Ray(origin.position, origin.forward);
            ActiveRay = ray;
            HasActiveRay = true;

            // a held 3D target owns the trigger until it is released
            if (_held != null)
            {
                if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _heldHand))
                {
                    var end = _held.RayDrag(ray);
                    _line.enabled = true;
                    _line.SetPosition(0, ray.origin);
                    _line.SetPosition(1, end);
                    return;
                }
                ReleaseHeld();
            }

            float dt = Time.unscaledDeltaTime;
            Vector3 hitPoint = Vector3.zero;
            Selectable hit = null;
            if (_scrolling == null || !_scrolling.Model.Dragging) hit = Raycast(ray, targets, out hitPoint); // no hover while dragging

            // a 3D target nearer than the UI hit wins
            IRayTarget target = null;
            if (_scrolling == null && Modal == null && RayTargets.All.Count > 0)
            {
                float uiDistance = hit != null ? Vector3.Distance(ray.origin, hitPoint) : maxDistanceM;
                var t = RayTargets.Pick(ray, maxDistanceM, out float d);
                if (t != null && d < uiDistance) { target = t; hit = null; hitPoint = ray.origin + ray.direction * d; }
            }
            if (hit == null && target == null) hitPoint = ray.origin + ray.direction * idleLaserLengthM;
            SetHover(hit);
            SetHover3D(target);
            var list = target != null ? null : _scrolling != null ? _scrolling : ScrollList.Under(ray, maxDistanceM);

            _line.enabled = true;
            _line.SetPosition(0, ray.origin);
            if (hit == null && list != null && list.LocalPoint(ray, out _))
                hitPoint = PlanePoint(ray, list.Viewport);
            _line.SetPosition(1, hitPoint);

            // right thumbstick scrolls the list under the ray
            if (list != null && _scrolling == null)
                list.Stick(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y, dt);

            var hand = leftDown && !rightDown ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            if (leftDown || rightDown)
            {
                ReleasePressed();
                if (target != null)
                {
                    _held = target;
                    _heldHand = hand;
                    target.RayPress(ray);
                    return;
                }
                var button = hit as Button;
                bool inList = list != null && (button == null || button.transform.IsChildOf(list.Content));
                if (inList && list.LocalPoint(ray, out Vector2 local))
                {
                    // inside a scroll list the click waits for the release: the press may become a drag
                    _scrolling = list;
                    _pressedHand = hand;
                    list.PointerDown(local.y);
                    _pendingClick = button;
                    if (button is UiButton lb) { lb.Press(); _pressed = lb; }
                }
                else if (button != null)
                {
                    if (button is UiButton ub)
                    {
                        ub.Press();
                        _pressed = ub;
                        _pressedHand = hand;
                    }
                    button.onClick.Invoke();
                }
            }

            if (_scrolling != null)
            {
                if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _pressedHand))
                {
                    if (_scrolling.LocalPoint(ray, out Vector2 local) && _scrolling.PointerMove(local.y, dt) && _pendingClick != null)
                    {
                        _pendingClick = null; // became a drag: no click
                        ReleasePressed();
                        SetHover(null);
                    }
                }
                else
                {
                    bool wasDrag = _scrolling.PointerUp();
                    var click = _pendingClick;
                    _pendingClick = null;
                    _scrolling = null;
                    ReleasePressed();
                    if (!wasDrag && click != null && Hovered == click) click.onClick.Invoke();
                }
                return;
            }
            if (_pressed != null && !OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _pressedHand)) ReleasePressed();
        }

        UiButton _pressed;
        OVRInput.Controller _pressedHand;
        IRayTarget _held, _hovered3D;
        OVRInput.Controller _heldHand;

        /// <summary>The 3D target being dragged, or null.</summary>
        public IRayTarget Held => _held;

        void ReleaseHeld()
        {
            if (_held == null) return;
            var h = _held;
            _held = null;
            h.RayRelease();
        }

        void SetHover3D(IRayTarget t)
        {
            if (_hovered3D == t) return;
            var old = _hovered3D;
            _hovered3D = t;
            old?.RayHover(false);
            t?.RayHover(true);
        }

        void OnDisable()
        {
            ReleaseHeld();
            SetHover3D(null);
        }
        ScrollList _scrolling;
        Button _pendingClick;

        static Vector3 PlanePoint(Ray ray, RectTransform rt)
        {
            float t = Vector3.Dot(rt.position - ray.origin, rt.forward) / Vector3.Dot(ray.direction, rt.forward);
            return ray.origin + ray.direction * t;
        }

        void ReleasePressed()
        {
            if (_pressed != null) _pressed.Release();
            _pressed = null;
        }

        static bool IsUsable(Selectable s) =>
            s != null && s.isActiveAndEnabled && s.interactable && (Modal == null || s.transform.IsChildOf(Modal))
            && !UiKit.InLeavingLayer(s.transform);

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
                if (ScrollList.ClipsOut(s.transform, p)) continue; // scrolled out of its list's viewport
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
