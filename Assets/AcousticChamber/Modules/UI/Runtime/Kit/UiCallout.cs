using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// UiCallout (docs/design): a small annotation attached to a 3D object ("drag me to the floor"). A flat <c>gray</c> pill
    /// (72 u high) with an icon and an optional label and a tail whose tip is at the target. It is a world-space canvas on the
    /// default layer (not an overlay canvas: that clips everything outside it). It follows <see cref="Target"/>, faces the head
    /// and keeps its angular size (1 u = 0.057 deg). No periodic animation: a simple fade, with <c>delay</c> before the fade-in
    /// (e.g. after the scene loaded); call <see cref="Dismiss"/> when the user touches the object.
    /// </summary>
    public class UiCallout : MonoBehaviour
    {
        public enum PlacementMode { Top, Bottom, Left, Right }

        public const float PillHeight = 72f, TailLength = 14f, TailHalfWidth = 14f;

        /// <summary>World point the tail's tip touches: the target position plus <see cref="Offset"/>.</summary>
        public Transform Target;
        public Vector3 Offset;

        CanvasGroup _group;
        float _from, _to, _t0, _dur, _delay;
        bool _shown, _began;

        public bool IsShown => _shown;

        /// <summary>The pill (a laid-out horizontal group): icon and label.</summary>
        public RectTransform Pill { get; private set; }

        public static UiCallout Create(Transform target, string label, string icon = "hand", PlacementMode placement = PlacementMode.Top,
                                       Vector3? offset = null)
        {
            var th = UiKit.Theme;
            var go = new GameObject("Callout", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var root = (RectTransform)go.transform;
            root.sizeDelta = Vector2.zero;
            var c = go.AddComponent<UiCallout>();
            c.Target = target;
            c.Offset = offset ?? Vector3.zero;
            c._group = go.GetComponent<CanvasGroup>();
            c._group.alpha = 0f;
            c._group.blocksRaycasts = false;
            c._group.interactable = false;

            var color = th.gray.pressed;

            // pill: horizontal layout, sized by its content; centered on its place relative to the tail tip (the canvas origin)
            var pill = UiLay.Node(root, "Pill");
            UiLay.Fill(pill, color, PillHeight / 2f);
            UiLay.H(pill.gameObject, 12f, UiLay.Pad(28, 0, 28, 0), TextAnchor.MiddleCenter);
            UiLay.Size(pill.gameObject, minH: PillHeight, prefH: PillHeight);
            UiLay.Icon(pill, "Icon", icon, 36f);
            if (!string.IsNullOrEmpty(label)) UiLay.Label(pill, "Label", label, th.smallSize, th.text);
            var fit = pill.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            c.Pill = pill;

            // tail: a diamond behind the pill; its corner is the tip (the origin), only a triangle of TailLength shows
            var tail = UiKit.Box(root, "Tail", Vector2.zero, new Vector2(TailHalfWidth * 2f, TailHalfWidth * 2f), color, 0f);
            tail.transform.SetAsFirstSibling();
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(pill);
            c.Place(placement);
            return c;
        }

        /// <summary>Puts the pill beside the tail tip: Top = above it (the tail points down at the target), and so on.</summary>
        public void Place(PlacementMode placement)
        {
            Vector2 dir = placement == PlacementMode.Top ? Vector2.up : placement == PlacementMode.Bottom ? Vector2.down
                        : placement == PlacementMode.Left ? Vector2.left : Vector2.right;
            var size = Pill.rect.size;
            float reach = TailLength + (Mathf.Abs(dir.x) > 0f ? size.x : size.y) / 2f;
            Pill.anchoredPosition = dir * reach;
            var tail = transform.Find("Tail");
            // the diamond's center sits half its diagonal from the tip, so the corner touches the tip
            tail.GetComponent<RectTransform>().anchoredPosition = dir * (TailHalfWidth * Mathf.Sqrt(2f));
        }

        /// <summary>Fades in after <paramref name="delaySeconds"/> (the fade-in only; a fade-out starts at once).</summary>
        public void Show(float delaySeconds = 0f)
        {
            if (_shown) return;
            Begin(true, delaySeconds);
        }

        /// <summary>Fades out (the user touched the object).</summary>
        public void Dismiss()
        {
            if (!_shown) return;
            Begin(false, 0f);
        }

        void Begin(bool shown, float delay)
        {
            _from = AlphaAt(Time.unscaledTime);
            _began = true;
            _shown = shown;
            _to = shown ? 1f : 0f;
            _t0 = Time.unscaledTime;
            _delay = shown ? delay : 0f;
            _dur = UiKit.Theme.Duration(UiKit.Theme.sheet);
            _group.alpha = AlphaAt(_t0);
        }

        /// <summary>The opacity at <paramref name="now"/> (unscaled time): idle until the delay has passed, then the settle fade.</summary>
        public float AlphaAt(float now)
        {
            if (!_began) return 0f;
            float elapsed = Mathf.Max(0f, now - _t0 - _delay);
            if (_dur <= 0f) return now - _t0 >= _delay ? _to : _from;
            return Mathf.LerpUnclamped(_from, _to, Motion.Progress(elapsed, _dur));
        }

        void LateUpdate()
        {
            if (Target == null) return;
            _group.alpha = AlphaAt(Time.unscaledTime);
            var head = Head();
            Vector3 tip = Target.position + Offset;
            transform.position = tip;
            if (head == null) return;
            Vector3 toTip = tip - head.position;
            float dist = Mathf.Max(toTip.magnitude, 0.3f);
            if (toTip.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(toTip, Vector3.up);
            transform.localScale = Vector3.one * (OverlayPanel.ScalePerMeter * dist);
        }

        static Transform Head()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null) return rig.centerEyeAnchor;
            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}
