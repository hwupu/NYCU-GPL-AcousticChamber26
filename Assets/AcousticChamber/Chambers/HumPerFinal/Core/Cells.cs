using System;
using System.Collections.Generic;
using GPL.Core;
using GPL.HearingLoss;

namespace AcousticChamber.Chambers.HumPerFinal
{
    /// <summary>The five hearing conditions (Design.md "Hearing loss"); the names are the values of the <c>hearing</c> column.</summary>
    public static class Hearing
    {
        public const string Normal = "normal";
        public const string Left40 = "left40";
        public const string Right40 = "right40";
        public const string Left80 = "left80";
        public const string Right80 = "right80";

        public static readonly IReadOnlyList<string> All = new[] { Normal, Left40, Right40, Left80, Right80 };

        public static bool IsKnown(string h) => h == Normal || h == Left40 || h == Right40 || h == Left80 || h == Right80;

        /// <summary>HLSim Severe (−40 dB at 4 kHz): 0, 0, −10, −20, −25, −30, −30, −40, −30, −20.</summary>
        public static int[] Severe => HearingPresets.Severe;

        /// <summary>Severe × 2 (ours, −80 dB at 4 kHz): 0, 0, −20, −40, −50, −60, −60, −80, −60, −40.</summary>
        public static int[] SevereX2
        {
            get
            {
                var b = HearingPresets.Severe;
                for (int i = 0; i < b.Length; i++) b[i] *= 2;
                return b;
            }
        }

        public static Ear EarOf(string h) => h == Left40 || h == Left80 ? Ear.Left : h == Right40 || h == Right80 ? Ear.Right : Ear.Both;

        /// <summary>The per-ear bands of a condition (unilateral: the other ear normal; <c>normal</c>: all zero).</summary>
        public static HearingProfile Profile(string h)
        {
            switch (h)
            {
                case Normal: return HearingProfile.Normal;
                case Left40: return HearingProfile.For(Ear.Left, Severe);
                case Right40: return HearingProfile.For(Ear.Right, Severe);
                case Left80: return HearingProfile.For(Ear.Left, SevereX2);
                case Right80: return HearingProfile.For(Ear.Right, SevereX2);
                default: throw new ArgumentException("unknown hearing: " + h);
            }
        }

        /// <summary>The 4 kHz band gain of the impaired ear (0, −40, −80), for the note <c>left_db / right_db</c>.</summary>
        public static int LabelDb(string h) => h == Left40 || h == Right40 ? -40 : h == Left80 || h == Right80 ? -80 : 0;

        /// <summary>Curve name for the log note: <c>none</c>, <c>severe</c>, <c>severe_x2</c>.</summary>
        public static string Curve(string h) => LabelDb(h) == -40 ? "severe" : LabelDb(h) == -80 ? "severe_x2" : "none";

        /// <summary>Config-page label: Normal, L −40, R −40, L −80, R −80.</summary>
        public static string Label(string h) =>
            h == Normal ? "Normal" : (EarOf(h) == Ear.Left ? "L " : "R ") + "−" + Fmt.I(-LabelDb(h));

        /// <summary>The log note of a condition: <c>hearing=left80 left_db=-80 right_db=0 curve=severe_x2</c>.</summary>
        public static string Note(string h)
        {
            int db = LabelDb(h);
            var ear = EarOf(h);
            return "hearing=" + h + " left_db=" + Fmt.I(ear == Ear.Left ? db : 0) + " right_db=" + Fmt.I(ear == Ear.Right ? db : 0) + " curve=" + Curve(h);
        }
    }

    /// <summary>Anchor conditions: the values of the <c>anchors</c> column.</summary>
    public static class Anchors
    {
        public const string None = "none";
        public const string On = "on";
    }

    /// <summary>
    /// The 10 cells = 5 hearing × 2 anchors (Design.md "Conditions and order"). Cell index = 2 × hearing index + (anchors on ? 1 : 0):
    /// 0 normal_none, 1 normal_on, 2 left40_none, ... 9 right80_on. The name is the value of the <c>condition</c> column.
    /// </summary>
    public static class Cells
    {
        public const int Count = 10;
        public const string Practice = "practice";

        public static string Name(int cell) => HearingOf(cell) + "_" + AnchorsOf(cell);
        public static string HearingOf(int cell) => Hearing.All[Check(cell) / 2];
        public static string AnchorsOf(int cell) => Check(cell) % 2 == 1 ? Anchors.On : Anchors.None;
        public static bool HasAnchors(int cell) => Check(cell) % 2 == 1;

