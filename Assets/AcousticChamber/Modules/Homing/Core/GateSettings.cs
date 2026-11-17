using System;

namespace GPL.Homing
{
    /// <summary>
    /// Head gate tolerances and the fixation dot's elevation. Defaults: PO 2026-11-06 ("looser" than HW2's 5 deg / 10 deg /
    /// 1 s); the dot 1 deg above eye level, against the head's natural drop (PO 2026-11-18).
    /// </summary>
    public sealed class GateSettings
    {
        /// <summary>Default elevation of the fixation dot (deg; positive = above eye level; PO 2026-11-18: +1, the head drops while resting).</summary>
        public const float DefaultDotElevationDeg = 1f;

        /// <summary>Allowed |yaw| from forward (deg).</summary>
        public float YawToleranceDeg = 10f;
        /// <summary>Allowed |pitch − dot elevation| (deg): the tolerance is centred on the dot.</summary>
        public float PitchToleranceDeg = 15f;
        /// <summary>Time the head must stay inside the tolerance without a break (s).</summary>
        public float HoldSec = 0.5f;
        /// <summary>The hint ("Look at the dot") shows after this long without passing (s); 0 = never.</summary>
        public float HintAfterSec = 4f;
        /// <summary>Elevation of the fixation dot from eye level (deg; positive = up), -45..45.</summary>
        public float DotElevationDeg = DefaultDotElevationDeg;

        public static GateSettings Defaults => new GateSettings();

        /// <summary>Throws when a tolerance or time is negative or not a number, or the dot elevation is outside -45..45.</summary>
        public GateSettings Validated()
        {
            Check(YawToleranceDeg, nameof(YawToleranceDeg));
            Check(PitchToleranceDeg, nameof(PitchToleranceDeg));
            Check(HoldSec, nameof(HoldSec));
            Check(HintAfterSec, nameof(HintAfterSec));
            if (float.IsNaN(DotElevationDeg) || DotElevationDeg < -45f || DotElevationDeg > 45f)
                throw new ArgumentOutOfRangeException(nameof(DotElevationDeg), DotElevationDeg, "must be -45..45");
            return this;
        }

        static void Check(float v, string name)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v < 0f) throw new ArgumentOutOfRangeException(name, v, "must be 0 or more");
        }
    }
}
