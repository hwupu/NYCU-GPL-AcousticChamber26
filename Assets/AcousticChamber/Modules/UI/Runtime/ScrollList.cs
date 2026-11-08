using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// A borderless scroll list (DesignSystem section 6): no frame, no scrollbar. The content is clipped to the viewport;
    /// the edges fade (solid gradient, 56 u) only where more content waits; a thin indicator (8 u, <c>text3</c>) shows
    /// while the list moves and fades 0.6 s after. Input comes from <see cref="UiPointer"/>: right thumbstick over the
    /// list, trigger held and dragged (a press becomes a drag after 12 u; shorter presses still click), momentum after
    /// release. Put the content under <see cref="Content"/> (anchored at its top, y going down) and set
    /// <see cref="ContentHeight"/>; <see cref="TopAnchored"/> helps place children.
    /// </summary>
    public class ScrollList : MonoBehaviour
    {
        public const float FadeHeight = 56f;
        public const float IndicatorWidth = 8f;
        public const float IndicatorLinger = 0.6f;

        static readonly List<ScrollList> Active = new List<ScrollList>();
        static Sprite _gradient;

        public readonly ScrollModel Model = new ScrollModel();

        public RectTransform Viewport => (RectTransform)transform;
        public RectTransform Content { get; private set; }

        Image _fadeTop, _fadeBottom, _indicator;
        float _lastMove = -10f;
        float _contentHeight;

        /// <summary>
        /// Creates a list of <paramref name="size"/> at <paramref name="pos"/>. <paramref name="fadeColor"/>: the color
        /// behind the list (default <c>surface</c>), so the edge fades blend into it.
        /// </summary>
        public static ScrollList Create(Transform parent, string name, Vector2 pos, Vector2 size, Color? fadeColor = null)
        {
            var th = UiKit.Theme;
            var rt = UiKit.Rect(parent, name, pos, size);
            rt.gameObject.AddComponent<RectMask2D>();
            var list = rt.gameObject.AddComponent<ScrollList>();

            var content = UiKit.Rect(rt, "Content", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            list.Content = content;

            var c = fadeColor ?? th.surface;
            list._fadeTop = Fade(rt, "Fade Top", c, top: true);
            list._fadeBottom = Fade(rt, "Fade Bottom", c, top: false);

            list._indicator = UiKit.Box(rt, "Indicator", Vector2.zero, new Vector2(IndicatorWidth, 40f), th.text3, IndicatorWidth / 2f);
            var ir = list._indicator.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(1f, 1f);
            ir.pivot = new Vector2(1f, 1f);
            list.Apply();
            return list;
        }

        /// <summary>Total height of the content; the list scrolls when it is taller than the viewport.</summary>
        public float ContentHeight
        {
            get => _contentHeight;
            set
            {
                _contentHeight = Mathf.Max(0f, value);
                Content.sizeDelta = new Vector2(0f, _contentHeight);
                Model.SetExtent(_contentHeight, Viewport.rect.height);
                Apply();
            }
        }

        /// <summary>
        /// Anchors <paramref name="child"/> (a direct child of <see cref="Content"/>) at the content's top center, its top
        /// edge <paramref name="top"/> units below the content's top.
        /// </summary>
        public static void TopAnchored(RectTransform child, float top, float x = 0f)
        {
            child.anchorMin = child.anchorMax = new Vector2(0.5f, 1f);
            child.anchoredPosition = new Vector2(x, -top - child.sizeDelta.y / 2f);
        }

        public void ScrollTo(float offset)
        {
            Model.ScrollTo(offset);
            Moved();
        }

        // ---- input from the pointer ----------------------------------------------------------------------------

        /// <summary>The pointer's position on the list plane, in viewport units (false if the ray misses the plane).</summary>
        public bool LocalPoint(Ray ray, out Vector2 local)
        {
            local = default;
            var rt = Viewport;
            Vector3 n = rt.forward;
            float denom = Vector3.Dot(ray.direction, n);
            if (Mathf.Abs(denom) < 1e-5f) return false;
            float t = Vector3.Dot(rt.position - ray.origin, n) / denom;
            if (t <= 0f) return false;
            local = rt.InverseTransformPoint(ray.origin + ray.direction * t);
            return true;
        }

        /// <summary>The nearest active, reachable list whose viewport the ray hits, or null.</summary>
        public static ScrollList Under(Ray ray, float maxDistance)
        {
            ScrollList best = null;
            float bestT = maxDistance;
            foreach (var l in Active)
            {
                if (l == null || !l.isActiveAndEnabled) continue;
                if (UiPointer.Modal != null && !l.transform.IsChildOf(UiPointer.Modal)) continue;
                if (UiKit.InLeavingLayer(l.transform)) continue;
                var rt = l.Viewport;
                float denom = Vector3.Dot(ray.direction, rt.forward);
                if (Mathf.Abs(denom) < 1e-5f) continue;
                float t = Vector3.Dot(rt.position - ray.origin, rt.forward) / denom;
                if (t <= 0f || t > bestT) continue;
                Vector2 local = rt.InverseTransformPoint(ray.origin + ray.direction * t);
                if (!rt.rect.Contains(local)) continue;
                best = l;
                bestT = t;
            }
            return best;
        }

        /// <summary>True if <paramref name="worldPoint"/> on <paramref name="t"/> lies outside the viewport of a list that contains it (clipped away).</summary>
        public static bool ClipsOut(Transform t, Vector3 worldPoint)
        {
            var list = t.GetComponentInParent<ScrollList>();
            if (list == null) return false;
            Vector2 local = list.Viewport.InverseTransformPoint(worldPoint);
            return !list.Viewport.rect.Contains(local);
        }

        internal void Stick(float y, float dt)
        {
            if (Model.Stick(y, dt)) Moved();
        }

        internal void PointerDown(float y) => Model.PointerDown(y);

        internal bool PointerMove(float y, float dt)
        {
            float before = Model.Offset;
            bool drag = Model.PointerMove(y, dt);
            if (Model.Offset != before) Moved();
            return drag;
        }

        internal bool PointerUp() => Model.PointerUp();

        // ---- view ----------------------------------------------------------------------------------------------

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void LateUpdate()
        {
            if (Model.Step(Time.unscaledDeltaTime)) Moved();
            Apply();
        }

        void Moved()
        {
            _lastMove = Time.unscaledTime;
            Apply();
        }

        void Apply()
        {
            if (Content == null) return;
            var th = UiKit.Theme;
            Content.anchoredPosition = new Vector2(0f, Model.Offset);

            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 1f;
            float k = th.reduceMotion || UiKit.MotionSuppressed ? 1f : Mathf.Clamp01(dt / 0.12f);
            SetAlpha(_fadeTop, Model.MoreAbove ? 1f : 0f, k);
            SetAlpha(_fadeBottom, Model.MoreBelow ? 1f : 0f, k);

            // indicator: length = visible share, position = offset share; shown while moving, fades after
            float vh = Viewport.rect.height;
            float ch = Mathf.Max(_contentHeight, vh);
            float len = Mathf.Max(40f, vh * vh / ch);
            float y = Model.Max > 0f ? (vh - len) * (Model.Offset / Model.Max) : 0f;
            var ir = _indicator.rectTransform;
            ir.sizeDelta = new Vector2(IndicatorWidth, len);
            ir.anchoredPosition = new Vector2(0f, -y);
            float since = Time.unscaledTime - _lastMove;
            float target = Model.CanScroll && (since < IndicatorLinger || Model.Dragging) ? 1f : 0f;
            float ki = target > 0f ? 1f : (th.reduceMotion ? 1f : Mathf.Clamp01(dt / th.hoverOut));
            SetAlpha(_indicator, target, ki);
        }

        static void SetAlpha(Image img, float target, float k)
        {
            var c = img.color;
            c.a = Mathf.Lerp(c.a, target, k);
            if (Mathf.Abs(c.a - target) < 0.01f) c.a = target;
            img.color = c;
        }

        static Image Fade(RectTransform parent, string name, Color color, bool top)
        {
            var rt = UiKit.Rect(parent, name, Vector2.zero, new Vector2(0f, FadeHeight));
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, 0.5f); // centered, so the bottom fade can be turned 180° in place
            rt.anchoredPosition = new Vector2(0f, top ? -FadeHeight / 2f : FadeHeight / 2f);
            rt.sizeDelta = new Vector2(0f, FadeHeight);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Gradient;
            img.raycastTarget = false;
            color.a = 0f;
            img.color = color;
            if (!top) rt.localEulerAngles = new Vector3(0f, 0f, 180f);
            return img;
        }

        /// <summary>Vertical alpha gradient: opaque at the top, clear at the bottom.</summary>
        static Sprite Gradient
        {
            get
            {
                if (_gradient != null) return _gradient;
                const int h = 64;
                var tex = new Texture2D(1, h, TextureFormat.RGBA32, false)
                {
                    name = "GPL Fade", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
                };
                var px = new Color32[h];
                for (int y = 0; y < h; y++)
                {
                    float a = y / (h - 1f);
                    a = a * a * (3f - 2f * a); // smoothstep
                    px[y] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _gradient = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                _gradient.name = "GPL Fade";
                _gradient.hideFlags = HideFlags.DontSave;
                return _gradient;
            }
        }
    }
}
