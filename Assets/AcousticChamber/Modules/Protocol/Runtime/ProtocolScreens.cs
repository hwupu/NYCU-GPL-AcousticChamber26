using System;
using System.Collections;
using System.Collections.Generic;
using GPL.Homing;
using GPL.Input;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace GPL.Protocol
{
    /// <summary>Participant strings of the stock screens (English, 中文). A chamber may override a key with its own table.</summary>
    public static class ProtocolStrings
    {
        public const string ChooseLanguage = "protocol.choose_language";
        public const string Handover = "protocol.handover";
        public const string BlockDone = "protocol.block_done";
        public const string Thanks = "protocol.thanks";
        public const string Finish = "protocol.finish";
        public const string LookAtDot = "protocol.look_at_dot";
        public const string PressA = "protocol.press_a";
        public const string HoldMeta = "protocol.hold_meta";
        public const string RecenterTitle = "protocol.recenter_title";
        public const string RecenterShort = "protocol.recenter_short";
        public const string HandoverTitle = "protocol.handover_title";
        public const string LanguageButton = "protocol.language_button";
        public const string LookAtDotToBegin = "protocol.look_at_dot_to_begin";
        public const string SlideNext = "protocol.slide_next";
        public const string SlideBack = "protocol.slide_back";

        public static readonly StringTable Table = new StringTable("GPL.Protocol")
            .Add(ChooseLanguage, "Choose a language", "請選擇語言")
            .Add(Handover, "1. Set the IPD.\n2. Hand the headset to the participant.", "1. 調整瞳距（IPD）。\n2. 將頭戴裝置交給受試者。")
            .Add(RecenterTitle, "Recenter the view", "重新置中畫面")
            .Add(RecenterShort, "Recenter", "重新置中")
            .Add(HandoverTitle, "Protocol", "流程")
            .Add(LanguageButton, "Language", "語言")
            .Add(HoldMeta, "Look straight ahead.\nHold the Meta button until the view recenters.", "請直視前方，\n長按 Meta 按鈕，直到畫面重新置中。")
            .Add(LookAtDotToBegin, "Look at the dot to begin", "請看圓點以開始")
            .Add(BlockDone, "Block {0} of {1} done.", "第 {0} 段（共 {1} 段）完成。")
            .Add(Thanks, "Thank you.\nPlease return the headset.", "謝謝您的參與。\n請將頭戴裝置交還給實驗人員。")
            .Add(Finish, "Finish", "完成")
            .Add(LookAtDot, "Look at the dot", "請看圓點")
            .Add(PressA, "Press A", "請按 A")
            .Add(SlideNext, "Next", "下一頁")
            .Add(SlideBack, "Back", "上一頁");

        public static void EnsureRegistered()
        {
            UiStrings.EnsureRegistered();
            Loc.Register(Table);
        }
    }

    /// <summary>
    /// The stock screens of a protocol, built at run time on the chamber's overlay canvas (ADR-0004): language, handover
    /// (no recenter), recenter slide, slides, final dot check, a message with a button, and bare cue text without a panel (shown while
    /// the world is visible, e.g. during the head gate). One screen at a time; <see cref="Hide"/> removes it. The A button
    /// and the buttons do nothing while the Stop dialog is open.
    /// </summary>
    public sealed class ProtocolScreens
    {
        /// <summary>Cue text sits this far below the canvas center (the canvas is already below eye level; clear of the dot).</summary>
        public const float CueY = -260f;

        readonly Transform _canvas;
        readonly GlobalActions _actions;
        UiPanel _screen;
        TextMeshProUGUI _cue;

        public ProtocolScreens(Transform canvas, GlobalActions actions)
        {
            _canvas = canvas != null ? canvas : throw new ArgumentNullException(nameof(canvas));
            _actions = actions;
            ProtocolStrings.EnsureRegistered();
        }

        Vector2 Size => ((RectTransform)_canvas).sizeDelta;

        /// <summary>True while the participant's input counts (no Stop dialog, no freeze).</summary>
        public static bool InputOpen => !StopDialog.IsOpen && !Freeze.Active;

        /// <summary>A pressed this frame (and input open).</summary>
        public bool PressedA() => InputOpen && _actions != null && _actions.Reader.Down(InputActions.Confirm);

        // ---- screens -----------------------------------------------------------------------------------------

        /// <summary>Two buttons, "English" and "中文"; sets <see cref="Loc"/> and reports the choice.</summary>
        public IEnumerator Language(Action<UiLanguage> chosen)
        {
            var th = UiKit.Theme;
            var page = Open("Language Screen");
            string title = Loc.Get(UiLanguage.English, ProtocolStrings.ChooseLanguage) + "\n" + Loc.Get(UiLanguage.Chinese, ProtocolStrings.ChooseLanguage);
            var row = ButtonToolbar(page);
            CenterText(page, true);
            MessageText(page.Body, "Title", title);
            UiLanguage? pick = null;
            UiKit.Button(row, "English", Loc.LabelOf(UiLanguage.English), UiStyle.Prominent, onClick: () => { if (InputOpen) pick = UiLanguage.English; });
            UiKit.Button(row, "Chinese", Loc.LabelOf(UiLanguage.Chinese), UiStyle.Prominent, onClick: () => { if (InputOpen) pick = UiLanguage.Chinese; });
            while (pick == null) yield return null;
            Hide();
            Loc.Set(pick.Value);
            chosen?.Invoke(pick.Value);
            yield return null;
        }

        /// <summary>
        /// A question with two buttons in the message layout (e.g. "Practice again" / "I'm ready"); reports 0 for the left
        /// (<paramref name="leftKey"/>) and 1 for the right button (<paramref name="rightKey"/>, prominent). Keys are localized.
        /// </summary>
        public IEnumerator Choice(string text, string leftKey, string rightKey, Action<int> chosen)
        {
            var page = Open("Choice Screen");
            var row = ButtonToolbar(page);
            CenterText(page, true);
            MessageText(page.Body, "Text", text);
            int pick = -1;
            UiKit.Button(row, "Left", Loc.Get(leftKey), UiStyle.Gray, onClick: () => { if (InputOpen) pick = 0; });
            UiKit.Button(row, "Right", Loc.Get(rightKey), UiStyle.Prominent, onClick: () => { if (InputOpen) pick = 1; });
            yield return null; // the click that closed the previous screen does not count here
            while (pick < 0) yield return null;
            Hide();
            chosen?.Invoke(pick);
            yield return null;
        }

        /// <summary>
        /// A message (Message layout); waits for the button (<paramref name="buttonKey"/>, localized) or until
        /// <paramref name="until"/> is true (checked every frame). No screen waits for A. The screen stays until
        /// <see cref="Hide"/> or the next screen.
        /// </summary>
        public IEnumerator Message(string text, string buttonKey = null, Func<bool> until = null)
        {
            var page = Open("Message Screen");
            bool hasButton = !string.IsNullOrEmpty(buttonKey);
            var row = hasButton ? ButtonToolbar(page) : null;
            CenterText(page, hasButton);
            MessageText(page.Body, "Text", text);
            bool clicked = false;
            if (hasButton)
                UiKit.Button(row, "Button", Loc.Get(buttonKey), UiStyle.Prominent, onClick: () => { if (InputOpen) clicked = true; });
            yield return null; // the click that closed the previous screen does not count here
            while (!clicked && !(until != null && until())) yield return null;
            yield return null;
        }

        /// <summary>
        /// Message layout: the buttons sit in the toolbar at the bottom (a centered row, <c>gap</c> apart); the text is centered
        /// vertically in the panel (not in the space above the buttons): the body keeps a margin above equal to the toolbar's
        /// height, so the text stays centered and never touches the buttons. Call <see cref="ButtonToolbar"/> first, then
        /// <see cref="CenterText"/> (adding a slot resets the body padding).
        /// </summary>
        static RectTransform ButtonToolbar(UiPage page)
        {
            var toolbar = page.AddToolbar();
            toolbar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            return toolbar;
        }

        /// <summary>Centers the body's content vertically in the panel (<paramref name="hasToolbar"/>: the toolbar's height is mirrored above).</summary>
        static void CenterText(UiPage page, bool hasToolbar)
        {
            var th = UiKit.Theme;
            var g = page.Body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            g.childAlignment = TextAnchor.MiddleCenter;
            g.spacing = 0f;
            float margin = hasToolbar ? th.buttonHeight + th.surfacePaddingBottom : th.surfacePaddingBottom;
            g.padding = UiKit.Pad(th.surfacePaddingX, margin, th.surfacePaddingX, hasToolbar ? 0f : th.surfacePaddingBottom);
        }

        /// <summary>A centered, wrapping, bold heading-size text across the body width.</summary>
        static TextMeshProUGUI MessageText(RectTransform parent, string name, string text)
        {
            var t = UiKit.Text(parent, name, text, UiKit.Theme.headingSize, Vector2.zero, Vector2.zero, bold: true);
            t.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(t.gameObject, prefW: 0f, flexW: 1f);
            return t;
        }

        /// <summary>
        /// The handover (Architecture section 8, PB-20b): "Set the IPD, then hand the headset to the participant." + Continue,
        /// pressed by the experimenter. No recenter here: the participant recenters with <see cref="MetaRecenter"/>.
        /// </summary>
        public IEnumerator Handover()
        {
            var th = UiKit.Theme;
            var page = Open("Handover Screen", Loc.Get(ProtocolStrings.HandoverTitle));
            // a numbered list: left-aligned in a centered column
            page.Body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            var t = UiKit.Text(page.Body, "Steps", Loc.Get(ProtocolStrings.Handover), th.headingSize, Vector2.zero, Vector2.zero, bold: true,
                               align: TextAlignmentOptions.Left);
            t.lineSpacing = 20f;
            t.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(t.gameObject, prefW: 720f, flexW: 0f);
            bool clicked = false;
            var toolbar = page.AddToolbar();
            toolbar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            var button = UiKit.Button(toolbar, "Button", Loc.Get(UiStrings.Continue), UiStyle.Prominent, onClick: () => { if (InputOpen) clicked = true; });
            button.GetComponentInChildren<TextMeshProUGUI>().gameObject.AddComponent<LocalizedText>().key = UiStrings.Continue;
            yield return null;
            while (!clicked) yield return null;
            Hide();
            yield return null;
        }

        /// <summary>
        /// The recenter slide (participant, PB-20b), in the slide layout: title, illustration (<paramref name="look"/>; without
        /// an image the caption "Look straight ahead. Hold the Meta button until the view recenters." fills the image area),
        /// Back labelled "Language" when <paramref name="language"/> is given (reopens the language choice, then this slide),
        /// and Next (always active; PO 2026-11-07). The result goes to <paramref name="done"/> (log it as <c>homing</c>): the
        /// last user recenter <paramref name="listener"/> reported while the slide was up (outcome <c>done</c>), or, when Next
        /// was pressed without one, outcome <c>cancelled</c> with detail <c>next without a Meta recenter</c> (<c>no recenter
        /// listener</c> without a listener). <paramref name="shown"/> as in <see cref="Slides"/>.
        /// </summary>
        public IEnumerator MetaRecenter(RecenterListener listener, Action<HomingResult> done, SlideSet.Slide look = null,
                                        Func<IEnumerator> language = null, Action<SlideDeck, SlidePage, string> shown = null,
                                        Func<UiLanguage, string> nextLabel = null)
        {
            HomingResult result = null;
            Action<HomingResult> got = r => result = r;
            if (listener != null) listener.Recentered += got;
            var page = RecenterPage(look);
            try
            {
                // Next names the following slide (nextLabel; "Next →" when unknown); Back "← Language" with the language choice
                yield return Slides(new[] { page }, 0, false, shown, language,
                                    language != null ? l => Loc.Get(l, ProtocolStrings.LanguageButton) : (Func<UiLanguage, string>)null,
                                    nextLabel ?? (_ => ""));
            }
            finally
            {
                if (listener != null) listener.Recentered -= got;
            }
            done?.Invoke(result ?? new HomingResult
            {
                Method = HomingMethods.SystemRecenterByUser, Outcome = HomingOutcome.Cancelled,
                Detail = listener == null ? "no recenter listener" : "next without a Meta recenter",
            });
        }

        /// <summary>The recenter slide as a page: <paramref name="look"/>'s title / image, else the stock title and text.</summary>
        public static SlidePage RecenterPage(SlideSet.Slide look) => new SlidePage
        {
            Id = look != null && !string.IsNullOrEmpty(look.id) ? look.id : "recenter",
            Title = l => look != null && !string.IsNullOrEmpty(look.Title(l)) ? look.Title(l) : Loc.Get(l, ProtocolStrings.RecenterTitle),
            ShortTitle = l => look != null && !string.IsNullOrEmpty(look.ShortTitle(l)) ? look.ShortTitle(l) : Loc.Get(l, ProtocolStrings.RecenterShort),
            Image = l => look != null ? look.Image(l) : null,
            Caption = l => Loc.Get(l, ProtocolStrings.HoldMeta),
        };

        /// <summary>
        /// The final check before the trials (PB-20b): "Look at the dot to begin" under the fixation dot, then the head gate
        /// (<paramref name="settings"/>). The gate result goes to <paramref name="done"/>. Without a rig or a dot it ends at once
        /// with outcome <c>unavailable</c>.
        /// </summary>
        public IEnumerator DotCheck(OVRCameraRig rig, FixationCue dot, GateSettings settings, Action<HomingResult> done)
        {
            Hide();
            HomingResult result = null;
            if (rig != null && dot != null)
            {
                Cue(Loc.Get(ProtocolStrings.LookAtDotToBegin));
                yield return new HeadGateMethod(rig, dot, settings).Run(r => result = r);
                Cue(null);
            }
            else result = new HomingResult { Method = HomingMethods.HeadGate, Outcome = HomingOutcome.Unavailable, Detail = rig == null ? "no rig" : "no dot" };
            done?.Invoke(result);
        }

        /// <summary>Arrow before the Back label and after the Next label of a slide.</summary>
        public const string BackArrow = "← ", NextArrow = " →";

        /// <summary>The slides of a <see cref="SlideSet"/> (see the overload with pages).</summary>
        public IEnumerator Slides(SlideSet set, int start, bool resumed, Action<SlideDeck, SlidePage, string> shown,
                                  Func<IEnumerator> firstBack = null, Func<UiLanguage, string> firstBackLabel = null,
                                  Func<UiLanguage, string> lastLabel = null) =>
            Slides(SlidePage.From(set), start, resumed, shown, firstBack, firstBackLabel, lastLabel);

        /// <summary>Width of the Back and Next buttons of a slide (they carry the neighbouring slide's short title).</summary>
        public const float SlideButtonWidth = 360f;

        /// <summary>The button label naming a slide: its short title, else its title; null when it has neither.</summary>
        public static string ButtonName(SlidePage page, UiLanguage l)
        {
            if (page == null) return null;
            string s = page.ShortTitle != null ? page.ShortTitle(l) : null;
            if (string.IsNullOrEmpty(s) && page.Title != null) s = page.Title(l);
            return string.IsNullOrEmpty(s) ? null : s;
        }

        /// <summary>
        /// Slides (PB-20): title in the navigation bar, the illustration over the full width between the bars (without an image
        /// the page's caption text fills the gray area), Back (borderless) and Next in the toolbar.
        /// Buttons name where they lead (accessibility, PO 2026-11-07): "← &lt;previous slide's short title&gt;" and
        /// "&lt;next slide's short title&gt; →", "← Back" / "Next →" when that slide has no title.
        /// On the first slide Back is shown only with <paramref name="firstBack"/> (another screen, e.g. the language choice or
        /// the previous stage's slide), labelled <paramref name="firstBackLabel"/>; the slide comes back afterwards (how
        /// <c>again</c>). On the last slide Next reads "Continue" without an arrow (the slides end), or, with
        /// <paramref name="lastLabel"/>, names what follows ("Next →" when it returns empty). Next is inactive while a page's
        /// <see cref="SlidePage.Ready"/> is false. Opens on slide <paramref name="start"/>; <paramref name="shown"/> runs for
        /// every slide that comes on screen (store its index, log it). No pages: nothing shown.
        /// </summary>
        public IEnumerator Slides(IReadOnlyList<SlidePage> pages, int start, bool resumed, Action<SlideDeck, SlidePage, string> shown,
                                  Func<IEnumerator> firstBack = null, Func<UiLanguage, string> firstBackLabel = null,
                                  Func<UiLanguage, string> lastLabel = null)
        {
            var deck = new SlideDeck(pages != null ? pages.Count : 0, start);
            if (deck.Done) yield break;
            var th = UiKit.Theme;
            string how = resumed ? SlideDeck.How.Resume : SlideDeck.How.First;
            int move = 0;

            while (!deck.Done)
            {
                // (re)build the screen; it is rebuilt only after another screen (firstBack) replaced it
                var uiPage = Open("Slides Screen", "");
                UiNavigationBar.CenterTitle(uiPage);
                var title = uiPage.NavigationBar.Find("Title Box/Label").GetComponent<TextMeshProUGUI>();
                var toolbar = uiPage.AddToolbar();
                // the illustration runs the full width between the bar and the toolbar (after the slots: they set the body padding)
                var bodyGroup = uiPage.Body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                bodyGroup.padding = UiKit.Pad(0, 12, 0, 0);
                var imageRt = UiKit.Node(uiPage.Body, "Image");
                UiKit.Size(imageRt.gameObject, minW: 0f, minH: 0f, prefW: 0f, prefH: 0f, flexW: 1f, flexH: 1f);
                var image = imageRt.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                image.raycastTarget = false;
                var caption = UiKit.Text(imageRt, "Caption", "", th.headingSize, Vector2.zero, Vector2.zero, bold: true);
                UiKit.Fill(caption.rectTransform, 48f);
                caption.raycastTarget = false;
                var back = UiKit.Button(toolbar, "Back", "…", UiStyle.Borderless, minWidth: false, onClick: () => { if (InputOpen && move == 0) move = -1; });
                var spacer = UiKit.Node(toolbar, "Spacer");
                UiKit.Size(spacer.gameObject, flexW: 1f);
                var next = UiKit.Button(toolbar, "Next", "…", UiStyle.Prominent, minWidth: false, onClick: () => { if (InputOpen && move == 0) move = 1; });
                foreach (var b in new[] { back, next })
                    UiKit.Size(b.gameObject, minW: SlideButtonWidth, minH: th.buttonHeight, prefW: SlideButtonWidth, prefH: th.buttonHeight, flexW: 0f, flexH: 0f);
                var backLabel = back.GetComponentInChildren<TextMeshProUGUI>();
                var nextLabel = next.GetComponentInChildren<TextMeshProUGUI>();
                backLabel.overflowMode = TextOverflowModes.Ellipsis; // a long title ends in …
                nextLabel.overflowMode = TextOverflowModes.Ellipsis;

                bool rebuild = false;
                while (!deck.Done && !rebuild)
                {
                    var page = pages[deck.Index];
                    var lang = Loc.Current;
                    bool firstWithBack = deck.IsFirst && firstBack != null;
                    title.text = page.Title != null ? page.Title(lang) : "";
                    var tex = page.Image != null ? page.Image(lang) : null;
                    image.texture = tex;
                    image.color = tex != null ? Color.white : th.raised; // no image: an empty gray area with the caption
                    caption.text = tex == null && page.Caption != null ? page.Caption(lang) : "";
                    back.gameObject.SetActive(!deck.IsFirst || firstWithBack);
                    // buttons name where they lead, with arrows; a final Continue (the slides end) has none
                    string backName = deck.IsFirst ? (firstBackLabel != null ? firstBackLabel(lang) : null) : ButtonName(pages[deck.Index - 1], lang);
                    backLabel.text = BackArrow + (string.IsNullOrEmpty(backName) ? Loc.Get(ProtocolStrings.SlideBack) : backName);
                    if (deck.IsLast && lastLabel == null) nextLabel.text = Loc.Get(UiStrings.Continue);
                    else
                    {
                        string nextName = deck.IsLast ? lastLabel(lang) : ButtonName(pages[deck.Index + 1], lang);
                        nextLabel.text = (string.IsNullOrEmpty(nextName) ? Loc.Get(ProtocolStrings.SlideNext) : nextName) + NextArrow;
                    }
                    next.interactable = page.Ready == null || page.Ready();
                    shown?.Invoke(deck, page, how);

                    move = 0;
                    yield return null; // the click that changed the slide does not count again
                    while (move == 0)
                    {
                        next.interactable = page.Ready == null || page.Ready();
                        yield return null;
                    }
                    if (move > 0)
                    {
                        deck.Next();
                        how = SlideDeck.How.Next;
                    }
                    else if (firstWithBack)
                    {
                        yield return firstBack();
                        how = SlideDeck.How.Again;
                        rebuild = true;
                    }
                    else
                    {
                        deck.Back();
                        how = SlideDeck.How.Back;
                    }
                }
            }
            Hide();
            yield return null;
        }

        /// <summary>Bare text on the canvas without a panel (the panel is hidden), below the center. Null or empty hides it.</summary>
        public void Cue(string text, float y = CueY)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (_cue != null) _cue.gameObject.SetActive(false);
                return;
            }
            if (_cue == null)
            {
                var th = UiKit.Theme;
                _cue = UiKit.Text(_canvas, "Protocol Cue", "", th.titleSize, Vector2.zero, new Vector2(Size.x, 90f), bold: true);
            }
            _cue.rectTransform.anchoredPosition = new Vector2(0f, y);
            _cue.text = text;
            _cue.gameObject.SetActive(true);
        }

        /// <summary>Removes the current screen (not the cue).</summary>
        public void Hide()
        {
            if (_screen != null) Remove(_screen.gameObject);
            _screen = null;
        }

        /// <summary>Removes the screen and the cue (end of the run, Stop dialog ended it).</summary>
        public void Clear()
        {
            Hide();
            if (_cue != null) Remove(_cue.gameObject);
            _cue = null;
        }

        static void Remove(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// A new screen: a <see cref="UiPanel"/> over the canvas (it replaces the previous one). With <paramref name="title"/> it has
        /// a navigation bar (no back button) showing it.
        /// </summary>
        UiPage Open(string name, string title = null)
        {
            Hide();
            _screen = UiPanel.Create(_canvas, name, Size);
            _screen.HasGrabber = false; // the participant's screens are not moved
            var page = _screen.Surface.Root;
            if (title != null) UiNavigationBar.Create(page, title, UiNavigationBar.Leading.None);
            return page;
        }
    }
}
