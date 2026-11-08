using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// Basic builders under a canvas (rects, rounded boxes, text, icons) and the theme and motion switches shared by the
    /// kit (docs/design, ADR-0005 / 0007). The factories of the components (<c>UiKit.Button</c>, <c>UiKit.Row</c>, ... one file
    /// per Storybook component, same name) and the classes <see cref="UiPanel"/>, <see cref="UiSurface"/> are laid out by uGUI layout
    /// groups; these builders are what they are made of. Every new object takes its parent's layer, so
    /// content added to an OVROverlayCanvas at run time stays on the hidden canvas layer. Sizes are canvas units.
    /// </summary>
    public static partial class UiKit
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

        // ---- icons -------------------------------------------------------------------------------------------

        /// <summary>An icon of the theme's icon set, <paramref name="size"/> u square (0 = <c>iconSize</c>), tinted <paramref name="color"/> (default: text).</summary>
        public static UiIcon BareIcon(Transform parent, string name, string icon, float size = 0f, Color? color = null)
        {
            if (size <= 0f) size = Theme.iconSize;
            var rt = Rect(parent, name, Vector2.zero, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            var ui = rt.gameObject.AddComponent<UiIcon>();
            ui.Color = color ?? Theme.text;
            ui.Name = icon;
            return ui;
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


        /// <summary>Sets <see cref="UiButton.Selected"/> on each button by index (null and destroyed buttons are skipped).</summary>
        public static void Select(System.Collections.Generic.IList<UiButton> buttons, Func<int, bool> on)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i] != null) buttons[i].Selected = on(i);
        }

        /// <summary>Left / right padding of the text in a row.</summary>
        public const float RowTextPad = 32f;

        /// <summary>
        /// True if <paramref name="t"/> lies under a canvas group that does not block the ray: a panel or page that is leaving
        /// (fading out), pushed back or hidden. Such content no longer takes the pointer.
        /// </summary>
        public static bool InLeavingLayer(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                var g = t.GetComponent<CanvasGroup>();
                if (g != null && !g.blocksRaycasts) return true;
            }
            return false;
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

        /// <summary>
        /// Destroys all children built by UI code (run time: Destroy; edit time: DestroyImmediate). Hidden children are
        /// kept: they belong to other components (OVROverlayCanvas puts its render camera, imposter and overlay under the
        /// canvas with HideInHierarchy / DontSave; destroying them breaks the overlay).
        /// </summary>
        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var go = parent.GetChild(i).gameObject;
                if ((go.hideFlags & (HideFlags.HideInHierarchy | HideFlags.DontSave)) != 0) continue;
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
