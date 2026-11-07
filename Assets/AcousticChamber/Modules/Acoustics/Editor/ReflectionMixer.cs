using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace GPL.Acoustics.Editor
{
    /// <summary>
    /// Audio mixer with the "Meta XR Audio Reflection" effect on its Master group. Meta XR Audio renders the shoebox room (early
    /// reflections and late reverb) in this effect: spatial sources not routed to it stay completely dry. Unity's mixer editing API
    /// is internal, so the mixer is created through reflection (HW2 Step 4; works in Unity 6000.6).
    /// </summary>
    public static class ReflectionMixer
    {
        public const string EffectName = "Meta XR Audio Reflection";

        /// <summary>Master group of the mixer at <paramref name="mixerPath"/> (e.g. <c>Assets/.../Audio/HumPerHW2Mixer.mixer</c>),
        /// creating the mixer with the effect if it is missing or lacks the effect. Null (with an error) if that failed.</summary>
        public static AudioMixerGroup Ensure(string mixerPath)
        {
            if (!HasEffect(mixerPath))
            {
                if (File.Exists(mixerPath)) AssetDatabase.DeleteAsset(mixerPath);
                Directory.CreateDirectory(Path.GetDirectoryName(mixerPath));
                Create(mixerPath);
            }
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            var groups = mixer != null ? mixer.FindMatchingGroups("Master") : new AudioMixerGroup[0];
            if (groups.Length == 0)
                Debug.LogError("[GPL] Could not create or load " + mixerPath + ". Create an Audio Mixer by hand and add the '" + EffectName + "' effect to Master.");
            return groups.Length > 0 ? groups[0] : null;
        }

        /// <summary>True if the mixer file exists and contains the effect.</summary>
        public static bool HasEffect(string mixerPath) => File.Exists(mixerPath) && File.ReadAllText(mixerPath).Contains(EffectName);

        /// <summary>Route every given source to <paramref name="group"/>; returns how many changed.</summary>
        public static int Route(IEnumerable<AudioSource> sources, AudioMixerGroup group)
        {
            int changed = 0;
            foreach (var s in sources)
            {
                if (s == null || s.outputAudioMixerGroup == group) continue;
                s.outputAudioMixerGroup = group;
                EditorUtility.SetDirty(s);
                changed++;
            }
            return changed;
        }

        /// <summary>Spatial sources (with a <c>MetaXRAudioSource</c>) in the open scene that are not routed to a mixer group with the effect.</summary>
        public static List<AudioSource> Unrouted()
        {
            var result = new List<AudioSource>();
            foreach (var meta in Object.FindObjectsByType<MetaXRAudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var source = meta.GetComponent<AudioSource>();
                if (source == null) continue;
                var group = source.outputAudioMixerGroup;
                string path = group != null ? AssetDatabase.GetAssetPath(group.audioMixer) : null;
                if (string.IsNullOrEmpty(path) || !HasEffect(path)) result.Add(source);
            }
            return result;
        }

        static void Create(string mixerPath)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            var effectType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerEffectController");
            if (controllerType == null || effectType == null) { Debug.LogError("[GPL] Unity's internal mixer API was not found"); return; }

            var controller = (Object)controllerType.GetMethod("CreateMixerControllerAtPath", all).Invoke(null, new object[] { mixerPath });
            object master = controllerType.GetProperty("masterGroup", all).GetValue(controller);
            var effects = (System.Array)master.GetType().GetProperty("effects", all).GetValue(master);
            var effect = (Object)System.Activator.CreateInstance(effectType, all, null, new object[] { EffectName }, null);
            effectType.GetMethod("PreallocateGUIDs", all).Invoke(effect, null);
            master.GetType().GetMethod("InsertEffect", all).Invoke(master, new object[] { effect, effects.Length });
            AssetDatabase.AddObjectToAsset(effect, controller);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GPL] Created " + mixerPath + " with the '" + EffectName + "' effect on Master.");
        }
    }
}
