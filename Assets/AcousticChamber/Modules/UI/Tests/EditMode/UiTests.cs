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

    public class MotionTests
    {
        [Test]
        public void SettleStartsFastLandsSoftlyAndNeverOvershoots()
        {
            Assert.AreEqual(0f, Motion.Settle.Evaluate(0f));
            Assert.AreEqual(1f, Motion.Settle.Evaluate(1f));
            Assert.Greater(Motion.Settle.Evaluate(0.25f), 0.6f, "fast start");
            float prev = 0f;
            for (int i = 1; i <= 100; i++)
            {
                float v = Motion.Settle.Evaluate(i / 100f);
                Assert.GreaterOrEqual(v, prev - 1e-5f, "monotonic");
                Assert.LessOrEqual(v, 1f + 1e-5f, "no overshoot");
                prev = v;
            }
            Assert.AreEqual(1f, Motion.Progress(0.2f, 0f), "zero duration = instant");
            Assert.AreEqual(0.5f, new CubicBezier(0, 0, 1, 1).Evaluate(0.5f), 1e-4f, "linear curve");
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
            var d = ModalDialog.Show(_canvas.transform, "Title", "Message",
                                     new DialogButton("A", UiStyle.Prominent, () => clicked = true));
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
        public void ButtonIsCapsuleWithMinimumSizeAndStyleColors()
        {
            var th = UiKit.Theme;
            var b = UiKit.Button(_canvas.transform, "B", "x", UiStyle.Destructive, Vector2.zero, new Vector2(100f, 0f));
            var rt = (RectTransform)b.transform;
            Assert.AreEqual(th.buttonMinWidth, rt.sizeDelta.x);
            Assert.AreEqual(th.buttonHeight, rt.sizeDelta.y);
            Assert.AreEqual(th.destructive.normal, b.CurrentFill);
            var img = b.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Sliced, img.type);
            Assert.AreEqual(64f / (th.buttonHeight / 2f), img.pixelsPerUnitMultiplier, 1e-4f, "corner radius = height / 2");
            Assert.AreEqual(Selectable.Transition.None, b.transition, "UiButton animates itself");
            Assert.AreEqual(TextWrappingModes.NoWrap, UiKit.LabelOf(b).textWrappingMode);
        }

        [Test]
        public void StatesGetBrighterTowardPressedInEveryStyle()
        {
            var th = UiKit.Theme;
            foreach (UiStyle s in System.Enum.GetValues(typeof(UiStyle)))
            {
                var c = th.Colors(s);
                float n = c.normal.a <= 0f ? 0f : Luma(c.normal);   // transparent normal = darkest (the surface shows)
                Assert.Less(n, Luma(c.hover), s + ": hover brighter than normal");
                Assert.Less(Luma(c.hover), Luma(c.pressed), s + ": pressed brighter than hover");
            }
            Color.RGBToHSV(th.destructive.normal, out float h0, out float s0, out _);
            Color.RGBToHSV(th.destructive.pressed, out float h1, out float s1, out _);
            Assert.AreEqual(h0, h1, 0.01f, "red keeps its hue");
            Assert.AreEqual(s0, s1, 0.02f, "red keeps its saturation (only brightness changes)");
        }

        [Test]
        public void HoverPressReleaseAndDisabled()
        {
            var th = UiKit.Theme;
            var b = UiKit.Button(_canvas.transform, "B", "Start", UiStyle.Prominent, Vector2.zero, Vector2.zero);
            b.OnPointerEnter(null);
            b.FinishTransition();
            Assert.AreEqual(th.prominent.hover, b.CurrentFill);
            b.Press();
            Assert.AreEqual(th.prominent.pressed, b.CurrentFill, "pressed is instant");
            Assert.AreEqual(th.pressedScale, b.transform.localScale.x, 1e-5f);
            b.Release();
            b.FinishTransition();
            Assert.AreEqual(th.prominent.normal, b.CurrentFill, "released, ray not on it (UiPointer.Hovered is null)");
            Assert.AreEqual(1f, b.transform.localScale.x, 1e-5f);
            b.interactable = false;
            Assert.AreEqual(th.prominent.normal.a * th.disabledAlpha, b.CurrentFill.a, 1e-5f);
        }

        [Test]
        public void ReduceMotionMakesTransitionsInstantAndThemeEditsRestyle()
        {
            var th = UiTheme.CreateDefault();
            th.reduceMotion = true;
            UiKit.Theme = th;
            try
            {
                var b = UiKit.Button(_canvas.transform, "B", "Back", UiStyle.Borderless, Vector2.zero, Vector2.zero);
                b.OnPointerEnter(null);
                Assert.AreEqual(th.borderless.hover, b.CurrentFill, "no ramp with reduce motion");
                b.OnPointerExit(null);
                th.borderless.normal = new Color(0.1f, 0.2f, 0.3f, 1f);
                th.NotifyEdited();
                Assert.AreEqual(th.borderless.normal, b.CurrentFill, "theme edit restyles live");
            }
            finally { UiKit.Theme = null; }
        }

        [Test]
        public void HitRegionIsAtLeast60UnitsAndTheRealRectWins()
        {
            var small = UiKit.IconButton(_canvas.transform, "Small", UiKit.CloseGlyph, Vector2.zero);
            ((RectTransform)small.transform).sizeDelta = new Vector2(40f, 40f);
            var hit = UiKit.HitRect((RectTransform)small.transform);
            Assert.AreEqual(60f, hit.width);
            Assert.AreEqual(60f, hit.height);
            var big = UiKit.Button(_canvas.transform, "Big", "Start", UiStyle.Gray, new Vector2(80f, 0f), new Vector2(100f, 0f));
            ((RectTransform)big.transform).sizeDelta = new Vector2(100f, 80f);    // spans x 30..130
            Assert.AreEqual(100f, UiKit.HitRect((RectTransform)big.transform).width, "large rects are not grown");

            var targets = new Selectable[] { small, big };
            Selectable Aim(float x) => UiPointer.Pick(new Ray(new Vector3(x, 0f, -5f), Vector3.forward), targets, 10f, out _);
            Assert.AreEqual(small, Aim(25f), "25 u from the center of a 40 u button: inside its 60 u hit region");
            Assert.AreEqual(big, Aim(31f), "inside the big button's own rect beats the small one's grown region");
            Assert.IsNull(Aim(-35f));
        }

        [Test]
        public void OldThemeAssetsMigrateKeepingFonts()
        {
            var old = ScriptableObject.CreateInstance<UiTheme>();
            old.version = 1;
            old.surface = Color.red;
            var font = ScriptableObject.CreateInstance<TMP_FontAsset>();
            old.regular = font;
            old.ResetTokens();
            Assert.AreEqual(UiTheme.CurrentVersion, old.version);
            Assert.AreEqual(UiTheme.Rgb(0x1C1C1F), old.surface);
            Assert.AreSame(font, old.regular);
            Object.DestroyImmediate(old);
            Object.DestroyImmediate(font);
        }

        static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

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
            CollectionAssert.AreEqual(new[] { "Drop out", "Keep going" }, DialogLabels(), "main action on the right");

            Click("Drop out");
            Assert.AreEqual("Drop out S0012?", ModalDialog.Current.transform.Find("Box/Title").GetComponent<TextMeshProUGUI>().text);
            CollectionAssert.AreEqual(new[] { "Cancel", "Drop out" }, DialogLabels());
            Click("Cancel");
            CollectionAssert.AreEqual(new[] { "Drop out", "Keep going" }, DialogLabels(), "Cancel goes back to the first step");
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
            CollectionAssert.AreEqual(new[] { "回到選單", "繼續" }, DialogLabels());
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
            ModalDialog.Current.GetComponentsInChildren<Button>().OrderBy(b => ((RectTransform)b.transform).anchoredPosition.x)
                       .Select(b => UiKit.LabelOf(b).text).ToArray();   // left to right

        static void Click(string label) =>
            ModalDialog.Current.GetComponentsInChildren<Button>().Single(b => UiKit.LabelOf(b).text == label).onClick.Invoke();

        static void Press(NumberPadEntry pad, string key) => pad.transform.Find("Keys/" + key).GetComponent<Button>().onClick.Invoke();
    }
}
