using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>Meaning of a button; one color per meaning (DesignSystem section 3).</summary>
    public enum UiStyle { Primary, Neutral, Warning, Danger }

    /// <summary>
    /// Design tokens (colors, fonts, type scale, control sizes). Defaults = the AcousticChamber26 design system
    /// (docs/DesignSystem.md). Sizes are canvas units; 1 unit = 0.002 m at 2 m (about 0.057 degrees).
    /// </summary>
    [CreateAssetMenu(menuName = "GPL/UI Theme", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        [Header("Fonts (TextMeshPro, dynamic; one family for English and 中文)")]
        public TMP_FontAsset regular;
        public TMP_FontAsset bold;

        [Header("Surfaces")]
        public Color surface = Rgb(0x171A24);
        public Color surfaceRaised = Rgb(0x292E3D);
        public Color neutral = Rgb(0x384052);

        [Header("Meaning")]
        public Color primary = Rgb(0x216B3D);
        public Color warning = Rgb(0x8A6A1F);
        public Color danger = Rgb(0xA62E2E);
        public Color accent = Rgb(0x3D7BD9);

        [Header("Text")]
        public Color text = Color.white;
        public Color textSecondary = Rgb(0xB8BECC);
        public Color textDisabled = Rgb(0x6B7385);

        [Header("Type scale (units)")]
        public int titleSize = 56;
        public int headingSize = 44;
        public int bodySize = 36;
        public int buttonSize = 36;
        public int smallSize = 28;
        public int minimumSize = 24;

        [Header("Controls (units)")]
        public float buttonHeight = 80f;
        public float buttonMinWidth = 220f;
        public float gap = 20f;
        public float cornerRadius = 12f;
        public float outlineWidth = 4f;
        [Range(0f, 1f)] public float hoverMix = 0.35f;
        [Range(0f, 1f)] public float pressedMix = 0.6f;
        [Range(0f, 1f)] public float disabledAlpha = 0.4f;

        public Color For(UiStyle style)
        {
            switch (style)
            {
                case UiStyle.Primary: return primary;
                case UiStyle.Warning: return warning;
                case UiStyle.Danger: return danger;
                default: return neutral;
            }
        }

        /// <summary>A theme with the default tokens and no fonts (TextMeshPro's default font is used).</summary>
        public static UiTheme CreateDefault()
        {
            var t = CreateInstance<UiTheme>();
            t.name = "UiTheme (default)";
            t.hideFlags = HideFlags.DontSave;
            return t;
        }

        public static Color Rgb(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
