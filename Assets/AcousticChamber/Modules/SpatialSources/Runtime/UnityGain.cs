using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>
    /// The lab's level rule (CLAUDE.md, Quest pitfalls): volume 1 and no distance attenuation (flat rolloff), so loudness comes from
    /// the clip (-23 LUFS) and the headset volume only; sources at 1.2 m and 2 m are equally loud.
    /// </summary>
    public static class UnityGain
    {
        public static void Apply(AudioSource s, bool loop)
        {
            if (s == null) return;
            s.playOnAwake = false;
            s.loop = loop;
            s.volume = 1f;
            s.rolloffMode = AudioRolloffMode.Custom;
            s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 500f, 1f));
        }
    }
}
