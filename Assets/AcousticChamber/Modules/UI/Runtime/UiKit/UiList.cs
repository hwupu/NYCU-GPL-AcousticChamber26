using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>How far a list reaches beyond its parent's right edge: the strip the scroll indicator lives in (24 u, as in the Storybook).</summary>
        public const float IndicatorMargin = 24f;

        /// <summary>
        /// UiList (docs/design): a borderless scrolling stack (<see cref="ScrollList"/>: edge fades, thin indicator, thumbstick and
        /// drag input) whose content is a vertical layout (24 u between sections, 8 u at the end). It fills the free height of
        /// its parent layout and sizes the scroll range from the content's preferred height. Returns the content to add to.
        /// The list reaches <see cref="IndicatorMargin"/> u beyond its parent's right edge (a negative right margin, in a wrapper
        /// named "&lt;name&gt; Margin") while the content keeps the same right padding, so the content stays aligned and the scroll
        /// indicator sits in the margin instead of over the content.
        /// </summary>
        public static RectTransform List(Transform parent, string name, out ScrollList list, Color? fadeColor = null)
        {
            var wrap = Node(parent, name + " Margin");
            var g = H(wrap.gameObject, 0f, Pad(0, 0, -(int)IndicatorMargin, 0), TextAnchor.UpperLeft);
            g.childForceExpandHeight = true;
            Size(wrap.gameObject, minH: 0f, prefH: 0f, flexW: 1f, flexH: 1f);
            list = ScrollList.Create(wrap, name, Vector2.zero, Vector2.zero, fadeColor);
            var rt = list.Viewport;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            Size(list.gameObject, minW: 0f, minH: 0f, prefW: 0f, prefH: 0f, flexW: 1f, flexH: 1f);
            V(list.Content.gameObject, 24f, Pad(0, 0, IndicatorMargin, 8), TextAnchor.UpperLeft);
            list.Content.gameObject.AddComponent<UiListSync>().List = list;
            return list.Content;
        }
    }

    /// <summary>Keeps a <see cref="ScrollList"/>'s scroll range equal to its laid-out content.</summary>
    public class UiListSync : MonoBehaviour
    {
        public ScrollList List;
        float _content = -1f, _viewport = -1f;

        void LateUpdate() => Sync();

        /// <summary>Re-measures now (also callable from tests).</summary>
        public void Sync()
        {
            if (List == null) return;
            var content = (RectTransform)transform;
            float h = LayoutUtility.GetPreferredHeight(content);
            float vh = List.Viewport.rect.height;
            if (Mathf.Approximately(h, _content) && Mathf.Approximately(vh, _viewport)) return;
            _content = h;
            _viewport = vh;
            List.ContentHeight = h;
        }
    }
}
