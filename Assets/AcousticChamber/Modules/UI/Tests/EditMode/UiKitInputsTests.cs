using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI.Tests
{
    /// <summary>Switch row, text field, stepper and dialog of the layout-based kit: measured sizes and behavior.</summary>
    public class UiKitInputsTests
    {
        GameObject _canvas;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = true;
            _canvas = new GameObject("Kit Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(1000f, 1000f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            UiKit.MotionSuppressed = false;
        }

        static void Layout(Transform t)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)t);
        }

        (UiPanel panel, RectTransform group) Surface()
        {
            var panel = UiPanel.Create(_canvas.transform, "Panel", new Vector2(1000f, 1000f));
            var group = UiLay.Section(panel.Surface.Body, "Group");
            return (panel, group);
        }

        [Test]
        public void SwitchRowIsAtLeastEightyEightAndTheWholeRowToggles()
        {
            var (panel, group) = Surface();
            bool state = false;
            var sw = UiLay.SwitchRow(group, "Row", "Subtitles", "Show text", false, on => state = on);
            Layout(panel.Surface.transform);
            var sr = (RectTransform)sw.transform;
            Assert.AreEqual(100f, sr.rect.width, 0.5f);
            Assert.AreEqual(60f, sr.rect.height, 0.5f);
            Assert.GreaterOrEqual(((RectTransform)sw.transform.parent).rect.height, 87.5f);
            sw.transform.parent.GetComponent<UiButton>().onClick.Invoke();
            Assert.IsTrue(sw.On);
            Assert.IsTrue(state);
        }

        [Test]
        public void TextFieldIsEightyHighShowsPlaceholderAndRing()
        {
            var (panel, _) = Surface();
            var f = UiLay.TextField(panel.Surface.Body, "Field", null, "Study ID");
            Layout(panel.Surface.transform);
            Assert.AreEqual(80f, ((RectTransform)f.transform).rect.height, 0.5f);
            var label = f.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Assert.AreEqual("Study ID", label.text);
            Assert.AreEqual(UiKit.Theme.text3, label.color);
            f.Value = "S0042";
            Assert.AreEqual("S0042", label.text);
            Assert.AreEqual(UiKit.Theme.text, label.color);
            Assert.IsFalse(f.transform.Find("Ring").gameObject.activeSelf);
            f.Focused = true;
            Assert.IsTrue(f.transform.Find("Ring").gameObject.activeSelf);
        }

        [Test]
        public void StepperClampsAndDisablesAtTheLimits()
        {
            var (panel, _) = Surface();
            float seen = -1f;
            var s = UiLay.Stepper(panel.Surface.Body, "Stepper", 1f, 0f, 2f, 0.5f, v => v.ToString("0.0") + " m", v => seen = v);
            Layout(panel.Surface.transform);
            var plus = s.transform.Find("Plus").GetComponent<UiButton>();
            var minus = s.transform.Find("Minus").GetComponent<UiButton>();
            Assert.AreEqual(60f, ((RectTransform)plus.transform).rect.height, 0.5f);
            Assert.GreaterOrEqual(((RectTransform)s.transform.Find("Value Box")).rect.width, 119.5f);
            plus.onClick.Invoke();
            plus.onClick.Invoke();
            plus.onClick.Invoke();
            Assert.AreEqual(2f, s.Value, 1e-4f);
            Assert.AreEqual(2f, seen, 1e-4f);
            Assert.IsFalse(plus.interactable);
            Assert.IsTrue(minus.interactable);
            Assert.AreEqual("2.0 m", s.GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
        }

        [Test]
        public void NumberPadIsFiveTwentyWideWithEqualEightyHighKeysAndEntersDigits()
        {
            var (panel, _) = Surface();
            var pad = NumberPadEntry.CreateLayout(panel.Surface.Body, "Pad", 4, s => s.Length > 0 ? "S" + s.PadLeft(4, '0') : "", "S____");
            Layout(panel.Surface.transform);
            var root = (RectTransform)pad.transform;
            Assert.AreEqual(520f, root.rect.width, 0.5f);
            var display = root.Find("Display");
            Assert.AreEqual(80f, ((RectTransform)display).rect.height, 0.5f);
            var keys = (RectTransform)root.Find("Keys");
            Assert.AreEqual(4 * 80f + 3 * 4f + 2 * 4f, keys.rect.height, 0.5f);
            float w = ((RectTransform)keys.Find("Row 1/Key 1")).rect.width;
            Assert.AreEqual((520f - 2f * 4f - 2f * 4f) / 3f, w, 0.5f);
            Assert.AreEqual(w, ((RectTransform)keys.Find("Row 4/Key Backspace")).rect.width, 0.5f);
            Assert.AreEqual(80f, ((RectTransform)keys.Find("Row 4/Key Backspace")).rect.height, 0.5f);
            Assert.AreEqual("S____", display.GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
            keys.Find("Row 1/Key 1").GetComponent<UiButton>().onClick.Invoke();
            keys.Find("Row 2/Key 4").GetComponent<UiButton>().onClick.Invoke();
            Assert.AreEqual("14", pad.Value);
            Assert.AreEqual("S0014", display.GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
            keys.Find("Row 4/Key Backspace").GetComponent<UiButton>().onClick.Invoke();
            Assert.AreEqual("1", pad.Value);
        }

        [Test]
        public void DialogIs820WideSizedByContentWithActionsAtTheRight()
        {
            var d = UiDialog.Create(_canvas.transform, "Dialog", "Stop the session?", "Your progress is saved.");
            var cancel = UiLay.Button(d.Actions, "Cancel", "Cancel", UiStyle.Borderless, minWidth: false);
            var stop = UiLay.Button(d.Actions, "Stop", "Stop", UiStyle.Prominent, minWidth: false);
            d.Fit();
            var surface = (RectTransform)d.Panel.Surface.transform;
            Assert.AreEqual(820f, surface.rect.width, 0.5f);
            Assert.AreEqual(d.Size.y, surface.rect.height, 0.5f);
            Assert.Greater(d.Size.y, 250f);
            // the main action is the rightmost; the actions stay inside the surface plus the 20 u optical overhang
            var cr = (RectTransform)cancel.transform;
            var sr = (RectTransform)stop.transform;
            Assert.Greater(sr.position.x, cr.position.x);
            var corners = new Vector3[4];
            sr.GetWorldCorners(corners);
            float right = surface.rect.xMax - surface.InverseTransformPoint(corners[2]).x;
            Assert.AreEqual(48f - 20f, right, 1f);
        }
    }
}
