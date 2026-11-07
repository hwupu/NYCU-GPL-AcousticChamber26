using System;
using GPL.HearingLoss;

namespace AcousticChamber.App
{
    /// <summary>
    /// The hearing-loss levels of the Hearing Loss Test Panel (PO 2026-11-08), traced in docs/references/HearingLossLevels.md.
    /// Each level is the HLSim noise-notch shape labelled by its 4 kHz band: -20 / -30 / -40 are HLSim's Mild / Moderate / Severe
    /// presets unchanged; -60 / -80 are Severe x 1.5 / x 2 (our extrapolation, PO choice; not HLSim levels). Pure.
    /// </summary>
    public static class TestPanelLevels
    {
        /// <summary>Panel labels in dB, in order (the 4 kHz band of each level).</summary>
        public static readonly int[] LevelDb = { 0, -20, -30, -40, -60, -80 };

        /// <summary>Pure-tone frequencies of the panel in Hz.</summary>
        public static readonly int[] ToneHz = { 500, 1000, 2000, 4000, 8000 };

        //                                        125  250  500  1k   1.5k  2k   3k   4k   6k   8k
        static readonly int[] _severeX15 = {  0,   0, -15, -30,  -38, -45, -45, -60, -45, -30 }; // -37.5 at 1.5 kHz rounded away from 0
        static readonly int[] _severeX2  = {  0,   0, -20, -40,  -50, -60, -60, -80, -60, -40 };

        /// <summary>The 10 band gains (dB) of a level, for <see cref="ANSIS344.BandHz"/>.</summary>
        public static int[] Bands(int levelDb)
        {
            switch (levelDb)
            {
                case 0: return HearingPresets.None;
                case -20: return HearingPresets.Mild;
                case -30: return HearingPresets.Moderate;
                case -40: return HearingPresets.Severe;
                case -60: return (int[])_severeX15.Clone();
                case -80: return (int[])_severeX2.Clone();
                default: throw new ArgumentOutOfRangeException(nameof(levelDb), levelDb, "Not a level on the test panel.");
            }
        }

        /// <summary>Where a level comes from, for logs and the doc.</summary>
        public static string Source(int levelDb)
        {
            switch (levelDb)
            {
                case 0: return "none (no filtering)";
                case -20: return "HLSim Mild (20 dB HL at 4000 Hz)";
                case -30: return "HLSim Moderate (30 dB HL at 4000 Hz)";
                case -40: return "HLSim Severe ('Moderate/severe', 40 dB HL at 4000 Hz)";
                case -60: return "HLSim Severe x 1.5 (extrapolated)";
                case -80: return "HLSim Severe x 2 (extrapolated)";
                default: throw new ArgumentOutOfRangeException(nameof(levelDb), levelDb, "Not a level on the test panel.");
            }
        }
    }
}
