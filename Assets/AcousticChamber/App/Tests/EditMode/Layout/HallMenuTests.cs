using GPL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AcousticChamber.App.Tests
{
    /// <summary>The Main Hall built from the layout kit: card grid sizes of <see cref="HallGrid"/> measured from the laid-out rects.</summary>
    public class HallMenuTests
    {
        GameObject _canvas;
        ChamberCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = true;
            _catalog = ScriptableObject.CreateInstance<ChamberCatalog>();
            for (int i = 1; i <= 3; i++)
            {
                var d = ScriptableObject.CreateInstance<ChamberDefinition>();
                d.id = "Chamber" + i;
                d.label = "C0" + i;
                d.title = "Chamber " + i;
                _catalog.chambers.Add(d);
            }
            _canvas = new GameObject("Hall Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var hall = _canvas.AddComponent<HallMenu>();
            hall.catalog = _catalog;
            hall.Build();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_canvas.GetComponent<HallMenu>().Panel.Surface.transform);
            _canvas.GetComponentInChildren<UiListSync>().Sync(); // sets the scroll range; the content is laid out again at its height
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_canvas.GetComponent<HallMenu>().Panel.Surface.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            foreach (var c in _catalog.chambers) Object.DestroyImmediate(c);
            Object.DestroyImmediate(_catalog);
            UiKit.MotionSuppressed = false;
        }

        RectTransform Find(string path) => (RectTransform)_canvas.GetComponent<HallMenu>().Panel.Surface.Root.transform.Find(path);

        [Test]
        public void SurfaceHasTheTitleBarWithoutBackAndAHiddenStatusLine()
        {
            var surface = (RectTransform)_canvas.GetComponent<HallMenu>().Panel.Surface.transform;
            Assert.AreEqual(HallMenu.Size.x, surface.rect.width, 0.5f);
            Assert.AreEqual(HallMenu.Size.y, surface.rect.height, 0.5f);
            Assert.AreEqual(116f, Find("Navigation Bar").rect.height, 0.5f);
            Assert.IsNull(Find("Navigation Bar/Back"), "the Hall is the root: no back button");
            var title = Find("Navigation Bar/Title Box").GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Assert.AreEqual("Acoustic Chambers", title.text);
            Assert.AreEqual(TMPro.TextAlignmentOptions.Center, title.alignment, "centered title");
            var bar = Find("Navigation Bar");
            var label = title.rectTransform; // the text itself, not only its box (the box was centered, the text sat at its left)
            Assert.AreEqual(bar.rect.center.x, bar.InverseTransformPoint(label.TransformPoint(label.rect.center)).x, 1f, "centered in the whole bar");
            Assert.IsNotNull(Find("Status"));
            Assert.IsFalse(Find("Status").gameObject.activeSelf, "the status line shows only with a message");
        }

        [Test]
        public void TwoRowsOfCardsFitTheWindowWithoutScrolling()
        {
            // PO 2026-11-10: 4 chambers (2 rows, row gap 28, 8 u margin) show at once in the 1000 x 700 Hall
            var grid = new HallGrid(HallMenu.Size.x - 96f - 2f * HallMenu.GridMargin, 4);
            var body = Find("Body");
            Assert.LessOrEqual(grid.TotalHeight + 2f * HallMenu.GridMargin, body.rect.height + 0.5f, "2 rows need " + grid.TotalHeight + " u of " + body.rect.height);
        }

        [Test]
        public void CardsFollowTheHallGridInTwoColumns()
        {
            var grid = new HallGrid(HallMenu.Size.x - 96f - 2f * HallMenu.GridMargin, 3);
            var content = Find("Body/Chambers Margin/Chambers/Content");
            var a = (RectTransform)content.Find("Row 1/Card Chamber1");
            var b = (RectTransform)content.Find("Row 1/Card Chamber2");
            var c = (RectTransform)content.Find("Row 2/Card Chamber3");
            Assert.AreEqual(grid.CardWidth, a.rect.width, 0.5f);
            Assert.AreEqual(grid.CardWidth, b.rect.width, 0.5f);
            Assert.AreEqual(grid.CardWidth, c.rect.width, 0.5f, "the last card keeps half width");
            Assert.AreEqual(grid.CardHeight, a.rect.height, 0.6f);
            Assert.AreEqual(grid.CoverHeight, ((RectTransform)a.Find("Cover")).rect.height, 0.5f);
            Assert.AreEqual(grid.CoverWidth, ((RectTransform)a.Find("Cover")).rect.width, 0.5f);
            // 24 u between the columns, 28 u between the rows (PO 2026-11-10: room to grow on hover)
            Assert.AreEqual(24f, grid.Gap);
            Assert.AreEqual(a.rect.width + grid.Gap, b.localPosition.x - a.localPosition.x, 0.5f);
            var row1 = (RectTransform)content.Find("Row 1");
            var row2 = (RectTransform)content.Find("Row 2");
            Assert.AreEqual(grid.CardHeight + 28f, row1.localPosition.y - row2.localPosition.y, 0.6f);
            Assert.AreEqual(HallMenu.GridMargin, content.GetComponent<VerticalLayoutGroup>().padding.left);
            Assert.AreEqual(HallMenu.GridMargin, content.GetComponent<VerticalLayoutGroup>().padding.top);
        }

        [Test]
        public void TheCoverCarriesTheIdAndTheTitleInWhiteWithASoftShadow()
        {
            var card = Find("Body/Chambers Margin/Chambers/Content/Row 1/Card Chamber1");
            Assert.IsNotNull(card.GetComponent<UiButton>());
            foreach (var t in card.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                Assert.IsTrue(t.transform.IsChildOf(card.Find("Cover")), "no text below the cover: " + t.name);
            var id = card.Find("Cover/C-ID").GetComponent<TMPro.TextMeshProUGUI>();
            var title = card.Find("Cover/Title").GetComponent<TMPro.TextMeshProUGUI>();
            Assert.AreEqual("C01", id.text);
            Assert.AreEqual("Chamber 1", title.text);
            Assert.IsNull(card.Find("Cover/Unfinished"), "no unfinished pill");
            foreach (var t in new[] { id, title })
            {
                Assert.AreEqual(new Color(1f, 1f, 1f, HallMenu.TextAlpha), t.color);
                Assert.IsTrue(t.fontSharedMaterial.IsKeywordEnabled(TMPro.ShaderUtilities.Keyword_Underlay), t.name + " has the shadow");
                Assert.AreEqual(HallMenu.ShadowAlpha, t.fontSharedMaterial.GetColor(TMPro.ShaderUtilities.ID_UnderlayColor).a, 1e-4);
            }
            Assert.AreNotSame(title.font.material, title.fontSharedMaterial, "the font's own material stays plain");
            Assert.IsFalse(title.font.material.IsKeywordEnabled(TMPro.ShaderUtilities.Keyword_Underlay));
            // C-ID 24 u in from the top left, the title 24 u in from the left, at the bottom
            var cover = (RectTransform)card.Find("Cover");
            Assert.AreEqual(new Vector2(HallMenu.TextInset, -HallMenu.TextInset), id.rectTransform.anchoredPosition);
            Assert.AreEqual(HallMenu.TextInset, title.rectTransform.offsetMin.x, 0.01f);
            Assert.AreEqual(UiKit.Theme.titleSize, title.fontSize);
            Assert.Less(title.rectTransform.rect.yMax, cover.rect.height / 2f, "the title sits in the lower half");
            // the whole title is laid out (a box shorter than the line made the ellipsis drop it: 0 characters, 2026-11-10)
            foreach (var t in new[] { id, title })
            {
                t.ForceMeshUpdate(true, true);
                Assert.AreEqual(t.text.Length, t.textInfo.characterCount, t.name + " lays out every character");
                Assert.IsTrue(t.textInfo.characterInfo[0].isVisible, t.name + " is visible");
                Assert.AreEqual(1, t.textInfo.lineCount, t.name + " on one line");
            }
        }

        [Test]
        public void ThumbnailFillsTheRoundedCover()
        {
            var tex = new Texture2D(1376, 768);
            try
            {
                _catalog.chambers[0].thumbnail = tex;
                _canvas.GetComponent<HallMenu>().Build();
                var cover = Find("Body/Chambers Margin/Chambers/Content/Row 1/Card Chamber1/Cover");
                var shape = cover.Find("Background");
                var mask = shape.GetComponent<Mask>();
                Assert.IsNotNull(mask, "rounded corners");
                Assert.IsTrue(mask.MaskEnabled(), "the mask has its rounded graphic (a Mask without one clips nothing)");
                var img = shape.Find("Thumbnail").GetComponent<RawImage>();
                Assert.AreSame(tex, img.texture);
                Assert.AreEqual(HallGrid.CoverAspect, img.uvRect.width * 1376f / (img.uvRect.height * 768f), 1e-3, "cropped to 16:9");
                Assert.AreEqual(0.5f, img.uvRect.center.x, 1e-4, "centered");
                Assert.Less(shape.GetSiblingIndex(), cover.Find("Title").GetSiblingIndex(), "text above the image");
                Assert.AreEqual("GPL/UI Saturation", img.material.shader.name, "the image can change saturation");
            }
            finally { Object.DestroyImmediate(tex); }
        }

        [Test]
        public void HoverGrowsTheCardAndSaturatesTheImage()
        {
            var tex = new Texture2D(16, 9);
            try
            {
                _catalog.chambers[0].thumbnail = tex;
                _canvas.GetComponent<HallMenu>().Build();
                var card = Find("Body/Chambers Margin/Chambers/Content/Row 1/Card Chamber1");
                var button = card.GetComponent<UiButton>();
                var hall = card.GetComponent<HallCard>();
                var img = card.Find("Cover/Background/Thumbnail").GetComponent<RawImage>();
                var tick = typeof(HallCard).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.AreEqual(1f, card.localScale.x, 1e-4);
                Assert.AreEqual(1f, hall.Saturation, 1e-4);

                button.OnPointerEnter(null); // the pointer hovers (motion suppressed: instant)
                tick.Invoke(hall, null);
                Assert.AreEqual(HallCard.HoverScale, card.localScale.x, 1e-4, "103 %");
                Assert.AreEqual(HallCard.HoverSaturation, hall.Saturation, 1e-4);
                Assert.AreEqual(HallCard.HoverSaturation, img.material.GetFloat(UiKit.SaturationId), 1e-4);

                button.Press();
                tick.Invoke(hall, null);
                Assert.AreEqual(HallCard.PressedScale, card.localScale.x, 1e-4, "pressed 101.5 %, not the 97 % of other buttons");
                Assert.AreEqual(HallCard.HoverSaturation, hall.Saturation, 1e-4, "still saturated while pressed");

                button.Release();
                button.OnPointerExit(null);
                tick.Invoke(hall, null);
                Assert.AreEqual(1f, card.localScale.x, 1e-4);
                Assert.AreEqual(1f, hall.Saturation, 1e-4);
            }
            finally { Object.DestroyImmediate(tex); }
        }

        [Test]
        public void OtherButtonsKeepTheThemePressedScale()
        {
            var b = UiKit.Button(_canvas.transform, "Plain", "Plain", UiStyle.Gray);
            b.Press();
            Assert.AreEqual(UiKit.Theme.pressedScale, b.transform.localScale.x, 1e-4);
            b.Release();
            b.OnPointerEnter(null);
            Assert.AreEqual(1f, b.transform.localScale.x, 1e-4, "no hover scale by default");
        }

        [Test]
        public void EveryChamberInTheRealCatalogHasItsCover()
        {
            // the covers must import as 2D textures: imported as cubemaps, every thumbnail reference loaded empty (2026-11-10)
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ChamberCatalog>("Assets/AcousticChamber/App/ChamberCatalog.asset");
            Assert.IsNotNull(catalog);
            Assert.Greater(catalog.Visible().Count, 0);
            foreach (var c in catalog.Visible())
                Assert.IsNotNull(c.thumbnail, c.id + ": its cover (App/Covers) loads as a 2D texture");
        }

        [Test]
        public void CoverUvCropsWideAndTallImagesToSixteenByNine()
        {
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), HallMenu.CoverUv(1600, 900));
            var wide = HallMenu.CoverUv(3200, 900);
            Assert.AreEqual(0.5f, wide.width, 1e-4);
            Assert.AreEqual(0.25f, wide.x, 1e-4);
            var tall = HallMenu.CoverUv(1600, 1800);
            Assert.AreEqual(0.5f, tall.height, 1e-4);
            Assert.AreEqual(0.25f, tall.y, 1e-4);
        }
    }
}
