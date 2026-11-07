using System.IO;
using AcousticChamber.App;
using AcousticChamber.App.Editor;
using GPL.Acoustics.Editor;
using GPL.SpatialSources;
using GPL.UI;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AcousticChamber.Chambers.HearingLoss.Editor
{
    /// <summary>
    /// Magic steps of the HearingLoss experience chamber (Docs/Design.md, Docs/SceneSetup.md). Idempotent; they edit only the
    /// HearingLoss scene and never touch the Building Blocks (the Camera Rig comes with the copy of MainHall).
    /// </summary>
    public static class HearingLossSetup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/HearingLoss";
        public const string ScenePath = Folder + "/Scenes/HearingLoss.unity";
        public const string DefinitionPath = Folder + "/HearingLoss.ChamberDefinition.asset";
        public const string MaterialsDir = Folder + "/Materials";
        public const string AudioDir = Folder + "/Audio";
        const string PanelName = "Experience Panel";
        const string LaserPath = "Assets/AcousticChamber/Settings/Materials/Laser.mat";
        const string Log = "[Chamber:HearingLoss] ";

        /// <summary>The sounds on the panel (HW2 clips for now, PO 2026-11-07): file, button label, load type.</summary>
        static readonly (string file, string label, AudioClipLoadType load)[] Sounds =
        {
            ("SFX-C-01-normalized.wav", "White noise", AudioClipLoadType.DecompressOnLoad),
            ("SFX-C-02-normalized.wav", "Podcast 1", AudioClipLoadType.Streaming),
            ("SFX-C-03-normalized.wav", "Podcast 2", AudioClipLoadType.Streaming),
            ("SFX-C-04-normalized.wav", "Street market", AudioClipLoadType.DecompressOnLoad),
        };

        public const float FloorSizeM = 30f;
        public const float EmitterHeightM = 0.3f;

        /// <summary>
        /// Step 1: the ChamberDefinition (C02), the scene (a copy of MainHall, the Hall panel removed), the experience panel (an
        /// overlay canvas 35° to the left of straight ahead with HearingLossExperience), Global Actions, the pointer and the scene
        /// fade; then Sync Chambers. Opens the scene (asks to save the current one first) and saves it.
        /// </summary>
        [MenuItem("Magic/HearingLoss/Step 1 - Chamber, scene and panel", priority = 120)]
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
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError(Log + "The scene has no Camera Rig building block.");
                return;
            }

            var theme = AppSetup.EnsureUiAssets();
            AppSetup.EnsureGlobalActions();
            var ui = UiSetup.EnsureGroup("UI");
            UiSetup.EnsureUiRoot(ui, theme);
            var hall = ui.Find("Hall");
            if (hall != null) Object.DestroyImmediate(hall.gameObject);

            // keep the links of an existing panel (Step 2 sets them)
            var old = Object.FindAnyObjectByType<HearingLossExperience>(FindObjectsInactive.Include);
            FloorDragSource speaker = old != null ? old.speaker : null;
            AudioSource source = old != null ? old.source : null;
            AudioClip[] clips = old != null ? old.clips : null;
            string[] names = old != null ? old.clipNames : null;

            var canvas = UiSetup.CreateOverlayCanvas(ui, PanelName, HearingLossExperience.Size);
            var panel = canvas.GetComponent<OverlayPanel>();
            panel.yawOffsetDeg = -35f;
            panel.pitchDownDeg = 8f;
            var logic = canvas.AddComponent<HearingLossExperience>();
            logic.speaker = speaker;
            logic.source = source;
            if (clips != null) logic.clips = clips;
            if (names != null) logic.clipNames = names;
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            HallSetup.EnsureSceneFade();
            UiSetup.MarkSceneDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            HallSetup.SyncChambers();
            Debug.Log(Log + "Step 1: panel added, scene saved, catalog synced.");
        }

        /// <summary>
        /// Step 2: Environment/Floor (30 × 30 m, URP Lit with a 1 m grid, no walls: dry, no reverb), Stimuli/Speaker (a box speaker
        /// standing on the floor 2 m ahead, FloorDragSource; child Emitter at 0.3 m with AudioSource + MetaXRAudioSource:
        /// spatialized, acoustics off, no mixer, volume 1, flat rolloff, loop), the four sounds (import settings), links to the panel.
        /// </summary>
        [MenuItem("Magic/HearingLoss/Step 2 - Floor, speaker and sounds", priority = 121)]
        public static void SpeakerStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;

            // floor
            var env = UiSetup.EnsureGroup("Environment");
            var floor = env.Find("Floor") != null ? env.Find("Floor").gameObject : null;
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                floor.name = "Floor";
                floor.transform.SetParent(env, false);
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
            }
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(FloorSizeM, FloorSizeM, 1f);
            floor.GetComponent<Renderer>().sharedMaterial = FloorMaterial();

            // speaker
            var stimuli = UiSetup.EnsureGroup("Stimuli");
            var speakerGo = Child(stimuli, "Speaker");
            speakerGo.transform.localPosition = new Vector3(0f, 0f, HearingLossExperience.StartDistanceM);
            speakerGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // faces the origin
            var body = Part(speakerGo.transform, "Body", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), Vector3.zero, new Vector3(0.3f, 0.5f, 0.26f),
                            LitMat("Speaker Body", new Color(0.82f, 0.82f, 0.86f)));
            Part(speakerGo.transform, "Woofer", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0.131f), new Vector3(90f, 0f, 0f), new Vector3(0.2f, 0.005f, 0.2f),
                 LitMat("Speaker Cone", new Color(0.12f, 0.12f, 0.14f)));
            Part(speakerGo.transform, "Tweeter", PrimitiveType.Cylinder, new Vector3(0f, 0.39f, 0.131f), new Vector3(90f, 0f, 0f), new Vector3(0.08f, 0.005f, 0.08f),
                 LitMat("Speaker Cone", new Color(0.12f, 0.12f, 0.14f)));
            var drag = Get<FloorDragSource>(speakerGo);
            drag.floorY = 0f;
            drag.hitRadiusM = 0.32f;
            drag.hitCenterHeightM = 0.25f;
            drag.minDistanceM = 0.5f;
            drag.maxDistanceM = 6f;
            drag.faceCenter = true;
            drag.tinted = new[] { body };
            EditorUtility.SetDirty(drag);

            var emitter = Child(speakerGo.transform, "Emitter");
            emitter.transform.localPosition = new Vector3(0f, EmitterHeightM, 0f);
            var clips = new AudioClip[Sounds.Length];
            var names = new string[Sounds.Length];
            for (int i = 0; i < Sounds.Length; i++)
            {
                string path = AudioDir + "/" + Sounds[i].file;
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[i] == null) { Debug.LogError(Log + "Missing " + path); return; }
                ClipImport.SetLoadType(clips[i], Sounds[i].load);
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                names[i] = Sounds[i].label;
            }
            var source = Spatial(emitter, clips[0]);

            Undo.RecordObject(logic, "HearingLoss speaker");
            logic.speaker = drag;
            logic.source = source;
            logic.clips = clips;
            logic.clipNames = names;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 2: floor, speaker and " + clips.Length + " sounds linked. Save the scene.");
        }

        /// <summary>Checks the scene against Design.md.</summary>
        [MenuItem("Magic/HearingLoss/Validate Setup", priority = 140)]
        public static int ValidateSetup()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return -1;
            int problems = 0;
            void Problem(string m) { Debug.LogError(Log + "Validate: " + m); problems++; }
            var logic = Object.FindAnyObjectByType<HearingLossExperience>(FindObjectsInactive.Include);
            if (logic == null) { Problem("no HearingLossExperience (Step 1)"); return problems; }
            if (logic.GetComponent<OverlayPanel>() == null) Problem("the panel is not an overlay canvas (Step 1)");
            if (Object.FindAnyObjectByType<GPL.Input.GlobalActions>(FindObjectsInactive.Include) == null) Problem("no Global Actions (Step 1)");
            if (Object.FindAnyObjectByType<UiPointer>(FindObjectsInactive.Include) == null) Problem("no UI Pointer (Step 1)");
            if (logic.speaker == null || logic.source == null) Problem("speaker or source not linked (Step 2)");
            if (logic.clips == null || logic.clips.Length != Sounds.Length) Problem("sounds not linked (Step 2)");
            else foreach (var c in logic.clips) if (c == null) Problem("a sound is missing (Step 2)");
            if (logic.source != null)
            {
                var s = logic.source;
                if (!s.spatialize || s.spatialBlend < 1f) Problem("the speaker's source is not spatialized");
                if (s.outputAudioMixerGroup != null) Problem("the speaker's source is routed to a mixer (the room must stay dry)");
                var meta = s.GetComponent<MetaXRAudioSource>();
                if (meta == null || meta.EnableAcoustics) Problem("MetaXRAudioSource missing or acoustics on (the room must stay dry)");
                if (s.volume != 1f) Problem("source volume is not 1");
            }
            if (Object.FindAnyObjectByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include) != null) Problem("a shoebox room is in the scene (the room must stay dry)");
            string spatializer = AudioSettings.GetSpatializerPluginName();
            if (spatializer != "Meta XR Audio") Problem("spatializer plugin is '" + spatializer + "', not 'Meta XR Audio' (Project Settings > Audio)");
            Debug.Log(Log + "Validate: " + (problems == 0 ? "OK." : problems + " problem(s)."));
            return problems;
        }

        /// <summary>Headless (mirror project): Steps 1 and 2, save, Validate (an error exit when it finds a problem).</summary>
        public static void BatchSteps()
        {
            ChamberStep();
            SpeakerStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            if (ValidateSetup() != 0) EditorApplication.Exit(1);
        }

        // ---- helpers ------------------------------------------------------------------------------------------

        static ChamberDefinition EnsureDefinition()
        {
            var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(DefinitionPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ChamberDefinition>();
                def.id = HearingLossExperience.Id;
                def.label = "C02";
                def.title = "Hearing Loss";
                def.description = "Experience simulated hearing loss on one or both ears while you move a sound around you.";
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

        static HearingLossExperience Logic()
        {
            var logic = Object.FindAnyObjectByType<HearingLossExperience>(FindObjectsInactive.Include);
            if (logic == null) Debug.LogError(Log + "No HearingLossExperience in the scene: run Step 1 first.");
            return logic;
        }

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
            var c = go.GetComponent<T>(); // Unity null when missing in the Editor: no ??
            return c != null ? c : go.AddComponent<T>();
        }

        static Renderer Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t != null) go = t.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.transform.SetParent(parent, false);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return r;
        }

        /// <summary>AudioSource + MetaXRAudioSource: spatialized, acoustics off (dry), no mixer, unity gain, flat rolloff, loop.</summary>
        static AudioSource Spatial(GameObject go, AudioClip clip)
        {
            var s = Get<AudioSource>(go);
            s.clip = clip;
            UnityGain.Apply(s, loop: true);
            s.spatialBlend = 1f;
            s.spatialize = true;
            s.outputAudioMixerGroup = null;
            var meta = Get<MetaXRAudioSource>(go);
            meta.EnableSpatialization = true;
            meta.EnableAcoustics = false;
            meta.GainBoostDb = 0f;
            EditorUtility.SetDirty(s);
            EditorUtility.SetDirty(meta);
            return s;
        }

        static Material LitMat(string name, Color color)
        {
            UiSetup.EnsureFolder(MaterialsDir);
            string path = MaterialsDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>URP Lit with a generated grid texture (one cell = 1 m on the 30 m floor).</summary>
        static Material FloorMaterial()
        {
            string texPath = MaterialsDir + "/FloorGrid.png";
            UiSetup.EnsureFolder(MaterialsDir);
            if (!File.Exists(texPath))
            {
                const int N = 256, Line = 3;
                var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
                var fill = new Color(0.30f, 0.30f, 0.33f);
                var line = new Color(0.45f, 0.45f, 0.50f);
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                        px[y * N + x] = x < Line || y < Line ? line : fill;
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (imp != null)
                {
                    imp.wrapMode = TextureWrapMode.Repeat;
                    imp.anisoLevel = 8;
                    imp.SaveAndReimport();
                }
            }
            var m = LitMat("Floor", Color.white);
            var grid = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            m.SetTexture("_BaseMap", grid);
            m.SetTextureScale("_BaseMap", new Vector2(FloorSizeM, FloorSizeM));
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
