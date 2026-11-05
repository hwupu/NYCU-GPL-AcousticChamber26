using System;

namespace GPL.Homing
{
    /// <summary>
    /// Head gate tolerances. Defaults: PO 2026-11-06 ("looser" than HW2's 5 deg / 10 deg / 1 s).
    /// </summary>
    public sealed class GateSettings
    {
        /// <summary>Allowed |yaw| from forward (deg).</summary>
        public float YawToleranceDeg = 10f;
        /// <summary>Allowed |pitch| from eye level (deg).</summary>
        public float PitchToleranceDeg = 15f;
        /// <summary>Time the head must stay inside the tolerance without a break (s).</summary>
        public float HoldSec = 0.5f;
        /// <summary>The hint ("Look at the dot") shows after this long without passing (s); 0 = never.</summary>
        public float HintAfterSec = 4f;

        public static GateSettings Defaults => new GateSettings();

        /// <summary>Throws when a value is negative or not a number.</summary>
        public GateSettings Validated()
        {
            Check(YawToleranceDeg, nameof(YawToleranceDeg));
            Check(PitchToleranceDeg, nameof(PitchToleranceDeg));
            Check(HoldSec, nameof(HoldSec));
            Check(HintAfterSec, nameof(HintAfterSec));
            return this;
        }

        static void Check(float v, string name)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v < 0f) throw new ArgumentOutOfRangeException(name, v, "must be 0 or more");
        }
    }
}
