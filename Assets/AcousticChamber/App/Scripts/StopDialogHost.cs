using System;
using GPL.Input;
using GPL.UI;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Connects the global Stop action (GPL.Input, Y) to the Stop dialog (GPL.UI) on <see cref="canvas"/>. The session
    /// owner sets <see cref="DebugRun"/> and <see cref="StudyId"/>, turns Y on with <c>GlobalActions.StopEnabled</c> and
    /// handles <see cref="Chosen"/> (log <c>participant_dropout</c>, return to the chamber menu, ...).
    /// </summary>
    public class StopDialogHost : MonoBehaviour
    {
        [Tooltip("Overlay canvas the dialog is shown on.")]
        public Transform canvas;

        GlobalActions _actions;

        public bool DebugRun { get; set; }
        public string StudyId { get; set; }

        /// <summary>Raised after the dialog has closed and the freeze is lifted.</summary>
        public event Action<StopChoice> Chosen;

        void Start()
        {
            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (_actions == null)
            {
                Debug.LogError("[GPL] StopDialogHost: no GlobalActions in the scene; Y will not open the Stop dialog.");
                return;
            }
            _actions.StopRequested += Open;
        }

        void OnDestroy()
        {
            if (_actions != null) _actions.StopRequested -= Open;
        }

        /// <summary>Opens the Stop dialog (also callable from a button). Ignored while any dialog is open.</summary>
        public void Open()
        {
            if (StopDialog.IsOpen || ModalDialog.Current != null) return;
            if (_actions != null) _actions.Blocked = true;
            StopDialog.Show(canvas != null ? canvas : transform, DebugRun, StudyId, choice =>
            {
                if (_actions != null) _actions.Blocked = false;
                Chosen?.Invoke(choice);
            });
        }
    }
}
