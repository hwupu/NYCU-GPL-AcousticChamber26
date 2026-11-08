using System.Collections;
using System.Linq;
using GPL.Protocol;
using GPL.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AcousticChamber.App.Tests
{
    /// <summary>The participant screens of a protocol, built from the layout kit and measured. Each coroutine runs to its first wait.</summary>
    public class ProtocolScreenTests
    {
        GameObject _canvas;
        ProtocolScreens _screens;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = true;
            Loc.ResetForTests();
            _canvas = new GameObject("Protocol Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(1000f, 700f);
            _screens = new ProtocolScreens(_canvas.transform, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            Loc.ResetForTests();
            UiKit.MotionSuppressed = false;
        }

        UiPage Page => _canvas.GetComponentInChildren<UiSurface>().Root;

        void Layout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Page.transform);
        }

        // positions in the surface's local space (0 = the panel's vertical center)
        float SurfaceY(RectTransform r) => Local(r).center.y;
        float SurfaceTop(RectTransform r) => Local(r).yMax;
        float SurfaceBottom(RectTransform r) => Local(r).yMin;

        UnityEngine.Rect Local(RectTransform r)
        {
            var surface = (RectTransform)Page.Surface.transform;
            var corners = new Vector3[4];
            r.GetWorldCorners(corners);
            var a = surface.InverseTransformPoint(corners[0]);
            var b = surface.InverseTransformPoint(corners[2]);
            return new UnityEngine.Rect(a.x, a.y, b.x - a.x, b.y - a.y);
        }

        static Button ButtonNamed(Component root, string name) => root.GetComponentsInChildren<Button>(true).Single(b => b.name == name);

        [Test]
        public void LanguageScreenCentersTheTitleAndTwoButtonsAndTheClickChoosesTheLanguage()
        {
            UiLanguage? picked = null;
            var run = _screens.Language(l => picked = l);
            Assert.IsTrue(run.MoveNext());
            Layout();
            var en = (RectTransform)ButtonNamed(Page, "English").transform;
            var zh = (RectTransform)ButtonNamed(Page, "Chinese").transform;
            Assert.AreEqual(80f, en.rect.height, 0.5f);
            Assert.AreEqual(80f, zh.rect.height, 0.5f);
            Assert.AreEqual(Page.Body.InverseTransformPoint(en.position).x, -Page.Body.InverseTransformPoint(zh.position).x, 0.5f,
                            "the two buttons are centered as a pair");
            Assert.IsNull(Page.NavigationBar, "a message screen has no navigation bar");
            Assert.AreEqual(Page.Toolbar.InverseTransformPoint(en.position).x, -Page.Toolbar.InverseTransformPoint(zh.position).x, 0.5f,
                            "the buttons are in the bottom toolbar, centered");
            var title = Page.Body.Find("Title").GetComponent<RectTransform>();
            Assert.AreEqual(0f, SurfaceY(title), 1f, "the text is vertically centered in the panel");
            Assert.Greater(Page.Toolbar.rect.yMax, 0f);
            Assert.Less(SurfaceTop(en), SurfaceBottom(title) + 0.01f, "buttons are below the text");

            ButtonNamed(Page, "Chinese").onClick.Invoke();
            while (run.MoveNext()) { }
            Assert.AreEqual(UiLanguage.Chinese, picked);
            Assert.AreEqual(UiLanguage.Chinese, Loc.Current);
            Assert.AreEqual(0, _canvas.GetComponentsInChildren<UiSurface>().Length, "the screen is removed after the choice");
        }

        [Test]
        public void MessageWithAButtonWaitsForTheClick()
        {
            var run = _screens.Message("Block 1 of 3 done.", UiStrings.Continue);
            Assert.IsTrue(run.MoveNext());
            Assert.IsTrue(run.MoveNext()); // one frame after the screen opened
            Layout();
            var text = Page.Body.GetComponentInChildren<TextMeshProUGUI>();
            Assert.AreEqual("Block 1 of 3 done.", text.text);
            Assert.AreEqual(UiKit.Theme.headingSize, text.fontSize);
            Assert.AreEqual(0f, SurfaceY((RectTransform)text.transform), 1f, "the text is vertically centered in the panel");
            Assert.AreEqual(0f, Local(ButtonNamed(Page, "Button").GetComponent<RectTransform>()).center.x, 0.5f, "the button is centered");
            ButtonNamed(Page, "Button").onClick.Invoke();
            while (run.MoveNext()) { }
        }

        [Test]
        public void HandoverShowsTheTitleBarTheStepsAndAContinueToolbar()
        {
            var run = _screens.Handover();
            Assert.IsTrue(run.MoveNext());
            Layout();
            Assert.AreEqual(116f, Page.NavigationBar.rect.height, 0.5f);
            Assert.IsNull(Page.BackButton, "no back button on the handover");
            Assert.IsNotNull(Page.Toolbar);
            var button = (RectTransform)ButtonNamed(Page.Toolbar, "Button").transform;
            Assert.AreEqual(80f, button.rect.height, 0.5f);
            var toolbar = (RectTransform)Page.Toolbar;
            Assert.AreEqual(Page.Toolbar.InverseTransformPoint(button.position).x, 0f, 0.5f, "Continue is centered");
            ButtonNamed(Page, "Button").onClick.Invoke();
            while (run.MoveNext()) { }
            Assert.IsNotNull(toolbar);
        }

        [Test]
        public void SlidesFillTheWidthBetweenBarAndToolbarAndNameTheirNeighbours()
        {
            var pages = new[]
            {
                new SlidePage { Id = "a", Title = l => "Procedure", ShortTitle = l => "Procedure", Caption = l => "First" },
                new SlidePage { Id = "b", Title = l => "Safety", ShortTitle = l => "Safety", Caption = l => "Second" },
            };
            var run = _screens.Slides(pages, 0, false, null);
            Assert.IsTrue(run.MoveNext());
            Layout();
            Assert.AreEqual("Procedure", Page.NavigationBar.Find("Title Box/Label").GetComponent<TextMeshProUGUI>().text);
            var titleLabel = (RectTransform)Page.NavigationBar.Find("Title Box");
            Assert.AreEqual(0f, Local(titleLabel).center.x, 0.5f, "the slide title is centered across the bar");
            var image = (RectTransform)Page.Body.Find("Image");
            Assert.AreEqual(1000f, image.rect.width, 0.5f, "the illustration spans the full width");
            Assert.Greater(image.rect.height, 400f);
            var back = ButtonNamed(Page, "Back");
            var next = ButtonNamed(Page, "Next");
            Assert.IsFalse(back.gameObject.activeSelf, "no Back on the first slide");
            Assert.AreEqual("Safety →", next.GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.AreEqual(ProtocolScreens.SlideButtonWidth, ((RectTransform)next.transform).rect.width, 0.5f);
            Assert.AreEqual("First", image.GetComponentInChildren<TextMeshProUGUI>().text, "no image: the caption fills the gray area");

            next.onClick.Invoke();
            Assert.IsTrue(run.MoveNext());
            Assert.IsTrue(run.MoveNext());
            Layout();
            Assert.AreEqual("Safety", Page.NavigationBar.Find("Title Box/Label").GetComponent<TextMeshProUGUI>().text);
            Assert.IsTrue(ButtonNamed(Page, "Back").gameObject.activeSelf);
            Assert.AreEqual("← Procedure", ButtonNamed(Page, "Back").GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.AreEqual("Continue", ButtonNamed(Page, "Next").GetComponentInChildren<TextMeshProUGUI>().text, "the last slide ends with Continue");
        }
    }
}
