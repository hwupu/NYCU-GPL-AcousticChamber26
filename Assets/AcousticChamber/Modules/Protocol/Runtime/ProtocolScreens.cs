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
        RectTransform _screen;
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
            var r = Open("Language Screen");
            string title = Loc.Get(UiLanguage.English, ProtocolStrings.ChooseLanguage) + "\n" + Loc.Get(UiLanguage.Chinese, ProtocolStrings.ChooseLanguage);
            // the thanks screen's layout: text centered in its box, buttons below (MessageTextY / MessageButtonY)
            float buttonY = MessageButtonY;
            UiKit.Text(r, "Title", title, th.headingSize, new Vector2(0f, MessageTextY), new Vector2(Size.x - 96f, MessageTextH), bold: true);
            UiLanguage? pick = null;
            UiKit.Button(r, "English", Loc.LabelOf(UiLanguage.English), UiStyle.Prominent, new Vector2(-170f, buttonY), new Vector2(300f, 0f),
                         () => { if (InputOpen) pick = UiLanguage.English; });
            UiKit.Button(r, "Chinese", Loc.LabelOf(UiLanguage.Chinese), UiStyle.Prominent, new Vector2(170f, buttonY), new Vector2(300f, 0f),
                         () => { if (InputOpen) pick = UiLanguage.Chinese; });
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
            var th = UiKit.Theme;
            var r = Open("Choice Screen");
            UiKit.Text(r, "Text", text, th.headingSize, new Vector2(0f, MessageTextY), new Vector2(Size.x - 96f, MessageTextH), bold: true);
            int pick = -1;
            UiKit.Button(r, "Left", Loc.Get(leftKey), UiStyle.Gray, new Vector2(-200f, MessageButtonY), new Vector2(360f, 0f),
                         () => { if (InputOpen) pick = 0; });
            UiKit.Button(r, "Right", Loc.Get(rightKey), UiStyle.Prominent, new Vector2(200f, MessageButtonY), new Vector2(360f, 0f),
                         () => { if (InputOpen) pick = 1; });
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
            var th = UiKit.Theme;
            var r = Open("Message Screen");
            bool hasButton = !string.IsNullOrEmpty(buttonKey);
            UiKit.Text(r, "Text", text, th.headingSize, new Vector2(0f, hasButton ? MessageTextY : 0f), new Vector2(Size.x - 96f, MessageTextH), bold: true);
            bool clicked = false;
            if (hasButton)
                UiKit.Button(r, "Button", Loc.Get(buttonKey), UiStyle.Prominent, new Vector2(0f, MessageButtonY), new Vector2(360f, 0f),
                             () => { if (InputOpen) clicked = true; });
            yield return null; // the click that closed the previous screen does not count here
            while (!clicked && !(until != null && until())) yield return null;
            yield return null;
        }

        /// <summary>
        /// Layout of a message screen with buttons (the thanks screen; PO 2026-11-07: the reference for the language choice and
        /// the block break): the text is vertically centered in a 360 u box at y 80, the buttons sit at y -180.
        /// </summary>
        public const float MessageTextY = 80f, MessageTextH = 360f, MessageButtonY = -180f;

        /// <summary>
        /// The handover (Architecture section 8, PB-20b): "Set the IPD, then hand the headset to the participant." + Continue,
        /// pressed by the experimenter. No recenter here: the participant recenters with <see cref="MetaRecenter"/>.
        /// </summary>
        public IEnumerator Handover()
        {
            var th = UiKit.Theme;
            var r = Open("Handover Screen");
            var title = UiKit.Text(r, "Title", Loc.Get(ProtocolStrings.HandoverTitle), th.headingSize,
                                   new Vector2(0f, SlideTitleY(Size.y)), new Vector2(Size.x - 96f, UiKit.Theme.iconButtonSize), bold: true);
            UiKit.SingleLine(title); // the slides' top bar
            // a numbered list: left-aligned in a centered column
            var t = UiKit.Text(r, "Steps", Loc.Get(ProtocolStrings.Handover), th.headingSize, new Vector2(0f, MessageTextY), new Vector2(720f, MessageTextH),
                               bold: true, align: TextAlignmentOptions.Left);
            t.lineSpacing = 20f;
            bool clicked = false;
            UiKit.LocButton(r, "Button", UiStrings.Continue, UiStyle.Prominent, new Vector2(0f, MessageButtonY), new Vector2(360f, 0f),
                            () => { if (InputOpen) clicked = true; });
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

        /// <summary>Top bar of a slide (title) = the page header: 40 u top padding + 80 u title row (120 u).</summary>
        public static float SlideTopBar => Page.HeaderHeight;

        /// <summary>Center of a slide title: like a page title, in the 80 u row below the 40 u top padding (PO 2026-11-07).</summary>
        public static float SlideTitleY(float height) => height / 2f - Page.TopPad - UiKit.Theme.iconButtonSize / 2f;
        /// <summary>Bottom bar of a slide (Back, Next); the image fills the height between the bars (468 u on a 700 u canvas).</summary>
        public const float SlideBottomBar = 112f;

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
        /// Slides (PB-20): title in the top bar, the illustration over the full width between the bars (1000 x 468 u on the 1000 x 700 u
        /// canvas; without an image the page's caption text fills the gray area), Back (borderless) and Next in the bottom bar.
        /// Buttons name where they lead (accessibility, PO 2026-11-07): "← &lt;previous slide's short title&gt;" and
        /// "&lt;next slide's short title&gt; →", "← Back" / "Next →" when that slide has no title.
        /// On the first slide Back is shown only with <paramref name="firstBack"/> (another screen, e.g. the language choice or
        /// the previous stage's slide), labelled <paramref name="firstBackLabel"/>; the slide comes back afterwards (how
        /// <c>again</c>). On the last slide Next reads "Continue" without an arrow (the slides end), or, with
        /// <paramref name="lastLabel"/>, names what follows ("Next →" when it returns empty). Next is inactive while a page's
        /// <see cref="SlidePage.Ready"/> is false. Opens on slide <paramref name="start"/>; <paramref name="shown"/> runs for
        /// every slide that comes on screen (store its index, log it). The window changes in place. No pages: nothing shown.
        /// </summary>
        public IEnumerator Slides(IReadOnlyList<SlidePage> pages, int start, bool resumed, Action<SlideDeck, SlidePage, string> shown,
                                  Func<IEnumerator> firstBack = null, Func<UiLanguage, string> firstBackLabel = null,
                                  Func<UiLanguage, string> lastLabel = null)
        {
            var deck = new SlideDeck(pages != null ? pages.Count : 0, start);
            if (deck.Done) yield break;
            var th = UiKit.Theme;
            float w = Size.x, h = Size.y, imageH = h - SlideTopBar - SlideBottomBar;
            float buttonY = -h / 2f + SlideBottomBar / 2f, buttonX = w / 2f - 48f - SlideButtonWidth / 2f;
            string how = resumed ? SlideDeck.How.Resume : SlideDeck.How.First;
            int move = 0;

            while (!deck.Done)
            {
                // (re)build the window; it is rebuilt only after another screen (firstBack) replaced it
                var r = Open("Slides Screen");
                var title = UiKit.Text(r, "Title", "", th.headingSize, new Vector2(0f, SlideTitleY(h)), new Vector2(w - 96f, th.iconButtonSize), bold: true);
                UiKit.SingleLine(title);
                var imageRt = UiKit.Rect(r, "Image", new Vector2(0f, h / 2f - SlideTopBar - imageH / 2f), new Vector2(w, imageH));
                var image = imageRt.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                image.raycastTarget = false;
                var caption = UiKit.Text(imageRt, "Caption", "", th.headingSize, Vector2.zero, new Vector2(w - 192f, imageH - 96f), bold: true);
                caption.raycastTarget = false;
                var back = UiKit.Button(r, "Back", "", UiStyle.Borderless, new Vector2(-buttonX, buttonY),
                                        new Vector2(SlideButtonWidth, 0f), () => { if (InputOpen && move == 0) move = -1; });
                var next = UiKit.Button(r, "Next", "", UiStyle.Prominent, new Vector2(buttonX, buttonY),
                                        new Vector2(SlideButtonWidth, 0f), () => { if (InputOpen && move == 0) move = 1; });
                var backLabel = back.transform.Find("Label").GetComponent<TextMeshProUGUI>();
                var nextLabel = next.transform.Find("Label").GetComponent<TextMeshProUGUI>();
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

        /// <summary>Bare text on the canvas without a panel (the window is hidden), below the center. Null or empty hides it.</summary>
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
            if (_screen != null) UnityEngine.Object.Destroy(_screen.gameObject);
            _screen = null;
        }

        /// <summary>Removes the screen and the cue (end of the run, Stop dialog ended it).</summary>
        public void Clear()
        {
            Hide();
            if (_cue != null) UnityEngine.Object.Destroy(_cue.gameObject);
            _cue = null;
        }

        RectTransform Open(string name)
        {
            Hide();
            _screen = UiKit.Window(_canvas, name);
            return _screen;
        }
    }
}
