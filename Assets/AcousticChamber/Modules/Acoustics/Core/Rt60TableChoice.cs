using System.Collections.Generic;

namespace GPL.Acoustics
{
    /// <summary>Where the calibration table of a room comes from.</summary>
    public enum Rt60TableSource
    {
        /// <summary>No table for this platform and size: the HW2 table scaled by V/S (estimate).</summary>
        None,
        /// <summary>Imported in the Editor and built into the app (the main protocol: calibrate -> copy -> import -> build).</summary>
        Imported,
        /// <summary>A C04 sweep marked "Use on this headset" (PB-27 alternative, no rebuild); not imported yet.</summary>
        Headset,
    }

    /// <summary>
    /// Picks the calibration table of a room (PB-27, PO 2026-11-17): an imported table for this platform and size always wins; a table
    /// used on this headset only fills a size that has no imported one. Unity-free; <c>ReverbTuner</c> feeds it.
    /// </summary>
    public static class Rt60TableChoice
    {
        public static Rt60Calibration Pick(IEnumerable<Rt60Calibration> imported, IEnumerable<Rt60Calibration> headset, string platform,
                                           double width, double depth, double height, out Rt60TableSource source)
        {
            foreach (var c in imported ?? new Rt60Calibration[0])
                if (c != null && c.CalibratedFor(platform, width, depth, height)) { source = Rt60TableSource.Imported; return c; }
            foreach (var c in headset ?? new Rt60Calibration[0])
                if (c != null && c.CalibratedFor(platform, width, depth, height)) { source = Rt60TableSource.Headset; return c; }
            source = Rt60TableSource.None;
            return null;
        }

        /// <summary>File name of a headset table, same stem as the imported asset: <c>RT60_Android_12x12x3p5.csv</c> (one per size).</summary>
        public static string HeadsetFileName(string platform, double width, double depth, double height) =>
            "RT60_" + platform + "_" + Rt60Protocol.RoomText(width, depth, height).Replace('.', 'p') + ".csv";

        /// <summary>
        /// The report a sweep CSV came from (its <c>report_id</c> column) + <c>.csv</c>, the source name the Editor import gives the
        /// same table; empty when the CSV has no rows.
        /// </summary>
        public static string SourceName(string csvText)
        {
            var lines = (csvText ?? "").Replace("\r", "").Split('\n');
            if (lines.Length < 2 || lines[1].Length == 0) return "";
            var head = GPL.Core.Csv.SplitLine(lines[0]);
            int i = head.IndexOf("report_id");
            var row = GPL.Core.Csv.SplitLine(lines[1]);
            return i >= 0 && i < row.Count ? row[i] + ".csv" : "";
        }
    }
}
