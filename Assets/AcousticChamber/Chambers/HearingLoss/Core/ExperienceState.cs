using System;
using GPL.HearingLoss;

namespace AcousticChamber.Chambers.HearingLoss
{
    /// <summary>
    /// What the HearingLoss panel shows and sets (Docs/Design.md): the sound, play / pause, the ear(s) and the loss level (a NIOSH
    /// notch preset). Pure; the experience script applies <see cref="Profile"/> to the output and plays the clip.
    /// Starts with the first sound, paused, both ears, normal hearing.
    /// </summary>
    public sealed class ExperienceState
    {
        /// <summary>The levels on the panel, in order.</summary>
        public static readonly HearingLossLevel[] Levels =
            { HearingLossLevel.None, HearingLossLevel.Mild, HearingLossLevel.Moderate, HearingLossLevel.Severe };

        /// <summary>The ear choices on the panel, in order.</summary>
        public static readonly Ear[] Ears = { Ear.Both, Ear.Left, Ear.Right };

        readonly int _clipCount;

        public ExperienceState(int clipCount)
        {
            if (clipCount < 1) throw new ArgumentOutOfRangeException(nameof(clipCount), "Need at least one sound.");
            _clipCount = clipCount;
        }

        public int ClipIndex { get; private set; }
        public bool Playing { get; private set; }
        public Ear Ear { get; private set; } = Ear.Both;
        public HearingLossLevel Level { get; private set; } = HearingLossLevel.None;

        public event Action Changed;

        public HearingProfile Profile => HearingProfile.For(Ear, Level);

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

        public void SetEar(Ear ear)
        {
            if (Ear == ear) return;
            Ear = ear;
            Changed?.Invoke();
        }

        public void SetLevel(HearingLossLevel level)
        {
            if (Array.IndexOf(Levels, level) < 0) throw new ArgumentException($"{level} is not offered on the panel.", nameof(level));
            if (Level == level) return;
            Level = level;
            Changed?.Invoke();
        }

        /// <summary>Button label of a level: "Normal", "-20 dB", "-30 dB", "-40 dB" (the preset's deepest band, at 4 kHz).</summary>
        public static string LevelButton(HearingLossLevel level) =>
            level == HearingLossLevel.None ? "Normal" : "-" + -DeepestDb(level) + " dB";

        /// <summary>"Mild", "Moderate", "Severe" or "Normal".</summary>
        public static string LevelName(HearingLossLevel level) => level == HearingLossLevel.None ? "Normal" : level.ToString();

        public static string EarButton(Ear ear) => ear == Ear.Both ? "Both ears" : ear == Ear.Left ? "Left ear" : "Right ear";

        /// <summary>The deepest band of a preset (negative dB; 0 for None).</summary>
        public static int DeepestDb(HearingLossLevel level)
        {
            int min = 0;
            foreach (int b in HearingPresets.Of(level)) min = Math.Min(min, b);
            return min;
        }

        /// <summary>One line for the panel: "Left ear: Mild, -20 dB at 4 kHz. Right ear: normal."</summary>
        public string Status()
        {
            if (Level == HearingLossLevel.None) return "Both ears: normal hearing.";
            string loss = LevelName(Level) + ", " + DeepestDb(Level) + " dB at 4 kHz";
            switch (Ear)
            {
                case Ear.Left: return "Left ear: " + loss + ". Right ear: normal.";
                case Ear.Right: return "Left ear: normal. Right ear: " + loss + ".";
                default: return "Both ears: " + loss + ".";
            }
        }
    }
}
