using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// Builds design-system elements (DesignSystem v2, ADR-0005) under a canvas, at run time or in Editor setup steps:
    /// panels, boxes with any corner radius, text, <see cref="UiButton"/>s, icon buttons, the DEBUG tag. Every new object
    /// takes its parent's layer, so content added to an OVROverlayCanvas at run time stays on the hidden canvas layer.
    /// Positions and sizes are canvas units; the anchor is the parent's center unless stated.
    /// </summary>
    public static class UiKit
    {
        static UiTheme _theme;
        static Sprite _round;

        /// <summary>Theme used by all builders and components. Set by <see cref="UiRoot"/>; falls back to the defaults.</summary>
        public static UiTheme Theme
        {
            get
            {
                if (_theme == null) _theme = UiTheme.CreateDefault();
                return _theme;
            }
            set
            {
                _theme = value;
                if (_theme != null) _theme.NotifyEdited();
            }
        }

        /// <summary>
        /// While true every UI transition is instant. Set it while a stimulus plays: nothing in the UI moves then
        /// (DesignSystem section 5).
        /// </summary>
        public static bool MotionSuppressed { get; set; }

        // ---- structure ---------------------------------------------------------------------------------------

        public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
            }
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Stretches <paramref name="rt"/> over its parent, inset by <paramref name="inset"/> units.</summary>
        public static RectTransform Fill(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>
        /// A solid box. <paramref name="radius"/>: corner radius in units; negative = the panel radius; 0 = square corners.
        /// </summary>
        public static Image Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color, float radius = -1f)
        {
            var rt = Rect(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Round(img, radius < 0f ? Theme.panelRadius : radius);
            return img;
        }

        /// <summary>Gives an image rounded corners of <paramref name="radius"/> units (0 = square). One sprite serves every radius.</summary>
        public static void Round(Image img, float radius)
        {
            if (radius <= 0f)
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                return;
            }
            img.sprite = RoundSprite;
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = SpriteRadiusPx / radius;
        }

        /// <summary>A full-size panel background in the surface color, panel corner radius.</summary>
        public static Image Background(Transform parent, string name = "Background", Color? color = null)
        {
            var img = Box(parent, name, Vector2.zero, Vector2.zero, color ?? Theme.surface);
            Fill(img.rectTransform);
            return img;
        }

        // ---- text --------------------------------------------------------------------------------------------

        public static TextMeshProUGUI Text(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box,
                                           bool bold = false, Color? color = null,
                                           TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(parent, name, pos, box);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = bold && Theme.bold != null ? Theme.bold : Theme.regular;
            if (font != null) t.font = font;
            t.text = text ?? "";
            t.fontSize = size;
            t.color = color ?? Theme.text;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Text that follows the current language (<see cref="LocalizedText"/>).</summary>
        public static TextMeshProUGUI LocText(Transform parent, string name, string key, int size, Vector2 pos, Vector2 box,
                                              bool bold = false, Color? color = null,
                                              TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Text(parent, name, Loc.Get(key), size, pos, box, bold, color, align);
            t.gameObject.AddComponent<LocalizedText>().key = key;
            return t;
        }

        // ---- buttons -----------------------------------------------------------------------------------------

        /// <summary>
        /// A capsule <see cref="UiButton"/> of <paramref name="style"/>. Height 0 = the theme's button height; the width is
        /// at least the minimum button width (set <c>sizeDelta</c> afterwards for keys and other narrow buttons).
        /// The label is a child named "Label"; it never wraps.
        /// </summary>
        public static UiButton Button(Transform parent, string name, string label, UiStyle style, Vector2 pos, Vector2 size,
                                      Action onClick = null, int fontSize = 0)
        {
            var th = Theme;
            size = new Vector2(Mathf.Max(size.x, th.buttonMinWidth), size.y > 0 ? size.y : th.buttonHeight);
            var img = Box(parent, name, pos, size, Color.clear, size.y / 2f);
            img.raycastTarget = true;
            var text = Text(img.transform, "Label", label, fontSize > 0 ? fontSize : th.buttonSize, Vector2.zero, Vector2.zero);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Fill(text.rectTransform, 8f);
            var button = img.gameObject.AddComponent<UiButton>();
            button.targetGraphic = img;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.Style = style;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>A button whose label follows the current language.</summary>
        public static UiButton LocButton(Transform parent, string name, string key, UiStyle style, Vector2 pos, Vector2 size,
                                         Action onClick = null)
        {
            var b = Button(parent, name, Loc.Get(key), style, pos, size, onClick);
            LabelOf(b).gameObject.AddComponent<LocalizedText>().key = key;
            return b;
        }

        /// <summary>
        /// A round borderless icon button (back ‹, close ×), <c>iconButtonSize</c> wide; the fill appears on hover. The
        /// glyph is centered on its own outline (punctuation like ‹ sits low in its line box otherwise).
        /// </summary>
        public static UiButton IconButton(Transform parent, string name, string glyph, Vector2 pos, Action onClick = null)
        {
            float d = Theme.iconButtonSize;
            var b = Button(parent, name, glyph, UiStyle.Borderless, pos, new Vector2(d, d), onClick, Theme.iconGlyphSize);
            ((RectTransform)b.transform).sizeDelta = new Vector2(d, d);
            var label = LabelOf(b);
            Fill(label.rectTransform, 0f);
            label.alignment = TextAlignmentOptions.MidlineGeoAligned;
            return b;
        }

        /// <summary>Gap between a header's ‹ / × and the title (the title is not interactive, so the 20 u control gap does not apply).</summary>
        public const float IconTitleGap = 8f;

        public const string BackGlyph = "‹";   // ‹ (Noto Sans TC has it; not ❮)
        public const string CloseGlyph = "×";  // × (not ✕)

        public static TextMeshProUGUI LabelOf(Button b) => b.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        /// <summary>The DEBUG tag: amber bold text at the top right of <paramref name="parent"/> (Debug mode only).</summary>
        public static TextMeshProUGUI DebugTag(Transform parent, string name = "DEBUG Tag")
        {
            var th = Theme;
            var t = Text(parent, name, "DEBUG", th.minimumSize, Vector2.zero, new Vector2(200f, 50f), bold: true, color: th.debug,
                         align: TextAlignmentOptions.Right);
            t.characterSpacing = 12f;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one;
            rt.anchoredPosition = new Vector2(-48f, -50f);
            return t;
        }

        // ---- hit regions -------------------------------------------------------------------------------------

        /// <summary>
        /// The region of <paramref name="rt"/> that reacts to the ray, in its local space: its rect, grown around the
        /// center to at least <c>hitMin</c> x <c>hitMin</c> units (DesignSystem section 4).
        /// </summary>
        public static UnityEngine.Rect HitRect(RectTransform rt)
        {
            var r = rt.rect;
            float min = Theme.hitMin;
            float w = Mathf.Max(r.width, min), h = Mathf.Max(r.height, min);
            return new UnityEngine.Rect(r.center.x - w / 2f, r.center.y - h / 2f, w, h);
        }

        // ---- helpers -----------------------------------------------------------------------------------------

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>Destroys all children (run time: Destroy; edit time: DestroyImmediate).</summary>
        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var go = parent.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ---- procedural sprite (no texture assets needed) ----------------------------------------------------

        const int SpriteRadiusPx = 64;
        const int SpriteSizePx = 2 * SpriteRadiusPx + 4;

        /// <summary>
        /// A white disc-cornered square, 9-sliced at the corner radius (64 px, mipmapped). <see cref="Round"/> scales the
        /// slice borders with <c>pixelsPerUnitMultiplier</c>, so one sprite draws every radius.
        /// </summary>
        public static Sprite RoundSprite => _round != null ? _round : (_round = MakeRoundSprite());

        static Sprite MakeRoundSprite()
        {
            const int size = SpriteSizePx;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "GPL Round", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = Coverage(x + 0.5f, y + 0.5f, size, SpriteRadiusPx);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            int border = SpriteRadiusPx + 1;
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                       SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = "GPL Round";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Anti-aliased coverage of a rounded square of the given corner radius.</summary>
        static float Coverage(float x, float y, int size, float radius)
        {
            float half = size * 0.5f;
            float core = half - radius;
            float dx = Mathf.Max(Mathf.Abs(x - half) - core, 0f);
            float dy = Mathf.Max(Mathf.Abs(y - half) - core, 0f);
            float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
            return Mathf.Clamp01(0.5f - d);
        }
    }
}
