using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// A switch (DesignSystem section 4): 100 x 60 u track, 52 u knob. Off = gray track, <c>text2</c> knob; on = prominent
    /// track, knob in the group color (<c>raised</c>). The knob settles over the hover-in duration (unscaled time). It has no hit region of its own: put
    /// it in a row that toggles it (<see cref="UiKit.SwitchRow"/>). While that row is hovered or pressed, the off track
    /// darkens to the group color (<c>raised</c>): the hovered row's fill is the track's own gray.
    /// </summary>
    public class UiSwitch : MonoBehaviour
    {
        public const float Width = 100f, Height = 60f, Knob = 52f;

        Image _track, _knob;
        bool _on;
        float _from, _to, _start, _duration;

        // row hover: the off track darkens to the group color, so it stays visible on the hovered row's fill
        UiButton _row;
        bool _rowLookup, _rowHot;
        float _hotFrom, _hotTo, _hotStart, _hotDuration;

        public bool On
        {
            get => _on;
            set
            {
                if (_on == value) return;
                _on = value;
                float now = Time.unscaledTime;
                _from = Current(now);
                _to = value ? 1f : 0f;
                _start = now;
                _duration = Application.isPlaying ? UiKit.Theme.Duration(UiKit.Theme.hoverIn * 1.5f) : 0f;
                Apply();
            }
        }

        public static UiSwitch Create(Transform parent, string name, Vector2 pos, bool on)
        {
            var th = UiKit.Theme;
            var track = UiKit.Box(parent, name, pos, new Vector2(Width, Height), th.gray.normal, Height / 2f);
            var knob = UiKit.Box(track.transform, "Knob", Vector2.zero, new Vector2(Knob, Knob), th.text2, Knob / 2f);
            var sw = track.gameObject.AddComponent<UiSwitch>();
            sw._track = track;
            sw._knob = knob;
            sw._on = on;
            sw._from = sw._to = on ? 1f : 0f;
            sw.Apply();
            return sw;
        }

        float Current(float now) => Mathf.LerpUnclamped(_from, _to, Motion.Progress(now - _start, _duration));

        void LateUpdate()
        {
            if (!_rowLookup)
            {
                _rowLookup = true;
                _row = GetComponentInParent<UiButton>();
            }
            bool hot = _row != null && (UiPointer.Hovered == _row || _row.IsPressedVisual);
            if (hot != _rowHot)
            {
                // same timing as the row's own fill (hover in fast, out slower)
                var th = UiKit.Theme;
                float now = Time.unscaledTime;
                _hotFrom = HotNow(now);
                _hotTo = hot ? 1f : 0f;
                _hotStart = now;
                _hotDuration = th.Duration(hot ? th.hoverIn : th.hoverOut);
                _rowHot = hot;
            }
            Apply();
        }

        float HotNow(float now) => Mathf.LerpUnclamped(_hotFrom, _hotTo, Motion.Progress(now - _hotStart, _hotDuration));

        void Apply()
        {
            if (_track == null) return;
            var th = UiKit.Theme;
            float t = Current(Time.unscaledTime);
            float travel = (Width - Knob) / 2f - (Height - Knob) / 2f; // 4 u from the track's end
            _knob.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, t), 0f);
            var offTrack = Color.Lerp(th.gray.normal, th.raised, HotNow(Time.unscaledTime));
            _track.color = Color.Lerp(offTrack, th.prominent.normal, t);
            _knob.color = Color.Lerp(th.text2, th.raised, t);
        }
    }
}
