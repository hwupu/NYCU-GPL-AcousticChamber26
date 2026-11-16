using System;
using GPL.Acoustics;

namespace AcousticChamber.Chambers.Reverberation
{
    /// <summary>A shoebox size (m) on the C04 panel; steps of <see cref="RoomSizes.StepM"/>.</summary>
    public readonly struct RoomSize : IEquatable<RoomSize>
    {
        public readonly double Width, Depth, Height;

        public RoomSize(double width, double depth, double height)
        {
            Width = width;
            Depth = depth;
            Height = height;
        }

        /// <summary>Same size to 1 cm (the tolerance of <see cref="Rt60Calibration.CalibratedFor"/>).</summary>
        public bool Equals(RoomSize o) =>
            Math.Abs(Width - o.Width) < 0.01 && Math.Abs(Depth - o.Depth) < 0.01 && Math.Abs(Height - o.Height) < 0.01;

        public override bool Equals(object obj) => obj is RoomSize o && Equals(o);
        public override int GetHashCode() => Math.Round(Width * 100).GetHashCode() ^ Math.Round(Depth * 100).GetHashCode() ^ Math.Round(Height * 100).GetHashCode();

        /// <summary><c>12 × 12 × 3.5 m</c>.</summary>
        public override string ToString() => RoomSizes.M(Width) + " × " + RoomSizes.M(Depth) + " × " + RoomSizes.M(Height) + " m";
    }

    /// <summary>The Room row of C04 (Docs/Design.md): presets, limits, the selected label.</summary>
    public static class RoomSizes
    {
        public const double StepM = 0.1;
        public const double MinWidth = 2, MaxWidth = 30, MinDepth = 2, MaxDepth = 30, MinHeight = 2, MaxHeight = 10;
        public const string Custom = "Custom";

        /// <summary>The rooms the chambers use: open space (C01, C02, ADR-0010) and the HW2 room (C05).</summary>
        public static readonly (string label, RoomSize size)[] Presets =
        {
            ("Open space", new RoomSize(Rt60Protocol.OpenSpaceWidth, Rt60Protocol.OpenSpaceDepth, Rt60Protocol.OpenSpaceHeight)),
            ("HW2 room", new RoomSize(8.72, 15.0, 3.5)),
        };

        /// <summary>Index into <see cref="Presets"/> of a size, or -1 (= Custom).</summary>
        public static int PresetIndex(RoomSize size)
        {
            for (int i = 0; i < Presets.Length; i++) if (Presets[i].size.Equals(size)) return i;
            return -1;
        }

        /// <summary>The labels of the Room row: the presets, then Custom.</summary>
        public static string[] Labels()
        {
            var l = new string[Presets.Length + 1];
            for (int i = 0; i < Presets.Length; i++) l[i] = Presets[i].label;
            l[Presets.Length] = Custom;
            return l;
        }

        /// <summary>A stepper change: clamped to the limits and rounded to 1 cm (8.72 + 0.1 stays 8.82).</summary>
        public static RoomSize Clamp(RoomSize s) => new RoomSize(
            Round(Math.Max(MinWidth, Math.Min(MaxWidth, s.Width))),
            Round(Math.Max(MinDepth, Math.Min(MaxDepth, s.Depth))),
            Round(Math.Max(MinHeight, Math.Min(MaxHeight, s.Height))));

        static double Round(double v) => Math.Round(v * 100.0) / 100.0;

        /// <summary><c>8.72 m</c>, <c>12 m</c>.</summary>
        public static string M(double v) => GPL.Core.Fmt.F(Round(v), 2);
    }
}
