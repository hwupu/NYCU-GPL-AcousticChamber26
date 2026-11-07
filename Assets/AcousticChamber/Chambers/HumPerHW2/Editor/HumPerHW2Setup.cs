using System.IO;
using AcousticChamber.App;
using AcousticChamber.App.Editor;
using GPL.Acoustics;
using GPL.Acoustics.Editor;
using GPL.SpatialSources;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AcousticChamber.Chambers.HumPerHW2.Editor
{
    /// <summary>
    /// Magic steps of the HumPerHW2 chamber (Docs/Design.md). Idempotent; they edit only the HumPerHW2 scene and never touch the
    /// Building Blocks (the Camera Rig comes with the copy of MainHall).
    /// </summary>
    public static class HumPerHW2Setup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/HumPerHW2";
        public const string ScenePath = Folder + "/Scenes/HumPerHW2.unity";
        public const string DefinitionPath = Folder + "/HumPerHW2.ChamberDefinition.asset";
        public const string DefaultParamsPath = Folder + "/Config/default.params";
        const string Log = "[Chamber:HumPerHW2] ";

        /// <summary>
        /// Step 1 (PB-08d-1): the ChamberDefinition (C01), the scene (a copy of MainHall, so it has the same Camera Rig; the Hall
        /// panel removed), Config/default.params (written once) and the chamber menu with HumPerHW2Chamber; then Sync Chambers.
        /// Opens the scene (asks to save the current one first) and saves it.
        /// </summary>
        [MenuItem("Magic/HumPerHW2/Step 1 - Chamber, scene and menu", priority = 110)]
        public static void ChamberStep()
        {
            var def = EnsureDefinition();
            if (!File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                UiSetup.EnsureFolder(Folder + "/Scenes");
                if (!AssetDatabase.CopyAsset(AppSetup.MainHallScene, ScenePath))
                {
                    Debug.LogError(Log + "Could not copy " + AppSetup.MainHallScene + " to " + ScenePath);
                    return;
                }
                Debug.Log(Log + "Scene created from MainHall: " + ScenePath);
            }
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var ui = UiSetup.EnsureGroup("UI");
            var hall = ui.Find("Hall");
            if (hall != null) Object.DestroyImmediate(hall.gameObject);

            var text = ChamberSetup.EnsureDefaultParams(DefaultParamsPath,
                HumPerHW2Chamber.CreateDefaults().Serialize("HumPerHW2 (C01) preset Default. Design: Docs/Design.md"));
            if (ChamberSetup.AddChamberMenu<HumPerHW2Chamber>(def, text) == null) return;
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            HallSetup.SyncChambers();
            Debug.Log(Log + "Step 1: chamber menu added, scene saved, catalog synced.");
        }

        /// <summary>Step 2 (PB-08d-1): the Homing object (fixation dot, recenter listener) and the links from HumPerHW2Chamber.</summary>
        [MenuItem("Magic/HumPerHW2/Step 2 - Homing (fixation dot, recenter listener)", priority = 111)]
        public static void HomingStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Object.FindAnyObjectByType<HumPerHW2Chamber>(FindObjectsInactive.Include);
            if (logic == null) { Debug.LogError(Log + "No HumPerHW2Chamber in the scene: run Step 1 first."); return; }
            var (cue, listener) = AppSetup.EnsureHoming();
            Undo.RecordObject(logic, "HumPerHW2 homing");
            logic.cue = cue;
            logic.listener = listener;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 2: fixation dot and recenter listener added. Save the scene.");
        }

        // ---- Step 3: room and lighting (PB-08d-2) ----------------------------------------------------------

        public const string LightingPath = Folder + "/Settings/HumPerHW2.lighting";
        public const string MaterialsDir = Folder + "/Materials";

        /// <summary>
        /// Step 3: baked lighting settings (no directional light, flat ambient 0.7) and the room in Environment: 8.72 × 15 × 3.5 m,
        /// white walls and ceiling, light-gray floor, two baked area lights, light probes, the Meta XR Audio shoebox of the same
        /// size (Design.md "Posture and setting"). Rebuilds Environment/Room and Environment/Shoebox Acoustics. Bake separately.
        /// </summary>
        [MenuItem("Magic/HumPerHW2/Step 3 - Room and lighting", priority = 112)]
        public static void RoomStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            int removed = RoomBuilder.ApplyBakedLighting(LightingPath);
            var env = UiSetup.EnsureGroup("Environment");
            var room = RoomBuilder.Build(env, RoomBuilder.Options.Default(MaterialsDir));
            Undo.RecordObject(logic, "HumPerHW2 room");
            logic.room = room;
            if (logic.reverb != null) { logic.reverb.room = ShoeboxComponent(); logic.reverb.roomSize = room; EditorUtility.SetDirty(logic.reverb); }
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 3: room built (" + removed + " directional light(s) removed). Save the scene, then Magic > HumPerHW2 > Bake Lighting.");
        }

        [MenuItem("Magic/HumPerHW2/Bake Lighting (slow)", priority = 130)]
        public static void BakeLighting()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            if (RoomBuilder.Bake()) Debug.Log(Log + "Lighting baked and the scene saved.");
        }

        // ---- Step 4: stimuli, background, mixer (PB-08d-2) ------------------------------------------------

        public const string AudioDir = Folder + "/Audio";
        public const string MixerPath = AudioDir + "/HumPerHW2Mixer.mixer";
        public const string StimulusClipPath = AudioDir + "/SFX-C-01-normalized.wav";
        public static readonly string[] BackgroundClipPaths =
        {
            AudioDir + "/SFX-C-02-normalized.wav", AudioDir + "/SFX-C-03-normalized.wav", AudioDir + "/SFX-C-04-normalized.wav",
        };
        public const string PicturePath = Folder + "/Images/Radio.jpg";
        const float PictureWidthM = 0.25f;
        const float EyeLevelM = 1.2f; // placeholder height in the scene; trials place everything at the measured eye height

        /// <summary>
        /// Step 4: the Reflection mixer; Stimuli/Stimulus (white noise SFX-C-01, Decompress On Load, PlacedSource + PulseTrain),
        /// Stimuli/Picture (the radio, 0.25 m wide, URP Unlit, hidden), Stimuli/Sound Marker (Debug sphere, hidden);
        /// Audio/Background (SFX-C-02..04 looping, Streaming, levels measured from the WAV files) and Audio/Reverb (ReverbTuner on the
        /// shoebox). Every spatial source is routed to the mixer; all gains at unity. Links everything to HumPerHW2Chamber.
        /// </summary>
        [MenuItem("Magic/HumPerHW2/Step 4 - Stimuli, background and mixer", priority = 113)]
        public static void AudioStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var group = ReflectionMixer.Ensure(MixerPath);
            if (group == null) return;

            var stimClip = AssetDatabase.LoadAssetAtPath<AudioClip>(StimulusClipPath);
            if (stimClip == null) { Debug.LogError(Log + "Missing " + StimulusClipPath); return; }
            ClipImport.DecompressOnLoad(stimClip);

            var stimuli = UiSetup.EnsureGroup("Stimuli");
            var audio = UiSetup.EnsureGroup("Audio");

            // stimulus
            var stimGo = Child(stimuli, "Stimulus");
            stimGo.transform.localPosition = new Vector3(0f, EyeLevelM, 2f);
            var stimSource = Spatial(stimGo, stimClip, group, loop: false);
            var placed = Get<PlacedSource>(stimGo);
            placed.Anchor = PlacedSource.Anchoring.Replaced;
            var pulse = Get<PulseTrain>(stimGo);

            // picture
            var picture = Child(stimuli, "Picture");
            picture.transform.localPosition = new Vector3(0f, EyeLevelM, 2f);
            picture.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // faces the origin
            BuildPictureQuad(picture.transform);
            picture.SetActive(false);

            // Debug marker
            var markerGo = stimuli.Find("Sound Marker") != null ? stimuli.Find("Sound Marker").gameObject : null;
            if (markerGo == null)
            {
                markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerGo.name = "Sound Marker";
                markerGo.transform.SetParent(stimuli, false);
                Object.DestroyImmediate(markerGo.GetComponent<Collider>());
            }
            markerGo.transform.localScale = Vector3.one * 0.1f;
            markerGo.GetComponent<Renderer>().sharedMaterial = UnlitMat("Sound Marker", new Color(1f, 0.55f, 0.1f), null);
            var marker = Get<SourceMarker>(markerGo);
            marker.target = stimGo.transform;
            markerGo.SetActive(false);

            // background
            var bedGo = Child(audio, "Background");
            var bed = Get<BackgroundBed>(bedGo);
            var sources = new AudioSource[BackgroundClipPaths.Length];
            var clips = new AudioClip[BackgroundClipPaths.Length];
            var levels = new float[BackgroundClipPaths.Length];
            float[] az = { -90f, 90f, 180f };
            for (int i = 0; i < BackgroundClipPaths.Length; i++)
            {
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(BackgroundClipPaths[i]);
                if (clips[i] == null) { Debug.LogError(Log + "Missing " + BackgroundClipPaths[i]); return; }
                ClipImport.SetLoadType(clips[i], AudioClipLoadType.Streaming);
                levels[i] = WavLevel.RmsDbfs(BackgroundClipPaths[i], out var info);
                var go = Child(bedGo.transform, "Background " + (i + 1));
                go.transform.localPosition = Ring.Position(new Vector3(0f, EyeLevelM, 0f), Quaternion.identity, 2f, az[i]);
                sources[i] = Spatial(go, clips[i], group, loop: true);
                Debug.Log(Log + "Background " + (i + 1) + ": " + System.IO.Path.GetFileName(BackgroundClipPaths[i]) + ", " +
                          info.Channels + " ch, " + info.SampleRate + " Hz, " + GPL.Core.Fmt.F(info.DurationS, 1) + " s, RMS " + GPL.Core.Fmt.F(levels[i], 2) + " dBFS");
            }
            bed.sources = sources;
            bed.clips = clips;
            bed.clipLevelsDbfs = levels;
            EditorUtility.SetDirty(bed);

            // reverb
            var reverbGo = Child(audio, "Reverb");
            var reverb = Get<ReverbTuner>(reverbGo);
            reverb.room = ShoeboxComponent();
            reverb.roomSize = logic.room;
            if (reverb.room == null) Debug.LogWarning(Log + "No shoebox in the scene yet: run Step 3, then Step 4 again.");
            EditorUtility.SetDirty(reverb);

            Undo.RecordObject(logic, "HumPerHW2 audio");
            logic.stimulus = placed;
            logic.pulse = pulse;
            logic.picture = picture.transform;
            logic.background = bed;
            logic.reverb = reverb;
            logic.marker = marker;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 4: stimulus, picture, marker, background (" + GPL.Core.Fmt.F(AudioLevelMath.PowerSumDb(levels), 2) +
                      " dBFS combined) and reverb linked; " + (stimSource != null ? "all sources routed to " + MixerPath : "") + ". Save the scene.");
        }

        // ---- Validate (PB-08d-2) ----------------------------------------------------------------------------

        /// <summary>Checks the scene against Design.md: links, clips, routing to the Reflection mixer, one shoebox of the room's size, the spatializer.</summary>
        [MenuItem("Magic/HumPerHW2/Validate Setup", priority = 140)]
        public static int ValidateSetup()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return -1;
            int problems = 0;
            void Problem(string m) { Debug.LogError(Log + "Validate: " + m); problems++; }
            var logic = Object.FindAnyObjectByType<HumPerHW2Chamber>(FindObjectsInactive.Include);
            if (logic == null) { Problem("no HumPerHW2Chamber (Step 1)"); return problems; }
            if (logic.cue == null || logic.listener == null) Problem("fixation dot or recenter listener not linked (Step 2)");
            if (logic.room == null) Problem("room not linked (Step 3)");
            if (logic.stimulus == null || logic.pulse == null || logic.picture == null || logic.marker == null) Problem("stimulus, pulse, picture or marker not linked (Step 4)");
            if (logic.background == null || logic.background.sources.Length != 3 || logic.background.clipLevelsDbfs.Length != 3) Problem("background not set up with 3 sources and levels (Step 4)");
            if (logic.reverb == null || logic.reverb.room == null) Problem("reverb tuner or its shoebox missing (Steps 3-4)");
            if (logic.stimulus != null && logic.stimulus.GetComponent<AudioSource>().clip == null) Problem("stimulus has no clip");
            foreach (var s in ReflectionMixer.Unrouted()) Problem(s.name + " is not routed to a mixer with the '" + ReflectionMixer.EffectName + "' effect (room would be dry)");
            var shoeboxes = Object.FindObjectsByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (shoeboxes.Length != 1) Problem(shoeboxes.Length + " shoebox components (need exactly 1)");
            else if (logic.room != null && (Mathf.Abs(shoeboxes[0].width - logic.room.width) > 1e-3f || Mathf.Abs(shoeboxes[0].depth - logic.room.depth) > 1e-3f || Mathf.Abs(shoeboxes[0].height - logic.room.height) > 1e-3f))
                Problem("shoebox size differs from the visual room");
            string spatializer = AudioSettings.GetSpatializerPluginName();
            if (spatializer != "Meta XR Audio") Problem("spatializer plugin is '" + spatializer + "', not 'Meta XR Audio' (Project Settings > Audio)");
            if (Lightmapping.lightingDataAsset == null) Debug.LogWarning(Log + "Validate: lighting not baked yet (Magic > HumPerHW2 > Bake Lighting).");
            Debug.Log(Log + "Validate: " + (problems == 0 ? "OK." : problems + " problem(s)."));
            return problems;
        }

        // ---- helpers ------------------------------------------------------------------------------------------

        static HumPerHW2Chamber Logic()
        {
            var logic = Object.FindAnyObjectByType<HumPerHW2Chamber>(FindObjectsInactive.Include);
            if (logic == null) Debug.LogError(Log + "No HumPerHW2Chamber in the scene: run Step 1 first.");
            return logic;
        }

        static MetaXRAudioRoomAcousticProperties ShoeboxComponent() =>
            Object.FindAnyObjectByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include);

        static GameObject Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        static T Get<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>(); // Unity null (not C# null) when missing in the Editor: no ??
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>AudioSource + MetaXRAudioSource as Meta's Spatial Audio block has them, unity gain, routed to <paramref name="group"/>.</summary>
        static AudioSource Spatial(GameObject go, AudioClip clip, UnityEngine.Audio.AudioMixerGroup group, bool loop)
        {
            var s = Get<AudioSource>(go);
            s.clip = clip;
            s.playOnAwake = false;
            s.loop = loop;
            s.volume = 1f;
            s.spatialBlend = 1f;
            s.spatialize = true;
            s.rolloffMode = AudioRolloffMode.Custom;
            s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 500f, 1f));
            s.outputAudioMixerGroup = group;
            var meta = Get<MetaXRAudioSource>(go);
            meta.EnableSpatialization = true;
            meta.EnableAcoustics = true;
            meta.GainBoostDb = 0f;
            EditorUtility.SetDirty(s);
            EditorUtility.SetDirty(meta);
            return s;
        }

        static void BuildPictureQuad(Transform picture)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PicturePath);
            if (tex == null) { Debug.LogError(Log + "Missing " + PicturePath); return; }
            var importer = AssetImporter.GetAtPath(PicturePath) as TextureImporter;
            if (importer != null && (importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp))
            {
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PicturePath);
            }
            var quadT = picture.Find("Image");
            GameObject quad;
            if (quadT != null) quad = quadT.gameObject;
            else
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Image";
                quad.transform.SetParent(picture, false);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
            }
            // a Quad shows its face toward -z; the root's +z points at the participant, so the quad turns around
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = new Vector3(PictureWidthM, PictureWidthM * tex.height / tex.width, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = UnlitMat("Picture", Color.white, tex);
        }

        static Material UnlitMat(string name, Color color, Texture2D tex)
        {
            UiSetup.EnsureFolder(MaterialsDir);
            string path = MaterialsDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(m);
            return m;
        }

        static ChamberDefinition EnsureDefinition()
        {
            var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(DefinitionPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ChamberDefinition>();
                def.id = HumPerHW2Chamber.Id;
                def.label = "C01";
                def.title = "HumPerHW2";
                def.description = "Ventriloquist effect: match a sound to a picture in quiet, noisy and reverberant rooms (HW2 rebuilt).";
                def.version = "0.1";
                AssetDatabase.CreateAsset(def, DefinitionPath);
                Debug.Log(Log + "Created " + DefinitionPath);
            }
            if (def.scenePath != ScenePath)
            {
                def.scenePath = ScenePath;
                EditorUtility.SetDirty(def);
                AssetDatabase.SaveAssets();
            }
            return def;
        }

        /// <summary>Headless (mirror project): Steps 1 to 4 (no bake), save, Validate (an error exit when it finds a problem).</summary>
        public static void BatchSteps()
        {
            ChamberStep();
            HomingStep();
            RoomStep();
            AudioStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            if (ValidateSetup() != 0) EditorApplication.Exit(1);
        }
    }
}
