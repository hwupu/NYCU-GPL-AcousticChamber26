namespace AcousticChamber.App
{
    /// <summary>The chamber menu's volume line (PB-20b): "Audio level: 10 (target 8)". Pure, so the wording is tested.</summary>
    public static class VolumeStatus
    {
        /// <summary>
        /// <paramref name="current"/> -1 = not readable (Editor); <paramref name="target"/> -1 = the chamber has no target.
        /// </summary>
        public static string Line(int current, int target) =>
            "Audio level: " + (current < 0 ? "not readable" : current.ToString(System.Globalization.CultureInfo.InvariantCulture)) +
            (target < 0 ? "" : " (target " + target.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");

        /// <summary>True when the line should warn: both known and different.</summary>
        public static bool Off(int current, int target) => current >= 0 && target >= 0 && current != target;
    }
}
