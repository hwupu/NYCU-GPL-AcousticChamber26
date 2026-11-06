using System;
using System.Collections;
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
        public const string LookAheadPressA = "protocol.look_ahead_press_a";
        public const string BlockDone = "protocol.block_done";
        public const string Thanks = "protocol.thanks";
        public const string Finish = "protocol.finish";
        public const string LookAtDot = "protocol.look_at_dot";
        public const string PressA = "protocol.press_a";

        public static readonly StringTable Table = new StringTable("GPL.Protocol")
            .Add(ChooseLanguage, "Choose a language", "請選擇語言")
            .Add(Handover, "Please hand the headset to the participant.", "請將頭戴裝置交給受試者。")
            .Add(LookAheadPressA, "Look straight ahead and press A.", "請直視前方，然後按 A。")
            .Add(BlockDone, "Block {0} of {1} done.\nPress A to continue.", "第 {0} 段（共 {1} 段）完成。\n請按 A 繼續。")
            .Add(Thanks, "Thank you.\nPlease return the headset.", "謝謝您的參與。\n請將頭戴裝置交還給實驗人員。")
            .Add(Finish, "Finish", "完成")
            .Add(LookAtDot, "Look at the dot", "請看圓點")
            .Add(PressA, "Press A", "請按 A");

        public static void EnsureRegistered()
        {
            UiStrings.EnsureRegistered();
            Loc.Register(Table);
        }
    }

    /// <summary>
    /// The stock screens of a protocol, built at run time on the chamber's overlay canvas (ADR-0004): language, handover
    /// (with the system recenter), a message that waits for A or a button, and bare cue text without a panel (shown while
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
            UiKit.Text(r, "Title", title, th.headingSize, new Vector2(0f, 120f), new Vector2(Size.x - 96f, 160f), bold: true);
            UiLanguage? pick = null;
            UiKit.Button(r, "English", Loc.LabelOf(UiLanguage.English), UiStyle.Prominent, new Vector2(-170f, -80f), new Vector2(300f, 0f),
                         () => { if (InputOpen) pick = UiLanguage.English; });
            UiKit.Button(r, "Chinese", Loc.LabelOf(UiLanguage.Chinese), UiStyle.Prominent, new Vector2(170f, -80f), new Vector2(300f, 0f),
                         () => { if (InputOpen) pick = UiLanguage.Chinese; });
            while (pick == null) yield return null;
            Hide();
            Loc.Set(pick.Value);
            chosen?.Invoke(pick.Value);
            yield return null;
        }

        /// <summary>
        /// A message; waits for A (<paramref name="pressA"/>) or for the button (<paramref name="buttonKey"/>, localized),
        /// whichever is given. The screen stays until <see cref="Hide"/> or the next screen.
        /// </summary>
        public IEnumerator Message(string text, bool pressA, string buttonKey = null)
        {
            var th = UiKit.Theme;
            var r = Open("Message Screen");
            bool hasButton = !string.IsNullOrEmpty(buttonKey);
            UiKit.Text(r, "Text", text, th.headingSize, new Vector2(0f, hasButton ? 80f : 0f), new Vector2(Size.x - 96f, 360f), bold: true);
            bool clicked = false;
            if (hasButton)
                UiKit.Button(r, "Button", Loc.Get(buttonKey), UiStyle.Prominent, new Vector2(0f, -180f), new Vector2(360f, 0f),
                             () => { if (InputOpen) clicked = true; });
            yield return null; // an A press that closed the previous screen does not count here
            while (!clicked && !(pressA && PressedA())) yield return null;
            yield return null;
        }

        /// <summary>
        /// The handover (Architecture section 8): "Please hand the headset to the participant." + Continue (experimenter),
        /// then "Look straight ahead and press A." (participant); A runs the system recenter. Reports the recenter result.
        /// </summary>
        public IEnumerator Handover(OVRCameraRig rig, Action<HomingResult> recentered)
        {
            yield return Message(Loc.Get(ProtocolStrings.Handover), false, UiStrings.Continue);
            yield return Message(Loc.Get(ProtocolStrings.LookAheadPressA), true);
            Hide();
            HomingResult result = null;
            if (rig != null) yield return new SystemRecenterMethod(rig).Run(r => result = r);
            else result = new HomingResult { Method = HomingMethods.SystemRecenter, Outcome = HomingOutcome.Unavailable, Detail = "no rig" };
            recentered?.Invoke(result);
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
