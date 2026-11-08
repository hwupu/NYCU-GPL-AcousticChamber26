using System.Collections;
using System.IO;
using GPL.DebugBench;
using GPL.Logging;
using GPL.Params;
using GPL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AcousticChamber.App.Tests
{
    /// <summary>The chamber menu built from the layout kit: sizes measured from the laid-out rects.</summary>
    public class ChamberMenuTests
    {
        sealed class TestChamber : ChamberBehaviour
        {
            public override ParameterSchema Schema { get; } = new ParameterSchema(ParamDef.Int("trials", 6, 1, 50, "Trials"));
            public override LogSchema LogSchema { get; } = new LogSchema("rt_s");
            public override IEnumerator Run(ChamberRun run) { yield break; }
        }

        GameObject _canvas;
        ChamberDefinition _def;
        ChamberMenu _menu;
        string _data;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = true;
            _def = ScriptableObject.CreateInstance<ChamberDefinition>();
            _def.id = "_LayoutTest";
            _def.label = "C99";
            _def.title = "Layout Test";
            _data = Path.Combine(Application.persistentDataPath, _def.id);
            _canvas = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _menu = _canvas.AddComponent<ChamberMenu>();
            _menu.chamber = _def;
            _menu.logic = _canvas.AddComponent<TestChamber>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no GlobalActions"));
            _menu.Build();
            Layout();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_def);
            if (Directory.Exists(_data)) Directory.Delete(_data, true);
            UiKit.MotionSuppressed = false;
        }

        UiPage Root => _canvas.GetComponentInChildren<UiSurface>().Root;

        void Layout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_canvas.GetComponentInChildren<UiSurface>().Top.transform);
        }

        [Test]
        public void MenuHasTheNavigationBarThePadAndTheButtonColumn()
        {
            var surface = (RectTransform)_canvas.GetComponentInChildren<UiSurface>().transform;
            Assert.AreEqual(ChamberMenu.Size.x, surface.rect.width, 0.5f);
            Assert.AreEqual(ChamberMenu.Size.y, surface.rect.height, 0.5f);
            var page = (RectTransform)Root.transform;
            Assert.AreEqual(116f, ((RectTransform)page.Find("Navigation Bar")).rect.height, 0.5f);
            Assert.IsNotNull(page.Find("Navigation Bar/Back"));

            var pad = (RectTransform)page.Find("Body/Content/Number Pad");
            float shared = 904f - 20f; // the 20 u gap is not shared
            Assert.AreEqual(shared * 2f / 5f, pad.rect.width, 0.5f, "the number pad takes 2/5");
            var column = (RectTransform)page.Find("Body/Content/Column");
            Assert.AreEqual(shared * 3f / 5f, column.rect.width, 0.5f, "the button column takes 3/5");
            foreach (var n in new[] { "Start", "Debug" })
            {
                var b = (RectTransform)column.Find(n);
                Assert.AreEqual(80f, b.rect.height, 0.5f, n);
                Assert.AreEqual(column.rect.width, b.rect.width, 0.5f, n + " fills the column");
            }
            // the pad does not run into the status line
            var status = (RectTransform)page.Find("Status");
            Assert.GreaterOrEqual(pad.rect.height, 440f - 1f);
            // the keys stay 80 high and equal in the narrower pad
            var key1 = (RectTransform)pad.Find("Keys/Row 1/Key 1");
            Assert.AreEqual(80f, key1.rect.height, 0.5f);
            Assert.AreEqual(((RectTransform)pad.Find("Keys/Row 1/Key 2")).rect.width, key1.rect.width, 0.5f);
            var corners = new Vector3[4];
            pad.GetWorldCorners(corners);
            float padBottom = page.InverseTransformPoint(corners[0]).y;
            status.GetWorldCorners(corners);
            float statusTop = page.InverseTransformPoint(corners[1]).y;
            Assert.GreaterOrEqual(padBottom, statusTop - 0.5f, "the pad does not overlap the status line");
        }

        [Test]
        public void ADebugPageOpensWithAListAndAStatusLine()
        {
            var surface = _canvas.GetComponentInChildren<UiSurface>();
            var debug = surface.Root.transform.Find("Body/Content/Column/Debug").GetComponent<UiButton>();
            debug.onClick.Invoke();
            Layout();
            var page = surface.Top;
            Assert.AreEqual("Config", page.Title);
            Assert.IsNotNull(page.Status, "the Debug page has its status line");
            var list = page.GetComponentInChildren<ScrollList>();
            Assert.IsNotNull(list);
            Assert.Greater(list.Viewport.rect.height, 200f);
            Assert.AreEqual(904f + UiKit.IndicatorMargin, list.Viewport.rect.width, 0.5f, "the list reaches 24 u into the right padding");
            Assert.AreEqual(904f, ((RectTransform)list.Content.GetChild(0)).rect.width, 0.5f, "the content keeps the body width");
            page.BackButton.onClick.Invoke();
            Assert.AreEqual(1, surface.Count);
        }
    }
}
