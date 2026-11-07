using System;
using GPL.HearingLoss;

namespace AcousticChamber.Chambers.HearingLoss
{
    /// <summary>Which ear(s) carry the loss on the panel; <see cref="None"/> = normal hearing on both ears.</summary>
    public enum LossOn { None = 0, Left = 1, Right = 2, Both = 3 }

    /// <summary>
    /// What the HearingLoss panel shows and sets (Docs/Design.md): the sound, play / pause, which ear(s) carry the loss and its level
    /// (a NIOSH notch preset). The other ear always stays at 0 dB (no adjustment). The two rows stay consistent:
    /// choosing an ear while the level is 0 dB sets -20 dB; choosing Normal hearing sets 0 dB; choosing 0 dB sets Normal hearing;
    /// choosing a loss while on Normal hearing puts it on both ears.
    /// Pure; the experience script applies <see cref="Profile"/> to the output and plays the clip.
    /// Starts with the first sound, paused, normal hearing (0 dB on both ears).
    /// </summary>
    public sealed class ExperienceState
    {
        /// <summary>The levels on the panel, in order.</summary>
        public static readonly HearingLossLevel[] Levels =
            { HearingLossLevel.None, HearingLossLevel.Mild, HearingLossLevel.Moderate, HearingLossLevel.Severe };

        /// <summary>The ear choices on the panel, in order.</summary>
        public static readonly LossOn[] Ears = { LossOn.None, LossOn.Left, LossOn.Right, LossOn.Both };

        /// <summary>The level set when an ear is chosen while the level is 0 dB.</summary>
        public const HearingLossLevel DefaultLoss = HearingLossLevel.Mild;

        /// <summary>The ear(s) set when a loss is chosen while on normal hearing.</summary>
        public const LossOn DefaultEar = LossOn.Both;

        readonly int _clipCount;

        public ExperienceState(int clipCount)
        {
            if (clipCount < 1) throw new ArgumentOutOfRangeException(nameof(clipCount), "Need at least one sound.");
            _clipCount = clipCount;
        }

        public int ClipIndex { get; private set; }
        public bool Playing { get; private set; }
        public LossOn Ear { get; private set; } = LossOn.None;
        public HearingLossLevel Level { get; private set; } = HearingLossLevel.None;

        public event Action Changed;

        /// <summary>The loss on the chosen ear(s), 0 dB (no filter) on the other.</summary>
        public HearingProfile Profile =>
            Ear == LossOn.None || Level == HearingLossLevel.None ? HearingProfile.Normal : HearingProfile.For(ToEar(Ear), Level);

        /// <summary>Selects a sound. Returns true if it changed (the player restarts it from the beginning).</summary>
        public bool SelectClip(int index)
        {
            if (index < 0 || index >= _clipCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (index == ClipIndex) return false;
            ClipIndex = index;
            Changed?.Invoke();
            return true;
        }

        public void SetPlaying(bool playing)
        {
            if (Playing == playing) return;
            Playing = playing;
            Changed?.Invoke();
        }

        public void TogglePlaying() => SetPlaying(!Playing);

        /// <summary>Chooses the ear(s). An ear while at 0 dB also sets -20 dB; Normal hearing also sets 0 dB.</summary>
        public void SetEar(LossOn ear)
        {
            var level = ear == LossOn.None ? HearingLossLevel.None : (Level == HearingLossLevel.None ? DefaultLoss : Level);
            Set(ear, level);
        }

        /// <summary>Chooses the level. 0 dB also sets Normal hearing; a loss while on Normal hearing also sets both ears.</summary>
        public void SetLevel(HearingLossLevel level)
        {
            if (Array.IndexOf(Levels, level) < 0) throw new ArgumentException($"{level} is not offered on the panel.", nameof(level));
            var ear = level == HearingLossLevel.None ? LossOn.None : (Ear == LossOn.None ? DefaultEar : Ear);
            Set(ear, level);
        }

        void Set(LossOn ear, HearingLossLevel level)
        {
            if (Ear == ear && Level == level) return;
            Ear = ear;
            Level = level;
            Changed?.Invoke();
        }

        /// <summary>The level on one ear in dB at its deepest band (4 kHz): 0 when that ear is not chosen.</summary>
        public int EarDb(Ear ear)
        {
            bool on = Ear == LossOn.Both || (ear == GPL.HearingLoss.Ear.Left && Ear == LossOn.Left) ||
                      (ear == GPL.HearingLoss.Ear.Right && Ear == LossOn.Right);
            return on ? DeepestDb(Level) : 0;
        }

        /// <summary>Button label of a level: "0 dB", "-20 dB", "-30 dB", "-40 dB" (the preset's deepest band, at 4 kHz).</summary>
        public static string LevelButton(HearingLossLevel level) => Db(DeepestDb(level));

        public static string EarButton(LossOn ear) =>
            ear == LossOn.None ? "Normal hearing" : ear == LossOn.Both ? "Both ears" : ear == LossOn.Left ? "Left ear" : "Right ear";

        /// <summary>The deepest band of a preset (negative dB; 0 for None).</summary>
        public static int DeepestDb(HearingLossLevel level)
        {
            int min = 0;
            foreach (int b in HearingPresets.Of(level)) min = Math.Min(min, b);
            return min;
        }

        /// <summary>One line for the panel: "Left ear 0 dB, right ear -20 dB (at 4 kHz)."</summary>
        public string Status() =>
            "Left ear " + Db(EarDb(GPL.HearingLoss.Ear.Left)) + ", right ear " + Db(EarDb(GPL.HearingLoss.Ear.Right)) + " (at 4 kHz).";

        static string Db(int db) => db == 0 ? "0 dB" : "-" + -db + " dB";

        static Ear ToEar(LossOn ear) =>
            ear == LossOn.Left ? GPL.HearingLoss.Ear.Left : ear == LossOn.Right ? GPL.HearingLoss.Ear.Right : GPL.HearingLoss.Ear.Both;
    }
}
