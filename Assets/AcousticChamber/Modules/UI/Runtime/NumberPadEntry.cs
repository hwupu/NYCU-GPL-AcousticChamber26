using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// On-canvas number pad (phone layout 3 x 4: 1-9, Clear, 0, backspace) with a display field above it, operated with the
    /// controller ray. Replaces the Quest system keyboard, which did not open reliably in HW2. The display shows the value
    /// through <see cref="Format"/> (e.g. Study ID: "1" -> "S0001"). From HW2's ParticipantIdField.
    /// Size: 400 x 480 units (keys 120 x 80, gap 20, display 400 x 80).
    /// </summary>
    public class NumberPadEntry : MonoBehaviour, ITextEntry
    {
        public const float KeyWidth = 120f, KeyHeight = 80f;
        public const float Width = 3 * KeyWidth + 2 * 20f;
        public const float Height = 5 * KeyHeight + 4 * 20f;

        public const string BackspaceLabel = "←";

        DigitEntry _model;
        TextMeshProUGUI _display;
        string _placeholder = "";

        /// <summary>Display text for a raw value; empty result shows the placeholder.</summary>
        public Func<string, string> Format = s => s;

        public string Value => _model != null ? _model.Value : "";
        public int MaxLength => _model != null ? _model.MaxLength : 0;
        public event Action<string> Changed;

        /// <summary>Builds a number pad under <paramref name="parent"/> centered at <paramref name="pos"/>.</summary>
        public static NumberPadEntry Create(Transform parent, Vector2 pos, int maxDigits, Func<string, string> format = null,
                                            string placeholder = "")
        {
            var th = UiKit.Theme;
            var root = UiKit.Rect(parent, "Number Pad", pos, new Vector2(Width, Height));
            var pad = root.gameObject.AddComponent<NumberPadEntry>();
            pad._model = new DigitEntry(maxDigits);
            pad._model.Changed += pad.OnModelChanged;
            if (format != null) pad.Format = format;
            pad._placeholder = placeholder ?? "";

            float gap = th.gap;
            float top = Height / 2f - KeyHeight / 2f;
            var field = UiKit.Box(root, "Display", new Vector2(0f, top), new Vector2(Width, KeyHeight), th.surfaceRaised);
            pad._display = UiKit.Text(field.transform, "Text", "", th.headingSize, Vector2.zero, Vector2.zero, bold: true);
            UiKit.Fill(pad._display.rectTransform, 8f);

            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", BackspaceLabel };
            for (int i = 0; i < keys.Length; i++)
            {
                int row = i / 3, col = i % 3;
                var p = new Vector2((col - 1) * (KeyWidth + gap), top - (row + 1) * (KeyHeight + gap));
                string k = keys[i];
                Button b;
                if (k == "C")
                {
                    b = UiKit.LocButton(root, "Key Clear", UiStrings.Clear, UiStyle.Neutral, p, new Vector2(KeyWidth, KeyHeight), pad.Clear);
                    UiKit.LabelOf(b).fontSize = th.smallSize;
                }
                else if (k == BackspaceLabel)
                    b = UiKit.Button(root, "Key Backspace", k, UiStyle.Neutral, p, new Vector2(KeyWidth, KeyHeight), () => pad._model.Backspace());
                else
                {
                    char d = k[0];
                    b = UiKit.Button(root, "Key " + k, k, UiStyle.Neutral, p, new Vector2(KeyWidth, KeyHeight), () => pad._model.Append(d), bold: true);
                }
                // keys are narrower than the minimum button width
                b.GetComponent<RectTransform>().sizeDelta = new Vector2(KeyWidth, KeyHeight);
            }
            pad.Refresh();
            return pad;
        }

        public void SetValue(string value) => _model?.Set(value);
        public void Clear() => _model?.Clear();
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>Changes the placeholder shown while the value is empty.</summary>
        public void SetPlaceholder(string placeholder)
        {
            _placeholder = placeholder ?? "";
            Refresh();
        }

        void OnModelChanged()
        {
            Refresh();
            Changed?.Invoke(Value);
        }

        void Refresh()
        {
            if (_display == null) return;
            string shown = Value.Length > 0 ? Format(Value) : "";
            bool empty = string.IsNullOrEmpty(shown);
            _display.text = empty ? _placeholder : shown;
            _display.color = empty ? UiKit.Theme.textSecondary : UiKit.Theme.text;
        }
    }
}
