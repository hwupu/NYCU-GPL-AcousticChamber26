using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GPL.Core;

namespace GPL.Acoustics
{
    /// <summary>One row of an RT60 report: one condition, source position, repeat, ear and band.</summary>
    public sealed class Rt60Row
    {
        public string Condition;
        public double TargetRt60;   // NaN for a sweep step
        public double Reflection;   // uniform coefficient sent to the plugin; NaN for a preset
        public int Position;        // 1-based
        public double AzimuthDeg, DistanceM;
        public int Repeat;          // 1-based
        public string Ear;          // L, R, mean
        public BandDecay Band;
    }

    /// <summary>
    /// The traceable record of one RT60 run (measurement, calibration sweep or block-start check): ordered metadata for the
    /// <c>.txt</c> header, all rows for the <c>.csv</c>, and the summary. Built by the runtime runner; no file access here.
    /// </summary>
    public sealed class Rt60Report
    {
        public const string KindMeasurement = "measurement";
        public const string KindSweep = "sweep";
        public const string KindBlockStart = "block_start";

        public static readonly string[] Columns =
        {
            "report_id", "kind", "chamber", "platform", "room_m", "room_kind", "condition", "target_rt60_s", "reflection", "position",
            "azimuth_deg", "distance_m", "repeat", "ear", "band_hz", "edt_s", "t20_s", "t30_s", "r_edt", "r_t20", "r_t30",
            "curvature_pct", "peak_to_noise_db", "edc_depth_db", "valid_t20", "valid_t30", "note"
        };

        public string ReportId { get; }
        public string Kind { get; }
        public string Chamber { get; }
        public string Platform { get; }
        public double RoomWidth { get; }
        public double RoomDepth { get; }
        public double RoomHeight { get; }
        /// <summary><c>visual_room</c> (the shoebox has the size of the visible room) or <c>open_space_default</c>.</summary>
        public string RoomKind { get; }
        public List<KeyValuePair<string, string>> Meta { get; } = new List<KeyValuePair<string, string>>();
        public List<Rt60Row> Rows { get; } = new List<Rt60Row>();
        public List<string> Problems { get; } = new List<string>();

        public string Room => Rt60Protocol.RoomText(RoomWidth, RoomDepth, RoomHeight);

        public Rt60Report(string reportId, string kind, string chamber, string platform, double w, double d, double h, bool openSpace)
        {
            ReportId = reportId; Kind = kind; Chamber = chamber; Platform = platform;
            RoomWidth = w; RoomDepth = d; RoomHeight = h;
            RoomKind = openSpace ? "open_space_default" : "visual_room";
        }

        /// <summary><c>RT60_HiddenSource_20261115-142301</c></summary>
        public static string FileStem(string chamber, DateTime local) =>
            "RT60_" + Sanitize(chamber) + "_" + local.ToString("yyyyMMdd-HHmmss", Fmt.Inv);

        public void AddMeta(string key, string value) => Meta.Add(new KeyValuePair<string, string>(key, value ?? ""));

        /// <summary>Adds the L, R and ear-mean rows of one recording (arrays from <see cref="ImpulseResponseAnalysis.Analyze"/>).</summary>
        public void Add(string condition, double target, double reflection, int position, double azimuth, double distance, int repeat,
                        BandDecay[] left, BandDecay[] right)
        {
            if (left == null || right == null || left.Length != right.Length) throw new ArgumentException("left and right need the same bands");
            for (int i = 0; i < left.Length; i++)
            {
                Rows.Add(Row(condition, target, reflection, position, azimuth, distance, repeat, "L", left[i]));
                Rows.Add(Row(condition, target, reflection, position, azimuth, distance, repeat, "R", right[i]));
                Rows.Add(Row(condition, target, reflection, position, azimuth, distance, repeat, "mean", ImpulseResponseAnalysis.Mean(left[i], right[i])));
            }
        }

        static Rt60Row Row(string c, double t, double refl, int p, double az, double dist, int rep, string ear, BandDecay b) =>
            new Rt60Row { Condition = c, TargetRt60 = t, Reflection = refl, Position = p, AzimuthDeg = az, DistanceM = dist, Repeat = rep, Ear = ear, Band = b };

        /// <summary>Conditions in the order they were measured.</summary>
        public List<string> Conditions()
        {
            var list = new List<string>();
            foreach (var r in Rows) if (!list.Contains(r.Condition)) list.Add(r.Condition);
            return list;
        }

        /// <summary>T_mid (ear mean, T30) of every recording of a condition, in order.</summary>
        public List<double> TmidValues(string condition, bool t30 = true)
        {
            var values = new List<double>();
            foreach (var g in Rows.Where(r => r.Condition == condition && r.Ear == "mean").GroupBy(r => r.Position * 1000 + r.Repeat))
                values.Add(Rt60Protocol.Tmid(g.Select(r => r.Band).ToList(), t30));
            return values;
        }