        public static readonly IReadOnlyList<string> All = BuildAll();

        public static int Index(string name)
        {
            for (int i = 0; i < Count; i++) if (All[i] == name) return i;
            return -1;
        }

        static int Check(int cell)
        {
            if (cell < 0 || cell >= Count) throw new ArgumentOutOfRangeException(nameof(cell), cell, "cell 0-9");
            return cell;
        }

        static string[] BuildAll()
        {
            var a = new string[Count];
            for (int i = 0; i < Count; i++) a[i] = Hearing.All[i / 2] + "_" + (i % 2 == 1 ? Anchors.On : Anchors.None);
            return a;
        }
    }

    /// <summary>
    /// Order of the cells per participant (Design.md "Order"). <c>williams</c>: balanced Latin square (Williams design, n = 10),
    /// row = Study ID number mod 10; row 0 = 0 1 9 2 8 3 7 4 6 5, row r = row 0 + r (mod 10): each cell once in each position and
    /// each cell directly after every other cell once over the 10 rows. <c>fixed</c>: cells 0-9 (Debug runs). A Study ID that is
    /// not <c>S0001</c>-style (Debug runs) gets row 0.
    /// </summary>
    public static class CellOrder
    {
        public const string Williams = "williams";
        public const string Fixed = "fixed";
        public static readonly string[] Modes = { Williams, Fixed };

        /// <summary>Row 0 of the Williams square for an even n: 0, 1, n−1, 2, n−2, ...</summary>
        public static int[] FirstRow(int n)
        {
            var r = new int[n];
            int lo = 1, hi = n - 1;
            for (int i = 1; i < n; i++) r[i] = i % 2 == 1 ? lo++ : hi--;
            return r;
        }

        public static int[] Row(int row)
        {
            var first = FirstRow(Cells.Count);
            var r = new int[Cells.Count];
            for (int i = 0; i < r.Length; i++) r[i] = (first[i] + row) % Cells.Count;
            return r;
        }

        /// <summary>Study ID number mod 10 (S0012 → 2, S0021 → 1); 0 when not a formatted ID.</summary>
        public static int RowOf(string studyId)
        {
            if (!StudyId.IsFormatted(studyId)) return 0;
            return int.Parse(studyId.Substring(StudyId.Prefix.Length), Fmt.Inv) % Cells.Count;
        }

        public static int[] For(string mode, int row)
        {
            if (mode == Fixed) { var a = new int[Cells.Count]; for (int i = 0; i < a.Length; i++) a[i] = i; return a; }
            if (mode == Williams) return Row(row);
            throw new ArgumentException("unknown condition_order: " + mode);
        }

        /// <summary>For the session file (<c>custom.condition_order</c>): the cell names, comma-separated.</summary>
        public static string Format(IReadOnlyList<int> order)
        {
            var names = new string[order.Count];
            for (int i = 0; i < names.Length; i++) names[i] = Cells.Name(order[i]);
            return string.Join(",", names);
        }

        /// <summary>Parses <see cref="Format"/>; false unless it is a permutation of the 10 cells.</summary>
        public static bool TryParse(string text, out int[] order)
        {
            var parts = string.IsNullOrEmpty(text) ? new string[0] : text.Split(',');
            order = new int[parts.Length];
            if (parts.Length != Cells.Count) return false;
            var seen = new HashSet<int>();
            for (int i = 0; i < parts.Length; i++)
            {
                order[i] = Cells.Index(parts[i]);
                if (order[i] < 0 || !seen.Add(order[i])) return false;
            }
            return true;
        }
    }

    /// <summary>Azimuth arithmetic for the free 360° adjustment.</summary>
    public static class Angles
    {
        /// <summary>Wraps to (−180, 180]: 190 → −170, −180 → 180, 540 → 180.</summary>
        public static double Wrap(double deg)
        {
            double a = deg % 360.0;
            if (a > 180.0) a -= 360.0;
            else if (a <= -180.0) a += 360.0;
            return a;
        }

        /// <summary>Shortest signed angle from <paramref name="toDeg"/> to <paramref name="fromDeg"/> (from − to), in (−180, 180].</summary>
        public static double SignedDiff(double fromDeg, double toDeg) => Wrap(fromDeg - toDeg);
    }
}
