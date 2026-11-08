using System;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>Button styles (DesignSystem section 4). Hierarchy by fill, not by hue.</summary>
    public enum UiStyle
    {
        /// <summary>The one main action of a screen (white fill, dark bold text).</summary>
        Prominent,
        /// <summary>A secondary action that needs a visible target.</summary>
        Gray,
        /// <summary>No fill until hovered: back, cancel, keys, list actions.</summary>
        Borderless,
        /// <summary>Borderless with red text: a destructive action that is not the final step.</summary>
        DestructiveText,
        /// <summary>Red fill: only the final confirmation (Drop out, Delete).</summary>
        Destructive,
    }

    /// <summary>Fills of one button style; each state is brighter than the one before (pressed is the brightest).</summary>
    [Serializable]
    public struct StateColors
    {
        public Color normal, hover, pressed, label;

        public StateColors(Color normal, Color hover, Color pressed, Color label)
        {
            this.normal = normal;
            this.hover = hover;
            this.pressed = pressed;
            this.label = label;
        }
    }

    /// <summary>
    /// Design tokens: colors, fonts, type scale, geometry, motion. Defaults = design system v2 (docs/DesignSystem.md,
    /// ADR-0005). Sizes are canvas units (1 u = 2 mm at 2 m ≈ 0.057°). Editing the asset restyles every
    /// <see cref="UiButton"/> at once (<see cref="Edited"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "GPL/UI Theme", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        /// <summary>Token layout version; assets below it are reset to the defaults by the setup step (fonts kept).</summary>
        public const int CurrentVersion = 3;
        [HideInInspector] public int version;

        [Header("Fonts (TextMeshPro, dynamic; one family for English and 中文)")]
        public TMP_FontAsset regular;
        public TMP_FontAsset bold;
        [Tooltip("Icon sprites by name (UiSetup.EnsureIconSet).")]
        public UiIconSet icons;

        [Header("Surfaces and text")]
        public Color surface = Rgb(0x1C1C1F);
        public Color raised = Rgb(0x2A2A2E);
        public Color cover = Rgb(0x4A4A50);
        public Color select = Rgb(0x4A4A50);
        public Color text = Color.white;
        public Color text2 = Rgb(0xA1A1A8);
        public Color text3 = Rgb(0x5E5E66);
        public Color debug = Rgb(0xE0A93B);
        public Color dim = new Color(0f, 0f, 0f, 0.45f);

        [Header("Buttons: normal / hover / pressed / label")]
        public StateColors prominent = new StateColors(Rgb(0xD6D6DB), Rgb(0xE9E9ED), Rgb(0xFFFFFF), Rgb(0x141416));
        public StateColors gray = new StateColors(Rgb(0x3A3A3F), Rgb(0x48484E), Rgb(0x5A5A61), Color.white);
        public StateColors borderless = new StateColors(Clear, Rgb(0x2E2E33), Rgb(0x3F3F45), Color.white);
        [Tooltip("Borderless buttons inside a raised group.")]
        public StateColors borderlessInGroup = new StateColors(Clear, Rgb(0x3A3A3F), Rgb(0x4A4A50), Color.white);
        [Tooltip("Red keeps hue and saturation; only its brightness (HSV value) rises: 75 / 85 / 95 %.")]
        public StateColors destructive = new StateColors(Rgb(0xBF3D37), Rgb(0xD9453F), Rgb(0xF24D46), Color.white);
        public Color redText = Rgb(0xFF6B63);
        [Range(0f, 1f)] public float disabledAlpha = 0.4f;

        [Header("Type scale (u)")]
        public int titleSize = 56;
        public int headingSize = 44;
        public int bodySize = 36;
        public int buttonSize = 36;
        public int smallSize = 28;
        public int minimumSize = 24;

        [Header("Geometry (u)")]
        public float buttonHeight = 80f;
        public float smallButtonHeight = 60f;
        public float buttonMinWidth = 220f;
        public float iconButtonSize = 80f;
        [Tooltip("Font size of the glyph in an icon button (‹, ×).")]
        public int iconGlyphSize = 72;
        [Tooltip("Minimum hit region of an interactive element (invisible).")]
        public float hitMin = 60f;
        [Tooltip("Gap between separate controls.")]
        public float gap = 20f;
        [Tooltip("Padding between a container and the shapes inside it, and between those shapes.")]
        public float inset = 4f;
        [Tooltip("Padding from a group's edge to its rows (rows are inset apart from each other).")]
        public float groupPadding = 8f;
        public float panelRadius = 32f;
        public float groupRadius = 36f;
        public float rowRadius = 28f;
        public float cardRadius = 40f;

        [Header("Kit v3 (docs/design theme.css): icons, label nudge, panel padding")]
        [Tooltip("Single-line text moves up by this many units (the font sits low). Icons are not moved.")]
        public float labelNudge = 2f;
        public float iconSize = 40f;
        public float iconSizeSmall = 32f;
        [Tooltip("Gap between an icon and a label.")]
        public float iconGap = 12f;
        [Tooltip("Left and right padding of a button's label.")]
        public float buttonPaddingX = 40f;
        [Tooltip("The side of a button or row that has an icon keeps this share of the padding (25 % less).")]
        [Range(0.5f, 1f)] public float iconSidePaddingShare = 0.75f;
        [Tooltip("Left and right padding of a surface's content.")]
        public float surfacePaddingX = 48f;
        [Tooltip("Top padding of a navigation bar (40 less 4: the font sits low).")]
        public float navTopPadding = 36f;
        [Tooltip("Distance from the bottom edge to the last row of a surface or its toolbar.")]
        public float surfacePaddingBottom = 40f;
        public float rowMinHeight = 88f;
        public float rowPaddingX = 28f;
        public float dialogWidth = 820f;
        public float panelWidth = 1000f;
        [Range(0.8f, 1f)] public float pressedScale = 0.97f;
        [Tooltip("Scale a sheet appears from (and leaves to).")]
        [Range(0.8f, 1f)] public float sheetScale = 0.94f;
        [Tooltip("Scale of a layer per sheet on top of it (94 %, two levels 88 %).")]
        [Range(0.8f, 1f)] public float pushScale = 0.94f;

        [Header("Motion (s, unscaled time; curve = Motion.Settle)")]
        public float hoverIn = 0.15f;
        public float hoverOut = 0.35f;
        public float release = 0.25f;
        public float sheet = 0.45f;
        public float page = 0.45f;
        [Tooltip("Scale a page appears from (and leaves to); pages change in place.")]
        [Range(0.8f, 1f)] public float pageScale = 0.98f;
        public float push = 0.45f;
        [Tooltip("Every transition instant (accessibility, or tests).")]
        public bool reduceMotion;

        /// <summary>Raised when any theme asset is edited (Inspector) or <see cref="NotifyEdited"/> is called.</summary>
        public static event Action Edited;

        public void NotifyEdited() => Edited?.Invoke();
        void OnValidate() => Edited?.Invoke();

        public StateColors Colors(UiStyle style)
        {
            switch (style)
            {
                case UiStyle.Prominent: return prominent;
                case UiStyle.Destructive: return destructive;
                case UiStyle.Borderless: return borderless;
                case UiStyle.DestructiveText:
                    var c = borderless;
                    c.label = redText;
                    return c;
                default: return gray;
            }
        }

        /// <summary>Duration of a transition, or 0 when motion is reduced (setting) or suppressed (stimulus playing).</summary>
        public float Duration(float seconds) => reduceMotion || UiKit.MotionSuppressed ? 0f : seconds;

        /// <summary>Copies every token from the defaults, keeping the fonts (migration of old assets).</summary>
        public void ResetTokens()
        {
            var fonts = (regular, bold);
            var iconSet = icons;
            var d = CreateInstance<UiTheme>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(d), this);
            DestroyImmediate(d);
            (regular, bold) = fonts;
            icons = iconSet;
            version = CurrentVersion;
        }

        /// <summary>A theme with the default tokens and no fonts (TextMeshPro's default font is used).</summary>
        public static UiTheme CreateDefault()
        {
            var t = CreateInstance<UiTheme>();
            t.name = "UiTheme (default)";
            t.hideFlags = HideFlags.DontSave;
            t.version = CurrentVersion;
            return t;
        }

        public static Color Rgb(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        static Color Clear => new Color(1f, 1f, 1f, 0f);
    }
}