        /// <summary>Mean, sample SD, min, max and count of the valid T_mid values of a condition (NaN when none).</summary>
        public Rt60Stats TmidStats(string condition, bool t30 = true) => Rt60Stats.Of(TmidValues(condition, t30));

        /// <summary>Mean over positions and repeats of the ear-mean T30 (or T20) of one band.</summary>
        public Rt60Stats BandStats(string condition, int bandHz, bool t30 = true) =>
            Rt60Stats.Of(Rows.Where(r => r.Condition == condition && r.Ear == "mean" && r.Band.BandHz == bandHz)
                         .Select(r => t30 ? (r.Band.ValidT30 ? r.Band.T30 : double.NaN) : (r.Band.ValidT20 ? r.Band.T20 : double.NaN)));

        public string Csv()
        {
            var sb = new StringBuilder();
            sb.Append(GPL.Core.Csv.Join(Columns)).Append('\n');
            foreach (var r in Rows)
            {
                var b = r.Band;
                sb.Append(GPL.Core.Csv.Join(new[]
                {
                    ReportId, Kind, Chamber, Platform, Room, RoomKind, r.Condition, Fmt.F(r.TargetRt60, 3), Fmt.F(r.Reflection, 4),
                    Fmt.I(r.Position), Fmt.F(r.AzimuthDeg, 1), Fmt.F(r.DistanceM, 2), Fmt.I(r.Repeat), r.Ear, b.Label,
                    Fmt.F(b.Edt, 4), Fmt.F(b.T20, 4), Fmt.F(b.T30, 4), Fmt.F(b.REdt, 5), Fmt.F(b.RT20, 5), Fmt.F(b.RT30, 5),
                    Fmt.F(b.CurvaturePct, 2), Fmt.F(b.PeakToNoiseDb, 1), Fmt.F(b.EdcDepthDb, 1), Fmt.B(b.ValidT20), Fmt.B(b.ValidT30), b.Note
                })).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>The <c>.txt</c>: header (method, setup, versions), problems, then the summary per condition.</summary>
        public string Text()
        {
            var sb = new StringBuilder();
            sb.Append("RT60 report ").Append(ReportId).Append('\n');
            sb.Append(new string('=', 60)).Append('\n');
            Line(sb, "kind", Kind);
            Line(sb, "chamber", Chamber);
            Line(sb, "platform", Platform);
            Line(sb, "room_m", Room + " (W x D x H)");
            Line(sb, "room_kind", RoomKind == "open_space_default"
                ? "open_space_default: the chamber has no walls; the shoebox uses the default open-space size " +
                  Rt60Protocol.RoomText(Rt60Protocol.OpenSpaceWidth, Rt60Protocol.OpenSpaceDepth, Rt60Protocol.OpenSpaceHeight) + " m"
                : "visual_room: the shoebox has the size of the visible room");
            Line(sb, "standard", Rt60Protocol.Standard);
            Line(sb, "excitation", Rt60Protocol.Excitation);
            Line(sb, "filters", Rt60Protocol.Filters);
            Line(sb, "evaluation", Rt60Protocol.Evaluation);
            Line(sb, "single_number", Rt60Protocol.SingleNumber);
            foreach (var kv in Meta) Line(sb, kv.Key, kv.Value);
            sb.Append('\n');
            var problems = Problems.Concat(QualityProblems()).ToList();
            if (problems.Count > 0)
            {
                sb.Append("PROBLEMS\n");
                foreach (var p in problems) sb.Append("- ").Append(p).Append('\n');
                sb.Append('\n');
            }
            sb.Append("SUMMARY (ear mean; mean +/- SD [min, max] over positions x repeats; n = valid recordings)\n");
            foreach (var c in Conditions())
            {
                var first = Rows.First(r => r.Condition == c);
                sb.Append('\n').Append(c);
                if (!double.IsNaN(first.TargetRt60)) sb.Append("  target ").Append(Fmt.F(first.TargetRt60, 2)).Append(" s");
                if (!double.IsNaN(first.Reflection)) sb.Append("  reflection ").Append(Fmt.F(first.Reflection, 4));
                sb.Append('\n');
                var tmid = TmidStats(c);
                sb.Append("  T_mid (T30)  ").Append(tmid.Text()).Append('\n');
                sb.Append("  T_mid (T20)  ").Append(TmidStats(c, false).Text()).Append('\n');
                if (!double.IsNaN(first.TargetRt60) && tmid.N > 0)
                    sb.Append("  deviation    ").Append(Fmt.F(100.0 * (tmid.Mean / first.TargetRt60 - 1.0), 1)).Append(" % of target\n");
                int flagged = TmidFlaggedCount(c, out int recordings);
                if (flagged > 0)
                    sb.Append("  flagged      ").Append(Fmt.I(flagged)).Append(" of ").Append(Fmt.I(recordings))
                      .Append(" recordings at 500 Hz / 1 kHz: ").Append(string.Join(", ", TmidFlagReasons(c))).Append('\n');
                foreach (int band in Rows.Where(r => r.Condition == c && r.Ear == "mean").Select(r => r.Band.BandHz).Distinct())
                {
                    var bs = BandStats(c, band);
                    string label = band == 0 ? "broadband" : band + " Hz";
                    sb.Append("  ").Append(label.PadRight(11)).Append(" T30 ").Append(bs.Text())
                      .Append("   T20 ").Append(BandStats(c, band, false).Text()).Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>Ear-mean rows at 500 Hz and 1 kHz of a condition (the T_mid bands).</summary>
        IEnumerable<Rt60Row> TmidRows(string condition) =>
            Rows.Where(r => r.Condition == condition && r.Ear == "mean" && (r.Band.BandHz == 500 || r.Band.BandHz == 1000));

        /// <summary>Recordings of a condition whose T_mid bands (500 Hz or 1 kHz, either ear) are flagged.</summary>
        public int TmidFlaggedCount(string condition, out int recordings)
        {
            var groups = TmidRows(condition).GroupBy(r => r.Position * 1000 + r.Repeat).ToList();
            recordings = groups.Count;
            return groups.Count(g => g.Any(r => r.Band.Flagged));
        }

        /// <summary>Kinds of flags in the T_mid bands of a condition: curved decay, non-linear fit, below the filter limit.</summary>
        public List<string> TmidFlagReasons(string condition)
        {
            var notes = TmidRows(condition).Where(r => r.Band.Flagged).Select(r => r.Band.Note ?? "").ToList();
            var reasons = new List<string>();
            if (notes.Any(n => n.Contains("curved decay"))) reasons.Add("curved decay (|C| > " + Fmt.F(DecayAnalysis.MaxCurvaturePct, 0) + " %)");
            if (notes.Any(n => n.Contains("non-linear fit"))) reasons.Add("non-linear fit (r > " + Fmt.F(DecayAnalysis.MaxFitCorrelation, 2) + ")");
            if (notes.Any(n => n.Contains("filter limit"))) reasons.Add("below the octave filter limit (B T < " + Fmt.F(DecayAnalysis.MinBandwidthTimeProduct, 0) + ")");
            return reasons;
        }

        /// <summary>Problems found in the data: flagged T_mid recordings, and conditions without reflections (no reverberation).</summary>
        public List<string> QualityProblems()
        {
            var list = new List<string>();
            foreach (var c in Conditions())
            {
                var first = Rows.First(r => r.Condition == c);
                if (!double.IsNaN(first.Reflection) && first.Reflection <= 0)
                {
                    list.Add(c + ": reflection 0, no reverberation is rendered; the decay values are the direct sound and the octave filters' " +
                             "ring-down, not a reverberation time (report it as no measurable reverberation).");
                    continue;
                }
                int flagged = TmidFlaggedCount(c, out int n);
                if (flagged > 0)
                    list.Add(c + ": T_mid flagged in " + Fmt.I(flagged) + " of " + Fmt.I(n) + " recordings (" + string.Join(", ", TmidFlagReasons(c)) +
                             "); the decay is not one exponential, so T_mid depends on the evaluation range; cite with this note.");
            }
            return list;
        }

        static void Line(StringBuilder sb, string key, string value) => sb.Append(key.PadRight(16)).Append(": ").Append(value).Append('\n');

        static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? "") sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            return sb.Length > 0 ? sb.ToString() : "Chamber";
        }
    }

    /// <summary>Mean, sample SD, min, max of the non-NaN values.</summary>
    public struct Rt60Stats
    {
        public int N;
        public double Mean, Sd, Min, Max;

        public static Rt60Stats Of(IEnumerable<double> values)
        {
            var v = values.Where(x => !double.IsNaN(x) && !double.IsInfinity(x)).ToList();
            var s = new Rt60Stats { N = v.Count, Mean = double.NaN, Sd = double.NaN, Min = double.NaN, Max = double.NaN };
            if (v.Count == 0) return s;
            s.Mean = v.Average();
            s.Min = v.Min();
            s.Max = v.Max();
            s.Sd = v.Count > 1 ? Math.Sqrt(v.Sum(x => (x - s.Mean) * (x - s.Mean)) / (v.Count - 1)) : 0.0;
            return s;
        }

        public string Text() => N == 0 ? "n/a (no valid value)"
            : Fmt.F(Mean, 3) + " +/- " + Fmt.F(Sd, 3) + " s [" + Fmt.F(Min, 3) + ", " + Fmt.F(Max, 3) + "] n=" + N;
    }
}
