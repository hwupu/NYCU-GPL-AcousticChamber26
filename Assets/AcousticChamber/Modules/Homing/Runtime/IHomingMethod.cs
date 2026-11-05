using System;
using System.Collections;

namespace GPL.Homing
{
    /// <summary>
    /// A way to establish "forward" and the participant's head pose (Architecture section 8). The caller runs
    /// <see cref="Run"/> as a coroutine and logs the result as a <c>homing</c> row (<see cref="HomingResult.Note"/>).
    /// </summary>
    public interface IHomingMethod
    {
        /// <summary>Logged name (<see cref="HomingMethods"/>).</summary>
        string Name { get; }
        bool Running { get; }
        /// <summary>Runs until done, cancelled or unavailable; <paramref name="done"/> is called exactly once.</summary>
        IEnumerator Run(Action<HomingResult> done);
        /// <summary>Stops a running method; it reports <see cref="HomingOutcome.Cancelled"/>.</summary>
        void Cancel();
    }
}
