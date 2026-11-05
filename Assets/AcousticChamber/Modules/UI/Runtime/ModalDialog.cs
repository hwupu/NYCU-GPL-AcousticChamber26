using System;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>One button of a <see cref="ModalDialog"/>.</summary>
    public readonly struct DialogButton
    {
        public readonly string Label;
        public readonly UiStyle Style;
        public readonly Action OnClick;
        /// <summary>If false, the dialog stays open after the click (e.g. "Sure?" steps that change the dialog).</summary>
        public readonly bool Closes;

        public DialogButton(string label, UiStyle style, Action onClick = null, bool closes = true)
        {
            Label = label;
            Style = style;
            OnClick = onClick;
            Closes = closes;
        }
    }

    /// <summary>
    /// Modal dialog on top of a canvas: dims the panel, shows a box with a colored top bar, a title, a message and a row of
    /// buttons. While open, the pointer can only reach this dialog (<see cref="UiPointer.Modal"/>). Used by the Stop dialog
    /// and the data manager's delete confirmations.
    /// </summary>
    [ExecuteAlways] // OnDestroy (Abandoned) also in edit mode / EditMode tests
    public class ModalDialog : MonoBehaviour
    {
        public static readonly Vector2 DefaultSize = new Vector2(900f, 380f);

        Transform _previousModal;

        /// <summary>The dialog currently open on any canvas, or null.</summary>
        public static ModalDialog Current { get; private set; }

        bool _closed;

        public event Action Closed;

        /// <summary>Raised when the dialog is destroyed without <see cref="Close"/> (e.g. its canvas or scene unloaded).</summary>
        public event Action Abandoned;

        public static ModalDialog Show(Transform canvas, string title, string message, Color barColor, params DialogButton[] buttons) =>
            Show(canvas, title, message, barColor, new Vector2(0f, 100f), DefaultSize, buttons);

        public static ModalDialog Show(Transform canvas, string title, string message, Color barColor, Vector2 pos, Vector2 size,
                                       params DialogButton[] buttons)
        {
            var th = UiKit.Theme;
            var root = UiKit.Fill(UiKit.Rect(canvas, "Dialog", Vector2.zero, Vector2.zero));
            var dialog = root.gameObject.AddComponent<ModalDialog>();
            UiKit.Background(root, "Dim", new Color(0f, 0f, 0f, 0.55f)).sprite = null;

            var box = UiKit.Box(root, "Box", pos, size, th.surface).rectTransform;
            var bar = UiKit.Box(box, "Bar", Vector2.zero, new Vector2(0f, 16f), barColor);
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            bar.rectTransform.sizeDelta = new Vector2(0f, 16f);

            float y = size.y / 2f - 16f - th.gap - th.headingSize * 0.75f;
            UiKit.Text(box, "Title", title, th.headingSize, new Vector2(0f, y), new Vector2(size.x - 2 * th.gap, th.headingSize * 1.4f), bold: true);
            UiKit.Text(box, "Message", message, th.bodySize, new Vector2(0f, 10f), new Vector2(size.x - 4 * th.gap, size.y - 220f),
                       color: th.textSecondary);

            int n = Mathf.Max(buttons.Length, 1);
            float w = Mathf.Min(360f, (size.x - (n + 1) * th.gap) / n);
            float by = -size.y / 2f + th.gap + th.buttonHeight / 2f + 10f;
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                float x = (i - (buttons.Length - 1) / 2f) * (w + th.gap);
                var button = UiKit.Button(box, "Button " + b.Label, b.Label, b.Style, new Vector2(x, by), new Vector2(w, th.buttonHeight),
                                          bold: b.Style == UiStyle.Primary);
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(w, th.buttonHeight);
                button.onClick.AddListener(() =>
                {
                    if (b.Closes) dialog.Close();
                    b.OnClick?.Invoke();
                });
            }

            dialog._previousModal = UiPointer.Modal;
            UiPointer.Modal = box;
            Current = dialog;
            return dialog;
        }

        public void Close()
        {
            if (this == null || _closed) return;
            _closed = true;
            if (Current == this) Current = null;
            UiPointer.Modal = _previousModal;
            Closed?.Invoke();
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        void OnDestroy()
        {
            if (_closed) return;
            if (Current == this)
            {
                Current = null;
                UiPointer.Modal = _previousModal;
            }
            Abandoned?.Invoke();
        }
    }
}
