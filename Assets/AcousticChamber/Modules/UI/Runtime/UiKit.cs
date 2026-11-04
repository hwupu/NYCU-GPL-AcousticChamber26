using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// Builds uGUI elements in the theme's style (panels, text, buttons, badges) under a canvas, at run time or in Editor
    /// setup steps. Every new object takes its parent's layer, so content added to an OVROverlayCanvas at run time stays on
    /// the hidden canvas layer (otherwise the eye cameras would draw it a second time).
    /// Positions and sizes are canvas units; the anchor is the parent's center unless stated.
    /// </summary>
    public static class UiKit
    {
        static UiTheme _theme;
        static Sprite _rounded, _ring;

        /// <summary>Theme used by all builders. Set by <see cref="UiRoot"/>; falls back to the default tokens.</summary>
        public static UiTheme Theme
        {
            get
            {
                if (_theme == null) _theme = UiTheme.CreateDefault();
                return _theme;
            }
            set => _theme = value;
        }

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

        /// <summary>A colored box; rounded corners (9-slice) unless <paramref name="rounded"/> is false.</summary>
        public static Image Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool rounded = true)
        {
            var rt = Rect(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (rounded)
            {
                img.sprite = RoundedSprite;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        /// <summary>A full-size panel background in the surface color.</summary>
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
        /// A rounded button in the color of <paramref name="style"/>: hover = 35 % toward white plus an accent outline
        /// (shown by <see cref="UiPointer"/>), disabled = 40 % opacity. The label is a child named "Label".
        /// </summary>
        public static Button Button(Transform parent, string name, string label, UiStyle style, Vector2 pos, Vector2 size,
                                    Action onClick = null, bool bold = false, int fontSize = 0)
        {
            var th = Theme;
            size = new Vector2(Mathf.Max(size.x, th.buttonMinWidth), size.y > 0 ? size.y : th.buttonHeight);
            var img = Box(parent, name, pos, size, Color.white);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            Paint(button, th.For(style));
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var outline = Box(img.transform, "Hover", Vector2.zero, Vector2.zero, th.accent);
            outline.sprite = RingSprite;
            Fill(outline.rectTransform, -th.outlineWidth);
            outline.gameObject.SetActive(false);
            img.gameObject.AddComponent<HoverHighlight>().outline = outline.gameObject;

            var text = Text(img.transform, "Label", label, fontSize > 0 ? fontSize : th.buttonSize, Vector2.zero, Vector2.zero, bold);
            Fill(text.rectTransform, 8f);
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>A button whose label follows the current language.</summary>
        public static Button LocButton(Transform parent, string name, string key, UiStyle style, Vector2 pos, Vector2 size,
                                       Action onClick = null, bool bold = false)
        {
            var b = Button(parent, name, Loc.Get(key), style, pos, size, onClick, bold);
            LabelOf(b).gameObject.AddComponent<LocalizedText>().key = key;
            return b;
        }

        public static TextMeshProUGUI LabelOf(Button b) => b.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        /// <summary>Re-colors a button (normal / hover / pressed / disabled from one base color).</summary>
        public static void Paint(Button button, Color c)
        {
            var th = Theme;
            var colors = button.colors;
            colors.normalColor = c;
            colors.selectedColor = c;
            colors.highlightedColor = Color.Lerp(c, Color.white, th.hoverMix);
            colors.pressedColor = Color.Lerp(c, Color.white, th.pressedMix);
            colors.disabledColor = new Color(c.r, c.g, c.b, c.a * th.disabledAlpha);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }

        /// <summary>A small rounded label, e.g. the DEBUG badge, anchored to the parent's top-right corner.</summary>
        public static Image Badge(Transform parent, string name, string text, Color color, Vector2 size)
        {
            var img = Box(parent, name, Vector2.zero, size, color);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one;
            rt.anchoredPosition = new Vector2(-Theme.gap, -Theme.gap);
            var t = Text(img.transform, "Label", text, Theme.smallSize, Vector2.zero, Vector2.zero, bold: true);
            Fill(t.rectTransform);
            return img;
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

        // ---- procedural sprites (no texture assets needed) ---------------------------------------------------

        /// <summary>Filled rounded rectangle, 9-sliced; corner radius = theme radius (1 sprite pixel = 1 canvas unit).</summary>
        public static Sprite RoundedSprite => _rounded != null ? _rounded : (_rounded = MakeSprite("GPL Rounded", Theme.cornerRadius, 0f));

        /// <summary>Rounded outline (ring) used for the hover highlight.</summary>
        public static Sprite RingSprite =>
            _ring != null ? _ring : (_ring = MakeSprite("GPL Ring", Theme.cornerRadius + Theme.outlineWidth, Theme.outlineWidth));

        static Sprite MakeSprite(string name, float radius, float thickness)
        {
            const int size = 64;
            int border = Mathf.Clamp(Mathf.CeilToInt(radius) + 2, 4, size / 2 - 1);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = Coverage(x + 0.5f, y + 0.5f, size, 0f, radius);
                if (thickness > 0f) a = Mathf.Clamp01(a - Coverage(x + 0.5f, y + 0.5f, size, thickness, Mathf.Max(radius - thickness, 0f)));
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                       SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Anti-aliased coverage of a rounded rectangle inset by <paramref name="inset"/> pixels.</summary>
        static float Coverage(float x, float y, int size, float inset, float radius)
        {
            float half = size * 0.5f;
            float core = half - inset - radius;
            float dx = Mathf.Max(Mathf.Abs(x - half) - core, 0f);
            float dy = Mathf.Max(Mathf.Abs(y - half) - core, 0f);
            float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
            return Mathf.Clamp01(0.5f - d);
        }
    }
}
