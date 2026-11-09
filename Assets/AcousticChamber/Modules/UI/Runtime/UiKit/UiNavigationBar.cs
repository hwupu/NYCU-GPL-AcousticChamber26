using System;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// UiNavigationBar (docs/design): 116 u high (36 u top padding + an 80 u row): back or close button, title, DEBUG tag and header
    /// actions at the right.
    /// </summary>
    public static class UiNavigationBar
    {
        public enum Leading { Back, Close, None }

        public static RectTransform Create(UiSurface surface, string title, Leading leading = Leading.Back, bool debugTag = false,
                                           Action onBack = null) =>
            Create(surface.Root, title, leading, debugTag, onBack ?? surface.Back);

        /// <summary>
        /// Centers the title across the bar (for a bar without a leading button, e.g. the protocol slides): the title takes the free
        /// width and is centered in it.
        /// </summary>
        public static void CenterTitle(UiPage page)
        {
            var bar = page.NavigationBar;
            var box = bar.Find("Title Box");
            UiKit.Size(box.gameObject, flexW: 1f);
            box.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            // the box lays out its label at the label's own width: place it in the middle too (it was left in the wide box)
            box.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            bar.Find("Spacer").gameObject.SetActive(false); // no spacer (and its gap): the title is centered in the whole bar
        }

        public static RectTransform Create(UiPage page, string title, Leading leading = Leading.Back, bool debugTag = false,
                                           Action onBack = null)
        {
            var th = UiKit.Theme;
            var bar = page.AddNavigationBar();
            UiKit.H(bar.gameObject, 8f, UiKit.Pad(th.surfacePaddingX, th.navTopPadding, th.surfacePaddingX, 0), TextAnchor.MiddleLeft);
            UiKit.Size(bar.gameObject, prefW: 0f, prefH: th.navTopPadding + th.buttonHeight, flexW: 1f, flexH: 0f); // spans the surface: the spacer pushes the tag and actions to the right
            if (leading != Leading.None)
                page.BackButton = UiKit.Button(bar, "Back", null, UiStyle.Borderless, leading == Leading.Back ? "chevron-left" : "cross", onClick: onBack);
            UiKit.Label(bar, "Title", title, th.titleSize, th.text, true);
            var spacer = UiKit.Node(bar, "Spacer");
            UiKit.Size(spacer.gameObject, flexW: 1f);
            if (debugTag)
            {
                var t = UiKit.Label(bar, "DEBUG Tag", "DEBUG", th.minimumSize, th.debug, true, TextAlignmentOptions.Right);
                t.characterSpacing = 12f;
            }
            return bar;
        }
    }
}
