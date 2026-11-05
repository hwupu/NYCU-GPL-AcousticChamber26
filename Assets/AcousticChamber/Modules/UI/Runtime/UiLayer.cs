using System;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// One layer of a canvas: the window (<see cref="UiKit.Window"/>) or a <see cref="Sheet"/>. Animates two things on
    /// unscaled time with the settle curve (DesignSystem section 5):
    /// <list type="bullet">
    /// <item><b>shown</b> 0..1: appearing in place (scale <c>sheetScale</c> → 1 and fade in), and the reverse to leave;</item>
    /// <item><b>depth</b>: how many sheets lie on top; each one scales the layer by <c>pushScale</c> (94 %, 88 %, ...)
    /// and the layer dims to the <c>dim</c> color.</item>
    /// </list>
    /// No blur (it would cost frame rate on Quest; DesignSystem section 5). Outside Play mode every change is instant.
    /// </summary>
    public class UiLayer : MonoBehaviour
    {
        struct Tween
        {
            public float From, To, Start, Duration;
            public float Value(float now) => Mathf.LerpUnclamped(From, To, Motion.Progress(now - Start, Duration));
            public bool Done(float now) => Duration <= 0f || now - Start >= Duration;
        }

        Tween _shown, _depth;
        CanvasGroup _group;
        Image _dim;
        Action _onHidden;

        /// <summary>Number of sheets on top of this layer (0 = front).</summary>
        public int Depth { get; private set; }

        /// <summary>False once <see cref="Show"/>(false) was called.</summary>
        public bool Visible => _shown.To > 0f;

        float _appearScale = -1f;   // < 0: theme sheetScale
        float _appearSeconds = -1f; // < 0: theme sheet

        /// <summary>
        /// Makes <paramref name="target"/> a layer: adds a dim overlay (child "Push Dim", corner radius
        /// <paramref name="radius"/>, invisible at depth 0). <paramref name="shown"/>: start visible (window) or hidden
        /// (a sheet that is about to appear).
        /// </summary>
        public static UiLayer Attach(RectTransform target, float radius, bool shown)
        {
            var layer = target.gameObject.AddComponent<UiLayer>();
            layer._group = target.gameObject.AddComponent<CanvasGroup>();
            layer._dim = UiKit.Box(target, "Push Dim", Vector2.zero, Vector2.zero, Color.clear, radius);
            UiKit.Fill(layer._dim.rectTransform);
            float v = shown ? 1f : 0f;
            layer._shown = new Tween { From = v, To = v };
            layer.Apply();
            return layer;
        }

        /// <summary>
        /// Makes <paramref name="target"/> a page: appears from <c>pageScale</c> over the <c>page</c> duration, never pushed
        /// back (no dim overlay).
        /// </summary>
        public static UiLayer AttachPage(RectTransform target, bool shown)
        {
            var layer = target.gameObject.AddComponent<UiLayer>();
            layer._group = target.gameObject.AddComponent<CanvasGroup>();
            layer._appearScale = UiKit.Theme.pageScale;
            layer._appearSeconds = UiKit.Theme.page;
            float v = shown ? 1f : 0f;
            layer._shown = new Tween { From = v, To = v };
            layer.Apply();
            return layer;
        }

        /// <summary>Appears or leaves (sheet or page duration); <paramref name="onHidden"/> runs once the layer is fully hidden.</summary>
        public void Show(bool on, Action onHidden = null)
        {
            _onHidden = on ? null : onHidden;
            _shown = Start(_shown, on ? 1f : 0f, _appearSeconds >= 0f ? _appearSeconds : UiKit.Theme.sheet);
            Apply();
        }

        /// <summary>
        /// True if <paramref name="t"/> lies in a layer that is leaving (a closing sheet, a page being left): its buttons
        /// no longer take the ray.
        /// </summary>
        public static bool InLeavingLayer(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                var layer = t.GetComponent<UiLayer>();
                if (layer != null && !layer.Visible) return true;
            }
            return false;
        }

        /// <summary>Pushes the layer back by <paramref name="depth"/> levels (0 = front) over the <c>push</c> duration.</summary>
        public void SetDepth(int depth)
        {
            if (depth == Depth && _depth.To == depth) return;
            if (_dim == null) return; // pages are not pushed back
            Depth = depth;
            if (depth > 0) _dim.transform.SetAsLastSibling(); // over content added after the layer was made
            _depth = Start(_depth, depth, UiKit.Theme.push);
            Apply();
        }

        /// <summary>Jumps to the end of every running transition (tests, reduce motion).</summary>
        public void Finish()
        {
            _shown.Duration = 0f;
            _depth.Duration = 0f;
            Apply();
        }

        static Tween Start(Tween t, float to, float seconds)
        {
            float now = Time.unscaledTime;
            float d = Application.isPlaying ? UiKit.Theme.Duration(seconds) : 0f;
            return new Tween { From = t.Value(now), To = to, Start = now, Duration = d };
        }

        void LateUpdate() => Apply();

        void Apply()
        {
            if (this == null) return;
            var th = UiKit.Theme;
            float now = Time.unscaledTime;
            float shown = _shown.Value(now);
            float depth = _depth.Value(now);
            float from = _appearScale >= 0f ? _appearScale : th.sheetScale;
            float scale = Mathf.LerpUnclamped(from, 1f, shown) * Mathf.Pow(th.pushScale, depth);
            transform.localScale = new Vector3(scale, scale, 1f);
            _group.alpha = Mathf.Clamp01(shown);
            if (_dim != null)
            {
                var dim = th.dim;
                dim.a *= Mathf.Clamp01(depth);
                _dim.color = dim;
            }

            if (_onHidden != null && _shown.To <= 0f && _shown.Done(now))
            {
                var done = _onHidden;
                _onHidden = null;
                done();
            }
        }
    }
}
