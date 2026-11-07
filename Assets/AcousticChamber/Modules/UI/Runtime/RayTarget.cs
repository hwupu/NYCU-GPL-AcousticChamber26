using System.Collections.Generic;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// A 3D object the <see cref="UiPointer"/> ray can point at, press and drag (PB-10), next to the uGUI selectables. The nearer
    /// of the two wins; while a target is held the trigger belongs to it (no UI clicks) until the trigger is released.
    /// Register in OnEnable and unregister in OnDisable (<see cref="RayTargets"/>). Ignored while a modal sheet is open.
    /// </summary>
    public interface IRayTarget
    {
        /// <summary>Distance along <paramref name="ray"/> to the target, if hit within <paramref name="maxDistance"/>.</summary>
        bool RayHit(Ray ray, float maxDistance, out float distance);

        /// <summary>The ray entered (true) or left (false) the target.</summary>
        void RayHover(bool on);

        /// <summary>Trigger pressed on the target.</summary>
        void RayPress(Ray ray);

        /// <summary>Every frame while held; returns where the laser should end.</summary>
        Vector3 RayDrag(Ray ray);

        /// <summary>Trigger released (or the pointer lost the target).</summary>
        void RayRelease();
    }

    /// <summary>Registry of the active <see cref="IRayTarget"/>s.</summary>
    public static class RayTargets
    {
        static readonly List<IRayTarget> _all = new List<IRayTarget>();

        public static IReadOnlyList<IRayTarget> All => _all;

        public static void Register(IRayTarget t)
        {
            if (t != null && !_all.Contains(t)) _all.Add(t);
        }

        public static void Unregister(IRayTarget t) => _all.Remove(t);

        /// <summary>The nearest target hit by <paramref name="ray"/>, or null.</summary>
        public static IRayTarget Pick(Ray ray, float maxDistance, out float distance)
        {
            IRayTarget best = null;
            distance = maxDistance;
            foreach (var t in _all)
                if (t.RayHit(ray, distance, out float d) && d <= distance) { best = t; distance = d; }
            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _all.Clear();
    }
}
