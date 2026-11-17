using System;
using System.Collections.Generic;
using GPL.Core;

namespace AcousticChamber.Chambers.HumPerFinal
{
    /// <summary>One trial: where the radio is, where the sound starts, on which circle (Design.md "Trial").</summary>
    public struct TrialSpec
    {
        /// <summary>Cell name (<c>left80_on</c>) or <c>practice</c>.</summary>
        public string Condition;
        /// <summary>1-based position in the cell's planned list (<c>trial_index</c>; stable over restarts).</summary>
        public int Index;
        /// <summary>1-based repeat of this position pair (radio position × its start side) within the block, in the order they come.</summary>
        public int Repeat;
        public float VisualAzDeg;
        public float AudioStartAzDeg;
        public float RadiusM;

        /// <summary><c>L</c> = the sound starts left (negative azimuth), <c>R</c> = right.</summary>
        public string StartSide => AudioStartAzDeg < 0f ? "L" : "R";

        public override string ToString() =>
            Condition + " " + Index + ": visual " + Fmt.F(VisualAzDeg) + ", audio start " + Fmt.F(AudioStartAzDeg) + ", repeat " + Repeat + ", r " + Fmt.F(RadiusM);
    }

    /// <summary>
    /// Pairing sets (Design.md "Position pairs"): each radio position gets one start side, one L and one R per hemisphere.
    /// Set s = 2a + b (a: left hemisphere, b: right). a = 0: −outer from L, −inner from R; a = 1: the reverse. b = 0: +inner from
    /// L, +outer from R; b = 1: the reverse. So set 0 = outer positions start on their own side, set 3 = on the opposite side.
    /// </summary>
    public static class PairSet
    {
        public const int Count = 4;

        /// <summary>Study ID number mod 4 (S0012 → 0, S0013 → 1); 0 when not a formatted ID (Debug runs).</summary>
        public static int Of(string studyId)
        {
            if (!StudyId.IsFormatted(studyId)) return 0;
            return int.Parse(studyId.Substring(StudyId.Prefix.Length), Fmt.Inv) % Count;
        }

        /// <summary>−1 (start left, L) or +1 (start right, R) for a radio position of set <paramref name="set"/>.</summary>
        /// <param name="outer">true for the outer position of its hemisphere (larger |azimuth|).</param>
        public static int StartSign(int set, float visualDeg, bool outer)
        {
            if (set < 0 || set >= Count) throw new ArgumentOutOfRangeException(nameof(set));
            int hemi = visualDeg < 0f ? -1 : 1;
            int bit = hemi < 0 ? set / 2 : set % 2;
            int outerSign = bit == 0 ? hemi : -hemi;   // bit 0: the outer position starts on its own side
            return outer ? outerSign : -outerSign;
        }
    }

    /// <summary>
    /// Trial lists of a cell (Design.md "Trials per block"): the 4 position pairs of the pairing set (radio azimuths = the two
    /// pool values mirrored left, each with one start side) × repeats, shuffled without constraints with the seed
    /// <c>session seed + cell index</c>, so a cell's list does not depend on its block position. <see cref="TrialSpec.Repeat"/>
    /// counts the copies of a position pair in the order they come after the shuffle.
    /// </summary>
    public static class TrialPlanner
    {
        public const int PositionPairs = 4;

        public static int TrialsPerBlock(int repeats) => PositionPairs * repeats;

        /// <param name="poolDeg">The inner and outer right-side azimuths (exactly two), mirrored for the left; default 7.5, 22.5.</param>
        /// <param name="pairSet">0-3, <see cref="PairSet"/>.</param>
        public static List<TrialSpec> Plan(int cell, int sessionSeed, IReadOnlyList<float> poolDeg, float audioStartDeg, int repeats, float radiusM, int pairSet)
        {
            if (poolDeg == null || poolDeg.Count != 2) throw new ArgumentException("the azimuth pool needs exactly two values (inner, outer)");
            if (repeats < 1) throw new ArgumentException("repeats must be ≥ 1");
            float inner = Math.Min(Math.Abs(poolDeg[0]), Math.Abs(poolDeg[1]));
            float outer = Math.Max(Math.Abs(poolDeg[0]), Math.Abs(poolDeg[1]));
            if (inner == outer) throw new ArgumentException("the two pool values must differ");
            string name = Cells.Name(cell);
            float start = Math.Abs(audioStartDeg);
            var visuals = new[] { -outer, -inner, inner, outer };

            var trials = new List<TrialSpec>();
            for (int r = 0; r < repeats; r++)
                foreach (float v in visuals)
                    trials.Add(new TrialSpec
                    {
                        Condition = name, VisualAzDeg = v, RadiusM = radiusM,
                        AudioStartAzDeg = start * PairSet.StartSign(pairSet, v, Math.Abs(v) == outer),
                    });

            new Rng(unchecked(sessionSeed + cell)).Shuffle(trials);

            var seen = new Dictionary<(float, float), int>();
            for (int i = 0; i < trials.Count; i++)
            {
                var t = trials[i];
                var key = (t.VisualAzDeg, t.AudioStartAzDeg);
                seen.TryGetValue(key, out int n);
                seen[key] = ++n;
                t.Index = i + 1;
                t.Repeat = n;
                trials[i] = t;
            }
            return trials;
        }

        /// <summary>Practice trial (as HW2): radio at <paramref name="visualDeg"/>, sound from <paramref name="audioStartDeg"/>, closer circle.</summary>
        public static TrialSpec Practice(int index, float visualDeg, float audioStartDeg, float radiusM) => new TrialSpec
        {
            Condition = Cells.Practice, Index = index, Repeat = 1, VisualAzDeg = visualDeg, AudioStartAzDeg = audioStartDeg, RadiusM = radiusM,
        };

        /// <summary>Parses <c>7.5,22.5</c> (invariant culture); false on an empty or malformed list.</summary>
        public static bool TryParseList(string text, out List<float> list)
        {
            list = new List<float>();
            if (string.IsNullOrWhiteSpace(text)) return false;
            foreach (var part in text.Split(','))
            {
                if (!Fmt.TryFloat(part.Trim(), out float v)) return false;
                list.Add(v);
            }
            return list.Count > 0;
        }
    }

    /// <summary>
    /// The trial in progress, kept in the session file (<c>custom.open_trial</c> = <c>block:trial:restarts</c>, 0-based block and
    /// trial) so a resume after the app closed mid-trial can log a <c>trial_restart</c> row and keep counting <c>restarts</c>
    /// (Design.md "Pause and restart"). Cleared when the trial row is written.
    /// </summary>
    public struct OpenTrial
    {
        public int Block;
        public int Trial;
        public int Restarts;

        public bool Is(int block, int trial) => Block == block && Trial == trial;

        public string Format() => Fmt.I(Block) + ":" + Fmt.I(Trial) + ":" + Fmt.I(Restarts);

        public static bool TryParse(string text, out OpenTrial t)
        {
            t = default;
            if (string.IsNullOrEmpty(text)) return false;
            var p = text.Split(':');
            if (p.Length != 3) return false;
            if (!int.TryParse(p[0], System.Globalization.NumberStyles.Integer, Fmt.Inv, out t.Block)) return false;
            if (!int.TryParse(p[1], System.Globalization.NumberStyles.Integer, Fmt.Inv, out t.Trial)) return false;
            if (!int.TryParse(p[2], System.Globalization.NumberStyles.Integer, Fmt.Inv, out t.Restarts)) return false;
            return t.Block >= 0 && t.Trial >= 0 && t.Restarts >= 0;
        }
    }
}
