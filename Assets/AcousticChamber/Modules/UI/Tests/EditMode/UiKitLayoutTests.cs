using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI.Tests
{
    /// <summary>The layout-based kit builds the sizes of the Storybook design (docs/design): measured from the laid-out rects.</summary>
    public class UiKitLayoutTests
    {
        GameObject _canvas;
        UiKitPoc _poc;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = true;
            _canvas = new GameObject("Kit Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)_canvas.transform).sizeDelta = UiKitPoc.Size;
            _poc = _canvas.AddComponent<UiKitPoc>();
            _poc.Build();
            Layout();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            UiKit.MotionSuppressed = false;
        }

        void Layout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_poc.Panel.Surface.transform);
        }

        RectTransform Find(string path) => (RectTransform)_poc.Panel.Surface.Root.transform.Find(path);

        [Test]
        public void SurfaceAndNavigationBarSizes()
        {
            var surface = (RectTransform)_poc.Panel.Surface.transform;
            Assert.AreEqual(1000f, surface.rect.width, 0.5f);
            Assert.AreEqual(820f, surface.rect.height, 0.5f);
            var nav = Find("Navigation Bar");
            Assert.AreEqual(116f, nav.rect.height, 0.5f);
            Assert.AreEqual(116f, nav.GetComponent<HorizontalLayoutGroup>().padding.top + 80f, 0.5f);
            var back = Find("Navigation Bar/Back");
            Assert.AreEqual(80f, back.rect.width, 0.5f);
            Assert.AreEqual(80f, back.rect.height, 0.5f);
        }

        [Test]
        public void BodyKeepsSidePaddingAndBottomPadding()
        {
            var body = Find("Body");
            Assert.AreEqual(1000f, body.rect.width, 0.5f);
            var g = body.GetComponent<VerticalLayoutGroup>();
            Assert.AreEqual(48, g.padding.left);
            Assert.AreEqual(48, g.padding.right);
            Assert.AreEqual(40, g.padding.bottom, "no status bar and no toolbar: 40 u bottom padding");
            Assert.AreEqual(12, g.padding.top, "below the navigation bar");
            var tools = Find("Body/Tools");
            var corners = new Vector3[4];
            body.GetWorldCorners(corners);
            var tc = new Vector3[4];
            tools.GetWorldCorners(tc);
            float scale = _canvas.transform.lossyScale.x;
            Assert.GreaterOrEqual((tc[0].y - corners[0].y) / scale, 39.5f, "content does not run into the bottom padding");
        }

        [Test]
        public void ButtonsAreEightyHighAndIconSideHasLessPadding()
        {
            var config = Find("Body/Tools/Config");
            Assert.AreEqual(80f, config.rect.height, 0.5f);
            Assert.GreaterOrEqual(config.rect.width, 220f);
            var pad = config.GetComponent<HorizontalLayoutGroup>().padding;
            Assert.AreEqual(30, pad.left, "icon side: 40 less 25 %");
            Assert.AreEqual(40, pad.right);
            Assert.NotNull(config.Find("Icon Leading"));
        }

        [Test]
        public void ChoiceGroupSegmentsAreEqualAndTrackIsSurfaceInsideAGroup()
        {
            var track = Find("Body/Hearing/Group/Ear");
            Assert.AreEqual(88f, track.rect.height, 0.5f);
            var segments = new[] { Find("Body/Hearing/Group/Ear/Both"), Find("Body/Hearing/Group/Ear/Left"), Find("Body/Hearing/Group/Ear/Right") };
            foreach (var s in segments) Assert.AreEqual(80f, s.rect.height, 0.5f);
            Assert.AreEqual(segments[0].rect.width, segments[1].rect.width, 0.5f);
            Assert.AreEqual(segments[1].rect.width, segments[2].rect.width, 0.5f);
            Assert.AreEqual(UiKit.Theme.surface, track.Find("Background").GetComponent<Image>().color);
            Assert.IsTrue(segments[0].GetComponent<UiButton>().Selected);
            Assert.IsFalse(segments[1].GetComponent<UiButton>().Selected);
        }

        [Test]
        public void ChoiceGroupGridHasEqualColumnsAndOneSelection()
        {
            var labels = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
            int chosen = -1;
            UiKit.ChoiceGroup(_poc.Panel.Surface.Body, "Grid", labels, 1, i => chosen = i, 4, out var buttons);
            Layout();
            var track = Find("Body/Grid");
            Assert.AreEqual(3, track.childCount - 1, "three rows besides the background");
            Assert.AreEqual(3f * 80f + 2f * 4f + 8f, track.rect.height, 0.5f, "rows 4 u apart, 4 u inset");
            float w = ((RectTransform)buttons[0].transform).rect.width;
            foreach (var b in buttons) Assert.AreEqual(w, ((RectTransform)b.transform).rect.width, 0.5f);
            Assert.AreEqual((track.rect.width - 8f - 3f * 4f) / 4f, w, 0.5f, "four equal columns");
            var lastRow = Find("Body/Grid/Row 3");
            Assert.AreEqual(4, lastRow.childCount, "two buttons and two empty cells");
            Assert.AreEqual(w, ((RectTransform)lastRow.GetChild(3)).rect.width, 0.5f);
            Assert.IsTrue(buttons[1].Selected);
            buttons[9].onClick.Invoke();
            Assert.AreEqual(9, chosen);
            Assert.IsTrue(buttons[9].Selected);
            Assert.IsFalse(buttons[1].Selected, "one selection across the rows");
        }

        [Test]
        public void RowsAreAtLeastEightyEightAndGrowWithLines()
        {
            var single = Find("Body/Path/Group/Approach");
            var two = Find("Body/Path/Group/Circle");
            var three = Find("Body/Path/Group/Fly-by");
            Assert.AreEqual(88f, single.rect.height, 0.5f);
            Assert.GreaterOrEqual(two.rect.height, 88f);
            Assert.Greater(three.rect.height, two.rect.height, "a longer subtitle makes a taller row");
            var pad = single.GetComponent<HorizontalLayoutGroup>().padding;
            Assert.AreEqual(21, pad.left, "icon side: 28 less 25 %");
            Assert.AreEqual(21, pad.right);
        }

        [Test]
        public void SingleLineLabelsAreNudgedUpTwoUnits()
        {
            var box = Find("Body/Path/Group/Approach/Text/Title Box");
            var label = (RectTransform)box.Find("Label");
            float dy = label.localPosition.y - box.localPosition.y;
            Assert.AreEqual(UiKit.Theme.labelNudge, dy, 0.1f);
            var buttonLabel = (RectTransform)Find("Body/Tools/Config/Label Box/Label");
            Assert.AreEqual(UiKit.Theme.labelNudge, buttonLabel.localPosition.y, 0.1f);
            Assert.AreEqual(0f, Find("Body/Tools/Config/Icon Leading").localPosition.y, 0.1f, "icons are not nudged");
        }

        [Test]
        public void PushingBackScalesDimsAndHidesTheGrabber()
        {
            var panel = _poc.Panel;
            Assert.IsTrue(panel.GrabberVisible);
            panel.Depth = 1;
            Assert.AreEqual(0.94f, panel.transform.localScale.x, 0.001f);
            Assert.IsFalse(panel.GrabberVisible);
            var dim = panel.Surface.transform.Find("Push Dim").GetComponent<Image>();
            Assert.AreEqual(UiKit.Theme.dim.a, dim.color.a, 0.001f);
            var surface = (RectTransform)panel.Surface.transform;
            Assert.AreEqual(surface.rect.size, ((RectTransform)dim.transform).rect.size, "the dim covers the surface, not the canvas");
            panel.Depth = 0;
            Assert.IsTrue(panel.GrabberVisible);
            panel.Shown = false;
            Assert.AreEqual(0f, panel.Alpha, 0.001f);
            Assert.IsFalse(panel.GrabberVisible);
        }
    }
}
