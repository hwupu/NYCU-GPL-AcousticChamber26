using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPL.Stimuli.Editor
{
    /// <summary>
    /// Import rule for every recording under <see cref="StimulusLibrary.Folder"/>: PCM (no Vorbis), Decompress On Load (so
    /// <c>StimulusClips.Leveled</c> and <c>GetData</c> can read it), no sample-rate override, channels kept. Applied on import.
    /// </summary>
    public class LibraryImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(StimulusLibrary.Folder + "/")) return;
            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.PCM;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            s.preloadAudioData = true;
            importer.defaultSampleSettings = s;
            importer.forceToMono = false;
            importer.loadInBackground = false;
        }

        /// <summary>Every catalog entry has its file, and every audio file has an entry.</summary>
        [MenuItem("Magic/Stimuli/Validate Library", priority = 300)]
        public static int Validate()
        {
            int problems = 0;
            foreach (var e in StimulusLibrary.All)
            {
                var path = StimulusLibrary.AssetPath(e);
                if (File.Exists(path)) continue;
                Debug.LogError("[GPL] Library: missing " + path);
                problems++;
            }
            if (Directory.Exists(StimulusLibrary.Folder))
                foreach (var f in Directory.GetFiles(StimulusLibrary.Folder, "*.*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".wav" && ext != ".mp3" && ext != ".flac" && ext != ".ogg" && ext != ".aif" && ext != ".aiff") continue;
                    var rel = f.Replace('\\', '/').Substring(StimulusLibrary.Folder.Length + 1);
                    if (StimulusLibrary.FindByFile(rel) == null)
                    {
                        Debug.LogError("[GPL] Library: " + rel + " has no catalog entry (StimulusLibrary + docs/AssetCredits.md)");
                        problems++;
                    }
                }
            Debug.Log("[GPL] Library: " + StimulusLibrary.All.Count + " entries, " + (problems == 0 ? "OK." : problems + " problem(s)."));
            return problems;
        }
    }
}
