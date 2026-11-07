using System;
using GPL.Core;

namespace GPL.Responses
{
    /// <summary>Settings of a method-of-adjustment response in azimuth (HW2 defaults).</summary>
    public struct AdjustSettings
    {
        /// <summary>Speed while a direction is held (deg/s).</summary>
        public double RateDegPerS;
        /// <summary>The azimuth is clamped to ±this (deg).</summary>
        public double LimitDeg;
        /// <summary>Confirm presses during the first seconds are ignored (an A still held from the previous screen).</summary>
        public double ConfirmIgnoreS;

        public static AdjustSettings HW2 => new AdjustSettings { RateDegPerS = 10, LimitDeg = 90, ConfirmIgnoreS = 0.3 };
    }

    /// <summary>What an adjustment ended with. Angles in degrees (0 = ahead, negative = left).</summary>
    public struct AdjustResult
    {
        public double StartDeg;
        public double FinalDeg;
        /// <summary>The reference the participant matched (e.g. the picture); NaN if none.</summary>
        public double TargetDeg;
        /// <summary>From the start to the confirm, on the clock given to <see cref="AzimuthAdjust.Step"/> (paused time excluded).</summary>
        public double DurationS;
        /// <summary>Times the direction of motion changed (left to right or back).</summary>
        public int Reversals;
        /// <summary>True when the final azimuth sits at the clamp.</summary>
        public bool AtLimit;

        /// <summary>final − target (positive: the response ended right of the target); NaN without a target.</summary>
        public double SignedErrorDeg => double.IsNaN(TargetDeg) ? double.NaN : FinalDeg - TargetDeg;

        /// <summary>Log note: <c>start_deg=60 final_deg=12.5 target_deg=7.5 duration_s=4.812 reversals=1 at_limit=0</c>.</summary>
        public string Note() =>
            "start_deg=" + Fmt.F(StartDeg, 2) + " final_deg=" + Fmt.F(FinalDeg, 2) + " target_deg=" + Fmt.F(TargetDeg, 2) +
            " duration_s=" + Fmt.F(DurationS, 3) + " reversals=" + Fmt.I(Reversals) + " at_limit=" + (AtLimit ? "1" : "0");
    }

    /// <summary>
    /// Pure method-of-adjustment state: one direction held moves the azimuth at a fixed rate, both or none hold still, clamped to
    /// ±limit; a confirm is accepted after the ignore time. Drive it once per frame with the held state and the frame's delta time.
    /// </summary>
    public sealed class AzimuthAdjust
    {
        readonly AdjustSettings _s;
        readonly double _start, _target;
        int _lastDir;

        public double AzimuthDeg { get; private set; }
        public double ElapsedS { get; private set; }
        public int Reversals { get; private set; }
        public bool Done { get; private set; }

        public AzimuthAdjust(double startDeg, double targetDeg, AdjustSettings settings)
        {
            if (settings.RateDegPerS < 0 || settings.LimitDeg <= 0) throw new ArgumentException("rate must be ≥ 0 and limit > 0");
            _s = settings;
            _start = Clamp(startDeg);
            _target = targetDeg;
            AzimuthDeg = _start;
        }

        public bool ConfirmAllowed => ElapsedS >= _s.ConfirmIgnoreS;

        /// <summary>Advance one frame. Returns true when this frame ended the adjustment (confirm accepted).</summary>
        public bool Step(bool leftHeld, bool rightHeld, bool confirmPressed, double dt)
        {
            if (Done) return false;
            if (dt < 0) dt = 0;
            int dir = (rightHeld ? 1 : 0) - (leftHeld ? 1 : 0);
            if (dir != 0 && _lastDir != 0 && dir != _lastDir) Reversals++;
            if (dir != 0) _lastDir = dir;
            AzimuthDeg = Clamp(AzimuthDeg + dir * _s.RateDegPerS * dt);
            ElapsedS += dt;
            if (confirmPressed && ConfirmAllowed) Done = true;
            return Done;
        }

        public AdjustResult Result() => new AdjustResult
        {
            StartDeg = _start,
            FinalDeg = AzimuthDeg,
            TargetDeg = _target,
            DurationS = ElapsedS,
            Reversals = Reversals,
            AtLimit = Math.Abs(AzimuthDeg) >= _s.LimitDeg - 1e-9,
        };

        double Clamp(double az) => Math.Max(-_s.LimitDeg, Math.Min(_s.LimitDeg, az));
    }
}
