using System;
using GPL.Input;
using GPL.UI;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// The way out of an experience chamber (ADR-0006): Y opens "Leave &lt;name&gt;?" with Back to Hall / Keep going (time and audio
    /// frozen while it is open); the panel's Back to Hall button calls <see cref="BackToHall"/>. Create it in Start, call
    /// <see cref="Dispose"/> in OnDestroy. A chamber may take Y first (<c>intercept</c> returns true when it used the press).
    /// </summary>
    public sealed class ExperienceExit
    {
        readonly Transform _host;
        readonly GlobalActions _actions;
        readonly string _name;
        readonly Func<bool> _intercept;
        readonly Action _beforeLeave;
        readonly RectTransform _window;
        bool _leaving, _frozen;

        /// <param name="host">The panel canvas (parent of the dialog).</param>
        /// <param name="window">The panel's window, hidden before the scene switch (overlay layers are not covered by the fade).</param>
        /// <param name="beforeLeave">Stops sounds, restores the normal hearing; runs once before the switch.</param>
        public ExperienceExit(Transform host, RectTransform window, GlobalActions actions, string name, Action beforeLeave, Func<bool> intercept = null)
        {
            _host = host;
            _window = window;
            _actions = actions;
            _name = name;
            _beforeLeave = beforeLeave;
            _intercept = intercept;
            if (_actions != null)
            {
                _actions.StopEnabled = true;
                _actions.StopRequested += OnStop;
            }
        }

        public void Dispose()
        {
            if (_actions != null)
            {
                _actions.StopRequested -= OnStop;
                _actions.StopEnabled = false;
            }
            EndFreeze();
        }

        void OnStop()
        {
            if (_intercept != null && _intercept()) return;
            Open();
        }

        public void Open()
        {
            if (_leaving || StopDialog.IsOpen || ModalDialog.Current != null) return;
            if (_actions != null) _actions.Blocked = true;
            Freeze.Begin();
            _frozen = true;
            ModalDialog.Show(_host, "Leave " + _name + "?", "Go back to the Main Hall, or keep going.",
                new DialogButton("Back to Hall", UiStyle.Borderless, () => { EndFreeze(); BackToHall(); }),
                new DialogButton("Keep going", UiStyle.Prominent, EndFreeze))
                .Abandoned += EndFreeze;
        }

        void EndFreeze()
        {
            if (!_frozen) return;
            _frozen = false;
            Freeze.End();
            if (_actions != null) _actions.Blocked = false;
        }

        public void BackToHall()
        {
            if (_leaving || AppNav.Busy) return;
            _leaving = true;
            _beforeLeave?.Invoke();
            var layer = _window != null ? _window.GetComponent<UiLayer>() : null;
            if (layer != null) layer.Show(false);
            if (!AppNav.BackToHall())
            {
                _leaving = false;
                if (layer != null) layer.Show(true);
            }
        }
    }
}
