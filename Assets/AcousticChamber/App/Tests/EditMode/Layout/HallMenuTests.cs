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
        public void SurfaceHasTheTitleBarWithoutBackAndTheStatusLine()
        {
            var surface = (RectTransform)_canvas.GetComponent<HallMenu>().Panel.Surface.transform;
            Assert.AreEqual(HallMenu.Size.x, surface.rect.width, 0.5f);
            Assert.AreEqual(HallMenu.Size.y, surface.rect.height, 0.5f);
            Assert.AreEqual(116f, Find("Navigation Bar").rect.height, 0.5f);
            Assert.IsNull(Find("Navigation Bar/Back"), "the Hall is the root: no back button");
            Assert.IsNotNull(Find("Status"));
        }

        [Test]
        public void CardsFollowTheHallGridInTwoColumns()
        {
            var grid = new HallGrid(HallMenu.Size.x - 96f, 3, 4f);
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
            // 4 u between the columns, 8 u between the rows
            Assert.AreEqual(a.rect.width + grid.Gap, b.localPosition.x - a.localPosition.x, 0.5f);
            var row1 = (RectTransform)content.Find("Row 1");
            var row2 = (RectTransform)content.Find("Row 2");
            Assert.AreEqual(grid.CardHeight + 8f, row1.localPosition.y - row2.localPosition.y, 0.6f);
        }

        [Test]
        public void ChoosingACardIsAButtonAndTheCoverShowsTheLabel()
        {
            var card = Find("Body/Chambers Margin/Chambers/Content/Row 1/Card Chamber1");
            Assert.IsNotNull(card.GetComponent<UiButton>());
            Assert.AreEqual("C01", card.Find("Cover/C-ID").GetComponent<TMPro.TextMeshProUGUI>().text);
            Assert.AreEqual("Chamber 1", card.Find("Title Box/Label").GetComponent<TMPro.TextMeshProUGUI>().text);
        }
    }
}
