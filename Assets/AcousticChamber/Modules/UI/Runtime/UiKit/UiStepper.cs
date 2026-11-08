using System;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>UiStepper: a "-" and a "+" small borderless in-group button around the value (at least 120 u wide).</summary>
        public static UiStepper Stepper(Transform parent, string name, float value, float min, float max, float step,
                                        Func<float, string> format = null, Action<float> changed = null)
        {
            var th = Theme;
            var node = Node(parent, name);
            H(node.gameObject, th.inset, null, TextAnchor.MiddleCenter);
            var minus = Button(node, "Minus", null, UiStyle.Borderless, iconLeading: "minus", small: true);
            minus.InGroup = true;
            var box = Node(node, "Value Box");
            V(box.gameObject, 0f, Pad(0, 0, 0, 2f * th.labelNudge), TextAnchor.MiddleCenter);
            Size(box.gameObject, minW: 120f);
            var text = Text(box, "Value", "", th.bodySize, Vector2.zero, Vector2.zero);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var plus = Button(node, "Plus", null, UiStyle.Borderless, iconLeading: "plus", small: true);
            plus.InGroup = true;
            var s = node.gameObject.AddComponent<UiStepper>();
            s.Init(minus, plus, text, min, max, step, format, changed);
            s.Value = value;
            return s;
        }
    }

    /// <summary>A - / + pair around a value (<see cref="UiKit.Stepper"/>); the buttons disable at the limits.</summary>
    public class UiStepper : MonoBehaviour
    {
        UiButton _minus, _plus;
        TextMeshProUGUI _text;
        float _min, _max, _step, _value;
        Func<float, string> _format;
        Action<float> _changed;

        internal void Init(UiButton minus, UiButton plus, TextMeshProUGUI text, float min, float max, float step,
                           Func<float, string> format, Action<float> changed)
        {
            _minus = minus;
            _plus = plus;
            _text = text;
            _min = min;
            _max = max;
            _step = step;
            _format = format;
            _changed = changed;
            minus.onClick.AddListener(() => Bump(-_step));
            plus.onClick.AddListener(() => Bump(_step));
        }

        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp(value, _min, _max);
                _text.text = _format != null ? _format(_value) : _value.ToString("0.##");
                _minus.interactable = _value > _min;
                _plus.interactable = _value < _max;
            }
        }

        void Bump(float d)
        {
            float before = _value;
            Value = Mathf.Round((_value + d) * 1e5f) / 1e5f;
            if (!Mathf.Approximately(before, _value)) _changed?.Invoke(_value);
        }
    }
}
