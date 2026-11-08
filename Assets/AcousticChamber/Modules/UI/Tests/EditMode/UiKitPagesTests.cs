using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI.Tests
{
    /// <summary>Pages of a UiSurface: push, pop, header actions and the root back.</summary>
    public class UiKitPagesTests
    {
        GameObject _canvas;
        UiPanel _panel;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = true;
            _canvas = new GameObject("Kit Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _panel = UiPanel.Create(_canvas.transform, "Panel", new Vector2(1000f, 800f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            UiKit.MotionSuppressed = false;
        }

        [Test]
        public void PushedPageHasABackBarAndTheRootPageGoesInactive()
        {
            var surface = _panel.Surface;
            bool rootBack = false;
            surface.RootBack = () => rootBack = true;
            UiNavigationBar.Create(surface, "Menu", UiNavigationBar.Leading.Back);
            Assert.AreEqual(1, surface.Count);

            var page = surface.Push("Debug", debugTag: true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)page.transform);
            Assert.AreEqual(2, surface.Count);
            Assert.AreSame(page, surface.Top);
            Assert.IsFalse(surface.Root.gameObject.activeSelf, "the page below stays alive but inactive");
            Assert.AreEqual(116f, ((RectTransform)page.NavigationBar).rect.height, 0.5f);
            Assert.AreEqual(1000f, ((RectTransform)page.transform).rect.width, 0.5f);
            Assert.IsNotNull(page.NavigationBar.Find("DEBUG Tag Box"));

            bool returned = false;
            surface.Root.Returned += () => returned = true;
            page.BackButton.onClick.Invoke(); // pops
            Assert.AreEqual(1, surface.Count);
            Assert.IsTrue(returned);
            Assert.IsTrue(surface.Root.gameObject.activeSelf);
            Assert.IsFalse(rootBack);

            surface.Root.BackButton.onClick.Invoke(); // root: RootBack
            Assert.IsTrue(rootBack);
        }

        [Test]
        public void PopToRootRemovesThePagesInBetween()
        {
            var surface = _panel.Surface;
            UiNavigationBar.Create(surface, "Menu", UiNavigationBar.Leading.None);
            surface.Push("A");
            surface.Push("B");
            surface.Push("C");
            Assert.AreEqual(4, surface.Count);
            surface.PopToRoot();
            Assert.AreEqual(1, surface.Count);
            Assert.AreSame(surface.Root, surface.Top);
        }

        [Test]
        public void HeaderActionSitsInTheBarAsASmallButton()
        {
            var surface = _panel.Surface;
            var page = surface.Push("Parameters");
            var revert = page.AddHeaderAction("Revert", UiStyle.Borderless, () => { });
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)page.transform);
            Assert.AreEqual(60f, ((RectTransform)revert.transform).rect.height, 0.5f);
            Assert.IsTrue(revert.transform.IsChildOf(page.NavigationBar));
            // the right edge is 48 u inside the surface
            var corners = new Vector3[4];
            ((RectTransform)revert.transform).GetWorldCorners(corners);
            float right = ((RectTransform)surface.transform).rect.xMax - surface.transform.InverseTransformPoint(corners[2]).x;
            Assert.LessOrEqual(right, 48f + 0.5f);
        }
    }
}
