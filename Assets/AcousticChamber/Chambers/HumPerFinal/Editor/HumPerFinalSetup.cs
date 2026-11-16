using System.Collections.Generic;
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

namespace AcousticChamber.Chambers.HumPerFinal.Editor
{
    /// <summary>
    /// Magic steps of the HumPerFinal chamber (Docs/Design.md, Docs/SceneSetup.md). Idempotent; they edit only the HumPerFinal
    /// scene and never touch the Building Blocks (the Camera Rig comes with the copy of MainHall). Same room as C05, own assets.
    /// </summary>
    public static class HumPerFinalSetup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/HumPerFinal";
        public const string ScenePath = Folder + "/Scenes/HumPerFinal.unity";
        public const string DefinitionPath = Folder + "/HumPerFinal.ChamberDefinition.asset";
        public const string DefaultParamsPath = Folder + "/Config/default.params";
        public const string CoverPath = "Assets/AcousticChamber/App/Covers/IMG-C-06.jpeg";
        const string Log = "[Chamber:HumPerFinal] ";

        /// <summary>Step 1: the ChamberDefinition (C06), the scene (a copy of MainHall without the Hall panel), Config/default.params and the chamber menu; Sync Chambers.</summary>
        [MenuItem("Magic/HumPerFinal/Step 1 - Chamber, scene and menu", priority = 110)]
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
                HumPerFinalChamber.CreateDefaults().Serialize("HumPerFinal (C06) preset Default. Design: Docs/Design.md"));
            if (ChamberSetup.AddChamberMenu<HumPerFinalChamber>(def, text) == null) return;
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            HallSetup.SyncChambers();
            Debug.Log(Log + "Step 1: chamber menu added, scene saved, catalog synced.");
        }

        /// <summary>Step 2: the Homing object (fixation dot, recenter listener) and the links from HumPerFinalChamber.</summary>
        [MenuItem("Magic/HumPerFinal/Step 2 - Homing (fixation dot, recenter listener)", priority = 111)]
        public static void HomingStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var (cue, listener) = AppSetup.EnsureHoming();
            Undo.RecordObject(logic, "HumPerFinal homing");
            logic.cue = cue;
            logic.listener = listener;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 2: fixation dot and recenter listener added. Save the scene.");
        }

        // ---- Step 3: room and lighting (as C05) -----------------------------------------------------------

        public const string LightingPath = Folder + "/Settings/HumPerFinal.lighting";
        public const string MaterialsDir = Folder + "/Materials";

        /// <summary>Step 3: baked lighting settings and the C05 room in Environment (8.72 × 15 × 3.5 m, white, shoebox of the same size). Bake separately.</summary>
        [MenuItem("Magic/HumPerFinal/Step 3 - Room and lighting", priority = 112)]
        public static void RoomStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            UiSetup.EnsureFolder(Folder + "/Settings");
            int removed = RoomBuilder.ApplyBakedLighting(LightingPath);
            var env = UiSetup.EnsureGroup("Environment");
            var room = RoomBuilder.Build(env, RoomBuilder.Options.Default(MaterialsDir));
            Undo.RecordObject(logic, "HumPerFinal room");
            logic.room = room;
            if (logic.reverb != null) { logic.reverb.room = ShoeboxComponent(); logic.reverb.roomSize = room; EditorUtility.SetDirty(logic.reverb); }
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 3: room built (" + removed + " directional light(s) removed). Save the scene, then Magic > HumPerFinal > Bake Lighting.");
        }

        [MenuItem("Magic/HumPerFinal/Bake Lighting (slow)", priority = 130)]
        public static void BakeLighting()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            if (RoomBuilder.Bake()) Debug.Log(Log + "Lighting baked and the scene saved.");
        }

        // ---- Step 4: target, radio, anchors, mixer -------------------------------------------------------

        public const string AudioDir = Folder + "/Audio";
        public const string MixerPath = AudioDir + "/HumPerFinalMixer.mixer";
        /// <summary>Placeholder anchor recordings (Design.md "Stimuli"; GPL.Stimuli OpenAIR library, credited in docs/AssetCredits.md).</summary>
        public const string DuckClipPath = "Assets/AcousticChamber/Modules/Stimuli/Library/OpenAIR/Bicycle_Horn/classichorn.wav";
        public const string TrumpetClipPath = "Assets/AcousticChamber/Modules/Stimuli/Library/OpenAIR/Natural_Baroque_Trumpet/tr-1780d-fanfare.wav";
        /// <summary>Placeholder boxes (Design.md "Stimuli"), metres: width × height × depth.</summary>
        public static readonly Vector3 RadioSize = new Vector3(0.25f, 0.18f, 0.12f);
        public static readonly Vector3 DuckSize = new Vector3(0.15f, 0.15f, 0.15f);
        public static readonly Vector3 TrumpetSize = new Vector3(0.30f, 0.10f, 0.10f);
        const float EyeLevelM = 1.2f; // placeholder height in the scene; trials place everything at the measured eye height

        /// <summary>
        /// Step 4: the Reflection mixer; Stimuli/Stimulus (white noise generated at run time, PlacedSource + PulseTrain),
        /// Stimuli/Radio (box placeholder, URP Lit, hidden), Stimuli/Anchors (AnchorLoop with Duck at 0° and Trumpet at +90°, each a
        /// spatial source with a box 'Model' child, hidden), Stimuli/Sound Marker (Debug sphere), Audio/Reverb (ReverbTuner on the
        /// shoebox). Every spatial source is routed to the mixer; all gains at unity. Links everything to HumPerFinalChamber.
        /// </summary>
        [MenuItem("Magic/HumPerFinal/Step 4 - Stimuli, anchors and mixer", priority = 113)]
        public static void AudioStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            UiSetup.EnsureFolder(AudioDir);
            var group = ReflectionMixer.Ensure(MixerPath);
            if (group == null) return;

            var stimuli = UiSetup.EnsureGroup("Stimuli");
            var audio = UiSetup.EnsureGroup("Audio");

            // target
            var stimGo = Child(stimuli, "Stimulus");
            stimGo.transform.localPosition = new Vector3(0f, EyeLevelM, 2f);
            Spatial(stimGo, null, group);
            var placed = Get<PlacedSource>(stimGo);
            placed.Anchor = PlacedSource.Anchoring.Replaced;
            var pulse = Get<PulseTrain>(stimGo);

            // radio (box placeholder)
            var radio = Child(stimuli, "Radio");
            radio.transform.localPosition = new Vector3(0f, EyeLevelM, 2f);
            radio.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Box(radio.transform, "Model", RadioSize, LitMat("Radio", new Color(0.25f, 0.25f, 0.28f)));
            radio.SetActive(false);

            // anchors
            var anchorsGo = Child(stimuli, "Anchors");
            var loop = Get<AnchorLoop>(anchorsGo);
            var duckClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DuckClipPath);
            var trumpetClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TrumpetClipPath);
            if (duckClip == null || trumpetClip == null) { Debug.LogError(Log + "Missing anchor recordings in the GPL.Stimuli library."); return; }
            loop.duck = Anchor(anchorsGo.transform, "Duck", 0f, DuckSize, LitMat("Duck", new Color(1f, 0.85f, 0.1f)), group);
            loop.trumpet = Anchor(anchorsGo.transform, "Trumpet", 90f, TrumpetSize, LitMat("Trumpet", new Color(0.8f, 0.62f, 0.2f)), group);
            loop.duckRecording = duckClip;
            loop.trumpetRecording = trumpetClip;
            EditorUtility.SetDirty(loop);

            // Debug marker
            var markerT = stimuli.Find("Sound Marker");
            var markerGo = markerT != null ? markerT.gameObject : null;
            if (markerGo == null)
            {
                markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerGo.name = "Sound Marker";
                markerGo.transform.SetParent(stimuli, false);
                Object.DestroyImmediate(markerGo.GetComponent<Collider>());
            }
            markerGo.transform.localScale = Vector3.one * 0.1f;
            markerGo.GetComponent<Renderer>().sharedMaterial = UnlitMat("Sound Marker", new Color(1f, 0.55f, 0.1f));
            var marker = Get<SourceMarker>(markerGo);
            marker.target = stimGo.transform;
            markerGo.SetActive(false);

            // reverb
            var reverbGo = Child(audio, "Reverb");
            var reverb = Get<ReverbTuner>(reverbGo);
            reverb.room = ShoeboxComponent();
            reverb.roomSize = logic.room;
            if (reverb.room == null) Debug.LogWarning(Log + "No shoebox in the scene yet: run Step 3, then Step 4 again.");
            EditorUtility.SetDirty(reverb);

            Undo.RecordObject(logic, "HumPerFinal audio");
            logic.stimulus = placed;
            logic.pulse = pulse;
            logic.picture = radio.transform;
            logic.anchors = loop;
            logic.reverb = reverb;
            logic.marker = marker;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 4: target, radio, anchors (duck 0°, trumpet +90°), marker and reverb linked; all sources routed to " + MixerPath + ". Save the scene.");
        }

        /// <summary>An anchor: spatial source at <paramref name="az"/> on the 2 m circle (room-fixed) with a box child 'Model' (hidden until the loop starts).</summary>
        static PlacedSource Anchor(Transform parent, string name, float az, Vector3 size, Material mat, UnityEngine.Audio.AudioMixerGroup group)
        {
            var go = Child(parent, name);
            go.transform.localPosition = Ring.Position(new Vector3(0f, EyeLevelM, 0f), Quaternion.identity, 2f, az);
            go.transform.localRotation = Ring.FacingOrigin(go.transform.localPosition, new Vector3(0f, EyeLevelM, 0f));
            Spatial(go, null, group); // the leveled clip is made at run time (AnchorLoop)
            var placed = Get<PlacedSource>(go);
            placed.Anchor = PlacedSource.Anchoring.RoomFixed;
            Box(go.transform, "Model", size, mat).SetActive(false);
            return placed;
        }

        // ---- Step 5: instruction slides (placeholders: C05's slides 3-5 until the PO supplies C06's) ------

        public const string SlidesFolder = Folder + "/Slides";
        public const string SlidesPath = SlidesFolder + "/Instructions.asset";

        static readonly (string id, string stem, string en, string zh)[] SlideList =
        {
            ("procedure", "3-procedure", "Procedure", "流程"),
            ("controllers", "4-controller", "Controllers", "控制器"),
            ("practice", "5-practise", "Practice rounds", "練習回合"),
        };

        /// <summary>Step 5: the SlideSet <c>Slides/Instructions.asset</c> from the fitted PNGs in Slides/ (copies of C05's, placeholders), linked to the chamber.</summary>
        [MenuItem("Magic/HumPerFinal/Step 5 - Instruction slides (placeholders)", priority = 114)]
        public static void SlidesStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var images = new Dictionary<string, Texture2D>();
            foreach (var s in SlideList)
                foreach (var lang in new[] { "en", "zh" })
                {
                    string path = SlidesFolder + "/" + lang + "-" + s.stem + ".png";
                    if (!File.Exists(path)) { Debug.LogError(Log + "Missing " + path); return; }
                    GPL.Protocol.Editor.SlideSetup.ImportForSlides(path);
                    images[lang + s.id] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            var set = GPL.Protocol.Editor.SlideSetup.EnsureSet(SlidesPath, () =>
            {
                var list = new List<GPL.Protocol.SlideSet.Slide>();
                foreach (var s in SlideList)
                    list.Add(new GPL.Protocol.SlideSet.Slide
                    {
                        id = s.id, titleEnglish = "", titleChinese = "", shortEnglish = s.en, shortChinese = s.zh,
                        imageEnglish = images["en" + s.id], imageChinese = images["zh" + s.id],
                    });
                return list;
            });
            Undo.RecordObject(logic, "HumPerFinal slides");
            logic.instructions = set;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 5: " + set.Count + " instruction slides linked (placeholders). Save the scene.");
        }

        // ---- Validate ---------------------------------------------------------------------------------------

        /// <summary>Checks the scene against Design.md: links, anchors, routing to the Reflection mixer, one shoebox of the room's size, the spatializer.</summary>
        [MenuItem("Magic/HumPerFinal/Validate Setup", priority = 140)]
        public static int ValidateSetup()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return -1;
            int problems = 0;
            void Problem(string m) { Debug.LogError(Log + "Validate: " + m); problems++; }
            var logic = Object.FindAnyObjectByType<HumPerFinalChamber>(FindObjectsInactive.Include);
            if (logic == null) { Problem("no HumPerFinalChamber (Step 1)"); return problems; }
            if (logic.cue == null || logic.listener == null) Problem("fixation dot or recenter listener not linked (Step 2)");
            if (logic.room == null) Problem("room not linked (Step 3)");
            if (logic.stimulus == null || logic.pulse == null || logic.picture == null || logic.marker == null) Problem("stimulus, pulse, radio or marker not linked (Step 4)");
            var a = logic.anchors;
            if (a == null || a.duck == null || a.trumpet == null || a.duckRecording == null || a.trumpetRecording == null || a.DuckModel == null || a.TrumpetModel == null)
                Problem("anchors not set up (duck, trumpet, recordings, models; Step 4)");
            else
            {
                foreach (var clip in new[] { a.duckRecording, a.trumpetRecording })
                    if (clip.loadType != AudioClipLoadType.DecompressOnLoad) Problem(clip.name + " is not Decompress On Load (leveling needs GetData)");
                if (a.duck.Anchor != PlacedSource.Anchoring.RoomFixed || a.trumpet.Anchor != PlacedSource.Anchoring.RoomFixed) Problem("anchors must be room-fixed");
            }
            if (logic.reverb == null || logic.reverb.room == null) Problem("reverb tuner or its shoebox missing (Steps 3-4)");
            if (GPL.Protocol.SlideSet.CountOf(logic.instructions) != 3) Problem("instruction slides not linked (Step 5)");
            if (logic.stimulus != null && logic.stimulus.GetComponent<AudioSource>().clip != null)
                Problem("stimulus still has a clip asset (the white noise is generated at run time)");
            foreach (var s in ReflectionMixer.Unrouted()) Problem(s.name + " is not routed to a mixer with the '" + ReflectionMixer.EffectName + "' effect (room would be dry)");
            var shoeboxes = Object.FindObjectsByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (shoeboxes.Length != 1) Problem(shoeboxes.Length + " shoebox components (need exactly 1)");
            else if (logic.room != null && (Mathf.Abs(shoeboxes[0].width - logic.room.width) > 1e-3f || Mathf.Abs(shoeboxes[0].depth - logic.room.depth) > 1e-3f || Mathf.Abs(shoeboxes[0].height - logic.room.height) > 1e-3f))
                Problem("shoebox size differs from the visual room");
            string spatializer = AudioSettings.GetSpatializerPluginName();
            if (spatializer != "Meta XR Audio") Problem("spatializer plugin is '" + spatializer + "', not 'Meta XR Audio' (Project Settings > Audio)");
            if (Lightmapping.lightingDataAsset == null) Debug.LogWarning(Log + "Validate: lighting not baked yet (Magic > HumPerFinal > Bake Lighting).");
            Debug.Log(Log + "Validate: " + (problems == 0 ? "OK." : problems + " problem(s)."));
            return problems;
        }

        // ---- helpers ------------------------------------------------------------------------------------------

        static HumPerFinalChamber Logic()
        {
            var logic = Object.FindAnyObjectByType<HumPerFinalChamber>(FindObjectsInactive.Include);
            if (logic == null) Debug.LogError(Log + "No HumPerFinalChamber in the scene: run Step 1 first.");
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

        /// <summary>A box child (no collider) of <paramref name="size"/> centred on the parent; an existing child (e.g. the PO's real model) is kept as it is.</summary>
        static GameObject Box(Transform parent, string name, Vector3 size, Material mat)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>AudioSource + MetaXRAudioSource as Meta's Spatial Audio block has them, unity gain, routed to <paramref name="group"/>.</summary>
        static AudioSource Spatial(GameObject go, AudioClip clip, UnityEngine.Audio.AudioMixerGroup group)
        {
            var s = Get<AudioSource>(go);
            s.clip = clip;
            s.playOnAwake = false;
            s.loop = false;
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

        static Material LitMat(string name, Color color) => Mat(name, color, "Universal Render Pipeline/Lit");
        static Material UnlitMat(string name, Color color) => Mat(name, color, "Universal Render Pipeline/Unlit");

        static Material Mat(string name, Color color, string shader)
        {
            UiSetup.EnsureFolder(MaterialsDir);
            string path = MaterialsDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        static ChamberDefinition EnsureDefinition()
        {
            var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(DefinitionPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ChamberDefinition>();
                def.id = HumPerFinalChamber.Id;
                def.label = "C06";
                def.title = "HumPerFinal";
                def.description = "Ventriloquist effect with simulated one-sided hearing loss and audio anchors (duck, trumpet) in a 0.7 s room.";
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
            if (def.thumbnail == null) // a free Hall cover until the PO picks one (docs/AssetCredits.md)
            {
                def.thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(CoverPath);
                EditorUtility.SetDirty(def);
                AssetDatabase.SaveAssets();
            }
            return def;
        }

        /// <summary>Headless (mirror project): Steps 1 to 5 (no bake), save, Validate (an error exit when it finds a problem).</summary>
        public static void BatchSteps()
        {
            ChamberStep();
            HomingStep();
            RoomStep();
            AudioStep();
            SlidesStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            if (ValidateSetup() != 0) EditorApplication.Exit(1);
        }
    }
}
