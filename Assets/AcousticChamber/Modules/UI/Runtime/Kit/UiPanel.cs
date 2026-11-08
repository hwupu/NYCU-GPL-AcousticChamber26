using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// UiPanel (docs/design): the transparent root of a panel. It owns appearance (fade + scale from <c>sheetScale</c>), push back
    /// (<c>pushScale</c> per level and a dim) and the placement of the canvas (8° below eye level, or level). The rounded
    /// rectangle with the background is its child <see cref="Surface"/>; the move handle (<see cref="PanelMover"/>, a 3D object
    /// outside the canvas) reads <see cref="GrabberVisible"/> and <see cref="Alpha"/>, so it hides and fades with the panel.
    /// Put it under an overlay canvas that is sized like the surface.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UiPanel : MonoBehaviour
    {
        public enum PlacementMode { Below8, Level }

        /// <summary>The surface (rounded rectangle) of this panel.</summary>
        public UiSurface Surface { get; private set; }

        CanvasGroup _group;
        Image _dim;
        float _shownFrom, _shownTo = 1f, _shownT0, _shownDur;
        float _depthFrom, _depthTo, _depthT0, _depthDur;
        bool _shown = true;
        int _depth;

        /// <summary>Appears (true) or leaves (false) in place.</summary>
        public bool Shown
        {
            get => _shown;
            set
            {
                if (_shown == value) return;
                _shown = value;
                _shownFrom = _shownValue;
                _shownTo = value ? 1f : 0f;
                _shownT0 = Time.unscaledTime;
                _shownDur = UiKit.Theme.Duration(UiKit.Theme.sheet);
                Apply();
            }
        }

        /// <summary>Panels on top of this one (0 = front): each pushes it back to 94 % and dims it.</summary>
        public int Depth
        {
            get => _depth;
            set
            {
                if (_depth == value) return;
                _depth = value;
                _depthFrom = _depthValue;
                _depthTo = value;
                _depthT0 = Time.unscaledTime;
                _depthDur = UiKit.Theme.Duration(UiKit.Theme.push);
                Apply();
            }
        }

        /// <summary>True while the move handle may show: in front, and shown or still fading out.</summary>
        public bool GrabberVisible => _depth == 0 && (_shown || Alpha > 0.01f);

        /// <summary>Opacity of the panel (0 to 1), also applied to the move handle.</summary>
        public float Alpha => _group != null ? _group.alpha : 1f;

        float _shownValue => Mathf.LerpUnclamped(_shownFrom, _shownTo, Motion.Progress(Time.unscaledTime - _shownT0, _shownDur));
        float _depthValue => Mathf.LerpUnclamped(_depthFrom, _depthTo, Motion.Progress(Time.unscaledTime - _depthT0, _depthDur));

        /// <summary>Where the canvas hangs: 8° below eye level (default) or level (0°). The move handle keeps clear of the floor.</summary>
        public PlacementMode Placement
        {
            get => _placement;
            set
            {
                _placement = value;
                var op = GetComponentInParent<OverlayPanel>();
                if (op == null) return;
                op.pitchDownDeg = value == PlacementMode.Level ? 0f : 8f;
                op.Place();
            }
        }

        PlacementMode _placement = PlacementMode.Below8;

        /// <summary>Builds the panel root under <paramref name="canvas"/> (filling it) with its surface of <paramref name="size"/> u.</summary>
        public static UiPanel Create(Transform canvas, string name, Vector2 size)
        {
            var rt = UiKit.Fill(UiKit.Rect(canvas, name, Vector2.zero, Vector2.zero));
            var panel = rt.gameObject.AddComponent<UiPanel>();
            panel._group = rt.GetComponent<CanvasGroup>();
            panel.Surface = UiSurface.Create(rt, size);
            panel._dim = UiKit.Box(rt, "Push Dim", Vector2.zero, Vector2.zero, UiKit.Theme.dim, UiKit.Theme.panelRadius);
            UiKit.Fill(panel._dim.rectTransform);
            panel._dim.color = new Color(0f, 0f, 0f, 0f);
            panel._shownFrom = panel._shownTo = 1f;
            return panel;
        }

        void Awake()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }

        void LateUpdate() => Apply();

        void Apply()
        {
            if (_group == null) return;
            float s = _shownValue, d = _depthValue;
            _group.alpha = s;
            _group.blocksRaycasts = _shown && _depth == 0;
            float scale = Mathf.Lerp(UiKit.Theme.sheetScale, 1f, s) * Mathf.Pow(UiKit.Theme.pushScale, d);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (_dim != null) _dim.color = new Color(0f, 0f, 0f, UiKit.Theme.dim.a * Mathf.Clamp01(d));
        }
    }
}
