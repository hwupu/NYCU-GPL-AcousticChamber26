using System;
using System.Collections.Generic;
using GPL.Core;

namespace AcousticChamber.Chambers.HumPerHW2
{
    /// <summary>The three acoustic conditions (Design.md "Conditions"); the names are the values of the <c>condition</c> column.</summary>
    public static class Conditions
    {
        public const string Quiet = "quiet";
        public const string Noisy = "noisy";
        public const string Reverb = "reverb";
        public const string Practice = "practice";

        /// <summary>In the order of their index (the trial-list seed offset): quiet 0, noisy 1, reverb 2.</summary>
        public static readonly IReadOnlyList<string> All = new[] { Quiet, Noisy, Reverb };

        public static int Index(string condition)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] == condition) return i;
            throw new ArgumentException("unknown condition: " + condition);
        }

        public static bool IsKnown(string condition) => condition == Quiet || condition == Noisy || condition == Reverb;

        /// <summary>Noisy and Reverb have the background; Quiet and practice do not.</summary>
        public static bool HasBackground(string condition) => condition == Noisy || condition == Reverb;

        /// <summary>The preset key of the condition's RT60 target (practice uses the quiet acoustics).</summary>
        public static string Rt60Key(string condition) => "rt60." + (condition == Practice ? Quiet : condition) + "_s";
    }

    /// <summary>
    /// Order of the conditions per participant (Design.md "Order", Q19 / Q29). <c>latin_square</c>: row = (Study ID number − 1)
    /// mod 3 of quiet-noisy-reverb, noisy-reverb-quiet, reverb-quiet-noisy; each condition once in each position, carry-over not
    /// balanced. <c>fixed</c>: HW2's quiet, noisy, reverb. A Study ID that is not <c>S0001</c>-style (Debug runs) gets row 1.
    /// </summary>
    public static class ConditionOrder
    {
        public const string LatinSquare = "latin_square";
        public const string Fixed = "fixed";
        public static readonly string[] Modes = { LatinSquare, Fixed };

        public static readonly IReadOnlyList<IReadOnlyList<string>> Rows = new IReadOnlyList<string>[]
        {
            new[] { Conditions.Quiet, Conditions.Noisy, Conditions.Reverb },
            new[] { Conditions.Noisy, Conditions.Reverb, Conditions.Quiet },
            new[] { Conditions.Reverb, Conditions.Quiet, Conditions.Noisy },
        };

        /// <summary>0-based Latin-square row for a Study ID (S0001 → 0, S0002 → 1, S0003 → 2, S0004 → 0); 0 when not a formatted ID.</summary>
        public static int Row(string studyId)
        {
            if (!StudyId.IsFormatted(studyId)) return 0;
            int n = int.Parse(studyId.Substring(StudyId.Prefix.Length), Fmt.Inv);
            return (n - 1) % Rows.Count;
        }

        public static string[] For(string mode, string studyId)
        {
            if (mode == Fixed) return Copy(Rows[0]);
            if (mode == LatinSquare) return Copy(Rows[Row(studyId)]);
            throw new ArgumentException("unknown condition_order: " + mode);
        }

        /// <summary>For the session file (<c>custom.condition_order</c>): <c>noisy,reverb,quiet</c>.</summary>
        public static string Format(IReadOnlyList<string> order) => string.Join(",", order);

        /// <summary>Parses <see cref="Format"/>; false unless it is a permutation of the three conditions.</summary>
        public static bool TryParse(string text, out string[] order)
        {
            order = string.IsNullOrEmpty(text) ? new string[0] : text.Split(',');
            if (order.Length != Conditions.All.Count) return false;
            var seen = new HashSet<string>();
            foreach (var c in order) if (!Conditions.IsKnown(c) || !seen.Add(c)) return false;
            return true;
        }

        static string[] Copy(IReadOnlyList<string> row)
        {
            var a = new string[row.Count];
            for (int i = 0; i < a.Length; i++) a[i] = row[i];
            return a;
        }
    }
}
