using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPL.Protocol.Editor
{
    /// <summary>
    /// Editor helpers for instruction slides (PB-21c), for a chamber's Magic steps: a <see cref="SlideSet"/> asset created
    /// once, gray placeholder PNGs at the slide size with a frame at the safe margin, the slide import settings, and the PNG
    /// size from the file header. Idempotent; existing images and assets are kept (their .meta and GUID stay).
    /// </summary>
    public static class SlideSetup
    {
        /// <summary>Slide image size in px (fills the 1000 x 468 u image area at 2 px per u; DesignSystem section 9).</summary>
        public const int Width = 2000, Height = 936;
        /// <summary>48 u safe margin in px: keep text and key details inside it.</summary>
        public const int SafeMarginPx = 96;
        const int FrameLinePx = 4;
        static readonly Color32 FrameColor = new Color32(0x6E, 0x6E, 0x78, 0xFF);

        /// <summary>Creates <paramref name="folder"/> and its missing parents (paths start with "Assets").</summary>
        public static void EnsureFolder(string folder)
        {
            folder = folder.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            int cut = folder.LastIndexOf('/');
            if (cut <= 0) throw new ArgumentException("Not an asset folder: " + folder);
            string parent = folder.Substring(0, cut);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(cut + 1));
        }

        /// <summary>
        /// The SlideSet at <paramref name="path"/>; created with the slides from <paramref name="initial"/> when missing (called
        /// only then, so placeholders are drawn only for a new set). An existing set is returned unchanged.
        /// </summary>
        public static SlideSet EnsureSet(string path, Func<IEnumerable<SlideSet.Slide>> initial)
        {
            var set = AssetDatabase.LoadAssetAtPath<SlideSet>(path);
            if (set != null) return set;
            int cut = path.LastIndexOf('/');
            if (cut > 0) EnsureFolder(path.Substring(0, cut));
            set = ScriptableObject.CreateInstance<SlideSet>();
            if (initial != null)
                foreach (var s in initial())
                    if (s != null) set.slides.Add(s);
            AssetDatabase.CreateAsset(set, path);
            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>
        /// A gray <see cref="Width"/> x <see cref="Height"/> PNG with a light frame at the safe margin. Drawn when the file is
        /// missing or has another size (an older placeholder; its .meta and GUID stay), else kept. Imported for slides.
        /// Use it only for placeholder files: a real illustration of another size would be overwritten.
        /// </summary>
        public static Texture2D Placeholder(string path, Color32 fill)
        {
            if (!File.Exists(path) || PngSize(path) != (Width, Height))
            {
                int cut = path.LastIndexOf('/');
                if (cut > 0) EnsureFolder(path.Substring(0, cut));
                var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                tex.SetPixels32(PlaceholderPixels(fill));
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            ImportForSlides(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>The placeholder pixels, row 0 at the bottom: <paramref name="fill"/> with a 4 px frame just inside the safe margin.</summary>
        public static Color32[] PlaceholderPixels(Color32 fill)
        {
            var px = new Color32[Width * Height];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                px[y * Width + x] = OnFrame(x, y) ? FrameColor : fill;
            return px;
        }

        static bool OnFrame(int x, int y)
        {
            int m = SafeMarginPx, l = FrameLinePx;
            bool inside = x >= m && x < Width - m && y >= m && y < Height - m;
            return inside && (x < m + l || x >= Width - m - l || y < m + l || y >= Height - m - l);
        }

        /// <summary>
        /// Slide import settings on the texture at <paramref name="path"/>: Non-Power of 2 = None, no mipmaps, max size at
        /// least 2048. Reimports only when something changes. Returns false when the path is not a texture.
        /// </summary>
        public static bool ImportForSlides(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return false;
            if (imp.npotScale != TextureImporterNPOTScale.None || imp.mipmapEnabled || imp.maxTextureSize < 2048)
            {
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.mipmapEnabled = false;
                if (imp.maxTextureSize < 2048) imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
            return true;
        }

        /// <summary>Width and height from a PNG header (IHDR), or (0, 0) for a missing, short or non-PNG file.</summary>
        public static (int w, int h) PngSize(string path)
        {
            try
            {
                var b = new byte[24];
                using (var f = File.OpenRead(path))
                    if (f.Read(b, 0, 24) < 24) return (0, 0);
                if (b[0] != 0x89 || b[1] != 'P' || b[2] != 'N' || b[3] != 'G') return (0, 0);
                int Be(int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
                return (Be(16), Be(20));
            }
            catch (IOException) { return (0, 0); }
            catch (UnauthorizedAccessException) { return (0, 0); }
        }
    }
}
