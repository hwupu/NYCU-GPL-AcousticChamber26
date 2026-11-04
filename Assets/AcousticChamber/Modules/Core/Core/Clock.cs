using System;

namespace GPL.Core
{
    /// <summary>Source of the current local time. Injected so logs and session files can be unit-tested.</summary>
    public interface IClock
    {
        DateTime Now { get; }
    }

    /// <summary>The real wall clock (local time).</summary>
    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new SystemClock();
        public DateTime Now => DateTime.Now;
    }

    /// <summary>A clock that only moves when told to (tests, replays).</summary>
    public sealed class ManualClock : IClock
    {
        public DateTime Now { get; set; }
        public ManualClock(DateTime start) => Now = start;
        public void Advance(TimeSpan by) => Now += by;
        public void AdvanceSeconds(double seconds) => Now += TimeSpan.FromSeconds(seconds);
    }
}
