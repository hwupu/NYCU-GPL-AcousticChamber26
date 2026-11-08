using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// UiSurface (docs/design): the rounded rectangle with the background (radius 32, <c>surface</c> fill) of a panel, a vertical
    /// stack of slots: <see cref="NavigationBar"/> (optional), <see cref="Body"/> (fills the rest), <see cref="Status"/> and
    /// <see cref="Toolbar"/> (optional). The body keeps 48 u at the sides; with neither status line nor toolbar it keeps 40 u at
    /// the bottom (call <see cref="Finish"/> after adding the slots).
    /// </summary>
    public class UiSurface : MonoBehaviour
    {
        public RectTransform NavigationBar { get; private set; }
        public RectTransform Body { get; private set; }
        public RectTransform Status { get; private set; }
        public RectTransform Toolbar { get; private set; }

        public static UiSurface Create(Transform parent, Vector2 size)
        {
            var th = UiKit.Theme;
            var rt = UiKit.Rect(parent, "Surface", Vector2.zero, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = th.surface;
            img.raycastTarget = false;
            UiKit.Round(img, th.panelRadius);
            UiLay.V(rt.gameObject, 0f, null, TextAnchor.UpperLeft);
            var s = rt.gameObject.AddComponent<UiSurface>();
            s.Body = UiLay.Node(rt, "Body");
            UiLay.V(s.Body.gameObject, 0f, UiLay.Pad(th.surfacePaddingX, 0, th.surfacePaddingX, 0), TextAnchor.UpperLeft);
            UiLay.Size(s.Body.gameObject, flexW: 1f, flexH: 1f);
            s.Finish();
            return s;
        }

        /// <summary>The navigation bar slot, above the body (created once).</summary>
        public RectTransform AddNavigationBar()
        {
            if (NavigationBar != null) return NavigationBar;
            NavigationBar = UiLay.Node(transform, "Navigation Bar");
            NavigationBar.SetSiblingIndex(0);
            Finish();
            return NavigationBar;
        }

        /// <summary>The status line slot (one small text2 line) below the body.</summary>
        public TextMeshProUGUI AddStatus(string text)
        {
            var th = UiKit.Theme;
            Status = UiLay.Node(transform, "Status");
            UiLay.V(Status.gameObject, 0f, UiLay.Pad(th.surfacePaddingX, 12, th.surfacePaddingX, 28), TextAnchor.MiddleLeft);
            var t = UiKit.Text(Status, "Label", text, th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            Finish();
            return t;
        }

        /// <summary>The bottom toolbar slot: an 80 u row, 48 u at the sides, 40 u from the bottom edge (mirrors the navigation bar).</summary>
        public RectTransform AddToolbar()
        {
            var th = UiKit.Theme;
            Toolbar = UiLay.Node(transform, "Toolbar");
            UiLay.H(Toolbar.gameObject, th.gap, UiLay.Pad(th.surfacePaddingX, 0, th.surfacePaddingX, th.surfacePaddingBottom), TextAnchor.MiddleLeft);
            UiLay.Size(Toolbar.gameObject, prefH: th.buttonHeight + th.surfacePaddingBottom, flexH: 0f);
            Finish();
            return Toolbar;
        }

        /// <summary>Re-applies the paddings that depend on which slots exist (body top after a bar, bottom without status / toolbar).</summary>
        public void Finish()
        {
            var th = UiKit.Theme;
            var g = Body.GetComponent<VerticalLayoutGroup>();
            g.padding = UiLay.Pad(th.surfacePaddingX, NavigationBar != null ? 12f : 0f, th.surfacePaddingX,
                                  Status == null && Toolbar == null ? th.surfacePaddingBottom : 0f);
        }
    }

    /// <summary>
    /// UiNavigationBar (docs/design): 116 u high (36 u top padding + an 80 u row): back or close button, title, DEBUG tag and header
    /// actions at the right.
    /// </summary>
    public static class UiNavigationBar
    {
        public enum Leading { Back, Close, None }

        public static RectTransform Create(UiSurface surface, string title, Leading leading = Leading.Back, bool debugTag = false,
                                           System.Action onBack = null)
        {
            var th = UiKit.Theme;
            var bar = surface.AddNavigationBar();
            UiLay.H(bar.gameObject, 8f, UiLay.Pad(th.surfacePaddingX, th.navTopPadding, th.surfacePaddingX, 0), TextAnchor.MiddleLeft);
            UiLay.Size(bar.gameObject, prefH: th.navTopPadding + th.buttonHeight, flexH: 0f);
            if (leading != Leading.None)
                UiLay.Button(bar, "Back", null, UiStyle.Borderless, leading == Leading.Back ? "chevron-left" : "cross", onClick: onBack);
            UiLay.Label(bar, "Title", title, th.titleSize, th.text, true);
            var spacer = UiLay.Node(bar, "Spacer");
            UiLay.Size(spacer.gameObject, flexW: 1f);
            if (debugTag)
            {
                var t = UiLay.Label(bar, "DEBUG Tag", "DEBUG", th.minimumSize, th.debug, true, TextAlignmentOptions.Right);
                t.characterSpacing = 12f;
            }
            return bar;
        }
    }
}
