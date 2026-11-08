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
                                                out List<UiButton> buttons)
        {
            var th = Theme;
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
                    Select(list, j => j == index);
                    changed?.Invoke(index);
                });
                b.InGroup = true;
                var pad = b.GetComponent<HorizontalLayoutGroup>();
                pad.padding = Pad(16, 0, 16, 0);
                Size(b.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f); // prefW 0: equal shares
                list.Add(b);
            }
            Select(list, j => j == selected);
            return node;
        }
    }
}
