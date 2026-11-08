using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// An icon (docs/design UiIcon): a white sprite from the theme's <see cref="UiIconSet"/>, tinted with the color of the
    /// label next to it. Never a glyph character. Icons are not moved by the label nudge.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UiIcon : MonoBehaviour
    {
        [SerializeField] string iconName;
        /// <summary>True = tinted with the label color of the button it sits in (button icons).</summary>
        public bool FollowsLabel;
        Image _image;

        Image Img => _image != null ? _image : (_image = GetComponent<Image>());

        public string Name
        {
            get => iconName;
            set
            {
                iconName = value;
                var sprite = UiKit.Theme.icons != null ? UiKit.Theme.icons.Get(value) : null;
                if (sprite == null && !string.IsNullOrEmpty(value))
                    Debug.LogWarning("[GPL] UiIcon: no icon named '" + value + "' (UiSetup.EnsureIconSet).", this);
                Img.sprite = sprite;
                Img.enabled = sprite != null;
            }
        }

        public Color Color
        {
            get => Img.color;
            set => Img.color = value;
        }
    }

    public static partial class UiKit
    {
        /// <summary>UiIcon in a layout: the icon of a button or row (<paramref name="size"/> 0 = theme icon size), fixed size, tinted <paramref name="color"/> if given.</summary>
        public static UiIcon Icon(Transform parent, string name, string icon, float size = 0f, Color? color = null)
        {
            var ic = IconNode(parent, name, icon, size > 0f ? size : Theme.iconSize);
            if (color.HasValue) ic.Color = color.Value;
            return ic;
        }

        static UiIcon IconNode(Transform parent, string name, string icon, float size)
        {
            var ic = BareIcon(parent, name, icon, size);
            Size(ic.gameObject, minW: size, minH: size, prefW: size, prefH: size, flexW: 0f, flexH: 0f);
            return ic;
        }
    }
}
