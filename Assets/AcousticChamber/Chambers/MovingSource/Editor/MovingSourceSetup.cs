using AcousticChamber.App;
using AcousticChamber.App.Editor;
using GPL.Acoustics;
using GPL.Acoustics.Editor;
using GPL.SpatialSources;
using GPL.UI;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AcousticChamber.App.Editor.ExperienceScene;

namespace AcousticChamber.Chambers.MovingSource.Editor
{
    /// <summary>
    /// Magic steps of the MovingSource experience chamber (Docs/Design.md, Docs/SceneSetup.md). Idempotent; they edit only the
    /// MovingSource scene and never touch the Building Blocks.
    /// </summary>
    public static class MovingSourceSetup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/MovingSource";
        public const string ScenePath = Folder + "/Scenes/MovingSource.unity";
        public const string DefinitionPath = Folder + "/MovingSource.ChamberDefinition.asset";
        public const string MaterialsDir = Folder + "/Materials";
        public const string MixerPath = Folder + "/Audio/MovingSourceMixer.mixer";
        /// <summary>HW2's clips used in place (a temporary asset link between chambers, as in HearingLoss); import settings untouched.</summary>
        public const string AudioDir = "Assets/AcousticChamber/Chambers/HumPerHW2/Audio";
        const string PanelName = "Experience Panel";
        const string Log = "[Chamber:MovingSource] ";
        const float OrbDiameterM = 0.25f;
        const float RoomW = 24f, RoomD = 24f, RoomH = 8f;

        static readonly (string file, string label)[] Sounds =
        {
            ("SFX-C-01-normalized.wav", "White noise"),
            ("SFX-C-02-normalized.wav", "Podcast 1"),
            ("SFX-C-03-normalized.wav", "Podcast 2"),
            ("SFX-C-04-normalized.wav", "Street market"),
        };

        /// <summary>Step 1: the ChamberDefinition (C01), the scene (a copy of MainHall), the panel with MovingSourceExperience, Sync Chambers.</summary>
        [MenuItem("Magic/MovingSource/Step 1 - Chamber, scene and panel", priority = 140)]
        public static void ChamberStep()
        {
            EnsureDefinition(DefinitionPath, ScenePath, MovingSourceExperience.Id, "C01", "Moving Source",
                             "Grab a sound and throw it around: hear its direction, distance and echoes change.");
            var logic = BuildPanelScene<MovingSourceExperience>(Log, ScenePath, PanelName, MovingSourceExperience.Size, -35f, 8f);
            if (logic == null) return;
            Debug.Log(Log + "Step 1: panel added, scene saved, catalog synced.");
        }

        /// <summary>
        /// Step 2: the floor, Stimuli/Source (an orb with GrabSource; child Emitter with AudioSource + MetaXRAudioSource, routed to the
        /// Reflection mixer), the four sounds, Audio/Reverb with the shoebox; links all to the panel.
        /// </summary>
        [MenuItem("Magic/MovingSource/Step 2 - Floor, source and room", priority = 141)]
        public static void SourceStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var group = ReflectionMixer.Ensure(MixerPath);
            if (group == null) return;

