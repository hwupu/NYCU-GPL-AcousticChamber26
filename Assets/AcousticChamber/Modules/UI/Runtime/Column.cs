using System;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// A page body that scrolls: a <see cref="ScrollList"/> below the page header, with content stacked top-down at the
    /// page's content width (aligned with the header edges). Add group labels, groups and blocks in order, then
    /// <see cref="Finish"/>. Used by settings-like pages (Debug, Parameters, Presets).
    /// </summary>
    public sealed class Column
    {
        /// <summary>Space between sections (a label + group).</summary>
        public const float SectionGap = 24f;
        /// <summary>Space between a group label and its group.</summary>
        public const float LabelGap = 8f;

        float _y;

        public ScrollList List { get; }
        public RectTransform Content => List.Content;
        public float Width { get; }

        Column(ScrollList list, float width)
        {
            List = list;
            Width = width;
        }

        /// <summary>
        /// A column filling <paramref name="page"/> below its header, leaving <paramref name="bottomReserve"/> units free at
        /// the bottom (e.g. a status line).
        /// </summary>
        public static Column In(Page page, float bottomReserve = 24f)
        {
            var size = page.Rect.rect.size;
            float width = Page.ContentWidth(size.x);
            float top = size.y / 2f - Page.HeaderHeight - 8f;
            float bottom = -size.y / 2f + bottomReserve;
            // 20 u wider than the content on each side: the scroll indicator sits in the margin
            var list = ScrollList.Create(page.Rect, "Body", new Vector2(0f, (top + bottom) / 2f), new Vector2(width + 40f, top - bottom));
            return new Column(list, width);
        }

        /// <summary>A group label (24 u, <c>text2</c>) above the next group; long text wraps.</summary>
        public TextMeshProUGUI Label(string text)
        {
            var th = UiKit.Theme;
            var t = UiKit.Text(Content, "Label " + text, text, th.minimumSize, Vector2.zero, new Vector2(Width - 2f * UiKit.RowTextPad, 0f),
                               color: th.text2, align: TextAlignmentOptions.TopLeft);
            float h = Mathf.Ceil(t.GetPreferredValues(text, Width - 2f * UiKit.RowTextPad, 0f).y);
            t.rectTransform.sizeDelta = new Vector2(Width - 2f * UiKit.RowTextPad, h);
            ScrollList.TopAnchored(t.rectTransform, _y);
            _y += h + LabelGap;
            return t;
        }

        /// <summary>A group of <paramref name="rows"/> rows (fill it with <see cref="UiKit.Row"/> and friends).</summary>
        public RectTransform Group(string name, int rows)
        {
            var g = UiKit.Group(Content, name, Vector2.zero, Width, rows);
            ScrollList.TopAnchored(g, _y);
            _y += g.sizeDelta.y + SectionGap;
            return g;
        }

        /// <summary>
        /// A group holding a row of buttons side by side (equal widths, <c>groupPadding</c> apart). Use Prominent for the
        /// main action and Borderless for the others (their fill appears on hover only). Returns the buttons.
        /// </summary>
        public UiButton[] Buttons(string name, params (string label, UiStyle style, Action onClick)[] buttons)
        {
            var th = UiKit.Theme;
            float pad = th.groupPadding;
            var g = UiKit.Box(Content, name, Vector2.zero, new Vector2(Width, th.buttonHeight + 2f * pad), th.raised, th.groupRadius).rectTransform;
            ScrollList.TopAnchored(g, _y);
            _y += g.sizeDelta.y + SectionGap;
            int n = buttons.Length;
            float gap = pad;
            float w = (Width - 2f * pad - (n - 1) * gap) / n;
            var result = new UiButton[n];
            for (int i = 0; i < n; i++)
            {
                var (label, style, onClick) = buttons[i];
                float x = -Width / 2f + pad + w / 2f + i * (w + gap);
                var b = UiKit.Button(g, "Button " + label, label, style, new Vector2(x, 0f), new Vector2(w, 0f), onClick);
                ((RectTransform)b.transform).sizeDelta = new Vector2(w, th.buttonHeight);
                b.InGroup = true; // borderless hover colors for a raised group
                result[i] = b;
            }
            return result;
        }

        /// <summary>Empty space.</summary>
        public void Gap(float height) => _y += height;

        /// <summary>Sets the scroll extent; call after the last item (and again after adding more).</summary>
        public void Finish() => List.ContentHeight = Mathf.Max(0f, _y - SectionGap);
    }
}
