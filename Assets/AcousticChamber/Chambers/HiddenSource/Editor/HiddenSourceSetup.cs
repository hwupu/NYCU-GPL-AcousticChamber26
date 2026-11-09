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

namespace AcousticChamber.Chambers.HiddenSource.Editor
{
    /// <summary>
    /// Magic steps of the HiddenSource experience chamber (Docs/Design.md, Docs/SceneSetup.md). Idempotent; they edit only the
    /// HiddenSource scene and never touch the Building Blocks.
    /// </summary>
    public static class HiddenSourceSetup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/HiddenSource";
        public const string ScenePath = Folder + "/Scenes/HiddenSource.unity";
        public const string DefinitionPath = Folder + "/HiddenSource.ChamberDefinition.asset";
        public const string MaterialsDir = Folder + "/Materials";
        public const string MixerPath = Folder + "/Audio/HiddenSourceMixer.mixer";
        const string PanelName = "Experience Panel";
        const string Log = "[Chamber:HiddenSource] ";

        /// <summary>Room for the audio shoebox: 20 x 20 x 6 m around the listener (sources are at most 6 m away).</summary>
        const float RoomW = 20f, RoomD = 20f, RoomH = 6f;

        /// <summary>Step 1: the ChamberDefinition (C02), the scene (a copy of MainHall), the menu panel with HiddenSourceExperience, Sync Chambers.</summary>
        [MenuItem("Magic/HiddenSource/Step 1 - Chamber, scene and panel", priority = 130)]
        public static void ChamberStep()
        {
            EnsureDefinition(DefinitionPath, ScenePath, HiddenSourceExperience.Id, "C02", "Hidden Source",
                             "Hide and seek, by ear only: point to where a hidden sound is, then see how close you were.");
            var logic = BuildPanelScene<HiddenSourceExperience>(Log, ScenePath, PanelName, HiddenSourceExperience.Size, 0f, 8f);
            if (logic == null) return;
            Debug.Log(Log + "Step 1: panel added, scene saved, catalog synced.");
        }

        /// <summary>
        /// Step 2: the floor, Stimuli/Source (Emitter at 0.3 m with AudioSource + MetaXRAudioSource + PulseTrain, no clip: the white noise is generated at run time, routed to
        /// the Reflection mixer), the true and guess spheres, the reveal line, Audio/Reverb with the shoebox; links all to the panel.
        /// </summary>
        [MenuItem("Magic/HiddenSource/Step 2 - Floor, source and room", priority = 131)]
        public static void SourceStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var group = ReflectionMixer.Ensure(MixerPath);
            if (group == null) return;

            EnsureFloor(MaterialsDir);
            var stimuli = UiSetup.EnsureGroup("Stimuli");
            var audio = UiSetup.EnsureGroup("Audio");

            var root = Child(stimuli, "Source");
            var emitter = Child(root.transform, "Emitter");
            emitter.transform.localPosition = new Vector3(0f, HiddenSourceExperience.EmitterHeightM, 0f);
            var source = Spatial(emitter, null, group); // the clip is generated at run time (GPL.Stimuli, HiddenSourceExperience.Stimulus)
            var pulse = Get<PulseTrain>(emitter);

            var trueMat = UnlitMat(MaterialsDir, "True Sphere", Color.white);
            var guessMat = UnlitMat(MaterialsDir, "Guess Sphere", new Color(1f, 0.78f, 0.35f));
            var lineMat = UnlitMat(MaterialsDir, "Reveal Line", new Color(1f, 0.95f, 0.8f));
            float d = HiddenSourceExperience.GuessRadiusM * 2f;
            var trueSphere = Sphere(stimuli, "True Sphere", d, trueMat);
            var guessSphere = Sphere(stimuli, "Guess Sphere", d, guessMat);

            var lineGo = Child(stimuli, "Reveal Line");
            var line = Get<LineRenderer>(lineGo);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.03f;
            line.sharedMaterial = lineMat;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            EditorUtility.SetDirty(line);

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

            Undo.RecordObject(logic, "HiddenSource links");
            logic.sourceRoot = root.transform;
            logic.source = source;
            logic.pulse = pulse;
            logic.trueSphere = trueSphere;
            logic.guessSphere = guessSphere;
            logic.line = line;
            logic.reverb = reverb;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 2: floor, source, spheres, line and room linked. Save the scene.");
        }

        /// <summary>Checks the scene against Design.md.</summary>
        [MenuItem("Magic/HiddenSource/Validate Setup", priority = 150)]
        public static int ValidateSetup()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return -1;
            int problems = 0;
            void Problem(string m) { Debug.LogError(Log + "Validate: " + m); problems++; }
            var logic = Object.FindAnyObjectByType<HiddenSourceExperience>(FindObjectsInactive.Include);
            if (logic == null) { Problem("no HiddenSourceExperience (Step 1)"); return problems; }
            if (logic.GetComponent<OverlayPanel>() == null) Problem("the panel is not an overlay canvas (Step 1)");
            if (Object.FindAnyObjectByType<GPL.Input.GlobalActions>(FindObjectsInactive.Include) == null) Problem("no Global Actions (Step 1)");
            if (Object.FindAnyObjectByType<UiPointer>(FindObjectsInactive.Include) == null) Problem("no UI Pointer (Step 1)");
            if (logic.sourceRoot == null || logic.source == null || logic.pulse == null) Problem("source not linked (Step 2)");
            if (logic.trueSphere == null || logic.guessSphere == null || logic.line == null) Problem("spheres or line not linked (Step 2)");
            if (logic.reverb == null || logic.reverb.room == null) Problem("reverb or shoebox not linked (Step 2)");
            if (logic.source != null)
            {
                if (logic.source.clip != null) Problem("the source still has a clip asset (re-run Step 2; the stimulus is generated at run time)");
                if (!logic.source.spatialize || logic.source.spatialBlend < 1f) Problem("the source is not spatialized");
            }
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

        // ---- helpers ------------------------------------------------------------------------------------------

        static HiddenSourceExperience Logic()
        {
            var logic = Object.FindAnyObjectByType<HiddenSourceExperience>(FindObjectsInactive.Include);
            if (logic == null) Debug.LogError(Log + "No HiddenSourceExperience in the scene: run Step 1 first.");
            return logic;
        }

        static Transform Sphere(Transform parent, string name, float diameter, Material mat)
        {
            var r = Part(parent, name, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * diameter, mat);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.gameObject.SetActive(false);
            return r.transform;
        }

        /// <summary>AudioSource + MetaXRAudioSource: spatialized, acoustics on, routed to the Reflection mixer, unity gain, natural rolloff, no loop.</summary>
        static AudioSource Spatial(GameObject go, AudioClip clip, UnityEngine.Audio.AudioMixerGroup group)
        {
            var s = Get<AudioSource>(go);
            s.clip = clip;
            s.playOnAwake = false;
            s.loop = false;
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
