using System;

namespace GPL.Acoustics
{
    /// <summary>The rooms an experience chamber offers: no reverb, or the HW2 Quiet / Reverb RT60 values.</summary>
    public enum RoomChoice { Dry = 0, Room = 1, Hall = 2 }

    public static class RoomChoices
    {
        public static readonly RoomChoice[] All = { RoomChoice.Dry, RoomChoice.Room, RoomChoice.Hall };

        /// <summary>Target reverberation time in seconds; 0 for the dry room. Room = HW2 Noisy 0.7 s, Hall = HW2 Reverb 2.2 s.</summary>
        public static float Rt60(RoomChoice r)
        {
            switch (r)
            {
                case RoomChoice.Room: return 0.7f;
                case RoomChoice.Hall: return 2.2f;
                case RoomChoice.Dry: return 0f;
                default: throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        public static string Label(RoomChoice r) => r == RoomChoice.Room ? "Room 0.7 s" : r == RoomChoice.Hall ? "Hall 2.2 s" : "Dry";
    }
}
