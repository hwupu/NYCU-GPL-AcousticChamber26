using System.IO;
using System.Text;

namespace GPL.Core
{
    /// <summary>
    /// Small state files (session progress, presets, pointers) written so that a power-off never leaves a half-written file:
    /// write <c>path.tmp</c>, then replace <c>path</c>. If the device dies between the delete and the move, <see cref="ReadAllText"/>
    /// recovers from the temp file. (Same approach as HW2's ProgressStore, which was proven on the Quest.)
    /// </summary>
    public static class AtomicFile
    {
        public const string TempSuffix = ".tmp";
        static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public static void WriteAllText(string path, string text)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string temp = path + TempSuffix;
            File.WriteAllText(temp, text, Utf8NoBom);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        /// <summary>The file's text; falls back to an orphaned temp file; null if neither exists.</summary>
        public static string ReadAllText(string path)
        {
            if (File.Exists(path)) return File.ReadAllText(path, Utf8NoBom);
            string temp = path + TempSuffix;
            return File.Exists(temp) ? File.ReadAllText(temp, Utf8NoBom) : null;
        }

        public static bool Exists(string path) => File.Exists(path) || File.Exists(path + TempSuffix);

        /// <summary>Deletes the file and any temp file. Returns the number of files removed.</summary>
        public static int Delete(string path)
        {
            int n = 0;
            if (File.Exists(path)) { File.Delete(path); n++; }
            if (File.Exists(path + TempSuffix)) { File.Delete(path + TempSuffix); n++; }
            return n;
        }
    }
}
