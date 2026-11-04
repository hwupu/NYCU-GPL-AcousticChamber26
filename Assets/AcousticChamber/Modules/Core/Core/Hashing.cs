using System.Text;

namespace GPL.Core
{
    /// <summary>Stable, platform-independent short hashes (string.GetHashCode differs between runtimes and runs).</summary>
    public static class Hashing
    {
        /// <summary>FNV-1a 64-bit over the UTF-8 bytes.</summary>
        public static ulong Fnv1a64(string text)
        {
            ulong h = 14695981039346656037UL;
            foreach (byte b in Encoding.UTF8.GetBytes(text ?? ""))
            {
                h ^= b;
                h = unchecked(h * 1099511628211UL);
            }
            return h;
        }

        /// <summary>First 8 hex digits of FNV-1a 64: short enough for a log column, long enough to tell parameter sets apart.</summary>
        public static string Short(string text) => Fnv1a64(text).ToString("x16").Substring(0, 8);
    }
}
