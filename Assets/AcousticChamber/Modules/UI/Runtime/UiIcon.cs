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
}
