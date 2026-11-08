using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// Lets the participant move an <see cref="OverlayPanel"/> (PO 2026-11-08), visionOS style: a small pill below the panel, outside it.
    /// Point at it (or near it: the target zone is much larger than the pill), hold the trigger and move the controller: swinging the ray
    /// turns the panel around the head (left / right, up / down) at a fixed distance (the canvas scale follows the distance, so moving it
    /// nearer or farther would look the same). Enable with <see cref="OverlayPanel.Movable"/>. The pill is a real 3D object, not part of the overlay canvas
    /// (the compositor layer clips everything outside the canvas); on hover or while held it grows and brightens with the settle curve.
    /// It is a 3D ray target (<see cref="IRayTarget"/>), shown only while some content is visible.
    /// </summary>
    [RequireComponent(typeof(OverlayPanel))]
    public class UiGrabber : MonoBehaviour, IRayTarget
    {
        public const string HandleName = "Move Handle";
        /// <summary>
        /// The pill (a rounded rectangle, canvas units, 1 unit = 0.057 deg): 120 x 8 at rest, 220 x 16 at full hover, and the gap
        /// between the surface's bottom edge and the pill's top edge (the top edge stays put while the pill grows downward).
        /// </summary>
        public const float PillLength = 120f, PillThickness = 8f, PillLengthHover = 220f, PillThicknessHover = 16f, EdgeGap = 16f;
        /// <summary>The ray reacts inside this zone just below the surface (canvas units, centered at the panel's x), much larger than the pill.</summary>
        public const float GrabWidth = 520f, GrabHeight = 120f;
        /// <summary>The pill's opacity at rest and at full hover.</summary>
        public const float RestAlpha = 0.45f, HoverAlpha = 0.95f;
        /// <summary>Space the pill needs below the panel (canvas units), for the floor clearance of <see cref="OverlayPanel"/>.</summary>
        public const float Reserve = EdgeGap + PillThicknessHover + 40f;
        public const float MinPitchDownDeg = -30f, MaxPitchDownDeg = 60f;

        OverlayPanel _panel;
        RectTransform _rect;
        Transform _handle;
        Renderer _renderer;
        Material _mat;
        bool _registered, _hover, _held;
        float _grabXDeg, _grabYDeg;
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
            if (_mesh != null) Destroy(_mesh);
        }

        float _thickness = PillThickness;
        Mesh _mesh;

        /// <summary>Pill thickness at hover amount <paramref name="level"/> (0 to 1).</summary>
        public static float ThicknessAt(float level) => Mathf.Lerp(PillThickness, PillThicknessHover, level);

        /// <summary>Pill length at hover amount <paramref name="level"/> (0 to 1).</summary>
        public static float LengthAt(float level) => Mathf.Lerp(PillLength, PillLengthHover, level);

        /// <summary>Center of the pill under the surface for a pill of <paramref name="thickness"/>: its top edge sits <see cref="EdgeGap"/> below the surface.</summary>
        float PillCenterY(float thickness) => -_rect.rect.height / 2f - EdgeGap - thickness / 2f;

        /// <summary>Center of the ray zone: it starts at the surface's bottom edge and reaches down.</summary>
        float ZoneCenterY => -_rect.rect.height / 2f - GrabHeight / 2f;

        /// <summary>Fills <paramref name="mesh"/> with a rounded rectangle (stadium: the end radius is half the thickness) in the xy plane, double-sided.</summary>
        public static void BuildPill(Mesh mesh, float length, float thickness, int arcSegments = 8)
        {
            float r = thickness / 2f, half = Mathf.Max(length / 2f - r, 0f);
            int n = arcSegments * 2 + 2; // perimeter points: right cap (-90..90), left cap (90..270)
            var v = new Vector3[n + 1];
            for (int i = 0; i <= arcSegments; i++)
            {
                float a = Mathf.Lerp(-90f, 90f, i / (float)arcSegments) * Mathf.Deg2Rad;
                v[i] = new Vector3(half + Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                v[arcSegments + 1 + i] = new Vector3(-half - Mathf.Cos(a) * r, -Mathf.Sin(a) * r, 0f);
            }
            v[n] = Vector3.zero;
            var t = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                t[i * 6 + 0] = n; t[i * 6 + 1] = i; t[i * 6 + 2] = j;   // front
                t[i * 6 + 3] = n; t[i * 6 + 4] = j; t[i * 6 + 5] = i;   // back
            }
            mesh.Clear();
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.RecalculateBounds();
        }

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

            float thickness = ThicknessAt(_level);
            if (!Mathf.Approximately(thickness, _thickness))
            {
                _thickness = thickness;
                BuildPill(_mesh, LengthAt(_level), thickness);
            }
            _handle.localPosition = new Vector3(0f, PillCenterY(thickness), 0f);
            SetColor(Mathf.Lerp(RestAlpha, HoverAlpha, _level) * PanelAlpha(), Mathf.Lerp(0.8f, 1f, _level));
        }

        /// <summary>Opacity of a <see cref="UiPanel"/> under the canvas (the handle fades with it); 1 without one.</summary>
        float PanelAlpha()
        {
            for (int i = 0; i < _rect.childCount; i++)
            {
                var up = _rect.GetChild(i).GetComponent<UiPanel>();
                if (up != null) return up.Alpha;
            }
            return 1f;
        }

        bool ContentVisible()
        {
            for (int i = 0; i < _rect.childCount; i++)
            {
                var c = _rect.GetChild(i);
                if (c == _handle || !c.gameObject.activeSelf) continue;
                var up = c.GetComponent<UiPanel>();
                if (up != null && up.GrabberVisible) return true;
            }
            return false;
        }

        void BuildHandle()
        {
            var go = new GameObject(HandleName, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = 0; // the eye cameras draw it; the canvas layers 30 / 31 are hidden from them
            _handle = go.transform;
            _handle.SetParent(_rect, false);
            _mesh = new Mesh { name = "Move Handle Pill" };
            _thickness = PillThickness;
            BuildPill(_mesh, PillLength, PillThickness);
            go.GetComponent<MeshFilter>().sharedMesh = _mesh;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            _mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            // transparent surface, so the opacity animates (URP Unlit)
            _mat.SetFloat("_Surface", 1f);
            _mat.SetFloat("_Blend", 0f);
            _mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetFloat("_ZWrite", 0f);
            _mat.SetFloat("_Cull", 0f); // double-sided
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
            return Mathf.Abs(local.x) <= GrabWidth / 2f && Mathf.Abs(local.y - ZoneCenterY) <= GrabHeight / 2f;
        }

        public void RayHover(bool on) => _hover = on;

        public void RayPress(Ray ray)
        {
            _held = true;
            if (!PlanePoint(ray, out _, out Vector2 local)) local = new Vector2(0f, ZoneCenterY);
            // the grab point relative to the panel center as an angle (1 canvas unit = 0.001 of the distance)
            _grabXDeg = Mathf.Atan(local.x * OverlayPanel.ScalePerMeter) * Mathf.Rad2Deg;       // right of center
            _grabYDeg = Mathf.Atan(-local.y * OverlayPanel.ScalePerMeter) * Mathf.Rad2Deg;      // below center
        }

        public Vector3 RayDrag(Ray ray)
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig == null) return ray.origin + ray.direction * _panel.distanceM;
            Vector3 head = rig.centerEyeAnchor.position;

            // the distance stays as it is (the canvas scale follows it, so a different distance would look the same)
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
