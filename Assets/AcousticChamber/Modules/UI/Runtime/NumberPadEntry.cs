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
    /// Design system v2: borderless keys inside a container (4 u inset and gap, concentric corners), capsule display field.
    /// Size: 376 x 432 units (keys 120 x 80; display 376 x 80; 12 u between display and keys).
    /// </summary>
    public class NumberPadEntry : MonoBehaviour, ITextEntry
    {
        public const float KeyWidth = 120f, KeyHeight = 80f, Inset = 4f, DisplayGap = 12f;
        /// <summary>Corner radius of the key container; keys use this minus the inset.</summary>
        public const float ContainerRadius = 44f;
        public const float Width = 3 * KeyWidth + 2 * Inset + 2 * Inset;
        public const float KeysHeight = 4 * KeyHeight + 3 * Inset + 2 * Inset;
        public const float Height = KeyHeight + DisplayGap + KeysHeight;

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

            float top = Height / 2f - KeyHeight / 2f;
            var field = UiKit.Box(root, "Display", new Vector2(0f, top), new Vector2(Width, KeyHeight), th.raised, KeyHeight / 2f);
            pad._display = UiKit.Text(field.transform, "Text", "", th.headingSize, Vector2.zero, Vector2.zero, bold: true);
            UiKit.Fill(pad._display.rectTransform, 8f);

            // key container: no fill, 4 u inset; keys 4 u apart with concentric corners
            float keysCenterY = -Height / 2f + KeysHeight / 2f;
            var keysBox = UiKit.Rect(root, "Keys", new Vector2(0f, keysCenterY), new Vector2(Width, KeysHeight));
            float firstY = KeysHeight / 2f - Inset - KeyHeight / 2f;
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", BackspaceLabel };
            for (int i = 0; i < keys.Length; i++)
            {
                int row = i / 3, col = i % 3;
                var p = new Vector2((col - 1) * (KeyWidth + Inset), firstY - row * (KeyHeight + Inset));
                string k = keys[i];
                UiButton b;
                if (k == "C")
                {
                    b = UiKit.LocButton(root, "Key Clear", UiStrings.Clear, UiStyle.Borderless, p, new Vector2(KeyWidth, KeyHeight), pad.Clear);
                    var label = UiKit.LabelOf(b);
                    label.fontSize = th.smallSize;
                }
                else if (k == BackspaceLabel)
                    b = UiKit.Button(root, "Key Backspace", k, UiStyle.Borderless, p, new Vector2(KeyWidth, KeyHeight), () => pad._model.Backspace(), th.headingSize);
                else
                {
                    char d = k[0];
                    b = UiKit.Button(root, "Key " + k, k, UiStyle.Borderless, p, new Vector2(KeyWidth, KeyHeight), () => pad._model.Append(d), th.headingSize);
                }
                // keys sit in the key container (positions above are relative to it) and are narrower than the minimum width
                var rt = (RectTransform)b.transform;
                rt.SetParent(keysBox, false);
                rt.anchoredPosition = p;
                rt.sizeDelta = new Vector2(KeyWidth, KeyHeight);
                UiKit.Round(b.GetComponent<Image>(), ContainerRadius - Inset);
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
            _display.color = empty ? UiKit.Theme.text3 : UiKit.Theme.text;
        }
    }
}
