using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>
        /// UiChoiceGroup (segmented, single selection): n equal borderless buttons on a capsule track, 4 u inset and gap. The
        /// track is <c>raised</c> on a surface and <c>surface</c> inside a raised group. <paramref name="changed"/> gets the index.
        /// </summary>
        public static RectTransform ChoiceGroup(Transform parent, string name, IList<string> labels, int selected, Action<int> changed) =>
            ChoiceGroup(parent, name, labels, selected, changed, out _);

        /// <summary>Like the plain overload; <paramref name="buttons"/> are the segments, so the owner can show a state with <see cref="Select"/>.</summary>
        public static RectTransform ChoiceGroup(Transform parent, string name, IList<string> labels, int selected, Action<int> changed,
                                                out List<UiButton> buttons) =>
            ChoiceGroup(parent, name, labels, selected, changed, 0, out buttons);

        /// <summary>
        /// Like the plain overload; <paramref name="columns"/> &gt; 0 lays the segments out as a grid of that many equal columns on one
        /// track (rows <c>inset</c> apart, children "Row 1", "Row 2"...; the last row fills from the left and its empty cells keep
        /// their width). One selection across all rows. 0 = one row (the plain group).
        /// </summary>
        public static RectTransform ChoiceGroup(Transform parent, string name, IList<string> labels, int selected, Action<int> changed,
                                                int columns, out List<UiButton> buttons)
        {
            var th = Theme;
            bool inGroup = parent.GetComponentInParent<ToggleSurface>() != null;
            var node = Node(parent, name);
            Fill(node, inGroup ? th.surface : th.raised, (th.buttonHeight + 2f * th.inset) / 2f);
            var pad = Pad(th.inset, th.inset, th.inset, th.inset);
            if (columns > 0) V(node.gameObject, th.inset, pad, TextAnchor.UpperLeft);
            else H(node.gameObject, th.inset, pad, TextAnchor.MiddleCenter);
            Size(node.gameObject, flexW: 1f);
            var list = new List<UiButton>();
            buttons = list;
            RectTransform row = node;
            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                if (columns > 0 && i % columns == 0) row = GridRow(node, "Row " + (i / columns + 1));
                var b = Button(row, labels[i], labels[i], UiStyle.Borderless, minWidth: false, onClick: () =>
                {
                    Select(list, j => j == index);
                    changed?.Invoke(index);
                });
                b.InGroup = true;
                b.GetComponent<HorizontalLayoutGroup>().padding = Pad(16, 0, 16, 0);
                Size(b.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f); // prefW 0: equal shares
                list.Add(b);
            }
            // empty cells of the last row keep the column width
            for (int i = labels.Count; columns > 0 && i % columns != 0; i++)
            {
                var cell = Node(row, "Empty");
                Size(cell.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f);
            }
            Select(list, j => j == selected);
            return node;
        }

        static RectTransform GridRow(RectTransform track, string name)
        {
            var row = Node(track, name);
            H(row.gameObject, Theme.inset, null, TextAnchor.MiddleCenter);
            Size(row.gameObject, prefW: 0f, flexW: 1f);
            return row;
        }
    }
}
