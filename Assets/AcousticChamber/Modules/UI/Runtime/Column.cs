using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// A page body that scrolls: a <see cref="UiLay.List"/> filling the body of a <see cref="UiPage"/>, with content stacked
    /// top-down: group labels, groups (raised sections) and button rows, in order. Used by settings-like pages (Debug,
    /// Parameters, Presets, Data files). Everything is a layout, so rows grow with their text.
    /// </summary>
    public sealed class Column
    {
        /// <summary>Space between sections (a label + group).</summary>
        public const float SectionGap = 24f;
        /// <summary>Space between a group label and its group.</summary>
        public const float LabelGap = 8f;

        public ScrollList List { get; }
        public RectTransform Content => List.Content;

        Column(ScrollList list) => List = list;

        /// <summary>A column filling the body of <paramref name="page"/>.</summary>
        public static Column In(UiPage page)
        {
            var content = UiLay.List(page.Body, "Column", out var list);
            content.GetComponent<VerticalLayoutGroup>().spacing = 0f;
            return new Column(list);
        }

        /// <summary>Removes the column (the list is rebuilt after a change).</summary>
        public void Destroy()
        {
            var go = List.gameObject;
            go.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>A group label (24 u, <c>text2</c>) above the next group; long text wraps.</summary>
        public TextMeshProUGUI Label(string text)
        {
            var th = UiKit.Theme;
            var box = UiLay.Node(Content, "Label Box");
            UiLay.V(box.gameObject, 0f, UiLay.Pad((int)UiKit.RowTextPad, 0, (int)UiKit.RowTextPad, (int)LabelGap), TextAnchor.UpperLeft);
            UiLay.Size(box.gameObject, prefW: 0f, flexW: 1f);
            var t = UiKit.Text(box, "Label", text, th.minimumSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            UiLay.Size(t.gameObject, prefW: 0f, flexW: 1f);
            return t;
        }

        /// <summary>A raised group; fill it with <see cref="UiLay.Row"/>, <see cref="UiLay.SwitchRow"/> and friends.</summary>
        public RectTransform Group(string name)
        {
            var group = UiLay.Section(Content, name);
            ((VerticalLayoutGroup)group.parent.GetComponent<VerticalLayoutGroup>()).padding = UiLay.Pad(0, 0, 0, (int)SectionGap);
            UiLay.Size(group.parent.gameObject, prefW: 0f, flexW: 1f);
            return group;
        }

        /// <summary>
        /// A group holding a row of buttons side by side (equal widths, <c>groupPadding</c> apart). Use Prominent for the
        /// main action and Borderless for the others (their fill appears on hover only). Returns the buttons.
        /// </summary>
        public UiButton[] Buttons(string name, params (string label, UiStyle style, Action onClick)[] buttons)
        {
            var th = UiKit.Theme;
            var group = Group(name);
            var row = UiLay.Node(group, "Buttons");
            UiLay.H(row.gameObject, th.groupPadding, null, TextAnchor.MiddleCenter);
            UiLay.Size(row.gameObject, prefW: 0f, flexW: 1f);
            var result = new UiButton[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, style, onClick) = buttons[i];
                var b = UiLay.Button(row, "Button " + label, label, style, minWidth: false, onClick: onClick);
                b.InGroup = true; // borderless hover colors for a raised group
                UiLay.Size(b.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f); // equal widths
                result[i] = b;
            }
            return result;
        }

        /// <summary>Empty space.</summary>
        public void Gap(float height)
        {
            var g = UiLay.Node(Content, "Gap");
            UiLay.Size(g.gameObject, minH: height, prefH: height, flexH: 0f);
        }

        /// <summary>Kept for the callers that finish a column; the layout sizes the scroll range itself.</summary>
        public void Finish() { }
    }
}
