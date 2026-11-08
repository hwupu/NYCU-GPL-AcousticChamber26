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

        /// <summary>
        /// The window of a canvas: a layer filling the canvas with the panel background. Build a screen's content under it;
        /// <see cref="Sheet"/>s opened on the canvas push it back and dim it.
        /// </summary>
        public static RectTransform Window(Transform canvas, string name = "Window")
        {
            var rt = Fill(Rect(canvas, name, Vector2.zero, Vector2.zero));
            Background(rt);
            UiLayer.Attach(rt, Theme.panelRadius, shown: true);
            return rt;
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
            Lift(text); // single line: up by OpticalLift (Noto's line box sits the letters low)
            var button = img.gameObject.AddComponent<UiButton>();
            button.targetGraphic = img;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.Style = style;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>
        /// The <paramref name="index"/>-th of <paramref name="count"/> equal segments (borderless, on a raised track) in a row that spans
        /// <paramref name="width"/> from <paramref name="left"/> (canvas x of the row's left edge) at height <paramref name="y"/>.
        /// Show the state with <see cref="Select"/>.
        /// </summary>
        public static UiButton ChoiceButton(Transform parent, string label, int index, int count, float left, float width, float y,
                                            Action onClick, float gap = 16f)
        {
            float w = (width - gap * (count - 1)) / count;
            // segmented control (DesignSystem): a raised track behind the row, borderless segments, the chosen one filled with `select`
            if (index == 0)
                Box(parent, "Choice Track", new Vector2(left + width / 2f, y), new Vector2(width + 8f, Theme.buttonHeight + 8f), Theme.raised,
                    (Theme.buttonHeight + 8f) / 2f);
            var b = Button(parent, label, label, UiStyle.Borderless, new Vector2(left + w / 2f + index * (w + gap), y), new Vector2(w, 0f), onClick);
            b.InGroup = true;
            var rt = (RectTransform)b.transform;
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y); // narrower than the minimum width on purpose
            return b;
        }

        /// <summary>Sets <see cref="UiButton.Selected"/> on each button by index (null and destroyed buttons are skipped).</summary>
        public static void Select(System.Collections.Generic.IList<UiButton> buttons, Func<int, bool> on)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i] != null) buttons[i].Selected = on(i);
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

        // ---- groups and rows (DesignSystem section 4) ------------------------------------------------------------

        /// <summary>Height of a row in a group.</summary>
        public const float RowHeight = 88f;

        /// <summary>
        /// A <c>raised</c> group container for <paramref name="rows"/> rows: <c>groupPadding</c> from its edge to the rows,
        /// rows <c>inset</c> apart, group radius.
        /// </summary>
        public static RectTransform Group(Transform parent, string name, Vector2 pos, float width, int rows)
        {
            var th = Theme;
            float h = rows * RowHeight + Mathf.Max(0, rows - 1) * th.inset + 2f * th.groupPadding;
            return Box(parent, name, pos, new Vector2(width, h), th.raised, th.groupRadius).rectTransform;
        }

        /// <summary>
        /// Row <paramref name="index"/> (0 = top) of a <see cref="Group"/>: a borderless button across the group (concentric
        /// corners: group radius − padding), label at the left, <paramref name="value"/> in <c>text2</c> and a › chevron at
        /// the right (<paramref name="chevron"/>: the row opens a page).
        /// </summary>
        public static UiButton Row(RectTransform group, int index, string label, string value = null, Action onClick = null,
                                   bool chevron = true, string sub = null)
        {
            var th = Theme;
            RowGeometry(group, index, out Vector2 pos, out Vector2 size);
            var b = Button(group, "Row " + label, label, UiStyle.Borderless, pos, size, onClick);
            b.InGroup = true;
            ((RectTransform)b.transform).sizeDelta = size;
            Round(b.GetComponent<Image>(), Mathf.Max(0f, th.groupRadius - th.groupPadding));
            var l = LabelOf(b);
            l.alignment = TextAlignmentOptions.Left;
            l.fontSize = th.bodySize;
            LayoutRowText(b.transform, l, sub);
            string trailing = (value ?? "") + (chevron ? (string.IsNullOrEmpty(value) ? "›" : "  ›") : "");
            if (trailing.Length > 0) RowValue(b.transform, trailing, RowTextPad);
            return b;
        }

        /// <summary>Left / right padding of the text in a row.</summary>
        public const float RowTextPad = 32f;

        /// <summary>
        /// A row that is not a button (a parameter with its own controls): label, optional secondary line, and a value text
        /// (child "Value") ending <paramref name="valueRight"/> units from the row's right edge (room for − / + or a switch).
        /// </summary>
        public static RectTransform InfoRow(RectTransform group, int index, string label, string sub = null, string value = null,
                                            float valueRight = RowTextPad)
        {
            var th = Theme;
            RowGeometry(group, index, out Vector2 pos, out Vector2 size);
            var row = Rect(group, "Row " + label, pos, size);
            var l = Text(row, "Label", label, th.bodySize, Vector2.zero, Vector2.zero, align: TextAlignmentOptions.Left);
            l.textWrappingMode = TextWrappingModes.NoWrap;
            Fill(l.rectTransform);
            LayoutRowText(row, l, sub);
            RowValue(row, value ?? "", valueRight);
            return row;
        }

        static void RowGeometry(RectTransform group, int index, out Vector2 pos, out Vector2 size)
        {
            var th = Theme;
            var g = group.rect.size;
            float pad = th.groupPadding;
            size = new Vector2(g.x - 2f * pad, RowHeight);
            pos = new Vector2(0f, g.y / 2f - pad - RowHeight / 2f - index * (RowHeight + th.inset));
        }

        /// <summary>Line height of Noto Sans TC as a multiple of the font size (ascender 1.16 + descender 0.29; CSS "normal").</summary>
        public const float LineFactor = 1.45f;

        /// <summary>One line of text at <paramref name="fontSize"/> (TMP needs at least this height, or an ellipsis line vanishes).</summary>
        public static float LineHeight(float fontSize) => fontSize * LineFactor;

        /// <summary>
        /// Single-line text sits visually low when centered by line metrics (the line box keeps room for 中文 ascenders):
        /// moved up by this fraction of the font size (PO 2026-11-07: about 1-2 u). Two-line rows are not moved.
        /// </summary>
        public const float OpticalLift = 0.05f;

        /// <summary>Moves a stretched single-line text up by <see cref="OpticalLift"/>.</summary>
        public static void Lift(TextMeshProUGUI t)
        {
            float d = t.fontSize * OpticalLift;
            var rt = t.rectTransform;
            rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y + d);
            rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y + d);
        }

        /// <summary>
        /// One line that ends in … when too long. Makes the box at least one line high (TMP hides a line that does not fit
        /// the height when the overflow mode is Ellipsis), keeping its center.
        /// </summary>
        public static TextMeshProUGUI SingleLine(TextMeshProUGUI t)
        {
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            var rt = t.rectTransform;
            float need = LineHeight(t.fontSize) + 4f, h = rt.rect.height;
            if (Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y)) { if (rt.sizeDelta.y < need) rt.sizeDelta = new Vector2(rt.sizeDelta.x, need); }
            else if (h < need)
            {
                float g = (need - h) / 2f;
                rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - g);
                rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y + g);
            }
            return t;
        }

        /// <summary>
        /// Label alone: centered vertically. With a secondary line: label and line (24 u, text2) stacked and centered together
        /// as one block, each in a box one line high, so they sit like two lines of text (DesignSystem, components.html).
        /// Text is centered by the font's line metrics (TMP "Left"/"Right", not "Midline", which centers the ink and moves
        /// words with descenders or 中文).
        /// </summary>
        static void LayoutRowText(Transform row, TextMeshProUGUI label, string sub)
        {
            var th = Theme;
            if (string.IsNullOrEmpty(sub))
            {
                PlaceRowLabel(label.rectTransform, null, label, null);
                return;
            }
            var s = Text(row, "Sub", sub, th.minimumSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.Left);
            s.textWrappingMode = TextWrappingModes.NoWrap;
            s.overflowMode = TextOverflowModes.Ellipsis; // a long line ends in … instead of running under the value
            Fill(s.rectTransform);
            PlaceRowLabel(label.rectTransform, s.rectTransform, label, s);
        }

        /// <summary>One line: the label fills the row. Two lines: label box above the sub box, the pair centered in the row.</summary>
        static void PlaceRowLabel(RectTransform lr, RectTransform sr, TextMeshProUGUI label, TextMeshProUGUI sub)
        {
            float right = -lr.offsetMax.x > RowTextPad ? lr.offsetMax.x : -RowTextPad; // keep a wider right margin set by the caller
            if (sr == null)
            {
                float up = label.fontSize * OpticalLift;
                lr.offsetMin = new Vector2(RowTextPad, up);
                lr.offsetMax = new Vector2(right, up);
                return;
            }
            float mid = RowHeight / 2f;
            float lh = LineHeight(label.fontSize), sh = LineHeight(sub.fontSize);
            float lc = sh / 2f, sc = -lh / 2f; // centers relative to the row center
            const float slack = 2f; // boxes 4 u taller than a line, same centers (text is centered in its box)
            lr.offsetMin = new Vector2(RowTextPad, mid + lc - lh / 2f - slack);
            lr.offsetMax = new Vector2(right, lc + lh / 2f - mid + slack);
            sr.offsetMin = new Vector2(RowTextPad, mid + sc - sh / 2f - slack);
            sr.offsetMax = new Vector2(right, sc + sh / 2f - mid + slack);
        }

        static TextMeshProUGUI RowValue(Transform row, string text, float right)
        {
            var th = Theme;
            var v = Text(row, "Value", text, th.bodySize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.Right);
            v.textWrappingMode = TextWrappingModes.NoWrap;
            Fill(v.rectTransform);
            float up = v.fontSize * OpticalLift;
            v.rectTransform.offsetMin = new Vector2(RowTextPad, up);
            v.rectTransform.offsetMax = new Vector2(-right, up);
            return v;
        }

        /// <summary>
        /// Changes a row's secondary line (made with a non-empty <c>sub</c>); an empty text hides it and centers the label.
        /// </summary>
        public static void SetRowSub(Transform row, string sub)
        {
            var s = RowText(row, "Sub");
            var l = RowText(row, "Label");
            if (s == null || l == null) return;
            bool show = !string.IsNullOrEmpty(sub);
            s.text = sub ?? "";
            s.gameObject.SetActive(show);
            PlaceRowLabel(l.rectTransform, show ? s.rectTransform : null, l, show ? s : null);
        }

        /// <summary>The text of a row's child "Value" / "Sub" / "Label" (null if it has none).</summary>
        public static TextMeshProUGUI RowText(Transform row, string child)
        {
            var t = row.Find(child);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        /// <summary>A small square gray button (− / + steppers): <c>smallButtonHeight</c> high, <paramref name="width"/> wide.</summary>
        public static UiButton SmallButton(Transform parent, string name, string label, Vector2 pos, float width, Action onClick,
                                           UiStyle style = UiStyle.Gray)
        {
            var th = Theme;
            var b = Button(parent, name, label, style, pos, new Vector2(width, th.smallButtonHeight), onClick);
            ((RectTransform)b.transform).sizeDelta = new Vector2(width, th.smallButtonHeight);
            return b;
        }

        public const string MinusGlyph = "−"; // − U+2212

        /// <summary>A switch (100 x 60 u) at <paramref name="pos"/>; put it in a row whose whole area toggles it.</summary>
        public static UiSwitch Switch(Transform parent, string name, Vector2 pos, bool on) => UiSwitch.Create(parent, name, pos, on);

        /// <summary>
        /// A row with a switch at the right; the whole row is the target. <paramref name="changed"/> gets the new state.
        /// </summary>
        public static UiSwitch SwitchRow(RectTransform group, int index, string label, string sub, bool on, Action<bool> changed)
        {
            UiSwitch sw = null;
            var row = Row(group, index, label, null, () =>
            {
                sw.On = !sw.On;
                changed?.Invoke(sw.On);
            }, chevron: false, sub: sub);
            var size = ((RectTransform)row.transform).sizeDelta;
            sw = Switch(row.transform, "Switch", new Vector2(size.x / 2f - 24f - UiSwitch.Width / 2f, 0f), on);
            return sw;
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
