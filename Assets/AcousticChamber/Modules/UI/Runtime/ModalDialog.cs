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
    /// A decision (docs/design UiDialog): a <see cref="UiDialog"/> with a title, a message and a row of buttons. It appears in
    /// place and pushes the panels behind it back; while open, the pointer can only reach this dialog. Dialogs stack: a
    /// confirmation opened from a dialog lies on top of it, and closing it returns to the one below. Used by the Stop dialog and the
    /// data manager's delete confirmations.
    /// </summary>
    [ExecuteAlways] // OnDestroy also in edit mode / EditMode tests
    public class ModalDialog : MonoBehaviour
    {
        static readonly List<ModalDialog> Open = new List<ModalDialog>();

        /// <summary>The top open dialog on any canvas, or null.</summary>
        public static ModalDialog Current => Open.Count > 0 ? Open[Open.Count - 1] : null;

        public UiDialog Dialog { get; private set; }
        public UiPanel Panel => Dialog.Panel;

        public event Action Closed;

        /// <summary>Raised when the dialog is destroyed without <see cref="Close"/> (e.g. its canvas or scene unloaded).</summary>
        public event Action Abandoned;

        /// <summary>
        /// Opens a dialog: title and message left-aligned, buttons right-aligned in the given order (put the main action last: it
        /// ends up on the right). Each button is as wide as its label needs.
        /// </summary>
        public static ModalDialog Show(Transform canvas, string title, string message, params DialogButton[] buttons)
        {
            var d = UiDialog.Create(canvas, "Dialog", title, message);
            var dialog = d.Panel.gameObject.AddComponent<ModalDialog>();
            dialog.Dialog = d;
            d.Panel.Closed += () => { Open.Remove(dialog); dialog.Closed?.Invoke(); };
            d.Panel.Abandoned += () => { Open.Remove(dialog); dialog.Abandoned?.Invoke(); };
            foreach (var b in buttons)
            {
                var button = b;
                UiKit.Button(d.Actions, "Button " + b.Label, b.Label, b.Style, minWidth: false, onClick: () =>
                {
                    if (button.Closes) dialog.Close();
                    button.OnClick?.Invoke();
                });
            }
            Open.Add(dialog);
            d.Show();
            return dialog;
        }

        public void Close()
        {
            if (this != null && Dialog != null) Dialog.Close();
        }

        void OnDestroy() => Open.Remove(this);
    }
}