            var clips = new AudioClip[Sounds.Length];
            var names = new string[Sounds.Length];
            for (int i = 0; i < Sounds.Length; i++)
            {
                string path = AudioDir + "/" + Sounds[i].file;
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[i] == null) { Debug.LogError(Log + "Missing " + path); return; }
                names[i] = Sounds[i].label;
            }

            EnsureFloor(MaterialsDir);
            var stimuli = UiSetup.EnsureGroup("Stimuli");
            var audio = UiSetup.EnsureGroup("Audio");

            var root = Child(stimuli, "Source");
            root.transform.localPosition = new Vector3(0f, MovingSourceExperience.StartHeightM, MovingSourceExperience.StartDistanceM);
            var orb = Part(root.transform, "Orb", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * OrbDiameterM,
                           UnlitMat(MaterialsDir, "Source Orb", new Color(0.82f, 0.82f, 0.86f)));
            orb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var grab = Get<GrabSource>(root);
            grab.hitRadiusM = 0.3f;
            grab.tinted = new[] { orb };
            EditorUtility.SetDirty(grab);

            var emitter = Child(root.transform, "Emitter");
            emitter.transform.localPosition = Vector3.zero;
            var source = Spatial(emitter, clips[0], group);

            var shoeboxGo = Child(UiSetup.EnsureGroup("Environment"), "Shoebox Acoustics");
            var shoebox = Get<MetaXRAudioRoomAcousticProperties>(shoeboxGo);
            shoebox.lockPositionToListener = true;
            shoebox.width = RoomW; shoebox.depth = RoomD; shoebox.height = RoomH;
            shoebox.clutterFactor = 0f;
            shoebox.leftMaterial = shoebox.rightMaterial = shoebox.frontMaterial = shoebox.backMaterial =
                shoebox.ceilingMaterial = shoebox.floorMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.Concrete;
            EditorUtility.SetDirty(shoebox);
            var reverb = Get<ReverbTuner>(Child(audio, "Reverb"));
            reverb.room = shoebox;
            EditorUtility.SetDirty(reverb);

            Undo.RecordObject(logic, "MovingSource links");
            logic.sourceRoot = root.transform;
            logic.grab = grab;
            logic.source = source;
            logic.clips = clips;
            logic.clipNames = names;
            logic.reverb = reverb;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 2: floor, source, " + clips.Length + " sounds and room linked. Save the scene.");
        }

        /// <summary>Checks the scene against Design.md.</summary>
        [MenuItem("Magic/MovingSource/Validate Setup", priority = 150)]
        public static int ValidateSetup()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return -1;
            int problems = 0;
            void Problem(string m) { Debug.LogError(Log + "Validate: " + m); problems++; }
            var logic = Object.FindAnyObjectByType<MovingSourceExperience>(FindObjectsInactive.Include);
            if (logic == null) { Problem("no MovingSourceExperience (Step 1)"); return problems; }
            if (logic.GetComponent<OverlayPanel>() == null) Problem("the panel is not an overlay canvas (Step 1)");
            if (Object.FindAnyObjectByType<GPL.Input.GlobalActions>(FindObjectsInactive.Include) == null) Problem("no Global Actions (Step 1)");
            if (Object.FindAnyObjectByType<UiPointer>(FindObjectsInactive.Include) == null) Problem("no UI Pointer (Step 1)");
            if (logic.sourceRoot == null || logic.grab == null || logic.source == null) Problem("source not linked (Step 2)");
            if (logic.reverb == null || logic.reverb.room == null) Problem("reverb or shoebox not linked (Step 2)");
            if (logic.clips == null || logic.clips.Length != Sounds.Length) Problem("sounds not linked (Step 2)");
            else foreach (var c in logic.clips) if (c == null) Problem("a sound is missing (Step 2)");
            if (logic.source != null && (!logic.source.spatialize || logic.source.spatialBlend < 1f)) Problem("the source is not spatialized");
            foreach (var s in ReflectionMixer.Unrouted())
                Problem(s.name + " is not routed to a mixer with the '" + ReflectionMixer.EffectName + "' effect (room would be dry)");
            if (Object.FindObjectsByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
                Problem("the scene needs exactly one shoebox (Meta allows one)");
            string spatializer = AudioSettings.GetSpatializerPluginName();
            if (spatializer != "Meta XR Audio") Problem("spatializer plugin is '" + spatializer + "', not 'Meta XR Audio' (Project Settings > Audio)");
            Debug.Log(Log + "Validate: " + (problems == 0 ? "OK." : problems + " problem(s)."));
            return problems;
        }

        /// <summary>Headless (mirror project): Steps 1 and 2, save, Validate (an error exit when it finds a problem).</summary>
        public static void BatchSteps()
        {
            ChamberStep();
            SourceStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            if (ValidateSetup() != 0) EditorApplication.Exit(1);
        }

        static MovingSourceExperience Logic()
        {
            var logic = Object.FindAnyObjectByType<MovingSourceExperience>(FindObjectsInactive.Include);
            if (logic == null) Debug.LogError(Log + "No MovingSourceExperience in the scene: run Step 1 first.");
            return logic;
        }

        /// <summary>AudioSource + MetaXRAudioSource: spatialized, acoustics on, routed to the Reflection mixer, unity gain, natural rolloff, loop.</summary>
        static AudioSource Spatial(GameObject go, AudioClip clip, UnityEngine.Audio.AudioMixerGroup group)
        {
            var s = Get<AudioSource>(go);
            s.clip = clip;
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 1f;
            s.spatialize = true;
            s.outputAudioMixerGroup = group;
            DistanceCue.Apply(s, natural: true);
            var meta = Get<MetaXRAudioSource>(go);
            meta.EnableSpatialization = true;
            meta.EnableAcoustics = true;
            meta.GainBoostDb = 0f;
            EditorUtility.SetDirty(s);
            EditorUtility.SetDirty(meta);
            return s;
        }
    }
}
