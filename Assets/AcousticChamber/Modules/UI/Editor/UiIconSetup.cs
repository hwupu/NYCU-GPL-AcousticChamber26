using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPL.UI.Editor
{
    /// <summary>
    /// Icons: the PNGs in <c>Modules/UI/Icons</c> (exported from docs/design by <c>pnpm icons</c>, white, 128 px) import as
    /// UI sprites, and <see cref="EnsureIconSet"/> collects them into a <see cref="UiIconSet"/> by name.
    /// </summary>
    public class UiIconImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/AcousticChamber/Modules/UI/Icons";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.alphaIsTransparency = true;
            t.mipmapEnabled = true;
            t.filterMode = FilterMode.Trilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.spritePixelsPerUnit = 100f;
            t.maxTextureSize = 128;
            t.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }

    public static class UiIconSetup
    {
        /// <summary>The icon set at <paramref name="assetPath"/>, rebuilt from the sprites in <see cref="UiIconImporter.Folder"/> (idempotent).</summary>
        public static UiIconSet EnsureIconSet(string assetPath)
        {
            var set = AssetDatabase.LoadAssetAtPath<UiIconSet>(assetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UiIconSet>();
                var dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(set, assetPath);
            }
            set.icons.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { UiIconImporter.Folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (sprite != null) set.icons.Add(new UiIconSet.Entry { name = Path.GetFileNameWithoutExtension(p), sprite = sprite });
            }
            set.icons.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log("[GPL] UiIconSetup: " + set.icons.Count + " icons in " + assetPath);
            return set;
        }

        /// <summary>Assigns <paramref name="set"/> to <paramref name="theme"/>.</summary>
        public static void Assign(UiTheme theme, UiIconSet set)
        {
            theme.icons = set;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }
    }
}
