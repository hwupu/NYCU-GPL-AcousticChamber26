using System;
using System.Collections.Generic;
using GPL.Core;

namespace GPL.Acoustics
{
    /// <summary>
    /// A target RT60 profile (PB-27, PO 2026-11-16): the T_mid targets a Measure run checks, and the sweep range of a calibration.
    /// <see cref="FullSweep"/> = the whole <see cref="Rt60Protocol.SweepReflections"/> grid; otherwise <see cref="SweepFor"/> picks the
    /// grid points whose expected RT60 lies within [<see cref="LowFactor"/> x the shortest target, <see cref="HighFactor"/> x the
    /// longest], plus one neighbour on each side.
    /// </summary>
    public sealed class Rt60Profile
    {
        public const double LowFactor = 0.7, HighFactor = 1.3;
        /// <summary>A sweep has at least this many points (widened symmetrically when the rule gives fewer).</summary>
        public const int MinSweepPoints = 4;

        public string Name { get; }
        public double[] Targets { get; }
        public bool FullSweep { get; }

        public Rt60Profile(string name, double[] targets, bool fullSweep)
        {
            if (targets == null || targets.Length == 0) throw new ArgumentException("a profile needs at least one target");
            Name = name ?? "";
            Targets = (double[])targets.Clone();
            Array.Sort(Targets);
            FullSweep = fullSweep;
        }

        public double ShortestTarget => Targets[0];
        public double LongestTarget => Targets[Targets.Length - 1];

        /// <summary>"0.7, 2.2 s".</summary>
        public string TargetsText() => string.Join(", ", Array.ConvertAll(Targets, t => Fmt.F(t, 2))) + " s";

        /// <summary>Condition name of a target in reports and the result dialog: <c>Target 0.7 s</c>.</summary>
        public static string TargetName(double seconds) => "Target " + Fmt.F(seconds, 2) + " s";

        /// <summary>
        /// The sweep coefficients for a room: <paramref name="expectedRt60"/> maps a coefficient to the RT60 the current table expects
        /// in this room (an imported table, or the HW2 table scaled by V/S). Ascending, a subset of <paramref name="grid"/> (default
        /// <see cref="Rt60Protocol.SweepReflections"/>).
        /// </summary>
        public double[] SweepFor(Func<double, double> expectedRt60, double[] grid = null)
        {
            grid = grid ?? Rt60Protocol.SweepReflections;
            if (FullSweep || expectedRt60 == null) return (double[])grid.Clone();
            double lo = LowFactor * ShortestTarget, hi = HighFactor * LongestTarget;
            int first = -1, last = -1;
            for (int i = 0; i < grid.Length; i++)
            {
                double t = expectedRt60(grid[i]);
                if (t < lo || t > hi) continue;
                if (first < 0) first = i;
                last = i;
            }
            if (first < 0)
            {
                // no point inside: the grid point nearest to the range (log distance)
                int best = 0; double bestD = double.MaxValue;
                for (int i = 0; i < grid.Length; i++)
                {
                    double t = Math.Max(1e-6, expectedRt60(grid[i]));
                    double d = t < lo ? Math.Log(lo / t) : Math.Log(t / hi);
                    if (d < bestD) { bestD = d; best = i; }
                }
                first = last = best;
            }
            first = Math.Max(0, first - 1);
            last = Math.Min(grid.Length - 1, last + 1);
            while (last - first + 1 < Math.Min(MinSweepPoints, grid.Length))
            {
                if (first > 0) first--;
                if (last - first + 1 < MinSweepPoints && last < grid.Length - 1) last++;
            }
            var list = new List<double>();
            for (int i = first; i <= last; i++) list.Add(grid[i]);
            return list.ToArray();
        }
    }

    /// <summary>The C04 Reverberation profiles (PO 2026-11-16). Order = the panel's order.</summary>
    public static class Rt60Profiles
    {
        public static readonly Rt60Profile Experience = new Rt60Profile("Experience", new[] { 0.7, 2.2 }, fullSweep: true);
        public static readonly Rt60Profile Hw2 = new Rt60Profile("HW2", new[] { 0.3, 0.7, 2.2 }, fullSweep: true);
        public static readonly Rt60Profile Short = new Rt60Profile("Short", new[] { 0.3, 0.5, 0.7, 1.0 }, fullSweep: false);
        public static readonly Rt60Profile Long = new Rt60Profile("Long", new[] { 1.5, 2.2, 3.0, 4.0 }, fullSweep: false);

        public static readonly Rt60Profile[] All = { Experience, Hw2, Short, Long };
    }

    /// <summary>
    /// Time left of a run for the progress dialog (PB-27): the plan's estimate, corrected by how long the finished recordings
    /// actually took (analysis, frame timing). Unity-free; the runner feeds it.
    /// </summary>
    public sealed class Rt60Eta
    {
        /// <summary>The correction is clamped to this range of the planned speed.</summary>
        public const double MinRatio = 0.75, MaxRatio = 3.0;

        readonly double _planned;
        double _plannedDone, _elapsed;

        /// <param name="plannedSeconds">The plan's estimate of the whole run (s).</param>
        public Rt60Eta(double plannedSeconds) { _planned = Math.Max(0.0, plannedSeconds); }

        /// <summary>After each recording: the planned seconds of the recordings done so far and the real time since the start.</summary>
        public void Update(double plannedDoneSeconds, double elapsedSeconds)
        {
            _plannedDone = Math.Max(0.0, Math.Min(_planned, plannedDoneSeconds));
            _elapsed = Math.Max(0.0, elapsedSeconds);
        }

        public double Fraction => _planned > 0 ? _plannedDone / _planned : 0.0;

        /// <summary>Real seconds still needed (planned remainder x the measured ratio once a recording has finished).</summary>
        public double SecondsLeft(double elapsedNow)
        {
            double ratio = _plannedDone > 0 ? Math.Max(MinRatio, Math.Min(MaxRatio, _elapsed / _plannedDone)) : 1.0;
            double finishAt = _elapsed + (_planned - _plannedDone) * ratio;
            return Math.Max(0.0, finishAt - Math.Max(elapsedNow, _elapsed));
        }

        /// <summary>"About 2 min 20 s left" (10 s steps from 1 min, 5 s below), "Almost done" under 5 s.</summary>
        public static string Text(double secondsLeft)
        {
            if (double.IsNaN(secondsLeft) || secondsLeft < 5) return "Almost done";
            if (secondsLeft < 60) return "About " + (int)(Math.Ceiling(secondsLeft / 5.0) * 5) + " s left";
            int s = (int)(Math.Ceiling(secondsLeft / 10.0) * 10);
            int m = s / 60; s %= 60;
            return "About " + m + " min" + (s > 0 ? " " + s + " s" : "") + " left";
        }

        /// <summary>"about 6 min" / "about 50 s" for a plan line.</summary>
        public static string Duration(double seconds)
        {
            if (seconds < 60) return "about " + (int)(Math.Ceiling(seconds / 5.0) * 5) + " s";
            return "about " + (int)Math.Round(seconds / 60.0, MidpointRounding.AwayFromZero) + " min";
        }
    }
}
