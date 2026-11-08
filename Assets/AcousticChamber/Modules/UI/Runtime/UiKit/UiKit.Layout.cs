using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// Layout helpers of the rebuilt kit (docs/design): every component is a tree of uGUI layout groups, so sizes come from
    /// the design numbers (padding, gaps, minimum heights), never from hand-computed positions. Units are canvas units (u).
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>A child object stretched over nothing in particular: the layout of its parent places it.</summary>
        public static RectTransform Node(Transform parent, string name)
        {
            var rt = UiKit.Rect(parent, name, Vector2.zero, Vector2.zero);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            return rt;
        }

        public static HorizontalLayoutGroup H(GameObject go, float spacing = 0f, RectOffset padding = null,
                                              TextAnchor align = TextAnchor.MiddleLeft, bool controlH = true)
        {
            var g = go.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = controlH;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup V(GameObject go, float spacing = 0f, RectOffset padding = null,
                                            TextAnchor align = TextAnchor.UpperLeft, bool controlW = true)
        {
            var g = go.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = controlW;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        /// <summary>Size hints for the parent's layout group (-1 = unset).</summary>
        public static LayoutElement Size(GameObject go, float minW = -1f, float minH = -1f, float prefW = -1f, float prefH = -1f,
                                         float flexW = -1f, float flexH = -1f)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = minW;
            le.minHeight = minH;
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            return le;
        }

        public static RectOffset Pad(float left, float top, float right, float bottom) =>
            new RectOffset(Mathf.RoundToInt(left), Mathf.RoundToInt(right), Mathf.RoundToInt(top), Mathf.RoundToInt(bottom));

        /// <summary>
        /// A single line of text with the label nudge: the text sits in a box whose bottom padding is twice the nudge, so it
        /// is centered <c>labelNudge</c> u higher (the font sits low). Returns the box; the TMP is its child "Label".
        /// </summary>
        public static TextMeshProUGUI Label(Transform parent, string name, string text, int size, Color color, bool bold = false,
                                            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var box = Node(parent, name + " Box");
            V(box.gameObject, 0f, Pad(0, 0, 0, 2f * UiKit.Theme.labelNudge), TextAnchor.MiddleLeft);
            var t = UiKit.Text(box, "Label", text, size, Vector2.zero, Vector2.zero, bold, color, align);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>
        /// The rounded background of a layout node: an Image on a child "Background" that the layout ignores (an Image on the node
        /// itself would add its sprite's 130 u slice size to the node's preferred size).
        /// </summary>
        public static Image Fill(RectTransform node, Color color, float radius)
        {
            var bg = UiKit.Fill(UiKit.Rect(node, "Background", Vector2.zero, Vector2.zero));
            bg.SetAsFirstSibling();
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var img = bg.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            UiKit.Round(img, radius);
            return img;
        }
    }
}
