using System;
using GPL.HearingLoss;

namespace AcousticChamber.Chambers.HearingLoss
{
    /// <summary>What a click on a sound button does to the player.</summary>
    public enum SoundAction { PlayFromStart, Pause, Resume }

    /// <summary>
    /// What the HearingLoss panel shows and sets (Docs/Design.md; layout like the Hearing Loss Test Panel, PO 2026-11-08):
    /// hearing condition (loss on both ears / left / right), hearing loss (0 / -20 / -30 / -40 dB, the HLSim presets), the sounds
    /// (generated noises and recordings; the pure tones moved to the Config page, PO 2026-11-10). One sound plays at a time from the speaker: click a sound to play it, click it again
    /// to pause, again to resume; another sound starts from the beginning. The ear(s) without the loss stay at 0 dB (no adjustment).
    /// Choosing an ear while at 0 dB sets -20 dB (PO 2026-11-08). Pure; the experience script plays the sounds and applies
    /// <see cref="Profile"/> to the output. Starts with nothing playing, both ears, 0 dB.
    /// </summary>
    public sealed class ExperienceState
    {
        /// <summary>The levels on the panel, in order (HLSim presets only, PO 2026-11-08).</summary>
        public static readonly HearingLossLevel[] Levels =
            { HearingLossLevel.None, HearingLossLevel.Mild, HearingLossLevel.Moderate, HearingLossLevel.Severe };

        /// <summary>The hearing conditions on the panel, in order.</summary>
        public static readonly Ear[] Ears = { Ear.Both, Ear.Left, Ear.Right };

        /// <summary>The level set when an ear is chosen while at 0 dB.</summary>
        public const HearingLossLevel DefaultLoss = HearingLossLevel.Mild;

        public ExperienceState(int recordingCount)
        {
            if (recordingCount < 0) throw new ArgumentOutOfRangeException(nameof(recordingCount));
            RecordingCount = recordingCount;
        }

        public int RecordingCount { get; }
        public int SoundCount => RecordingCount;

        /// <summary>The selected sound (index into the recordings), -1 before the first click.</summary>
        public int Sound { get; private set; } = -1;
        public bool Playing { get; private set; }
        public Ear Ear { get; private set; } = Ear.Both;
        public HearingLossLevel Level { get; private set; } = HearingLossLevel.None;

        public event Action Changed;

        public HearingProfile Profile => HearingProfile.For(Ear, Level);

        /// <summary>A click on sound <paramref name="sound"/>: a new sound plays from the start; the selected one pauses / resumes.</summary>
        public SoundAction ClickSound(int sound)
        {
            if (sound < 0 || sound >= SoundCount) throw new ArgumentOutOfRangeException(nameof(sound));
            SoundAction action;
            if (sound != Sound) { Sound = sound; Playing = true; action = SoundAction.PlayFromStart; }
            else if (Playing) { Playing = false; action = SoundAction.Pause; }
            else { Playing = true; action = SoundAction.Resume; }
            Changed?.Invoke();
            return action;
        }

        /// <summary>Stops playback (leaving); the selection stays.</summary>
        public void Stop()
        {
            if (!Playing) return;
            Playing = false;
            Changed?.Invoke();
        }

        /// <summary>Chooses the ear(s) with the loss; at 0 dB this also sets -20 dB.</summary>
        public void SetEar(Ear ear)
        {
            var level = Level == HearingLossLevel.None ? DefaultLoss : Level;
            if (Ear == ear && Level == level) return;
            Ear = ear;
            Level = level;
            Changed?.Invoke();
        }

        public void SetLevel(HearingLossLevel level)
        {
            if (Array.IndexOf(Levels, level) < 0) throw new ArgumentException($"{level} is not offered on the panel.", nameof(level));
            if (Level == level) return;
            Level = level;
            Changed?.Invoke();
        }

        /// <summary>The level on one ear at its 4 kHz band: 0 when that ear has no loss.</summary>
        public int EarDb(Ear ear)
        {
            bool on = Ear == Ear.Both || Ear == ear;
            return on ? DeepestDb(Level) : 0;
        }

        /// <summary>Button label of a level: "0 dB", "-20 dB", "-30 dB", "-40 dB" (the preset's 4 kHz band).</summary>
        public static string LevelButton(HearingLossLevel level) => Db(DeepestDb(level));

        public static string EarButton(Ear ear) => ear == Ear.Both ? "Both ears" : ear == Ear.Left ? "Left ear" : "Right ear";

        /// <summary>The deepest band of a preset (negative dB; 0 for None).</summary>
        public static int DeepestDb(HearingLossLevel level)
        {
            int min = 0;
            foreach (int b in HearingPresets.Of(level)) min = Math.Min(min, b);
            return min;
        }

        /// <summary>One line for the panel: "Left ear 0 dB, right ear -20 dB (at 4 kHz)."</summary>
        public string Status() =>
            "Left ear " + Db(EarDb(Ear.Left)) + ", right ear " + Db(EarDb(Ear.Right)) + " (at 4 kHz).";

        static string Db(int db) => db == 0 ? "0 dB" : "-" + -db + " dB";
    }
}
