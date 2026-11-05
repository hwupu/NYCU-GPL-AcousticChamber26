using System;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>Outcome of the Stop dialog.</summary>
    public enum StopChoice
    {
        KeepGoing,
        /// <summary>Real session: the participant withdraws (log <c>participant_dropout</c>; not resumable).</summary>
        DropOut,
        /// <summary>Debug run: end the run and return to the chamber menu.</summary>
        BackToMenu,
    }

    /// <summary>
    /// The Stop dialog (opened by the global Stop action, Y). Freezes the session while open (<see cref="Freeze"/>).
    /// Real session: Keep going / Drop out, and Drop out asks once more ("Drop out S0012?" Cancel / Drop out; Cancel
    /// returns to the first step). Debug run: Keep going / Back to the chamber menu.
    /// <paramref name="onChoice"/> is called once, after the dialog has closed and the freeze is lifted.
    /// </summary>
    public static class StopDialog
    {
        /// <summary>True while a Stop dialog (either step) is open.</summary>
        public static bool IsOpen { get; private set; }

        public static void Show(Transform canvas, bool debugRun, string studyId, Action<StopChoice> onChoice, bool freeze = true)
        {
            if (IsOpen) return;
            UiStrings.EnsureRegistered();
            IsOpen = true;
            if (freeze) Freeze.Begin();
            string who = string.IsNullOrEmpty(studyId) ? "?" : studyId;

            void Finish(StopChoice choice)
            {
                IsOpen = false;
                if (freeze) Freeze.End();
                Debug.Log("[GPL] Stop dialog: " + choice);
                onChoice?.Invoke(choice);
            }

            void First()
            {
                var th = UiKit.Theme;
                if (debugRun)
                {
                    Watch(ModalDialog.Show(canvas, Loc.Get(UiStrings.StopTitle), Loc.Get(UiStrings.StopMessageDebug), th.warning,
                        new DialogButton(Loc.Get(UiStrings.StopKeepGoing), UiStyle.Primary, () => Finish(StopChoice.KeepGoing)),
                        new DialogButton(Loc.Get(UiStrings.StopBackToMenu), UiStyle.Warning, () => Finish(StopChoice.BackToMenu))));
                    return;
                }
                Watch(ModalDialog.Show(canvas, Loc.Get(UiStrings.StopTitle), Loc.Get(UiStrings.StopMessage), th.danger,
                    new DialogButton(Loc.Get(UiStrings.StopKeepGoing), UiStyle.Primary, () => Finish(StopChoice.KeepGoing)),
                    new DialogButton(Loc.Get(UiStrings.StopDropOut), UiStyle.Danger, Confirm)));
            }

            void Confirm()
            {
                var th = UiKit.Theme;
                Watch(ModalDialog.Show(canvas, Loc.Get(UiStrings.StopConfirmTitle, who), Loc.Get(UiStrings.StopConfirmMessage), th.danger,
                    new DialogButton(Loc.Get(UiStrings.Cancel), UiStyle.Neutral, First),
                    new DialogButton(Loc.Get(UiStrings.StopDropOut), UiStyle.Danger, () => Finish(StopChoice.DropOut))));
            }

            // If the canvas is destroyed (scene change) while the dialog is open, lift the freeze without a choice.
            void Watch(ModalDialog d) => d.Abandoned += () => Abandon(freeze);

            First();
        }

        static void Abandon(bool freeze)
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (freeze) Freeze.End();
            Debug.LogWarning("[GPL] Stop dialog destroyed without a choice (scene change?).");
        }

        /// <summary>Test hook.</summary>
        public static void ResetForTests()
        {
            IsOpen = false;
            Freeze.ResetForTests();
        }
    }

    /// <summary>
    /// Freezes a running session: <c>Time.timeScale = 0</c> (trials, timers, gates stop) and <c>AudioListener.pause</c>
    /// (all audio pauses; sources with <c>ignoreListenerPause</c> keep playing). The pointer and OVRInput keep working.
    /// Nested Begin/End pairs are counted; the values from before the first Begin are restored by the last End.
    /// </summary>
    public static class Freeze
    {
        static int _depth;
        static float _timeScale = 1f;
        static bool _audioPaused;

        public static bool Active => _depth > 0;

        public static void Begin()
        {
            if (_depth++ > 0) return;
            _timeScale = Time.timeScale;
            _audioPaused = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        public static void End()
        {
            if (_depth == 0) return;
            if (--_depth > 0) return;
            Time.timeScale = _timeScale;
            AudioListener.pause = _audioPaused;
        }

        public static void ResetForTests()
        {
            if (_depth > 0)
            {
                _depth = 1;
                End();
            }
        }
    }
}
