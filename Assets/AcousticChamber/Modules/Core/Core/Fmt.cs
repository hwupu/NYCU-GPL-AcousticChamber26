using System;
using System.Globalization;

namespace GPL.Core
{
    /// <summary>Culture-invariant number formatting and parsing for logs and text files (a German or Chinese locale must not turn 0.5 into "0,5").</summary>
    public static class Fmt
    {
        public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Up to three decimals, no trailing zeros (HW2 log format). NaN and infinity become an empty field.</summary>
        public static string F(float v) => float.IsNaN(v) || float.IsInfinity(v) ? "" : v.ToString("0.###", Inv);
        public static string F(double v) => double.IsNaN(v) || double.IsInfinity(v) ? "" : v.ToString("0.###", Inv);
        public static string F(double v, int decimals) =>
            double.IsNaN(v) || double.IsInfinity(v) ? "" : v.ToString("0." + new string('#', Math.Max(1, decimals)), Inv);

        /// <summary>Round-trip precision for values stored in state files (presets, sessions).</summary>
        public static string R(float v) => v.ToString("R", Inv);
        public static string R(double v) => v.ToString("R", Inv);

        public static string I(int v) => v.ToString(Inv);
        public static string I(long v) => v.ToString(Inv);
        public static string B(bool v) => v ? "true" : "false";

        /// <summary>ISO-like local timestamp with milliseconds, e.g. 2026-11-05T14:03:07.123.</summary>
        public static string Timestamp(DateTime t) => t.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", Inv);
        public static bool TryParseTimestamp(string s, out DateTime t) =>
            DateTime.TryParseExact(s, "yyyy-MM-dd'T'HH:mm:ss.fff", Inv, DateTimeStyles.None, out t);

        public static bool TryFloat(string s, out float v) => float.TryParse(s, NumberStyles.Float, Inv, out v);
        public static bool TryDouble(string s, out double v) => double.TryParse(s, NumberStyles.Float, Inv, out v);
        public static bool TryInt(string s, out int v) => int.TryParse(s, NumberStyles.Integer, Inv, out v);

        /// <summary>Accepts true/false (any case), 1/0, yes/no.</summary>
        public static bool TryBool(string s, out bool v)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "true": case "1": case "yes": v = true; return true;
                case "false": case "0": case "no": v = false; return true;
                default: v = false; return false;
            }
        }
    }
}
