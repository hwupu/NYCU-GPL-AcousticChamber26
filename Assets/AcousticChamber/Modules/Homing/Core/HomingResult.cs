using System.Collections.Generic;
using System.Text;
using GPL.Core;

namespace GPL.Homing
{
    /// <summary>Names of the homing methods as logged (<c>method=...</c>).</summary>
    public static class HomingMethods
    {
        public const string HeadGate = "head_gate";
        public const string SystemRecenter = "system_recenter";
        /// <summary>The participant or experimenter long-pressed the Meta button (not requested by the app).</summary>
        public const string SystemRecenterByUser = "system_recenter_user";
    }

    public enum HomingOutcome
    {
        /// <summary>The method finished.</summary>
        Done,
        /// <summary>Cancelled before it finished (Stop dialog: back to the menu, experimenter cancel).</summary>
        Cancelled,
        /// <summary>Could not run here (no headset, e.g. in the Editor without Link).</summary>
        Unavailable,
    }

    /// <summary>
    /// What a homing method did; the chamber logs it as a <c>homing</c> row with <see cref="Note"/> in the note column
    /// (key=value pairs, invariant culture). Angles in degrees relative to the rig's forward (0 = forward, + = right / up).
    /// </summary>
    public sealed class HomingResult
    {
        public string Method = "";
        public HomingOutcome Outcome = HomingOutcome.Done;
        /// <summary>Game time the method took (s); the Stop dialog's freeze is not counted.</summary>
        public float DurationSec;
        /// <summary>Head yaw / pitch before the method (for a recenter: the correction it applied).</summary>
        public float YawBeforeDeg, PitchBeforeDeg;
        /// <summary>Head yaw / pitch after the method (the residual error).</summary>
        public float YawAfterDeg, PitchAfterDeg;
        /// <summary>Eye height above the floor-level tracking origin (m), measured after the method.</summary>
        public float EyeHeightM;
        /// <summary>Head gate only: breaks during the hold, and when the hint showed (s; -1 = never).</summary>
        public int Breaks;
        public float HintAtSec = -1f;
        /// <summary>Free text (e.g. why it was unavailable).</summary>
        public string Detail = "";

        public bool Ok => Outcome == HomingOutcome.Done;

        /// <summary>The pairs of <see cref="Note"/>, in a fixed order.</summary>
        public IEnumerable<KeyValuePair<string, string>> Pairs()
        {
            yield return P("method", Method);
            yield return P("outcome", Outcome.ToString().ToLowerInvariant());
            yield return P("duration_s", Fmt.F(DurationSec, 3));
            yield return P("yaw_before_deg", Fmt.F(YawBeforeDeg, 2));
            yield return P("pitch_before_deg", Fmt.F(PitchBeforeDeg, 2));
            yield return P("yaw_after_deg", Fmt.F(YawAfterDeg, 2));
            yield return P("pitch_after_deg", Fmt.F(PitchAfterDeg, 2));
            yield return P("eye_height_m", Fmt.F(EyeHeightM, 3));
            if (Method == HomingMethods.HeadGate)
            {
                yield return P("breaks", Fmt.I(Breaks));
                yield return P("hint_at_s", HintAtSec < 0f ? "" : Fmt.F(HintAtSec, 3));
            }
            if (!string.IsNullOrEmpty(Detail)) yield return P("detail", Detail);
        }

        /// <summary><c>method=head_gate outcome=done duration_s=0.734 ...</c> (space separated; a detail with spaces is quoted).</summary>
        public string Note()
        {
            var sb = new StringBuilder();
            foreach (var kv in Pairs())
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(kv.Key).Append('=');
                string v = kv.Value ?? "";
                if (v.IndexOf(' ') >= 0 || v.IndexOf('"') >= 0) sb.Append('"').Append(v.Replace("\"", "'")).Append('"');
                else sb.Append(v);
            }
            return sb.ToString();
        }

        public override string ToString() => Note();

        static KeyValuePair<string, string> P(string k, string v) => new KeyValuePair<string, string>(k, v);
    }
}
