using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// Lets the participant move an <see cref="OverlayPanel"/> (PO 2026-11-08), visionOS style: a small pill below the panel, outside it.
    /// Point at it (or near it: the target zone is much larger than the pill), hold the trigger and move the controller: swinging the ray
    /// turns the panel around the head (left / right, up / down), pushing the hand away from the head or pulling it back moves the panel
    /// farther or nearer. Enable with <see cref="OverlayPanel.Movable"/>. The pill is a real 3D object, not part of the overlay canvas
    /// (the compositor layer clips everything outside the canvas); on hover or while held it grows and brightens with the settle curve.
    /// It is a 3D ray target (<see cref="IRayTarget"/>), shown only while some content is visible.
    /// </summary>
    [RequireComponent(typeof(OverlayPanel))]
    public class PanelMover : MonoBehaviour, IRayTarget
    {
        public const string HandleName = "Move Handle";
        /// <summary>The pill, in canvas units (1 unit = 0.057 deg): length, thickness, and the gap between the panel edge and the pill's center.</summary>
        public const float PillLength = 220f, PillThickness = 16f, EdgeGap = 56f;
        /// <summary>The ray reacts inside this zone around the pill (canvas units, centered on it), much larger than the pill.</summary>
        public const float GrabWidth = 520f, GrabHeight = 150f;
        /// <summary>The pill's size at full hover (factor) and its opacity at rest and at full hover.</summary>
        public const float HoverScale = 1.7f, RestAlpha = 0.45f, HoverAlpha = 0.95f;
        public const float MinPitchDownDeg = -30f, MaxPitchDownDeg = 60f;
        public const float MinDistanceM = 0.8f, MaxDistanceM = 5f;
        /// <summary>Panel distance change per meter the hand moves away from the head.</summary>
        public const float DistanceGain = 2f;

        OverlayPanel _panel;
        RectTransform _rect;
        Transform _handle;
        Renderer _renderer;
        Material _mat;
        bool _registered, _hover, _held;
        float _grabXDeg, _grabYDeg, _grabDistance, _grabHand;
        // the settle tween of the hover amount (0 = rest, 1 = hovered or held)
        float _level, _from, _to, _t0, _dur;

        void Awake()
        {
            _panel = GetComponent<OverlayPanel>();
            _rect = (RectTransform)transform;
        }

        void OnDisable() => Register(false);

        void OnDestroy()
        {
            Register(false);
            if (_handle != null) Destroy(_handle.gameObject);
            if (_mat != null) Destroy(_mat);
        }

        float CenterY => -_rect.rect.height / 2f - EdgeGap;

        void LateUpdate()
        {
            if (_handle == null) BuildHandle();
            if (_handle == null) return;
            bool visible = _held || ContentVisible();
            if (_handle.gameObject.activeSelf != visible) _handle.gameObject.SetActive(visible);
            Register(visible);
            if (!visible) return;

            float target = _held || _hover ? 1f : 0f;
            if (!Mathf.Approximately(target, _to))
            {
                _from = _level;
                _to = target;
                _t0 = Time.unscaledTime;
                _dur = UiKit.Theme != null ? UiKit.Theme.Duration(target > _level ? UiKit.Theme.hoverIn : UiKit.Theme.hoverOut) : 0f;
            }
            _level = Mathf.LerpUnclamped(_from, _to, Motion.Progress(Time.unscaledTime - _t0, _dur));

            float k = Mathf.Lerp(1f, HoverScale, _level);
            _handle.localPosition = new Vector3(0f, CenterY, 0f);
            _handle.localScale = new Vector3(PillThickness * k, PillLength * k / 2f, PillThickness * k);
            SetColor(Mathf.Lerp(RestAlpha, HoverAlpha, _level), Mathf.Lerp(0.8f, 1f, _level));
        }

        bool ContentVisible()
        {
            for (int i = 0; i < _rect.childCount; i++)
            {
                var c = _rect.GetChild(i);
                if (c == _handle || !c.gameObject.activeSelf) continue;
                var layer = c.GetComponent<UiLayer>();
                if (layer == null || layer.Visible) return true;
            }
            return false;
        }

        void BuildHandle()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = HandleName;
            Destroy(go.GetComponent<Collider>());
            go.layer = 0; // the eye cameras draw it; the canvas layers 30 / 31 are hidden from them
            _handle = go.transform;
            _handle.SetParent(_rect, false);
            _handle.localRotation = Quaternion.Euler(0f, 0f, 90f); // the capsule's long axis lies along the panel's x

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            _mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            // transparent surface, so the opacity animates (URP Unlit)
            _mat.SetFloat("_Surface", 1f);
            _mat.SetFloat("_Blend", 0f);
            _mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetFloat("_ZWrite", 0f);
            _mat.SetOverrideTag("RenderType", "Transparent");
            _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            _renderer = go.GetComponent<Renderer>();
            _renderer.sharedMaterial = _mat;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            SetColor(RestAlpha, 0.8f);
            _panel.Place(); // the handle needs room below the panel: lift it off the floor if needed
        }

        void SetColor(float alpha, float gray)
        {
            if (_mat == null) return;
            var c = new Color(0.88f * gray, 0.88f * gray, 0.92f * gray, alpha);
            _mat.SetColor("_BaseColor", c);
            _mat.color = c;
        }

        void Register(bool on)
        {
            if (on == _registered) return;
            _registered = on;
            if (on) RayTargets.Register(this);
            else
            {
                RayTargets.Unregister(this);
                _hover = false;
            }
        }

        // ---- IRayTarget ----------------------------------------------------------------------------------------------

        bool PlanePoint(Ray ray, out float t, out Vector2 local)
        {
            local = Vector2.zero;
            t = 0f;
            float denom = Vector3.Dot(ray.direction, _rect.forward);
            if (Mathf.Abs(denom) < 1e-5f) return false;
            t = Vector3.Dot(_rect.position - ray.origin, _rect.forward) / denom;
            if (t <= 0f) return false;
            local = _rect.InverseTransformPoint(ray.origin + ray.direction * t);
            return true;
        }

        public bool RayHit(Ray ray, float maxDistance, out float distance)
        {
            distance = 0f;
            if (_handle == null || !_handle.gameObject.activeInHierarchy || !PlanePoint(ray, out distance, out Vector2 local)) return false;
            if (distance > maxDistance) return false;
            return Mathf.Abs(local.x) <= GrabWidth / 2f && Mathf.Abs(local.y - CenterY) <= GrabHeight / 2f;
        }

        public void RayHover(bool on) => _hover = on;

        public void RayPress(Ray ray)
        {
            _held = true;
            if (!PlanePoint(ray, out _, out Vector2 local)) local = new Vector2(0f, CenterY);
            // the grab point relative to the panel center as an angle (1 canvas unit = 0.001 of the distance)
            _grabXDeg = Mathf.Atan(local.x * OverlayPanel.ScalePerMeter) * Mathf.Rad2Deg;       // right of center
            _grabYDeg = Mathf.Atan(-local.y * OverlayPanel.ScalePerMeter) * Mathf.Rad2Deg;      // below center
            _grabDistance = _panel.distanceM;
            var rig = FindAnyObjectByType<OVRCameraRig>();
            _grabHand = rig != null ? (ray.origin - rig.centerEyeAnchor.position).magnitude : 0f;
        }

        public Vector3 RayDrag(Ray ray)
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig == null) return ray.origin + ray.direction * _panel.distanceM;
            Vector3 head = rig.centerEyeAnchor.position;

            // distance: the hand moving away from the head pushes the panel away, pulling it back brings the panel nearer
            float hand = (ray.origin - head).magnitude;
            _panel.distanceM = Mathf.Clamp(_grabDistance + (hand - _grabHand) * DistanceGain, MinDistanceM, MaxDistanceM);

            // direction: where the ray meets the sphere of the panel's distance around the head (the far hit)
            Vector3 oc = ray.origin - head;
            float b = Vector3.Dot(oc, ray.direction);
            float c = oc.sqrMagnitude - _panel.distanceM * _panel.distanceM;
            float disc = b * b - c;
            Vector3 point = disc >= 0f ? ray.origin + ray.direction * (-b + Mathf.Sqrt(disc)) : ray.origin + ray.direction * _panel.distanceM;

            Vector3 v = point - head;
            if (v.sqrMagnitude > 1e-6f)
            {
                float yawWorld = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                float pitchUp = Mathf.Asin(Mathf.Clamp(v.y / v.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
                _panel.yawOffsetDeg = Mathf.DeltaAngle(0f, yawWorld - rig.transform.eulerAngles.y - _grabXDeg);
                _panel.pitchDownDeg = Mathf.Clamp(-(pitchUp + _grabYDeg), MinPitchDownDeg, MaxPitchDownDeg);
                _panel.Place();
            }
            return point;
        }

        public void RayRelease() => _held = false;
    }
}
