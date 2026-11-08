using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>
        /// UiRow: icon, title (+ subtitle, several lines allowed), value, trailing icon, at least 88 u high; the icon side has 25 %
        /// less padding. A row with <paramref name="onClick"/> is a borderless in-group button; without it an info row.
        /// Children: "Icon Leading", "Text" > "Title" / "Subtitle", "Value", "Icon Trailing".
        /// </summary>
        public static RectTransform Row(Transform group, string name, string title, string subtitle = null, string value = null,
                                        string iconLeading = null, string iconTrailing = null, Action onClick = null)
        {
            var th = Theme;
            float left = iconLeading != null ? th.rowPaddingX * th.iconSidePaddingShare : th.rowPaddingX;
            float right = iconTrailing != null ? th.rowPaddingX * th.iconSidePaddingShare : th.rowPaddingX;
            var node = Node(group, name);
            var img = Fill(node, Color.clear, th.rowRadius);
            H(node.gameObject, th.gap, Pad(left, 12, right, 12), TextAnchor.MiddleLeft);
            Size(node.gameObject, minH: th.rowMinHeight, flexW: 1f);
            if (onClick != null)
            {
                img.raycastTarget = true;
                var b = node.gameObject.AddComponent<UiButton>();
                b.targetGraphic = img;
                var nav = b.navigation;
                nav.mode = Navigation.Mode.None;
                b.navigation = nav;
                b.Style = UiStyle.Borderless;
                b.InGroup = true;
                b.onClick.AddListener(() => onClick());
            }
            if (iconLeading != null) Icon(node, "Icon Leading", iconLeading, th.iconSize, th.text2);

            var text = Node(node, "Text");
            V(text.gameObject, 2f, null, TextAnchor.MiddleLeft);
            Size(text.gameObject, flexW: 1f);
            if (subtitle == null) Label(text, "Title", title, th.bodySize, th.text); // one centered line
            else
            {
                var t = Text(text, "Title", title, th.bodySize, Vector2.zero, Vector2.zero, align: TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                var s = Text(text, "Subtitle", subtitle, th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
                s.textWrappingMode = TextWrappingModes.Normal; // line breaks and wrapping: several lines
            }
            if (value != null) Label(node, "Value", value, th.bodySize, th.text2, align: TextAlignmentOptions.Right);
            if (iconTrailing != null) Icon(node, "Icon Trailing", iconTrailing, th.iconSize, th.text2);
            return node;
        }

        /// <summary>The title of a <see cref="Row"/>.</summary>
        public static TextMeshProUGUI RowTitle(Transform row)
        {
            var t = row.Find("Text/Title");
            if (t == null) t = row.Find("Text/Title Box/Label");
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        /// <summary>The value (right) of a <see cref="Row"/> created with a non-null value, or null.</summary>
        public static TextMeshProUGUI RowValue(Transform row)
        {
            var t = row.Find("Value Box/Label");
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        /// <summary>The subtitle of a <see cref="Row"/> created with a non-null subtitle, or null.</summary>
        public static TextMeshProUGUI RowSubtitle(Transform row)
        {
            var t = row.Find("Text/Subtitle");
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        /// <summary>A row of a label (at the left, <c>text2</c>, <paramref name="labelWidth"/> wide) and room for controls: add them to the returned row.</summary>
        public static RectTransform LabeledRow(Transform parent, string name, string label, float labelWidth = 200f)
        {
            var th = Theme;
            var row = Node(parent, name);
            H(row.gameObject, th.gap, null, TextAnchor.MiddleLeft);
            Size(row.gameObject, flexW: 1f);
            var l = Label(row, "Label", label, th.bodySize, th.text2);
            Size(l.transform.parent.gameObject, minW: labelWidth, prefW: labelWidth, flexW: 0f);
            return row;
        }
    }
}
