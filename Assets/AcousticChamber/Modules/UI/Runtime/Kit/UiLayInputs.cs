using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiLay
    {
        /// <summary>
        /// A row with a switch at the right (100 x 60 u); the whole row toggles it. <paramref name="changed"/> gets the new state.
        /// </summary>
        public static UiSwitch SwitchRow(Transform group, string name, string title, string subtitle, bool on, Action<bool> changed,
                                         string iconLeading = null)
        {
            UiSwitch sw = null;
            var row = Row(group, name, title, subtitle, null, iconLeading, null, () =>
            {
                sw.On = !sw.On;
                changed?.Invoke(sw.On);
            });
            sw = UiSwitch.Create(row, "Switch", Vector2.zero, on);
            Size(sw.gameObject, UiSwitch.Width, UiSwitch.Height, UiSwitch.Width, UiSwitch.Height, 0f, 0f);
            return sw;
        }

        /// <summary>
        /// UiTextField: a <c>raised</c> capsule, 80 u high, showing a value (or a placeholder in <c>text3</c>); focused = a 4 u
        /// <c>select</c> ring. It shows a value and does not own the keyboard (fed by an <see cref="ITextEntry"/>).
        /// </summary>
        public static UiTextField TextField(Transform parent, string name, string value = null, string placeholder = null,
                                            bool alignLeft = false)
        {
            var th = UiKit.Theme;
            var node = Node(parent, name);
            var ring = UiKit.Fill(UiKit.Rect(node, "Ring", Vector2.zero, Vector2.zero), -th.inset);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.color = th.select;
            ringImg.raycastTarget = false;
            UiKit.Round(ringImg, th.buttonHeight / 2f + th.inset);
            ring.gameObject.SetActive(false);
            Fill(node, th.raised, th.buttonHeight / 2f);
            H(node.gameObject, th.gap, Pad(40, 0, 40, 0), alignLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            Size(node.gameObject, minH: th.buttonHeight, prefH: th.buttonHeight, flexW: 1f, flexH: 0f);
            var label = Label(node, "Value", "", th.bodySize, th.text);
            var f = node.gameObject.AddComponent<UiTextField>();
            f.Init(label, ring.gameObject, placeholder);
            f.Value = value;
            return f;
        }

        /// <summary>UiStepper: a "-" and a "+" small borderless in-group button around the value (at least 120 u wide).</summary>
        public static UiStepper Stepper(Transform parent, string name, float value, float min, float max, float step,
                                        Func<float, string> format = null, Action<float> changed = null)
        {
            var th = UiKit.Theme;
            var node = Node(parent, name);
            H(node.gameObject, th.inset, null, TextAnchor.MiddleCenter);
            var minus = Button(node, "Minus", null, UiStyle.Borderless, iconLeading: "minus", small: true);
            minus.InGroup = true;
            var box = Node(node, "Value Box");
            V(box.gameObject, 0f, Pad(0, 0, 0, 2f * th.labelNudge), TextAnchor.MiddleCenter);
            Size(box.gameObject, minW: 120f);
            var text = UiKit.Text(box, "Value", "", th.bodySize, Vector2.zero, Vector2.zero);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var plus = Button(node, "Plus", null, UiStyle.Borderless, iconLeading: "plus", small: true);
            plus.InGroup = true;
            var s = node.gameObject.AddComponent<UiStepper>();
            s.Init(minus, plus, text, min, max, step, format, changed);
            s.Value = value;
            return s;
        }
    }

    /// <summary>The value display of <see cref="UiLay.TextField"/>.</summary>
    public class UiTextField : MonoBehaviour
    {
        TextMeshProUGUI _label;
        GameObject _ring;
        string _placeholder, _value;
        bool _focused;

        internal void Init(TextMeshProUGUI label, GameObject ring, string placeholder)
        {
            _label = label;
            _ring = ring;
            _placeholder = placeholder;
        }

        /// <summary>The text shown; empty or null shows the placeholder.</summary>
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                bool empty = string.IsNullOrEmpty(value);
                _label.text = empty ? _placeholder ?? "" : value;
                _label.color = empty ? UiKit.Theme.text3 : UiKit.Theme.text;
            }
        }

        /// <summary>True = the 4 u select ring (this entry takes the next key).</summary>
        public bool Focused
        {
            get => _focused;
            set
            {
                _focused = value;
                _ring.SetActive(value);
            }
        }
    }

    /// <summary>A - / + pair around a value (<see cref="UiLay.Stepper"/>); the buttons disable at the limits.</summary>
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

    /// <summary>
    /// UiDialog (docs/design): a decision. A <see cref="UiPanel"/> 820 u wide, no navigation bar and no grabber; title and
    /// message left-aligned, actions right-aligned and sized to their labels, the main action last. The height follows the
    /// content: size the overlay canvas to <see cref="UiDialog.Size"/> after building.
    /// </summary>
    public class UiDialog
    {
        public UiPanel Panel { get; private set; }
        /// <summary>Where the actions go (right-aligned row).</summary>
        public RectTransform Actions { get; private set; }
        public Vector2 Size { get; private set; }

        public static UiDialog Create(Transform canvas, string name, string title, string message = null)
        {
            var th = UiKit.Theme;
            var d = new UiDialog { Panel = UiPanel.Create(canvas, name, new Vector2(th.dialogWidth, 400f), startHidden: true) };
            d.Panel.Modal = true;
            d.Panel.HasGrabber = false;
            var body = d.Panel.Surface.Body;
            var content = UiLay.Node(body, "Content");
            UiLay.V(content.gameObject, 0f, UiLay.Pad(0, 48, 0, 28), TextAnchor.UpperLeft);
            UiLay.Size(content.gameObject, flexW: 1f);
            var t = UiKit.Text(content, "Title", title, th.headingSize, Vector2.zero, Vector2.zero, true, th.text, TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            UiLay.Size(t.gameObject, prefW: 0f, flexW: 1f); // preferred width 0 + flexible: it takes the width and wraps
            if (!string.IsNullOrEmpty(message))
            {
                var pad = UiLay.Node(content, "Message Box");
                UiLay.V(pad.gameObject, 0f, UiLay.Pad(0, 16, 0, 0), TextAnchor.UpperLeft);
                UiLay.Size(pad.gameObject, prefW: 0f, flexW: 1f);
                var m = UiKit.Text(pad, "Message", message, th.bodySize, Vector2.zero, Vector2.zero, false, th.text2, TextAlignmentOptions.TopLeft);
                m.textWrappingMode = TextWrappingModes.Normal;
                UiLay.Size(m.gameObject, prefW: 0f, flexW: 1f);
            }
            d.Actions = UiLay.Node(content, "Actions");
            UiLay.H(d.Actions.gameObject, 8f, UiLay.Pad(0, 40, -20, 0), TextAnchor.MiddleRight);
            UiLay.Size(d.Actions.gameObject, prefW: 0f, flexW: 1f);
            d.Panel.Surface.Finish();
            return d;
        }

        /// <summary>Measures the content and opens the dialog in place on top of the canvas's panels (pushed back); call after adding the actions.</summary>
        public void Show()
        {
            Fit();
            Panel.Present();
        }

        /// <summary>Closes the dialog: the panels behind come forward, then it fades out.</summary>
        public void Close() => Panel.Close();

        /// <summary>Re-measures the content and sets the surface height; call after adding the actions.</summary>
        public void Fit()
        {
            var surface = (RectTransform)Panel.Surface.transform;
            var page = (RectTransform)Panel.Surface.Root.transform;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            float h = LayoutUtility.GetPreferredHeight(page);
            surface.sizeDelta = new Vector2(UiKit.Theme.dialogWidth, h);
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            Size = surface.sizeDelta;
        }
    }
}
