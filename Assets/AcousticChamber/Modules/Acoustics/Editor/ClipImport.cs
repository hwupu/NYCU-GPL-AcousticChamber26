using UnityEditor;
using UnityEngine;

namespace GPL.Acoustics.Editor
{
    public static class ClipImport
    {
        /// <summary>Sets Load Type "Decompress On Load" (needed by <c>ClipLevels.RmsDbfs</c>, which reads the samples). True if it changed.</summary>
        public static bool DecompressOnLoad(AudioClip clip)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
            if (importer == null) return false;
            var s = importer.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.DecompressOnLoad) return false;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
            return true;
        }
    }
}
