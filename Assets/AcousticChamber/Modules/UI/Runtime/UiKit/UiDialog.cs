using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
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
        /// <summary>
        /// The default slot (docs/design UiDialog, PB-27): content between the message and the actions (a progress bar, result
        /// lines), 32 u below the message; null unless created with <c>withSlot</c>.
        /// </summary>
        public RectTransform Slot { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        /// <summary>The message (null when the dialog was created without one).</summary>
        public TextMeshProUGUI MessageText { get; private set; }
        public Vector2 Size { get; private set; }

        public static UiDialog Create(Transform canvas, string name, string title, string message = null) =>
            Create(canvas, name, title, message, false);

        public static UiDialog Create(Transform canvas, string name, string title, string message, bool withSlot)
        {
            var th = UiKit.Theme;
            var d = new UiDialog { Panel = UiPanel.Create(canvas, name, new Vector2(th.dialogWidth, 400f), startHidden: true) };
            d.Panel.Modal = true;
            d.Panel.HasGrabber = false;
            var body = d.Panel.Surface.Body;
            var content = UiKit.Node(body, "Content");
            UiKit.V(content.gameObject, 0f, UiKit.Pad(0, 48, 0, 28), TextAnchor.UpperLeft);
            UiKit.Size(content.gameObject, flexW: 1f);
            var t = UiKit.Text(content, "Title", title, th.headingSize, Vector2.zero, Vector2.zero, true, th.text, TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(t.gameObject, prefW: 0f, flexW: 1f); // preferred width 0 + flexible: it takes the width and wraps
            if (!string.IsNullOrEmpty(message))
            {
                var pad = UiKit.Node(content, "Message Box");
                UiKit.V(pad.gameObject, 0f, UiKit.Pad(0, 16, 0, 0), TextAnchor.UpperLeft);
                UiKit.Size(pad.gameObject, prefW: 0f, flexW: 1f);
                var m = UiKit.Text(pad, "Message", message, th.bodySize, Vector2.zero, Vector2.zero, false, th.text2, TextAlignmentOptions.TopLeft);
                m.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Size(m.gameObject, prefW: 0f, flexW: 1f);
                d.MessageText = m;
            }
            d.TitleText = t;
            if (withSlot)
            {
                d.Slot = UiKit.Node(content, "Slot");
                UiKit.V(d.Slot.gameObject, 12f, UiKit.Pad(0, 32, 0, 0), TextAnchor.UpperLeft);
                UiKit.Size(d.Slot.gameObject, prefW: 0f, flexW: 1f);
            }
            d.Actions = UiKit.Node(content, "Actions");
            UiKit.H(d.Actions.gameObject, 8f, UiKit.Pad(0, 40, -20, 0), TextAnchor.MiddleRight);
            UiKit.Size(d.Actions.gameObject, prefW: 0f, flexW: 1f);
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

        /// <summary>Re-measures the content and sets the surface height; call after adding the actions, and after changing texts or the slot of an open dialog.</summary>
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
