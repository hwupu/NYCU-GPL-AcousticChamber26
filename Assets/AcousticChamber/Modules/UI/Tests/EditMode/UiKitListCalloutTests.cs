using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI.Tests
{
    /// <summary>The layout-based list and the world-space callout.</summary>
    public class UiKitListCalloutTests
    {
        GameObject _root;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = false;
            _root = new GameObject("Root", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)_root.transform).sizeDelta = new Vector2(1000f, 1000f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            UiKit.MotionSuppressed = false;
        }

        [Test]
        public void ListFillsTheFreeHeightAndScrollsWhenTheContentIsTaller()
        {
            UiKit.MotionSuppressed = true;
            var panel = UiPanel.Create(_root.transform, "Panel", new Vector2(1000f, 600f));
            UiNavigationBar.Create(panel.Surface, "List", UiNavigationBar.Leading.Back);
            var content = UiKit.List(panel.Surface.Body, "List", out var list);
            for (int i = 0; i < 6; i++)
            {
                var s = UiKit.Section(content, "Section " + i, "S" + i);
                UiKit.Row(s, "Row", "Row " + i, "line one\nline two");
            }
            panel.Surface.Finish();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel.Surface.transform);
            content.GetComponent<UiListSync>().Sync();

            // 600 surface - 116 nav - 12 body top - 40 bottom
            Assert.AreEqual(600f - 116f - 12f - 40f, list.Viewport.rect.height, 0.5f);
            Assert.AreEqual(1000f - 96f + UiKit.IndicatorMargin, list.Viewport.rect.width, 0.5f, "24 u into the right padding: the indicator's strip");
            Assert.AreEqual(1000f - 96f, ((RectTransform)content.GetChild(0)).rect.width, 0.5f, "the sections keep the body width");
            Assert.Greater(list.Model.Max, 0f, "the content is taller than the viewport: it scrolls");
            list.ScrollTo(40f);
            Assert.AreEqual(40f, list.Content.anchoredPosition.y, 0.5f);
        }

        [Test]
        public void CalloutPillIs72HighTheTailPointsAtTheTargetAndItFadesWithADelay()
        {
            UiKit.MotionSuppressed = false;
            var target = new GameObject("Target").transform;
            var c = UiCallout.Create(target, "Drag me", "hand", UiCallout.PlacementMode.Top);
            Assert.AreEqual(72f, c.Pill.rect.height, 0.5f);
            Assert.Greater(c.Pill.rect.width, 150f);
            // Top: the pill is above the tip (origin), by the tail length plus half its height
            Assert.AreEqual(UiCallout.TailLength + 36f, c.Pill.anchoredPosition.y, 0.5f);
            Assert.AreEqual(0f, c.Pill.anchoredPosition.x, 0.5f);

            Assert.AreEqual(0f, c.AlphaAt(Time.unscaledTime), 1e-4f);
            c.Show(2f);
            float t = Time.unscaledTime;
            Assert.AreEqual(0f, c.AlphaAt(t + 1.5f), 1e-4f, "nothing happens during the delay");
            Assert.Greater(c.AlphaAt(t + 2f + UiKit.Theme.sheet * 0.5f), 0.05f);
            Assert.AreEqual(1f, c.AlphaAt(t + 2f + UiKit.Theme.sheet + 0.1f), 1e-3f);

            c.Dismiss();
            Assert.IsFalse(c.IsShown);
            Assert.AreEqual(0f, c.AlphaAt(Time.unscaledTime + UiKit.Theme.sheet + 0.1f), 1e-3f, "the fade-out starts at once, without the delay");
            Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void CalloutPlacementMovesThePillToTheSide()
        {
            UiKit.MotionSuppressed = true;
            var target = new GameObject("Target").transform;
            var c = UiCallout.Create(target, "Left", "hand", UiCallout.PlacementMode.Left);
            Assert.Less(c.Pill.anchoredPosition.x, -UiCallout.TailLength);
            Assert.AreEqual(0f, c.Pill.anchoredPosition.y, 0.5f);
            Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }
    }
}
