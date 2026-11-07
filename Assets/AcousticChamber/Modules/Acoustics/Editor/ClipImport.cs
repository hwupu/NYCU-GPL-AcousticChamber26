using UnityEditor;
using UnityEngine;

namespace GPL.Acoustics.Editor
{
    public static class ClipImport
    {
        /// <summary>Sets Load Type "Decompress On Load" (needed by <c>ClipLevels.RmsDbfs</c>, which reads the samples). True if it changed.</summary>
        public static bool DecompressOnLoad(AudioClip clip) => SetLoadType(clip, AudioClipLoadType.DecompressOnLoad);

        /// <summary>Sets the clip's default load type (e.g. Streaming for long backgrounds). True if it changed.</summary>
        public static bool SetLoadType(AudioClip clip, AudioClipLoadType loadType)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
            if (importer == null) return false;
            var s = importer.defaultSampleSettings;
            if (s.loadType == loadType) return false;
            s.loadType = loadType;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
            return true;
        }
    }
}
