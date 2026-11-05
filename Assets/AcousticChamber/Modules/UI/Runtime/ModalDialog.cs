using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>One button of a <see cref="ModalDialog"/>.</summary>
    public readonly struct DialogButton
    {
        public readonly string Label;
        public readonly UiStyle Style;
        public readonly Action OnClick;
        /// <summary>If false, the dialog stays open after the click (e.g. a step that opens a confirmation sheet on top).</summary>
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
    /// A decision sheet (design system v2): a <see cref="Sheet"/> with a title, a message and a row of buttons. It appears
    /// in place and pushes the window (and any sheet) behind it back; while open, the pointer can only reach this dialog.
    /// Dialogs stack: a confirmation opened from a dialog lies on top of it, and closing it returns to the one below.
    /// Used by the Stop dialog and the data manager's delete confirmations.
    /// </summary>
    [ExecuteAlways] // OnDestroy also in edit mode / EditMode tests
    [RequireComponent(typeof(Sheet))]
    public class ModalDialog : MonoBehaviour
    {
        public static readonly Vector2 DefaultSize = new Vector2(820f, 400f);

        static readonly List<ModalDialog> Open = new List<ModalDialog>();

        /// <summary>The top open dialog on any canvas, or null.</summary>
        public static ModalDialog Current => Open.Count > 0 ? Open[Open.Count - 1] : null;

        public Sheet Sheet { get; private set; }

        public event Action Closed;

        /// <summary>Raised when the dialog is destroyed without <see cref="Close"/> (e.g. its canvas or scene unloaded).</summary>
        public event Action Abandoned;

        public static ModalDialog Show(Transform canvas, string title, string message, params DialogButton[] buttons) =>
            Show(canvas, title, message, Vector2.zero, DefaultSize, buttons);

        /// <summary>
        /// Opens a dialog sheet at <paramref name="pos"/>: title and message left-aligned, buttons right-aligned in the given
        /// order (put the main action last: it ends up on the right). Each button is as wide as its label needs, at least
        /// the minimum button width.
        /// </summary>
        public static ModalDialog Show(Transform canvas, string title, string message, Vector2 pos, Vector2 size,
                                       params DialogButton[] buttons)
        {
            var th = UiKit.Theme;
            const float pad = 48f;
            var sheet = Sheet.Open(canvas, "Dialog", pos, size);
            var dialog = sheet.gameObject.AddComponent<ModalDialog>();
            dialog.Sheet = sheet;
            sheet.Closed += () => { Open.Remove(dialog); dialog.Closed?.Invoke(); };
            sheet.Abandoned += () => { Open.Remove(dialog); dialog.Abandoned?.Invoke(); };
            var box = sheet.Rect;

            float top = size.y / 2f - pad;
            float innerW = size.x - 2 * pad;
            UiKit.Text(box, "Title", title, th.headingSize, new Vector2(0f, top - th.headingSize * 0.7f), new Vector2(innerW, th.headingSize * 1.4f),
                       bold: true, align: TMPro.TextAlignmentOptions.Left);
            float msgTop = top - th.headingSize * 1.4f - 16f;
            float msgBottom = -size.y / 2f + pad + th.buttonHeight + 24f;
            UiKit.Text(box, "Message", message, 32, new Vector2(0f, (msgTop + msgBottom) / 2f), new Vector2(innerW, msgTop - msgBottom),
                       color: th.text2, align: TMPro.TextAlignmentOptions.TopLeft);

            float by = -size.y / 2f + pad + th.buttonHeight / 2f;
            float right = size.x / 2f - pad;
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                var b = buttons[i];
                var button = UiKit.Button(box, "Button " + b.Label, b.Label, b.Style, Vector2.zero, new Vector2(0f, th.buttonHeight));
                var label = UiKit.LabelOf(button);
                float w = Mathf.Max(th.buttonMinWidth, label.GetPreferredValues(b.Label).x + 64f);
                var rt = (RectTransform)button.transform;
                rt.sizeDelta = new Vector2(w, th.buttonHeight);
                rt.anchoredPosition = new Vector2(right - w / 2f, by);
                right -= w + th.gap;
                button.onClick.AddListener(() =>
                {
                    if (b.Closes) dialog.Close();
                    b.OnClick?.Invoke();
                });
            }

            Open.Add(dialog);
            return dialog;
        }

        public void Close()
        {
            if (this != null && Sheet != null) Sheet.Close();
        }

        void OnDestroy() => Open.Remove(this);
    }
}
