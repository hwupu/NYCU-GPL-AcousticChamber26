using System;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// One page of a <see cref="PageStack"/>: fills the stack's container and has a header (‹ back at the top left when
    /// there is somewhere to go back to, the title next to it, optionally the DEBUG tag at the right). Build the content
    /// under <see cref="Rect"/> in its coordinates (center = 0,0); keep the top <see cref="HeaderHeight"/> units free.
    /// </summary>
    public class Page : MonoBehaviour
    {
        /// <summary>Padding from the page edge to the header (left / right) and from the top.</summary>
        public const float EdgePad = 48f, TopPad = 40f;

        /// <summary>Height of the header band from the top edge (top padding + icon button).</summary>
        public static float HeaderHeight => TopPad + UiKit.Theme.iconButtonSize;

        /// <summary>Width of full-width content (groups, lists) on a page <paramref name="pageWidth"/> wide: aligned with the header edges.</summary>
        public static float ContentWidth(float pageWidth) => pageWidth - 2f * EdgePad;

        internal UiLayer Layer;

        public PageStack Stack { get; internal set; }
        public RectTransform Rect => (RectTransform)transform;
        public string Title { get; internal set; }
        public TextMeshProUGUI TitleText { get; internal set; }

        /// <summary>The ‹ button, or null on a root page without <see cref="PageStack.RootBack"/>.</summary>
        public UiButton BackButton { get; internal set; }

        /// <summary>Raised each time the page becomes the top page again (after a page above it was popped).</summary>
        public event Action Returned;

        internal void RaiseReturned() => Returned?.Invoke();

        /// <summary>
        /// A header action at the right of the header (Revert, Delete all): a small borderless button (or red text for
        /// <see cref="UiStyle.DestructiveText"/>), as wide as its label.
        /// </summary>
        public UiButton AddHeaderAction(string label, UiStyle style, Action onClick)
        {
            var th = UiKit.Theme;
            var size = Rect.rect.size;
            float y = size.y / 2f - TopPad - th.iconButtonSize / 2f;
            var b = UiKit.Button(Rect, "Action " + label, label, style, Vector2.zero, new Vector2(0f, th.smallButtonHeight), onClick, th.smallSize);
            float w = Mathf.Max(th.smallButtonHeight * 2f, UiKit.LabelOf(b).GetPreferredValues(label).x + 48f);
            var rt = (RectTransform)b.transform;
            rt.sizeDelta = new Vector2(w, th.smallButtonHeight);
            rt.anchoredPosition = new Vector2(size.x / 2f - EdgePad - w / 2f, y);
            return b;
        }
    }
}
