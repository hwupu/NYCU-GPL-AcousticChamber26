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
    /// Modal dialog on top of a canvas (design system v2): dims the panel, shows a surface box with a title, a message and a row of
    /// buttons. While open, the pointer can only reach this dialog (<see cref="UiPointer.Modal"/>). Used by the Stop dialog
    /// and the data manager's delete confirmations.
    /// </summary>
    [ExecuteAlways] // OnDestroy (Abandoned) also in edit mode / EditMode tests
    public class ModalDialog : MonoBehaviour
    {
        public static readonly Vector2 DefaultSize = new Vector2(820f, 400f);

        Transform _previousModal;

        /// <summary>The dialog currently open on any canvas, or null.</summary>
        public static ModalDialog Current { get; private set; }

        bool _closed;

        public event Action Closed;

        /// <summary>Raised when the dialog is destroyed without <see cref="Close"/> (e.g. its canvas or scene unloaded).</summary>
        public event Action Abandoned;

        public static ModalDialog Show(Transform canvas, string title, string message, params DialogButton[] buttons) =>
            Show(canvas, title, message, Vector2.zero, DefaultSize, buttons);

        /// <summary>
        /// Opens a dialog box at <paramref name="pos"/>: title and message left-aligned, buttons right-aligned in the given
        /// order (put the main action last: it ends up on the right). Each button is as wide as its label needs, at least
        /// the minimum button width.
        /// </summary>
        public static ModalDialog Show(Transform canvas, string title, string message, Vector2 pos, Vector2 size,
                                       params DialogButton[] buttons)
        {
            var th = UiKit.Theme;
            const float pad = 48f;
            var root = UiKit.Fill(UiKit.Rect(canvas, "Dialog", Vector2.zero, Vector2.zero));
            var dialog = root.gameObject.AddComponent<ModalDialog>();
            UiKit.Background(root, "Dim", th.dim);

            var box = UiKit.Box(root, "Box", pos, size, th.surface).rectTransform;
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
