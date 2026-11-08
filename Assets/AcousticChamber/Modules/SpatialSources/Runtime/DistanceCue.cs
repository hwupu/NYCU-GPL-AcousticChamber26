using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>
    /// Distance attenuation of a spatial source, for the experience chambers (Meta XR Audio's own distance attenuation is unused; the
    /// AudioSource rolloff does it): <b>natural</b> = inverse distance (logarithmic rolloff, 1 m reference), <b>flat</b> = the lab rule
    /// of <see cref="UnityGain"/> (loudness never changes with distance).
    /// </summary>
    public static class DistanceCue
    {
        public const float ReferenceM = 1f;
        public const float MaxM = 500f;

        public static void Apply(AudioSource s, bool natural)
        {
            if (s == null) return;
            s.volume = 1f;
            if (natural)
            {
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.minDistance = ReferenceM;
                s.maxDistance = MaxM;
            }
            else
            {
                s.rolloffMode = AudioRolloffMode.Custom;
                s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, MaxM, 1f));
            }
        }
    }
}
