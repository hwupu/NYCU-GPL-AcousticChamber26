namespace GPL.Homing
{
    /// <summary>
    /// The head gate (HW2, before every trial) as a pure state machine: feed it the head's yaw error and pitch every frame
    /// with the frame time; it passes once the head stayed inside the tolerance for <see cref="GateSettings.HoldSec"/>
    /// without a break. Leaving the tolerance restarts the hold. Time is whatever the caller feeds (scaled game time, so the
    /// Stop dialog freezes the gate).
    /// </summary>
    public sealed class HeadGate
    {
        public GateSettings Settings { get; }

        /// <summary>Time inside the tolerance since the last break (s).</summary>
        public float Held { get; private set; }
        /// <summary>Total time since <see cref="Reset"/> (s).</summary>
        public float Elapsed { get; private set; }
        /// <summary>Inside the tolerance in the last update.</summary>
        public bool Aligned { get; private set; }
        /// <summary>Times the head left the tolerance during a hold.</summary>
        public int Breaks { get; private set; }
        /// <summary>The hint is due: not passed after <see cref="GateSettings.HintAfterSec"/>. Stays on until passed.</summary>
        public bool ShowHint { get; private set; }
        /// <summary>When the hint first showed (s since reset); -1 if it never did.</summary>
        public float HintAt { get; private set; } = -1f;
        public bool Passed { get; private set; }
        /// <summary>Yaw error and pitch when the gate passed (the residual) and at the first update (before).</summary>
        public float YawAtPass { get; private set; }
        public float PitchAtPass { get; private set; }
        public float YawAtStart { get; private set; }
        public float PitchAtStart { get; private set; }

        bool _started;

        public HeadGate(GateSettings settings = null) { Settings = (settings ?? GateSettings.Defaults).Validated(); }

        public void Reset()
        {
            Held = Elapsed = 0f;
            Aligned = ShowHint = Passed = _started = false;
            Breaks = 0;
            HintAt = -1f;
            YawAtPass = PitchAtPass = YawAtStart = PitchAtStart = 0f;
        }

        /// <summary>True when yaw error and pitch are inside the tolerance (inclusive).</summary>
        public bool Inside(float yawErrorDeg, float pitchDeg) =>
            System.Math.Abs(yawErrorDeg) <= Settings.YawToleranceDeg && System.Math.Abs(pitchDeg) <= Settings.PitchToleranceDeg;

        /// <summary>One frame. Returns <see cref="Passed"/>. After passing, further updates change nothing.</summary>
        public bool Update(float yawErrorDeg, float pitchDeg, float dt)
        {
            if (Passed) return true;
            if (dt < 0f || float.IsNaN(dt)) dt = 0f;
            if (!_started)
            {
                _started = true;
                YawAtStart = yawErrorDeg;
                PitchAtStart = pitchDeg;
            }
            Elapsed += dt;
            bool inside = Inside(yawErrorDeg, pitchDeg);
            if (inside)
            {
                // the frame that brings the head in counts from the next frame on: the hold starts at 0
                if (Aligned) Held += dt;
            }
            else
            {
                if (Aligned && Held > 0f) Breaks++;
                Held = 0f;
            }
            Aligned = inside;
            if (Aligned && Held >= Settings.HoldSec)
            {
                Passed = true;
                ShowHint = false;
                YawAtPass = yawErrorDeg;
                PitchAtPass = pitchDeg;
                return true;
            }
            if (!ShowHint && Settings.HintAfterSec > 0f && Elapsed >= Settings.HintAfterSec)
            {
                ShowHint = true;
                HintAt = Elapsed;
            }
            return false;
        }
    }
}
