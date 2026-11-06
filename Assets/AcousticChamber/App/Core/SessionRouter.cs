using System;
using System.Collections.Generic;

namespace AcousticChamber.App
{
    /// <summary>
    /// Launch routing (Architecture section 5, PB-06b): when the app starts, the last chamber entered opens directly at its
    /// chamber menu if it is still in the Hall and has unfinished sessions; otherwise the Hall stays. Never opens the trials.
    /// </summary>
    public static class SessionRouter
    {
        /// <summary>The chamber ID to open at launch, or null for the Hall.</summary>
        /// <param name="lastChamber">From <see cref="AppState.LastChamber"/>.</param>
        /// <param name="visibleIds">Chambers shown in the Hall (enabled, in the catalog).</param>
        /// <param name="unfinished">Number of resumable sessions of a chamber (exceptions count as 0).</param>
        public static string Route(string lastChamber, IEnumerable<string> visibleIds, Func<string, int> unfinished)
        {
            if (string.IsNullOrEmpty(lastChamber) || visibleIds == null || unfinished == null) return null;
            bool visible = false;
            foreach (var id in visibleIds)
                if (id == lastChamber) { visible = true; break; }
            if (!visible) return null;
            int n;
            try { n = unfinished(lastChamber); }
            catch (Exception) { n = 0; }
            return n > 0 ? lastChamber : null;
        }
    }
}
