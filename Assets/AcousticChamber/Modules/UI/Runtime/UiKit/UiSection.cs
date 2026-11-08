using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>
        /// UiSection: an optional small header over a <c>raised</c> group (radius 36, padding 8, rows 4 apart). Returns the
        /// group; add rows to it.
        /// </summary>
        public static RectTransform Section(Transform parent, string name, string header = null)
        {
            var th = Theme;
            var node = Node(parent, name);
            V(node.gameObject, 8f, null, TextAnchor.UpperLeft);
            if (!string.IsNullOrEmpty(header))
            {
                var hb = Label(node, "Header", header, th.minimumSize, th.text2, true).transform.parent;
                hb.GetComponent<VerticalLayoutGroup>().padding = Pad(28, 0, 28, 0);
            }
            var group = Node(node, "Group");
            Fill(group, th.raised, th.groupRadius);
            V(group.gameObject, th.inset, Pad(th.groupPadding, th.groupPadding, th.groupPadding, th.groupPadding), TextAnchor.UpperLeft);
            group.gameObject.AddComponent<ToggleSurface>();
            return group;
        }

        /// <summary>Marks a raised group: segmented tracks inside it use the surface color (docs/design).</summary>
        public class ToggleSurface : MonoBehaviour { }
    }
}
