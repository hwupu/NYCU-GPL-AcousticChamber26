using UnityEngine;

namespace GPL.Acoustics
{
    public static class ClipLevels
    {
        /// <summary>RMS level of a clip in dBFS; needs Load Type "Decompress On Load" (Editor: <c>ClipImport</c>). -200 if unavailable.</summary>
        public static float RmsDbfs(AudioClip clip)
        {
            if (clip == null) return AudioLevelMath.Silent;
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            return clip.GetData(data, 0) ? AudioLevelMath.RmsDbfs(data) : AudioLevelMath.Silent;
        }

        /// <summary>Software level of a source playing <paramref name="clip"/>: clip RMS + gain boost (dB) + 20 log10(volume).</summary>
        public static float SourceDbfs(AudioClip clip, float volume = 1f, float gainBoostDb = 0f)
        {
            float rms = RmsDbfs(clip);
            return rms <= AudioLevelMath.Silent ? rms : rms + gainBoostDb + AudioLevelMath.GainToDb(volume);
        }
    }
}
