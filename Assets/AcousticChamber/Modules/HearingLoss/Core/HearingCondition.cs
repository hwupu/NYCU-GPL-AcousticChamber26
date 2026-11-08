using System;

namespace GPL.HearingLoss
{
    /// <summary>
    /// The hearing condition of the experience chambers that offer a hearing-loss selector (MovingSource, HiddenSource; PO 2026-11-08):
    /// an ear (Both, Left, Right) and a level (0, -20, -40 dB). The dB is the preset's deepest band (4 kHz): -20 dB = Mild,
    /// -40 dB = Severe. The other ear stays normal. Level 0 dB is normal hearing whatever the ear.
    /// </summary>
    public readonly struct HearingCondition : IEquatable<HearingCondition>
    {
        public readonly Ear Ear;
        public readonly HearingLossLevel Level;

        public HearingCondition(Ear ear, HearingLossLevel level)
        {
            Ear = ear;
            Level = level;
        }

        public static readonly HearingCondition Normal = new HearingCondition(Ear.Both, HearingLossLevel.None);

        public HearingCondition WithEar(Ear ear) => new HearingCondition(ear, Level);
        public HearingCondition WithLevel(HearingLossLevel level) => new HearingCondition(Ear, level);

        public bool Equals(HearingCondition o) => Ear == o.Ear && Level == o.Level;
        public override bool Equals(object obj) => obj is HearingCondition o && Equals(o);
        public override int GetHashCode() => (int)Ear * 31 + (int)Level;
    }

    public static class HearingConditions
    {
        /// <summary>The ears in panel order (first column).</summary>
        public static readonly Ear[] Ears = { Ear.Both, Ear.Left, Ear.Right };

        /// <summary>The levels in panel order (second column): 0, -20, -40 dB.</summary>
        public static readonly HearingLossLevel[] Levels = { HearingLossLevel.None, HearingLossLevel.Mild, HearingLossLevel.Severe };

        public static HearingProfile Profile(HearingCondition c)
        {
            if (c.Level == HearingLossLevel.None) return HearingProfile.Normal;
            if (c.Level != HearingLossLevel.Mild && c.Level != HearingLossLevel.Severe) throw new ArgumentOutOfRangeException(nameof(c));
            return HearingProfile.For(c.Ear, c.Level);
        }

        public static string EarLabel(Ear ear) => ear == Ear.Left ? "Left" : ear == Ear.Right ? "Right" : "Both";

        public static string LevelLabel(HearingLossLevel level) =>
            level == HearingLossLevel.Mild ? "-20 dB" : level == HearingLossLevel.Severe ? "-40 dB" : "0 dB";

        /// <summary>"Normal", "Both -20 dB", "Left -40 dB", ...</summary>
        public static string Label(HearingCondition c) => c.Level == HearingLossLevel.None ? "Normal" : EarLabel(c.Ear) + " " + LevelLabel(c.Level);
    }
}
