using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>
        /// UiTextField: a <c>raised</c> capsule, 80 u high, showing a value (or a placeholder in <c>text3</c>); focused = a 4 u
        /// <c>select</c> ring. It shows a value and does not own the keyboard (fed by an <see cref="ITextEntry"/>).
        /// </summary>
        public static UiTextField TextField(Transform parent, string name, string value = null, string placeholder = null,
                                            bool alignLeft = false)
        {
            var th = Theme;
            var node = Node(parent, name);
            var ring = Fill(Rect(node, "Ring", Vector2.zero, Vector2.zero), -th.inset);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.color = th.select;
            ringImg.raycastTarget = false;
            Round(ringImg, th.buttonHeight / 2f + th.inset);
            ring.gameObject.SetActive(false);
            Fill(node, th.raised, th.buttonHeight / 2f);
            H(node.gameObject, th.gap, Pad(40, 0, 40, 0), alignLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            Size(node.gameObject, minH: th.buttonHeight, prefH: th.buttonHeight, flexW: 1f, flexH: 0f);
            var label = Label(node, "Value", "", th.bodySize, th.text);
            var f = node.gameObject.AddComponent<UiTextField>();
            f.Init(label, ring.gameObject, placeholder);
            f.Value = value;
            return f;
        }
    }

    /// <summary>The value display of <see cref="UiKit.TextField"/>.</summary>
    public class UiTextField : MonoBehaviour
    {
        TextMeshProUGUI _label;
        GameObject _ring;
        string _placeholder, _value;
        bool _focused;

        internal void Init(TextMeshProUGUI label, GameObject ring, string placeholder)
        {
            _label = label;
            _ring = ring;
            _placeholder = placeholder;
        }

        /// <summary>The text shown; empty or null shows the placeholder.</summary>
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                bool empty = string.IsNullOrEmpty(value);
                _label.text = empty ? _placeholder ?? "" : value;
                _label.color = empty ? UiKit.Theme.text3 : UiKit.Theme.text;
            }
        }

        /// <summary>True = the 4 u select ring (this entry takes the next key).</summary>
        public bool Focused
        {
            get => _focused;
            set
            {
                _focused = value;
                _ring.SetActive(value);
            }
        }
    }
}
