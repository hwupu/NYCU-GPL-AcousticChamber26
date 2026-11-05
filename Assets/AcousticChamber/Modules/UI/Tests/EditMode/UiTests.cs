using System.Linq;
using GPL.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GPL.UI.Tests
{
    public class LocTests
    {
        [SetUp] public void SetUp() => Loc.ResetForTests();
        [TearDown] public void TearDown() => Loc.ResetForTests();

        [Test]
        public void GetReturnsCurrentLanguageAndFormatsArgs()
        {
            Loc.Register(new StringTable("t").Add("part", "Part {0} of 3", "第 {0} 部分（共 3 部分）"));
            Assert.AreEqual("Part 2 of 3", Loc.Get("part", 2));
            Loc.Set(UiLanguage.Chinese);
            Assert.AreEqual("第 2 部分（共 3 部分）", Loc.Get("part", 2));
        }

        [Test]
        public void MissingKeyIsVisible() => Assert.AreEqual("[nope]", Loc.Get("nope"));

        [Test]
        public void LaterTableOverridesEarlier()
        {
            Loc.Register(new StringTable("module").Add("k", "module", "模組"));
            Loc.Register(new StringTable("chamber").Add("k", "chamber", "實驗室"));
            Assert.AreEqual("chamber", Loc.Get("k"));
        }

        [Test]
        public void ChangedRaisedOnlyOnRealChange()
        {
            int n = 0;
            Loc.Changed += () => n++;
            Loc.Set(UiLanguage.English);
            Loc.Set(UiLanguage.Chinese);
            Loc.Set(UiLanguage.Chinese);
            Assert.AreEqual(1, n);
        }

        [Test]
        public void LanguageLabelsAndCodes()
        {
            Assert.AreEqual("中文", Loc.LabelOf(UiLanguage.Chinese));
            Assert.AreEqual("English", Loc.LabelOf(UiLanguage.English));
            Assert.IsTrue(Loc.TryParseCode(Loc.CodeOf(UiLanguage.Chinese), out var l));
            Assert.AreEqual(UiLanguage.Chinese, l);
            Assert.IsFalse(Loc.TryParseCode("fr", out _));
        }

        [Test]
        public void MissingChineseFallsBackToEnglish()
        {
            Loc.Register(new StringTable("t").Add("only_en", "Only English", null));
            Loc.Set(UiLanguage.Chinese);
            Assert.AreEqual("Only English", Loc.Get("only_en"));
        }
    }

    public class DigitEntryTests
    {
        [Test]
        public void AppendStopsAtMaxAndIgnoresNonDigits()
        {
            var e = new DigitEntry(4);
            foreach (char c in "12a345") e.Append(c);
            Assert.AreEqual("1234", e.Value);
            Assert.IsTrue(e.IsFull);
        }

        [Test]
        public void BackspaceClearAndSet()
        {
            var e = new DigitEntry(4);
            int changes = 0;
            e.Changed += () => changes++;
            e.Set("S0042x9");
            Assert.AreEqual("0042", e.Value);
            Assert.IsTrue(e.Backspace());
            Assert.AreEqual("004", e.Value);
            e.Clear();
            Assert.IsTrue(e.IsEmpty);
            Assert.IsFalse(e.Backspace());
            e.Clear();
            Assert.AreEqual(3, changes);
        }
    }

    /// <summary>Builds GUI in edit mode (no headset): layers, number pad formatting, dialog modality.</summary>
    public class UiBuildTests
    {
        GameObject _canvas;

        [SetUp]
        public void SetUp()
        {
            Loc.ResetForTests();
            UiPointer.Modal = null;
            _canvas = new GameObject("Test Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            StopDialog.ResetForTests();
            UiPointer.Modal = null;
            Loc.ResetForTests();
        }

        [Test]
        public void ShowcaseBuildsAndEveryObjectIsOnTheCanvasLayer()
        {
            var showcase = _canvas.AddComponent<UiShowcase>();
            showcase.Build();
            var all = _canvas.GetComponentsInChildren<Transform>(true);
            Assert.Greater(all.Length, 30);
            var wrong = all.Where(t => t.gameObject.layer != OverlayPanel.HiddenCanvasLayer).Select(t => t.name).ToArray();
            CollectionAssert.IsEmpty(wrong, "objects not on the hidden canvas layer");
            Assert.GreaterOrEqual(_canvas.GetComponentsInChildren<Button>(true).Length, 17);
        }

        [Test]
        public void NumberPadShowsFormattedStudyId()
        {
            var pad = NumberPadEntry.Create(_canvas.transform, Vector2.zero, StudyId.MaxDigits, StudyId.FromDigits, "S____");
            var display = pad.transform.Find("Display/Text").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual("S____", display.text);
            Press(pad, "Key 1");
            Assert.AreEqual("1", pad.Value);
            Assert.AreEqual("S0001", display.text);
            foreach (var k in new[] { "Key 2", "Key 3", "Key 4", "Key 5" }) Press(pad, k);
            Assert.AreEqual("1234", pad.Value, "max 4 digits");
            Press(pad, "Key Backspace");
            Assert.AreEqual("S0123", display.text);
            Press(pad, "Key Clear");
            Assert.AreEqual("S____", display.text);
            Press(pad, "Key 0");
            Assert.AreEqual("S____", display.text, "0 alone is not a valid Study ID: placeholder stays");
        }

        [Test]
        public void DialogIsModalAndRestoresPreviousModal()
        {
            bool clicked = false;
            var d = ModalDialog.Show(_canvas.transform, "Title", "Message", Color.red,
                                     new DialogButton("A", UiStyle.Primary, () => clicked = true));
            Assert.IsNotNull(UiPointer.Modal);
            Assert.IsTrue(UiPointer.Modal.IsChildOf(d.transform));
            Assert.AreEqual(ModalDialog.Current, d);
            var b = d.GetComponentsInChildren<Button>().Single();
            b.onClick.Invoke();
            Assert.IsTrue(clicked);
            Assert.IsNull(UiPointer.Modal);
            Assert.IsNull(ModalDialog.Current);
        }

        [Test]
        public void ButtonsRespectMinimumSizeAndPaintHover()
        {
            var th = UiKit.Theme;
            var b = UiKit.Button(_canvas.transform, "B", "x", UiStyle.Danger, Vector2.zero, new Vector2(100f, 0f));
            var rt = (RectTransform)b.transform;
            Assert.AreEqual(th.buttonMinWidth, rt.sizeDelta.x);
            Assert.AreEqual(th.buttonHeight, rt.sizeDelta.y);
            Assert.AreEqual(th.danger, b.colors.normalColor);
            Assert.AreEqual(Color.Lerp(th.danger, Color.white, th.hoverMix), b.colors.highlightedColor);
            Assert.IsNotNull(b.GetComponent<HoverHighlight>().outline);
            Assert.IsFalse(b.GetComponent<HoverHighlight>().outline.activeSelf);
        }

        [Test]
        public void StopDialogRealSessionDropOutAsksTwiceAndFreezes()
        {
            StopDialog.ResetForTests();
            var choices = new System.Collections.Generic.List<StopChoice>();
            StopDialog.Show(_canvas.transform, false, "S0012", choices.Add);
            Assert.IsTrue(StopDialog.IsOpen);
            Assert.IsTrue(Freeze.Active);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            CollectionAssert.AreEqual(new[] { "Keep going", "Drop out" }, DialogLabels());

            Click("Drop out");
            Assert.AreEqual("Drop out S0012?", ModalDialog.Current.transform.Find("Box/Title").GetComponent<TextMeshProUGUI>().text);
            CollectionAssert.AreEqual(new[] { "Cancel", "Drop out" }, DialogLabels());
            Click("Cancel");
            CollectionAssert.AreEqual(new[] { "Keep going", "Drop out" }, DialogLabels(), "Cancel goes back to the first step");
            Assert.IsEmpty(choices);
            Assert.IsTrue(Freeze.Active, "still frozen between the steps");

            Click("Drop out");
            Click("Drop out");
            CollectionAssert.AreEqual(new[] { StopChoice.DropOut }, choices);
            Assert.IsFalse(StopDialog.IsOpen);
            Assert.IsNull(ModalDialog.Current);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void StopDialogDebugRunAndChinese()
        {
            StopDialog.ResetForTests();
            Loc.Set(UiLanguage.Chinese);
            StopChoice? got = null;
            StopDialog.Show(_canvas.transform, true, "", c => got = c);
            CollectionAssert.AreEqual(new[] { "繼續", "回到選單" }, DialogLabels());
            Click("回到選單");
            Assert.AreEqual(StopChoice.BackToMenu, got);
            Assert.IsFalse(Freeze.Active);
        }

        [Test]
        public void StopDialogAbandonedLiftsFreeze()
        {
            StopDialog.ResetForTests();
            StopDialog.Show(_canvas.transform, false, "S0001", null);
            Assert.IsTrue(Freeze.Active);
            LogAssert.Expect(LogType.Warning, "[GPL] Stop dialog destroyed without a choice (scene change?).");
            Object.DestroyImmediate(ModalDialog.Current.gameObject);
            Assert.IsFalse(StopDialog.IsOpen);
            Assert.IsFalse(Freeze.Active);
            Assert.AreEqual(1f, Time.timeScale);
        }

        static string[] DialogLabels() =>
            ModalDialog.Current.GetComponentsInChildren<Button>().Select(b => UiKit.LabelOf(b).text).ToArray();

        static void Click(string label) =>
            ModalDialog.Current.GetComponentsInChildren<Button>().Single(b => UiKit.LabelOf(b).text == label).onClick.Invoke();

        static void Press(NumberPadEntry pad, string key) => pad.transform.Find(key).GetComponent<Button>().onClick.Invoke();
    }
}
