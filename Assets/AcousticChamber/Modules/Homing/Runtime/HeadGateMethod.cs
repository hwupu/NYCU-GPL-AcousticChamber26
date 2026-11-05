using System;
using System.Collections;
using UnityEngine;

namespace GPL.Homing
{
    /// <summary>
    /// Head gate (HW2, before every trial): shows the fixation dot straight ahead and waits until the head stays within
    /// the yaw / pitch tolerance for the hold time (<see cref="HeadGate"/>). Uses scaled game time, so the Stop dialog
    /// (time scale 0) freezes it. The hint text is the chamber's: it listens to <see cref="HintChanged"/>.
    /// </summary>
    public sealed class HeadGateMethod : IHomingMethod
    {
        readonly OVRCameraRig _rig;
        readonly FixationCue _cue;
        bool _cancel;

        public HeadGateMethod(OVRCameraRig rig, FixationCue cue, GateSettings settings = null)
        {
            _rig = rig != null ? rig : throw new ArgumentNullException(nameof(rig));
            _cue = cue;
            Gate = new HeadGate(settings);
        }

        public string Name => HomingMethods.HeadGate;
        public bool Running { get; private set; }
        /// <summary>The live gate state (held time, aligned, hint) for a readout.</summary>
        public HeadGate Gate { get; }
        /// <summary>The head pose of the last frame.</summary>
        public HeadPose Last { get; private set; }

        /// <summary>True: show the hint ("Look at the dot"); false: hide it (passed, cancelled).</summary>
        public event Action<bool> HintChanged;

        public void Cancel() => _cancel = true;

        public IEnumerator Run(Action<HomingResult> done)
        {
            if (Running) throw new InvalidOperationException("The head gate is already running.");
            Running = true;
            _cancel = false;
            Gate.Reset();
            bool hint = false;
            var outcome = HomingOutcome.Done;
            try
            {
                if (_cue != null) _cue.Show(_rig);
                while (true)
                {
                    if (_cancel) { outcome = HomingOutcome.Cancelled; break; }
                    Last = HeadPose.Of(_rig);
                    if (Gate.Update(Last.YawDeg, Last.PitchDeg, Time.deltaTime)) break;
                    if (Gate.ShowHint != hint)
                    {
                        hint = Gate.ShowHint;
                        HintChanged?.Invoke(hint);
                    }
                    yield return null;
                }
            }
            finally
            {
                if (_cue != null) _cue.Hide();
                if (hint) HintChanged?.Invoke(false);
                Running = false;
            }

            var r = new HomingResult
            {
                Method = Name,
                Outcome = outcome,
                DurationSec = Gate.Elapsed,
                YawBeforeDeg = Gate.YawAtStart,
                PitchBeforeDeg = Gate.PitchAtStart,
                YawAfterDeg = Gate.Passed ? Gate.YawAtPass : Last.YawDeg,
                PitchAfterDeg = Gate.Passed ? Gate.PitchAtPass : Last.PitchDeg,
                EyeHeightM = Last.EyeHeightM,
                Breaks = Gate.Breaks,
                HintAtSec = Gate.HintAt,
            };
            Debug.Log("[GPL] Homing: " + r.Note());
            done?.Invoke(r);
        }
    }
}
