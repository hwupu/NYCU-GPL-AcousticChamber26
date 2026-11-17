using System.Collections.Generic;
using GPL.Params;

namespace GPL.Homing
{
    /// <summary>
    /// Preset parameters of the head gate (ADR-0002): a chamber adds <see cref="GateDefs"/> to its schema and reads them back
    /// with <see cref="ReadGate"/>. Defaults = <see cref="GateSettings.Defaults"/>.
    /// </summary>
    public static class HomingParams
    {
        public const string YawTolerance = "gate.yaw_tol_deg";
        public const string PitchTolerance = "gate.pitch_tol_deg";
        public const string Hold = "gate.hold_s";
        public const string HintAfter = "gate.hint_after_s";
        public const string DotElevation = "gate.dot_elevation_deg";

        public static IEnumerable<ParamDef> GateDefs(string group = "Head gate")
        {
            var d = GateSettings.Defaults;
            yield return ParamDef.Float(YawTolerance, d.YawToleranceDeg, 1f, 45f, 1f, "Yaw tolerance", "°", group)
                .Describe("head may turn this far left or right of forward");
            yield return ParamDef.Float(PitchTolerance, d.PitchToleranceDeg, 1f, 45f, 1f, "Pitch tolerance", "°", group)
                .Describe("head may tilt this far up or down");
            yield return ParamDef.Float(Hold, d.HoldSec, 0f, 3f, 0.1f, "Hold", "s", group)
                .Describe("time inside the tolerance without a break");
            yield return ParamDef.Float(HintAfter, d.HintAfterSec, 0f, 30f, 1f, "Hint after", "s", group)
                .Describe("\"Look at the dot\" after this long; 0 = never");
            yield return ParamDef.Float(DotElevation, d.DotElevationDeg, -30f, 30f, 0.5f, "Dot elevation", "°", group)
                .Describe("dot above (+) or below (−) eye level; the pitch tolerance is centred on it");
        }

        /// <summary>Gate settings from a parameter set; keys missing from its schema keep their defaults.</summary>
        public static GateSettings ReadGate(ParameterSet values)
        {
            var s = GateSettings.Defaults;
            if (values == null) return s;
            var schema = values.Schema;
            if (schema.Contains(YawTolerance)) s.YawToleranceDeg = values.GetFloat(YawTolerance);
            if (schema.Contains(PitchTolerance)) s.PitchToleranceDeg = values.GetFloat(PitchTolerance);
            if (schema.Contains(Hold)) s.HoldSec = values.GetFloat(Hold);
            if (schema.Contains(HintAfter)) s.HintAfterSec = values.GetFloat(HintAfter);
            if (schema.Contains(DotElevation)) s.DotElevationDeg = values.GetFloat(DotElevation);
            return s.Validated();
        }
    }
}
