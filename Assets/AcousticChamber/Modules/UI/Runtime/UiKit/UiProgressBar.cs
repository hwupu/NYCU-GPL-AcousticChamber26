using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>
        /// UiProgressBar (docs/design UiProgressBar, PB-27): a read-only bar, groove 12 u in <c>gray</c>, fill <c>prominent</c>, capsule
        /// ends, the full width of its parent; an optional caption under it (text2, small), e.g. the time left.
        /// </summary>
        public static UiProgressBar ProgressBar(Transform parent, string name, float value = 0f, string caption = null)
        {
            var th = Theme;
            var node = Node(parent, name);
            V(node.gameObject, 12f, null, TextAnchor.UpperLeft);
            Size(node.gameObject, prefW: 0f, flexW: 1f, minW: 240f);

            var groove = Box(node, "Groove", Vector2.zero, Vector2.zero, th.gray.normal, Track / 2f);
            Size(groove.gameObject, prefW: 0f, flexW: 1f, minH: Track, prefH: Track);
            var fill = Box(groove.transform, "Fill", Vector2.zero, Vector2.zero, th.prominent.normal, Track / 2f);
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0f, 1f);
            frt.pivot = new Vector2(0f, 0.5f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            var text = Text(node, "Caption", caption ?? "", th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
            Size(text.gameObject, prefW: 0f, flexW: 1f);

            var bar = node.gameObject.AddComponent<UiProgressBar>();
            bar.Init(fill.rectTransform, text);
            bar.Value = value;
            bar.Caption = caption;
            return bar;
        }

        const float Track = 12f;
    }

    /// <summary>A read-only progress bar (<see cref="UiKit.ProgressBar"/>): <see cref="Value"/> 0..1 and an optional <see cref="Caption"/>.</summary>
    public class UiProgressBar : MonoBehaviour
    {
        RectTransform _fill;
        TextMeshProUGUI _caption;
        float _value;

        internal void Init(RectTransform fill, TextMeshProUGUI caption)
        {
            _fill = fill;
            _caption = caption;
        }

        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(float.IsNaN(value) ? 0f : value);
                _fill.anchorMax = new Vector2(_value, 1f);
                _fill.gameObject.SetActive(_value > 0f); // a zero-width rounded box would still draw its caps
            }
        }

        /// <summary>Text under the bar; empty or null hides it.</summary>
        public string Caption
        {
            get => _caption.text;
            set
            {
                _caption.text = value ?? "";
                _caption.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }
    }
}
