using System;
using GPL.Params;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// The headset's media volume step, read from Android's AudioManager (STREAM_MUSIC; the Quest volume buttons change it).
    /// Not available in the Editor or on other platforms (<see cref="TryRead"/> returns false). A chamber declares its target
    /// step with <see cref="TargetDef"/> (preset parameter <c>volume.target_step</c>); the chamber menu shows the current
    /// and the target level, and the session start / resume row and the chamber's final check log the read value.
    /// </summary>
    public static class HeadsetVolume
    {
        public const string TargetKey = "volume.target_step";
        const int StreamMusic = 3;

        /// <summary>The preset parameter of the target step (0..15, group Audio).</summary>
        public static ParamDef TargetDef(int @default) =>
            ParamDef.Int(TargetKey, @default, 0, 15, "Target volume step", "", "Audio")
                    .Describe("headset volume buttons; checked on the chamber menu and at the final check");

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _audio;
        static bool _failed;
#endif

        /// <summary>Current step and the maximum step; false where the volume cannot be read.</summary>
        public static bool TryRead(out int step, out int max)
        {
            step = -1;
            max = -1;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_failed) return false;
            try
            {
                if (_audio == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        _audio = activity.Call<AndroidJavaObject>("getSystemService", "audio");
                }
                step = _audio.Call<int>("getStreamVolume", StreamMusic);
                max = _audio.Call<int>("getStreamMaxVolume", StreamMusic);
                return true;
            }
            catch (Exception e)
            {
                _failed = true;
                Debug.LogWarning("[GPL] HeadsetVolume: cannot read the volume (" + e.Message + ").");
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>The read step, or -1.</summary>
        public static int ReadOrUnknown() => TryRead(out int s, out _) ? s : -1;
    }
}
