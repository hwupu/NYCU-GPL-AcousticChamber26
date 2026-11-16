using System;
using System.Collections.Generic;
using System.IO;
using GPL.Core;
using UnityEngine;

namespace GPL.Acoustics
{
    /// <summary>
    /// Calibration tables used on this headset without a rebuild (PB-27 alternative, PO 2026-11-17). "Use on this headset" in the
    /// C04 dialog copies a completed sweep CSV to <c>persistentDataPath/Calibration/Tables/RT60_&lt;platform&gt;_&lt;W&gt;x&lt;D&gt;x&lt;H&gt;.csv</c>
    /// (one per size; a newer one replaces it). Every <see cref="ReverbTuner"/> reads them with the same parser as the Editor import
    /// (<see cref="Rt60Calibration.FromSweepCsv"/>), so the table is identical to its imported version; an imported table for the same
    /// platform and size always wins (<see cref="Rt60TableChoice"/>). The main protocol stays calibrate -> copy -> import -> build.
    /// </summary>
    public static class Rt60HeadsetTables
    {
        public static string Directory => Path.Combine(Application.persistentDataPath, "Calibration", "Tables");

        /// <summary>
        /// Uses a completed sweep on this headset: parses it (throws with the reason when it is not a usable sweep) and copies it to
        /// <see cref="Directory"/>. Returns the table.
        /// </summary>
        public static Rt60Calibration Use(string sweepCsvPath)
        {
            string text = File.ReadAllText(sweepCsvPath);
            string source = Rt60TableChoice.SourceName(text);
            if (source == "") source = Path.GetFileName(sweepCsvPath);
            var cal = Rt60Calibration.FromSweepCsv(text, source);
            System.IO.Directory.CreateDirectory(Directory);
            string path = Path.Combine(Directory, Rt60TableChoice.HeadsetFileName(cal.Platform, cal.RoomWidth, cal.RoomDepth, cal.RoomHeight));
            AtomicFile.WriteAllText(path, text);
            Debug.Log("[GPL] RT60: table used on this headset (not imported): " + cal.Describe() + " -> " + path);
            return cal;
        }

        /// <summary>All headset tables (unusable files are skipped with a warning).</summary>
        public static List<Rt60Calibration> LoadAll()
        {
            var list = new List<Rt60Calibration>();
            string dir;
            try { dir = Directory; } catch (Exception) { return list; } // persistentDataPath may not be ready in edit-mode tests
            if (!System.IO.Directory.Exists(dir)) return list;
            foreach (var path in System.IO.Directory.GetFiles(dir, "*.csv"))
            {
                try
                {
                    string text = File.ReadAllText(path);
                    string source = Rt60TableChoice.SourceName(text);
                    list.Add(Rt60Calibration.FromSweepCsv(text, source != "" ? source : Path.GetFileName(path)));
                }
                catch (Exception e) { Debug.LogWarning("[GPL] RT60 headset table " + Path.GetFileName(path) + " is unusable: " + e.Message); }
            }
            return list;
        }
    }
}
