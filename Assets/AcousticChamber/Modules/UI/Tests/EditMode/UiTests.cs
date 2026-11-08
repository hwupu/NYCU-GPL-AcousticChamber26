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

    public class ScrollModelTests
    {
        static ScrollModel Model()
        {
            var m = new ScrollModel();
            m.SetExtent(1100f, 500f); // 600 u to scroll
            return m;
        }

        [Test]
        public void ShortPressStaysAClickLongerMoveBecomesADrag()
        {
            var m = Model();
            m.PointerDown(0f);
            Assert.IsFalse(m.PointerMove(11f, 0.016f), "under 12 u: still a click");
            Assert.AreEqual(0f, m.Offset);
            Assert.IsFalse(m.PointerUp(), "released as a click");

            m.PointerDown(0f);
            Assert.IsTrue(m.PointerMove(12f, 0.016f), "12 u: a drag");
            Assert.AreEqual(0f, m.Offset, "no jump when the drag starts");
            m.PointerMove(112f, 0.016f);
            Assert.AreEqual(100f, m.Offset, 1e-4f, "pointer up 100 u: content up 100 u");
            Assert.IsTrue(m.PointerUp(), "released as a drag: no click");
        }

        [Test]
        public void ClampsAtBothEndsAndShowsWhereMoreContentWaits()
        {
            var m = Model();
            Assert.IsFalse(m.MoreAbove);
            Assert.IsTrue(m.MoreBelow);
            m.ScrollBy(-50f);
            Assert.AreEqual(0f, m.Offset);
            m.ScrollBy(10000f);
            Assert.AreEqual(600f, m.Offset);
            Assert.IsTrue(m.MoreAbove);
            Assert.IsFalse(m.MoreBelow);
            m.SetExtent(400f, 500f);
            Assert.AreEqual(0f, m.Offset, "content shorter than the viewport: nothing to scroll");
            Assert.IsFalse(m.CanScroll);
            m.PointerDown(0f);
            Assert.IsFalse(m.PointerMove(50f, 0.016f), "no drag when nothing can scroll: the press stays a click");
        }

        [Test]
        public void MomentumCarriesOnAndSlowsDown()
        {
            var m = Model();
            m.PointerDown(0f);
            m.PointerMove(12f, 0.016f);
            for (int i = 1; i <= 5; i++) m.PointerMove(12f + 16f * i, 0.016f); // 1000 u/s
            m.PointerUp();
            float v0 = m.Velocity;
            Assert.Greater(v0, 500f);
            float before = m.Offset;
            Assert.IsTrue(m.Step(0.016f));
            Assert.Greater(m.Offset, before, "keeps moving after release");
            Assert.Less(m.Velocity, v0, "and slows down");
            for (int i = 0; i < 300 && m.Velocity != 0f; i++) m.Step(0.016f);
            Assert.AreEqual(0f, m.Velocity, "comes to rest");
            Assert.LessOrEqual(m.Offset, 600f);
        }

        [Test]
        public void ThumbstickScrollsWithADeadZone()
        {
            var m = Model();
            Assert.IsFalse(m.Stick(-0.1f, 0.1f), "dead zone");
            Assert.IsTrue(m.Stick(-1f, 0.1f), "stick down: content above moves up");
            Assert.AreEqual(m.StickSpeed * 0.1f, m.Offset, 1e-3f);
            m.Stick(1f, 0.05f);
            Assert.AreEqual(m.StickSpeed * 0.05f, m.Offset, 1e-3f);
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

    /// <summary>Builds GUI in edit mode (no headset): buttons, the scroll list, dialog modality and the Stop dialog.</summary>
    public class UiBuildTests
    {
        GameObject _canvas;

        [SetUp]
        public void SetUp()
        {
            Loc.ResetForTests();
            UiPointer.Modal = null;
            UiKit.MotionSuppressed = true;
            _canvas = new GameObject("Test Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            StopDialog.ResetForTests();
            UiPointer.Modal = null;
            UiKit.MotionSuppressed = false;
            Loc.ResetForTests();
        }

        static TextMeshProUGUI LabelOf(Selectable b) => b.GetComponentInChildren<TextMeshProUGUI>();

        /// <summary>A kit button directly on the canvas (no layout group around it): sized by hand.</summary>
        UiButton Button(string label, UiStyle style, Vector2 pos, Vector2 size, string icon = null)
        {
            var b = UiKit.Button(_canvas.transform, label, label, style, icon, minWidth: false);
            var rt = (RectTransform)b.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return b;
        }

        [Test]
        public void ClearKeepsHiddenHelperObjects()
        {
            // OVROverlayCanvas creates its render camera as a hidden child of the canvas
            var helper = new GameObject("Overlay Camera") { hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy };
            helper.transform.SetParent(_canvas.transform, false);
            UiPanel.Create(_canvas.transform, "Panel", new Vector2(400f, 300f));
            UiKit.Clear(_canvas.transform);
            Assert.IsTrue(helper != null, "the hidden helper survives");
            Assert.AreEqual(1, _canvas.transform.childCount, "our own objects are gone");
            Object.DestroyImmediate(helper);
        }

        [Test]
        public void EveryObjectOfAPanelIsOnTheCanvasLayer()
        {
            var poc = _canvas.AddComponent<UiKitPoc>();
            poc.Build();
            var all = _canvas.GetComponentsInChildren<Transform>(true);
            Assert.Greater(all.Length, 30);
            var wrong = all.Where(t => t.gameObject.layer != OverlayPanel.HiddenCanvasLayer).Select(t => t.name).ToArray();
            CollectionAssert.IsEmpty(wrong, "objects not on the hidden canvas layer");
        }

        [Test]
        public void NumberPadShowsFormattedStudyId()
        {
            var panel = UiPanel.Create(_canvas.transform, "Panel", new Vector2(1000f, 800f));
            var pad = UiNumberPad.CreateLayout(panel.Surface.Body, "Pad", StudyId.MaxDigits, StudyId.FromDigits, "S____");
            var display = pad.transform.Find("Display/Value Box/Label").GetComponent<TextMeshProUGUI>();
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
        public void ScrollListMovesContentFadesEdgesAndClipsHits()
        {
            var panel = UiPanel.Create(_canvas.transform, "Panel", new Vector2(1000f, 700f));
            var content = UiKit.List(panel.Surface.Body, "List", out var list);
            var group = UiKit.Section(content, "Group");
            var rows = Enumerable.Range(0, 12).Select(i => UiKit.Row(group, "Row " + i, "Row " + i)).ToArray();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel.Surface.Root.transform);
            content.GetComponent<UiListSync>().Sync();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel.Surface.Root.transform);
            Assert.Greater(list.Model.Max, 0f);
            Image Part(string n) => list.transform.Find(n).GetComponent<Image>();
            Assert.AreEqual(0f, Part("Fade Top").color.a, 1e-3f, "at the top: no fade above");
            Assert.AreEqual(1f, Part("Fade Bottom").color.a, 1e-3f, "more content below");

            list.ScrollTo(list.Model.Max);
            Assert.AreEqual(list.Model.Max, list.Content.anchoredPosition.y, 1e-3f);
            Assert.AreEqual(1f, Part("Fade Top").color.a, 1e-3f);
            Assert.AreEqual(0f, Part("Fade Bottom").color.a, 1e-3f);

            // row 0 is now scrolled out above the viewport: the pointer must not hit it there
            var row0 = rows[0];
            Assert.IsTrue(ScrollList.ClipsOut(row0, row0.TransformPoint(Vector3.zero)));
            var last = rows[11];
            Assert.IsFalse(ScrollList.ClipsOut(last, last.TransformPoint(Vector3.zero)), "the last row is visible");
        }

        [Test]
        public void SelectedButtonUsesTheSelectColour()
        {
            var th = UiKit.Theme;
            var b = Button("Play", UiStyle.Borderless, Vector2.zero, new Vector2(200f, 80f));
            b.InGroup = true;
            b.FinishTransition();
            Assert.AreEqual(th.borderlessInGroup.normal, b.CurrentFill);
            b.Selected = true;
            b.FinishTransition();
            Assert.AreEqual(th.select, b.CurrentFill, "selected = segmented-control select colour");
            b.Selected = false;
            b.FinishTransition();
            Assert.AreEqual(0f, b.CurrentFill.a, 1e-6f, "back to no fill (the fade keeps the tint at alpha 0)");

            var p = Button("Run", UiStyle.Prominent, Vector2.zero, new Vector2(200f, 80f));
            p.Selected = true;
            p.FinishTransition();
            Assert.AreEqual(th.prominent.normal, p.CurrentFill, "prominent ignores Selected");
        }

        [Test]
        public void ButtonIsCapsuleWithStyleColors()
        {
            var th = UiKit.Theme;
            var b = Button("x", UiStyle.Destructive, Vector2.zero, new Vector2(300f, 80f));
            Assert.AreEqual(th.destructive.normal, b.CurrentFill);
            var img = (Image)b.targetGraphic;
            Assert.AreEqual(Image.Type.Sliced, img.type);
            Assert.AreEqual(64f / (th.buttonHeight / 2f), img.pixelsPerUnitMultiplier, 1e-4f, "corner radius = height / 2");
            Assert.AreEqual(Selectable.Transition.None, b.transition, "UiButton animates itself");
            Assert.AreEqual(TextWrappingModes.NoWrap, LabelOf(b).textWrappingMode);
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
            var b = Button("Start", UiStyle.Prominent, Vector2.zero, new Vector2(200f, 80f));
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

            var revert = Button("Revert", UiStyle.Borderless, Vector2.zero, new Vector2(200f, 80f));
            revert.interactable = false;
            Assert.AreEqual(th.text3, LabelOf(revert).color, "disabled borderless: dim text, not faded white");
            revert.interactable = true;
            revert.FinishTransition();
            Assert.AreEqual(th.borderless.label, LabelOf(revert).color);

            // the click disables the button (Revert after reverting): the release must not bring back the normal look
            revert.onClick.AddListener(() => revert.interactable = false);
            revert.Press();
            revert.onClick.Invoke();
            revert.Release();
            revert.FinishTransition();
            Assert.AreEqual(th.text3, LabelOf(revert).color, "stays disabled after the release");
            Assert.AreEqual(1f, revert.transform.localScale.x, 1e-5f, "pressed scale is undone");
        }

        [Test]
        public void ReduceMotionMakesTransitionsInstantAndThemeEditsRestyle()
        {
            var th = UiTheme.CreateDefault();
            th.reduceMotion = true;
            UiKit.Theme = th;
            try
            {
                var b = Button("Back", UiStyle.Borderless, Vector2.zero, new Vector2(200f, 80f));
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
            var small = Button("Small", UiStyle.Borderless, Vector2.zero, new Vector2(40f, 40f), "cross");
            var hit = UiKit.HitRect((RectTransform)small.transform);
            Assert.AreEqual(60f, hit.width);
            Assert.AreEqual(60f, hit.height);
            var big = Button("Big", UiStyle.Gray, new Vector2(80f, 0f), new Vector2(100f, 80f));   // spans x 30..130
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
        public void StopDialogReportsOpenAndCloseBeforeTheChoice()
        {
            StopDialog.ResetForTests();
            var events = new System.Collections.Generic.List<string>();
            void OnOpen(bool open) => events.Add(open ? "open" : "closed");
            StopDialog.OpenChanged += OnOpen;
            try
            {
                StopDialog.Show(_canvas.transform, false, "S0012", c => events.Add("choice " + c));
                CollectionAssert.AreEqual(new[] { "open" }, events);
                Click("Keep going");
                CollectionAssert.AreEqual(new[] { "open", "closed", "choice KeepGoing" }, events);
            }
            finally { StopDialog.OpenChanged -= OnOpen; }
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
            CollectionAssert.AreEqual(new[] { "Drop out", "Keep going" }, DialogLabels(), "main action on the right");

            Click("Drop out");
            Assert.AreEqual("Drop out S0012?", ModalDialog.Current.transform.Find("Surface/Page/Body/Content/Title").GetComponent<TextMeshProUGUI>().text);
            CollectionAssert.AreEqual(new[] { "Cancel", "Drop out" }, DialogLabels());
            Assert.AreEqual(2, UiPanel.Stack(_canvas.transform).Count, "the confirmation lies on top of the first step");
            Click("Cancel");
            CollectionAssert.AreEqual(new[] { "Drop out", "Keep going" }, DialogLabels(), "Cancel goes back to the first step");
            Assert.IsEmpty(choices);
            Assert.IsTrue(Freeze.Active, "still frozen between the steps");

            Click("Drop out");
            Click("Drop out");
            CollectionAssert.AreEqual(new[] { StopChoice.DropOut }, choices);
            Assert.IsFalse(StopDialog.IsOpen);
            Assert.IsNull(ModalDialog.Current);
            Assert.IsEmpty(UiPanel.Stack(_canvas.transform), "Drop out closes both dialogs");
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
            ModalDialog.Current.GetComponentsInChildren<Button>().OrderBy(b => b.transform.position.x)
                       .Select(b => LabelOf(b).text).ToArray();   // left to right

        static void Click(string label) =>
            ModalDialog.Current.GetComponentsInChildren<Button>().Single(b => LabelOf(b).text == label).onClick.Invoke();

        static void Press(UiNumberPad pad, string key) =>
            pad.GetComponentsInChildren<Button>(true).Single(b => b.name == key).onClick.Invoke();
    }

    public class UiIconTests
    {
        [Test]
        public void IconSetLooksUpByNameAndUiKitIconTintsIt()
        {
            var set = ScriptableObject.CreateInstance<UiIconSet>();
            var tex = new Texture2D(4, 4);
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            set.icons.Add(new UiIconSet.Entry { name = "gear", sprite = sprite });
            var theme = UiKit.Theme;
            var old = theme.icons;
            theme.icons = set;
            var parent = new GameObject("P", typeof(RectTransform));
            try
            {
                Assert.AreSame(sprite, set.Get("gear"));
                Assert.IsNull(set.Get("missing"));
                var icon = UiKit.Icon(parent.transform, "Icon", "gear", 0f, Color.red);
                var img = icon.GetComponent<Image>();
                Assert.AreSame(sprite, img.sprite);
                Assert.AreEqual(Color.red, img.color);
                Assert.AreEqual(theme.iconSize, ((RectTransform)icon.transform).sizeDelta.x);
            }
            finally
            {
                theme.icons = old;
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(set);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(tex);
            }
        }
    }
}
