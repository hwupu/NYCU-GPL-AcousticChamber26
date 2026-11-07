using System;
using System.Collections;
using GPL.Input;
using UnityEngine;

namespace GPL.Responses
{
    /// <summary>
    /// Rotate-with-triggers response (HW2): left trigger held moves the azimuth left, right trigger right, confirm (A) ends it.
    /// A coroutine over <see cref="AzimuthAdjust"/>; the chamber moves its sound in <c>onAzimuth</c> (e.g. GPL.SpatialSources
    /// <c>PlacedSource.SetAzimuth</c>). Actions are read through GPL.Input (<see cref="InputActions.TriggerLeft"/>,
    /// <see cref="InputActions.TriggerRight"/>, <see cref="InputActions.Confirm"/>): declare them in the chamber's binding table.
    /// Runs on scaled time: while the Stop dialog is open (time scale 0, or <c>paused</c> returns true) nothing moves, no confirm is
    /// taken and the duration does not grow.
    /// </summary>
    public sealed class TriggerAdjust
    {
        readonly ActionReader _input;
        readonly Func<bool> _paused;

        public string LeftAction = InputActions.TriggerLeft;
        public string RightAction = InputActions.TriggerRight;
        public string ConfirmAction = InputActions.Confirm;

        /// <summary>The adjustment in progress (null before <see cref="Run"/>).</summary>
        public AzimuthAdjust Current { get; private set; }

        public TriggerAdjust(ActionReader input, Func<bool> paused = null)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _paused = paused ?? (() => Time.timeScale <= 0f);
        }

        /// <summary>Coroutine: runs until confirm. <paramref name="onAzimuth"/> is called with the start value and after every frame.</summary>
        public IEnumerator Run(float startDeg, float targetDeg, AdjustSettings settings, Action<float> onAzimuth, Action<AdjustResult> done)
        {
            var adjust = Current = new AzimuthAdjust(startDeg, targetDeg, settings);
            onAzimuth?.Invoke((float)adjust.AzimuthDeg);
            bool wasPaused = false;
            while (true)
            {
                yield return null;
                bool paused = _paused();
                if (paused || wasPaused) { wasPaused = paused; continue; } // also skip the frame the dialog closes (its button press)
                bool left = _input.Held(LeftAction), right = _input.Held(RightAction), confirm = _input.Down(ConfirmAction);
                bool finished = adjust.Step(left, right, confirm, Time.deltaTime);
                onAzimuth?.Invoke((float)adjust.AzimuthDeg);
                if (finished) break;
            }
            done?.Invoke(adjust.Result());
        }
    }
}
