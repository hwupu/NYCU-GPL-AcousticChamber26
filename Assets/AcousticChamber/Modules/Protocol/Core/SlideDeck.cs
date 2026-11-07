using System;
using GPL.Core;
using GPL.Session;

namespace GPL.Protocol
{
    /// <summary>
    /// Navigation through a list of instruction slides (generalized from HW2's Onboarding): Back and Next, the last Next
    /// ends the deck. The slide on screen is kept in the session record (custom key <c>slide.&lt;stage&gt;</c>), so a resume
    /// reopens the slide the participant was on. Pure; the screen calls <see cref="Back"/> / <see cref="Next"/> and logs
    /// every slide shown.
    /// </summary>
    public sealed class SlideDeck
    {
        /// <summary>How a slide came on screen (the note of its <c>slide</c> row).</summary>
        public static class How
        {
            public const string First = "first";
            public const string Next = "next";
            public const string Back = "back";
            public const string Resume = "resume";
            /// <summary>Shown again after another screen opened from it (e.g. the language choice from the recenter slide).</summary>
            public const string Again = "again";
        }

        /// <summary>Custom key of the slide on screen in a stage: <c>slide.&lt;stage&gt;</c> = 0-based index.</summary>
        public static string IndexKey(string stage) => "slide." + stage;

        public int Count { get; }
        /// <summary>0-based slide on screen; <see cref="Count"/> once the deck has ended.</summary>
        public int Index { get; private set; }
        public bool Done => Index >= Count;
        public bool IsFirst => Index == 0;
        public bool IsLast => Index == Count - 1;

        /// <param name="start">Slide to open on (clamped to the deck).</param>
        public SlideDeck(int count, int start = 0)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            Index = count == 0 ? 0 : Math.Max(0, Math.Min(start, count - 1));
        }

        /// <summary>One slide back; false on the first slide.</summary>
        public bool Back()
        {
            if (Done || Index == 0) return false;
            Index--;
            return true;
        }

        /// <summary>One slide on; on the last slide it ends the deck (<see cref="Done"/>) and returns false.</summary>
        public bool Next()
        {
            if (Done) return false;
            Index++;
            return !Done;
        }

        /// <summary>The slide to open a stage on: the stored one on a resume, else the first.</summary>
        public static int Restore(SessionRecord rec, string stage, int count)
        {
            if (rec == null || count == 0) return 0;
            if (!rec.Custom.TryGetValue(IndexKey(stage), out var v) || !int.TryParse(v, out int i)) return 0;
            return Math.Max(0, Math.Min(i, count - 1));
        }

        /// <summary>Stores the slide on screen (call before saving the record).</summary>
        public static void Store(SessionRecord rec, string stage, int index)
        {
            if (rec != null) rec.Custom[IndexKey(stage)] = Fmt.I(index);
        }

        /// <summary>Note of a <c>slide</c> row: <c>&lt;id&gt; 2/5 next</c>.</summary>
        public string Note(string id, string how) => (id ?? "") + " " + Fmt.I(Index + 1) + "/" + Fmt.I(Count) + " " + how;
    }
}
