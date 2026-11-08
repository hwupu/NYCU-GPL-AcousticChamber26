using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiLay
    {
        /// <summary>
        /// UiButton of the rebuilt kit: a capsule laid out by a horizontal group. 80 u high (small 60), width = label + padding
        /// (at least 220 u unless <paramref name="minWidth"/> is false). The side with an icon has 25 % less padding; an icon
        /// button is a circle. Children: "Icon Leading", "Label Box" > "Label", "Icon Trailing".
        /// </summary>
        public static UiButton Button(Transform parent, string name, string label, UiStyle style, string iconLeading = null,
                                      string iconTrailing = null, bool small = false, bool minWidth = true, Action onClick = null)
        {
            var th = UiKit.Theme;
            float h = small ? th.smallButtonHeight : th.buttonHeight;
            bool iconOnly = string.IsNullOrEmpty(label) && (iconLeading != null || iconTrailing != null);
            float padX = small ? 32f : th.buttonPaddingX;
            float left = iconLeading != null ? padX * th.iconSidePaddingShare : padX;
            float right = iconTrailing != null ? padX * th.iconSidePaddingShare : padX;
            if (iconOnly) left = right = 0f;

            var node = Node(parent, name);
            var img = Fill(node, Color.clear, h / 2f);
            img.raycastTarget = true;
            H(node.gameObject, th.iconGap, Pad(left, 0, right, 0), TextAnchor.MiddleCenter);
            Size(node.gameObject, minW: iconOnly ? h : (minWidth && !small ? th.buttonMinWidth : 0f), minH: h, prefH: h, flexH: 0f,
                 prefW: iconOnly ? h : -1f);
            var b = node.gameObject.AddComponent<UiButton>();
            b.targetGraphic = img;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            float icon = small ? th.iconSizeSmall : th.iconSize;
            if (iconLeading != null) IconNode(node, "Icon Leading", iconLeading, icon).FollowsLabel = true;
            if (!string.IsNullOrEmpty(label))
                Label(node, "Label", label, th.buttonSize, th.text, style == UiStyle.Prominent || style == UiStyle.Destructive, TextAlignmentOptions.Center);
            if (iconTrailing != null) IconNode(node, "Icon Trailing", iconTrailing, icon).FollowsLabel = true;
            b.Style = style;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        static UiIcon IconNode(Transform parent, string name, string icon, float size)
        {
            var ic = UiKit.Icon(parent, name, icon, size);
            Size(ic.gameObject, minW: size, minH: size, prefW: size, prefH: size, flexW: 0f, flexH: 0f);
            return ic;
        }

        /// <summary>The icon of a button or row follows the label color (set when the style changes).</summary>
        public static UiIcon Icon(Transform parent, string name, string icon, float size = 0f, Color? color = null)
        {
            var ic = IconNode(parent, name, icon, size > 0f ? size : UiKit.Theme.iconSize);
            if (color.HasValue) ic.Color = color.Value;
            return ic;
        }

        // ---- section and row -------------------------------------------------------------------------------------------

        /// <summary>
        /// UiSection: an optional small header over a <c>raised</c> group (radius 36, padding 8, rows 4 apart). Returns the
        /// group; add rows to it.
        /// </summary>
        public static RectTransform Section(Transform parent, string name, string header = null)
        {
            var th = UiKit.Theme;
            var node = Node(parent, name);
            V(node.gameObject, 8f, null, TextAnchor.UpperLeft);
            if (!string.IsNullOrEmpty(header))
            {
                var hb = Label(node, "Header", header, th.minimumSize, th.text2, true).transform.parent;
                ((VerticalLayoutGroup)hb.GetComponent<VerticalLayoutGroup>()).padding = Pad(28, 0, 28, 0);
            }
            var group = Node(node, "Group");
            Fill(group, th.raised, th.groupRadius);
            V(group.gameObject, th.inset, Pad(th.groupPadding, th.groupPadding, th.groupPadding, th.groupPadding), TextAnchor.UpperLeft);
            group.gameObject.AddComponent<ToggleSurface>();
            return group;
        }

        /// <summary>Marks a raised group: segmented tracks inside it use the surface color (docs/design).</summary>
        public class ToggleSurface : MonoBehaviour { }

        /// <summary>
        /// UiRow: icon, title (+ subtitle, several lines allowed), value, trailing icon, at least 88 u high; the icon side has 25 %
        /// less padding. A row with <paramref name="onClick"/> is a borderless in-group button; without it an info row.
        /// Children: "Icon Leading", "Text" > "Title" / "Subtitle", "Value", "Icon Trailing".
        /// </summary>
        public static RectTransform Row(Transform group, string name, string title, string subtitle = null, string value = null,
                                        string iconLeading = null, string iconTrailing = null, Action onClick = null)
        {
            var th = UiKit.Theme;
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
                var t = UiKit.Text(text, "Title", title, th.bodySize, Vector2.zero, Vector2.zero, align: TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                var s = UiKit.Text(text, "Subtitle", subtitle, th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
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

        // ---- choice group ----------------------------------------------------------------------------------------------

        /// <summary>
        /// UiChoiceGroup (segmented, single selection): n equal borderless buttons on a capsule track, 4 u inset and gap. The
        /// track is <c>raised</c> on a surface and <c>surface</c> inside a raised group. <paramref name="changed"/> gets the index.
        /// </summary>
        public static RectTransform ChoiceGroup(Transform parent, string name, IList<string> labels, int selected, Action<int> changed) =>
            ChoiceGroup(parent, name, labels, selected, changed, out _);

        /// <summary>A row of a label (at the left, <c>text2</c>, <paramref name="labelWidth"/> wide) and room for controls: add them to the returned row.</summary>
        public static RectTransform LabeledRow(Transform parent, string name, string label, float labelWidth = 200f)
        {
            var th = UiKit.Theme;
            var row = Node(parent, name);
            H(row.gameObject, th.gap, null, TextAnchor.MiddleLeft);
            Size(row.gameObject, flexW: 1f);
            var l = Label(row, "Label", label, th.bodySize, th.text2);
            Size(l.transform.parent.gameObject, minW: labelWidth, prefW: labelWidth, flexW: 0f);
            return row;
        }

        /// <summary>Like the plain overload; <paramref name="buttons"/> are the segments, so the owner can show a state with <see cref="UiKit.Select"/>.</summary>
        public static RectTransform ChoiceGroup(Transform parent, string name, IList<string> labels, int selected, Action<int> changed,
                                                out List<UiButton> buttons)
        {
            var th = UiKit.Theme;
            bool inGroup = parent.GetComponentInParent<ToggleSurface>() != null;
            var node = Node(parent, name);
            Fill(node, inGroup ? th.surface : th.raised, (th.buttonHeight + 2f * th.inset) / 2f);
            H(node.gameObject, th.inset, Pad(th.inset, th.inset, th.inset, th.inset), TextAnchor.MiddleCenter);
            Size(node.gameObject, flexW: 1f);
            var list = new List<UiButton>();
            buttons = list;
            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                var b = Button(node, labels[i], labels[i], UiStyle.Borderless, minWidth: false, onClick: () =>
                {
                    UiKit.Select(list, j => j == index);
                    changed?.Invoke(index);
                });
                b.InGroup = true;
                var pad = b.GetComponent<HorizontalLayoutGroup>();
                pad.padding = Pad(16, 0, 16, 0);
                Size(b.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f); // prefW 0: equal shares
                list.Add(b);
            }
            UiKit.Select(list, j => j == selected);
            return node;
        }
    }
}
