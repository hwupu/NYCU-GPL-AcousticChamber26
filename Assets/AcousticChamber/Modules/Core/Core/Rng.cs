using System;
using System.Collections.Generic;

namespace GPL.Core
{
    /// <summary>
    /// Deterministic random numbers that give the same sequence for the same seed on every platform and runtime
    /// (System.Random's algorithm is an implementation detail; Mono on the Quest and .NET in tools could differ).
    /// Algorithm: SplitMix64 to expand the seed, xoshiro128** style generator on 64-bit state (xorshift64*).
    /// Used for trial plans: the seed is logged and stored in the session so a resumed session rebuilds the identical plan.
    /// </summary>
    public sealed class Rng
    {
        ulong _state;

        public Rng(int seed)
        {
            ulong z = unchecked((ulong)(uint)seed + 0x9E3779B97F4A7C15UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            z ^= z >> 31;
            _state = z == 0 ? 0x2545F4914F6CDD1DUL : z;
        }

        public ulong NextULong()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return unchecked(x * 0x2545F4914F6CDD1DUL);
        }

        /// <summary>Uniform integer in [0, maxExclusive), without modulo bias.</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong bound = (ulong)maxExclusive;
            ulong limit = ulong.MaxValue - ulong.MaxValue % bound;
            ulong r;
            do r = NextULong(); while (r >= limit);
            return (int)(r % bound);
        }

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        public float Range(float minInclusive, float maxExclusive) => (float)(minInclusive + (maxExclusive - minInclusive) * NextDouble());

        /// <summary>Fisher-Yates shuffle in place.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        /// <summary>A new seed for a session (from the clock); log it.</summary>
        public static int NewSeed(DateTime now) => unchecked((int)(now.Ticks ^ (now.Ticks >> 32)) & int.MaxValue);
    }
}
