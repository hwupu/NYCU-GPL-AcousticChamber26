using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// The design-system button (DesignSystem section 4, ADR-0005). A uGUI <see cref="Button"/> (so <c>onClick</c> and
    /// the pointer work as before) whose look comes from the theme: style colors per state, capsule shape, settle motion
    /// on unscaled time. States brighten normal → hover → pressed; disabled = 40 % opacity. Hover ramps in fast and out
    /// slower; pressed is instant and scales to 97 %; release settles back. Restyles itself when the theme is edited.
    /// </summary>
    public class UiButton : Button
    {
        [SerializeField] UiStyle style = UiStyle.Gray;
        [Tooltip("Borderless hover colors for a button inside a raised group.")]
        [SerializeField] bool inGroup;

        Image _fill;
        TMP_Text _label;
        Color _from, _to, _labelFrom, _labelTo;
        float _scaleFrom = 1f, _scaleTo = 1f;
        float _start, _duration;
        bool _animating, _pressed;
        Visual _visual = Visual.Normal;
        UiIcon[] _icons;

        enum Visual { Normal, Hover, Pressed, Disabled }

        public UiStyle Style
        {
            get => style;
            set { style = value; Restyle(); }
        }

        public bool InGroup
        {
            get => inGroup;
            set { inGroup = value; Restyle(); }
        }

        /// <summary>
        /// The active state of a toggle or of the chosen button in a set (DesignSystem section 4, segmented controls): the fill
        /// is the theme's <c>select</c> colour, hover a step lighter. Not for Prominent or Destructive buttons.
        /// </summary>
        public bool Selected
        {
            get => _selected;
            set { if (_selected == value) return; _selected = value; Restyle(); }
        }

        bool _selected;

        /// <summary>True while the trigger holds this button down (set by <see cref="UiPointer"/>).</summary>
        public bool IsPressedVisual => _pressed;

        public TMP_Text Label => _label != null ? _label : (_label = GetComponentInChildren<TMP_Text>(true));

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
            _fill = targetGraphic as Image ?? GetComponent<Image>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UiTheme.Edited += Restyle;
            Restyle();
        }

        protected override void OnDisable()
        {
            UiTheme.Edited -= Restyle;
            _pressed = false;
            transform.localScale = Vector3.one;
            base.OnDisable();
        }

        /// <summary>Trigger down on this button: instant pressed look (the pointer invokes onClick).</summary>
        public void Press()
        {
            if (!IsInteractable()) return;
            _pressed = true;
            Go(Visual.Pressed, 0f);
        }

        /// <summary>Trigger up: settle back to hover (ray still on it) or normal.</summary>
        public void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            // the click may have disabled the button (Revert, Save): then it stays disabled
            if (!IsInteractable()) Go(Visual.Disabled, 0f);
            else Go(UiPointer.Hovered == this ? Visual.Hover : Visual.Normal, Theme.Duration(Theme.release));
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (!gameObject.activeInHierarchy) return;
            Visual v = state == SelectionState.Disabled ? Visual.Disabled
                     : _pressed ? Visual.Pressed
                     : state == SelectionState.Highlighted || state == SelectionState.Pressed ? Visual.Hover
                     : Visual.Normal;
            float d = instant ? 0f
                    : v == Visual.Hover ? Theme.Duration(Theme.hoverIn)
                    : v == Visual.Normal && _visual == Visual.Pressed ? Theme.Duration(Theme.release)
                    : v == Visual.Normal ? Theme.Duration(Theme.hoverOut)
                    : 0f;
            Go(v, d);
        }

        /// <summary>Re-reads the theme and snaps to the current state.</summary>
        public void Restyle()
        {
            if (this == null) return;
            if (_fill == null) _fill = targetGraphic as Image ?? GetComponent<Image>();
            if (Label != null)
            {
                bool bold = style == UiStyle.Prominent || style == UiStyle.Destructive;
                var font = bold && Theme.bold != null ? Theme.bold : Theme.regular;
                if (font != null) _label.font = font;
                _label.fontStyle = FontStyles.Normal;
            }
            Go(!IsInteractable() ? Visual.Disabled : _pressed ? Visual.Pressed : UiPointer.Hovered == this ? Visual.Hover : Visual.Normal, 0f);
        }

        void Go(Visual v, float duration)
        {
            _visual = v;
            TargetLook(v, out var fill, out var label, out var scale);
            _from = _fill != null ? _fill.color : fill;
            _labelFrom = Label != null ? _label.color : label;
            _scaleFrom = transform.localScale.x;
            _to = fill;
            _labelTo = label;
            _scaleTo = scale;
            _start = Time.unscaledTime;
            _duration = duration;
            _animating = true;
            Apply(duration <= 0f ? 1f : 0f);
            if (duration <= 0f) _animating = false;
        }

        void TargetLook(Visual v, out Color fill, out Color label, out float scale)
        {
            var c = Theme.Colors(style);
            if (inGroup && (style == UiStyle.Borderless || style == UiStyle.DestructiveText))
            {
                c.normal = Theme.borderlessInGroup.normal;
                c.hover = Theme.borderlessInGroup.hover;
                c.pressed = Theme.borderlessInGroup.pressed;
            }
            if (_selected && style != UiStyle.Prominent && style != UiStyle.Destructive)
            {
                c.normal = Theme.select;
                c.hover = Theme.gray.pressed;
                c.pressed = Theme.gray.pressed;
            }
            label = c.label;
            scale = 1f;
            switch (v)
            {
                case Visual.Hover: fill = c.hover; break;
                case Visual.Pressed: fill = c.pressed; scale = Theme.pressedScale; break;
                case Visual.Disabled:
                    fill = c.normal;
                    fill.a *= Theme.disabledAlpha;
                    // no fill: a faded white label reads like a plain text label, so use the dim text color instead
                    if (style == UiStyle.Borderless || style == UiStyle.DestructiveText) label = Theme.text3;
                    else label.a *= Theme.disabledAlpha;
                    break;
                default: fill = c.normal; break;
            }
        }

        void LateUpdate()
        {
            if (!_animating) return;
            float p = Motion.Progress(Time.unscaledTime - _start, _duration);
            Apply(p);
            if (p >= 1f) _animating = false;
        }

        void Apply(float p)
        {
            if (_fill != null) _fill.color = LerpFill(_from, _to, p);
            if (Label != null) _label.color = Color.LerpUnclamped(_labelFrom, _labelTo, p);
            if (_icons == null) _icons = GetComponentsInChildren<UiIcon>(true);
            var lc = Label != null ? _label.color : _labelTo;
            foreach (var ic in _icons) if (ic != null && ic.FollowsLabel) ic.Color = lc;
            float s = Mathf.LerpUnclamped(_scaleFrom, _scaleTo, p);
            transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Fades a transparent fill in its target's own color (no gray or white flash on the way).</summary>
        static Color LerpFill(Color a, Color b, float p)
        {
            if (a.a <= 0f) a = new Color(b.r, b.g, b.b, 0f);
            if (b.a <= 0f) b = new Color(a.r, a.g, a.b, 0f);
            return Color.LerpUnclamped(a, b, p);
        }

        static UiTheme Theme => UiKit.Theme;

        /// <summary>Current fill color (tests).</summary>
        public Color CurrentFill => _fill != null ? _fill.color : Color.clear;

        /// <summary>Jumps to the end of a running transition (tests).</summary>
        public void FinishTransition()
        {
            if (!_animating) return;
            Apply(1f);
            _animating = false;
        }
    }
}
