using UnityEngine;

namespace GPL.Stimuli
{
    /// <summary>
    /// Turns a <see cref="StimulusRecipe"/> into a Unity <see cref="AudioClip"/>: mono, uncompressed float samples held in memory
    /// (like an imported PCM clip with Decompress On Load, so <c>PlayScheduled</c> and <c>GetData</c> work). Render once when the
    /// chamber loads, not on the audio thread; the caller owns the clip and destroys it when done.
    /// </summary>
    public static class StimulusClips
    {
        /// <summary>The device output sample rate (48000 before the audio system reports one).</summary>
        public static int OutputRate => AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : StimulusRecipe.DefaultSampleRate;

        /// <summary>Renders <paramref name="recipe"/> (<see cref="Synth.Render"/>) into a new clip named <paramref name="name"/> or the recipe's name.</summary>
        public static AudioClip Create(StimulusRecipe recipe, string name = null)
        {
            var data = Synth.Render(recipe);
            var clip = AudioClip.Create(string.IsNullOrEmpty(name) ? recipe.Name : name, data.Length, 1, recipe.SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Renders the recipe at the device output rate (no resampling by the mixer).</summary>
        public static AudioClip CreateAtOutputRate(StimulusRecipe recipe, string name = null) =>
            Create(recipe.SampleRate == OutputRate ? recipe : recipe.WithSampleRate(OutputRate), name);

        /// <summary>
        /// A mono in-memory copy of a recording whose RMS over the whole clip is <paramref name="levelDbfs"/> (the file is not
        /// changed; gains stay at unity). Needs Load Type Decompress On Load (the Library import rule). Throws if it would clip.
        /// <paramref name="gainDb"/> = the gain applied, for the log. <paramref name="silenceAfterS"/> seconds of silence are appended
        /// after leveling (they do not count in the RMS): looped, the sound repeats with that gap.
        /// </summary>
        public static AudioClip Leveled(AudioClip recording, double levelDbfs, out double gainDb, string name = null, double silenceAfterS = 0)
        {
            if (recording == null) throw new System.ArgumentNullException(nameof(recording));
            if (recording.loadType != AudioClipLoadType.DecompressOnLoad)
                throw new System.InvalidOperationException(recording.name + " is not Decompress On Load (GetData cannot read it).");
            if (recording.loadState != AudioDataLoadState.Loaded) recording.LoadAudioData();
            var raw = new float[recording.samples * recording.channels];
            if (!recording.GetData(raw, 0)) throw new System.InvalidOperationException("GetData failed on " + recording.name);
            var mono = Synth.MixToMono(raw, recording.channels);
            gainDb = Synth.LevelTo(mono, levelDbfs);
            mono = Synth.AppendSilence(mono, silenceAfterS, recording.frequency);
            var clip = AudioClip.Create(string.IsNullOrEmpty(name) ? recording.name : name, mono.Length, 1, recording.frequency, false);
            clip.SetData(mono, 0);
            return clip;
        }

        /// <summary>Destroys a generated clip (safe with null).</summary>
        public static void Release(AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) Object.Destroy(clip);
            else Object.DestroyImmediate(clip);
        }
    }
}
