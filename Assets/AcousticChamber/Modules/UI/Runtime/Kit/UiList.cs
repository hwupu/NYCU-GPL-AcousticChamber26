using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiLay
    {
        /// <summary>
        /// UiList (docs/design): a borderless scrolling stack (<see cref="ScrollList"/>: edge fades, thin indicator, thumbstick and
        /// drag input) whose content is a vertical layout (24 u between sections, 8 u at the end). It fills the free height of
        /// its parent layout and sizes the scroll range from the content's preferred height. Returns the content to add to.
        /// </summary>
        public static RectTransform List(Transform parent, string name, out ScrollList list, Color? fadeColor = null)
        {
            list = ScrollList.Create(parent, name, Vector2.zero, Vector2.zero, fadeColor);
            var rt = list.Viewport;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            Size(list.gameObject, minH: 0f, prefH: 0f, flexW: 1f, flexH: 1f);
            V(list.Content.gameObject, 24f, Pad(0, 0, 0, 8), TextAnchor.UpperLeft);
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
