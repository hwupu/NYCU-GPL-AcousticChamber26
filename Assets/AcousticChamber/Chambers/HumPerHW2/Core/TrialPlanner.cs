using System;
using System.Collections.Generic;
using GPL.Core;

namespace AcousticChamber.Chambers.HumPerHW2
{
    /// <summary>One trial: where the picture is, where the sound starts, on which circle (Design.md "Trial").</summary>
    public struct TrialSpec
    {
        public string Condition;
        /// <summary>1-based within the block.</summary>
        public int Index;
        public float VisualAzDeg;
        public float AudioStartAzDeg;
        public float RadiusM;

        public override string ToString() =>
            Condition + " " + Index + ": visual " + Fmt.F(VisualAzDeg) + ", audio start " + Fmt.F(AudioStartAzDeg) + ", r " + Fmt.F(RadiusM);
    }

    /// <summary>
    /// HW2 trial lists (HW2 <c>TrialPlanner</c>, Design.md "Trial"): per condition, audio starts +a, −a, +a, −a, +a, −a; picture side
    /// left, left, right, right, left, right; each side's three azimuths once per condition, shuffled with the seed
    /// <c>session seed + condition index</c> (quiet 0, noisy 1, reverb 2), so the positions do not depend on the order. Uses the
    /// GPL.Core <see cref="Rng"/> (same lists on every platform; HW2 used System.Random, so the lists differ from HW2's for the same
    /// seed).
    /// </summary>
    public static class TrialPlanner
    {
        /// <summary>true = picture on the right (positive azimuth).</summary>
        public static readonly bool[] VisualRightSequence = { false, false, true, true, false, true };

        public static int TrialsPerCondition => VisualRightSequence.Length;

        /// <param name="poolDeg">Right-side azimuths (positive), mirrored for the left; HW2 7.5, 22.5, 37.5.</param>
        public static List<TrialSpec> Plan(string condition, int sessionSeed, IReadOnlyList<float> poolDeg, float audioStartDeg, float radiusM)
        {
            if (poolDeg == null || poolDeg.Count == 0) throw new ArgumentException("empty azimuth pool");
            var rng = new Rng(unchecked(sessionSeed + Conditions.Index(condition)));
            var right = new List<float>();
            foreach (float a in poolDeg) right.Add(Math.Abs(a));
            var left = new List<float>();
            foreach (float a in right) left.Add(-a);
            rng.Shuffle(left);
            rng.Shuffle(right);

            var trials = new List<TrialSpec>();
            int l = 0, r = 0;
            for (int i = 0; i < VisualRightSequence.Length; i++)
            {
                float visual = VisualRightSequence[i] ? right[r++ % right.Count] : left[l++ % left.Count];
                trials.Add(new TrialSpec
                {
                    Condition = condition,
                    Index = i + 1,
                    VisualAzDeg = visual,
                    AudioStartAzDeg = i % 2 == 0 ? audioStartDeg : -audioStartDeg,
                    RadiusM = radiusM,
                });
            }
            return trials;
        }

        /// <summary>Practice trial (HW2): picture at <paramref name="visualDeg"/>, sound from <paramref name="audioStartDeg"/>, closer circle.</summary>
        public static TrialSpec Practice(int index, float visualDeg, float audioStartDeg, float radiusM) => new TrialSpec
        {
            Condition = Conditions.Practice, Index = index, VisualAzDeg = visualDeg, AudioStartAzDeg = audioStartDeg, RadiusM = radiusM,
        };

        /// <summary>Parses <c>7.5,22.5,37.5</c> (invariant culture); false on an empty or malformed list.</summary>
        public static bool TryParsePool(string text, out List<float> pool)
        {
            pool = new List<float>();
            if (string.IsNullOrWhiteSpace(text)) return false;
            foreach (var part in text.Split(','))
            {
                if (!Fmt.TryFloat(part.Trim(), out float v)) return false;
                pool.Add(v);
            }
            return pool.Count > 0;
        }
    }
}
