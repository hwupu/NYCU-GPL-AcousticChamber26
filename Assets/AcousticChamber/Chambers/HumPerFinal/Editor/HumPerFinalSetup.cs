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
    /// scene and never touch the Building Blocks (the Camera Rig comes with the copy of MainHall). The dining room and the real models come from Models/ (HumPerFinalModels).
    /// </summary>
    public static class HumPerFinalSetup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/HumPerFinal";
        public const string ScenePath = Folder + "/Scenes/HumPerFinal.unity";
        public const string DefinitionPath = Folder + "/HumPerFinal.ChamberDefinition.asset";
        public const string DefaultParamsPath = Folder + "/Config/default.params";
        public const string CoverPath = "Assets/AcousticChamber/App/Covers/IMG-C-06.jpeg";
        const string Log = "[Chamber:HumPerFinal] ";

        /// <summary>Step 1: the ChamberDefinition (C05), the scene (a copy of MainHall without the Hall panel), Config/default.params and the chamber menu; Sync Chambers.</summary>
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
                HumPerFinalChamber.CreateDefaults().Serialize("HumPerFinal (C05) preset Default. Design: Docs/Design.md"));
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

        // ---- Step 3: the dining room and its lighting (PO 2026-11-18) ----------------------------------------

        public const string LightingPath = Folder + "/Settings/HumPerFinal.lighting";
        public const string MaterialsDir = Folder + "/Materials";
        public const string PropLightName = "Prop Light";
        /// <summary>Flat ambient for the Lit props (the room itself is Unlit with baked textures).</summary>
        const float Ambient = 0.55f;

        /// <summary>
        /// Step 3 (Design.md "Posture and setting"): <c>Environment/Room</c> = the dining room from <c>Models/Room.fbx</c> (URP Unlit,
        /// the light is baked into its textures), the participant's seat on the origin and the table ahead (+z); the room info and
        /// <c>Environment/Shoebox Acoustics</c> stay 8.72 × 15 × 3.5 m (the calibrated acoustic room); no lightmaps; one real-time
        /// light from the window side (<c>Environment/Prop Light</c>, no shadows) and flat ambient for the Lit props. Rebuilds Room,
        /// Shoebox Acoustics and Prop Light; nothing else.
        /// </summary>
        [MenuItem("Magic/HumPerFinal/Step 3 - Room and lighting", priority = 112)]
        public static void RoomStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var man = HumPerFinalModels.Load();
            if (man == null) return;
            HumPerFinalModels.ConfigureImports(man);
            var mats = HumPerFinalModels.Materials(man);

            var env = UiSetup.EnsureGroup("Environment");
            foreach (var n in new[] { RoomBuilder.RoomName, RoomBuilder.ShoeboxName, PropLightName })
                for (int i = env.childCount - 1; i >= 0; i--)
                    if (env.GetChild(i).name == n) Undo.DestroyObjectImmediate(env.GetChild(i).gameObject);

            var size = RoomBuilder.Options.Default(MaterialsDir);
            var roomGo = new GameObject(RoomBuilder.RoomName);
            roomGo.transform.SetParent(env, false);
            Undo.RegisterCreatedObjectUndo(roomGo, "HumPerFinal room");
            var room = roomGo.AddComponent<ShoeboxRoom>();
            room.width = size.Width; room.depth = size.Depth; room.height = size.Height;
            var roomModel = HumPerFinalModels.Place(man, HumPerFinalModels.Room, roomGo.transform, "Model", mats, true);
            if (roomModel == null) return;
            WindowBackdrop(roomGo.transform, roomModel.transform);
            RoomBuilder.Shoebox(env, size);

            var light = new GameObject(PropLightName).AddComponent<Light>();
            light.transform.SetParent(env, false);
            light.transform.localRotation = Quaternion.Euler(50f, 90f, 0f); // from the window (left) toward the right, 50° down
            light.type = LightType.Directional;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.intensity = 1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.None;
            Undo.RegisterCreatedObjectUndo(light.gameObject, "HumPerFinal prop light");
            ApplyLighting(light);

            Undo.RecordObject(logic, "HumPerFinal room");
            logic.room = room;
            if (logic.reverb != null) { logic.reverb.room = ShoeboxComponent(); logic.reverb.roomSize = room; EditorUtility.SetDirty(logic.reverb); }
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 3: dining room placed (seat on the origin, table ahead), shoebox " + size.Width + " × " + size.Depth + " × " +
                      size.Height + " m, prop light; no lightmaps. Save the scene.");
        }

        /// <summary>
        /// A bright Unlit quad 0.3 m outside the window ("Window Backdrop", child of the room): between the blind's slats the eye
        /// cameras would otherwise show their clear colour (the Camera Rig is a Building Block and is not changed).
        /// </summary>
        static void WindowBackdrop(Transform room, Transform roomModel)
        {
            Renderer window = null;
            foreach (var r in roomModel.GetComponentsInChildren<Renderer>(true))
                if (r.name == "window") { window = r; break; }
            if (window == null) { Debug.LogWarning(Log + "No 'window' in the room model: no backdrop."); return; }
            var b = window.bounds;
            bool alongX = b.size.x < b.size.z; // the window lies in a wall facing ±x
            var outward = alongX ? new Vector3(Mathf.Sign(b.center.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(b.center.z));
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Window Backdrop";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(room, false);
            quad.transform.position = b.center + outward * 0.3f;
            quad.transform.rotation = Quaternion.LookRotation(outward, Vector3.up); // a quad faces -forward: toward the room
            quad.transform.localScale = new Vector3((alongX ? b.size.z : b.size.x) + 4f, b.size.y + 1f, 1f); // generous: it is outside the wall
            var mat = UnlitMat("Window Backdrop", new Color(0.93f, 0.95f, 0.98f));
            quad.GetComponent<Renderer>().sharedMaterial = mat;
            quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(quad, StaticEditorFlags.BatchingStatic);
            Undo.RegisterCreatedObjectUndo(quad, "HumPerFinal window backdrop");
        }

        /// <summary>No baked GI (the room's light is in its textures), flat ambient, no skybox, the prop light as the sun; drops the old lightmaps of the white HW2 room.</summary>
        static void ApplyLighting(Light sun)
        {
            UiSetup.EnsureFolder(Folder + "/Settings");
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            if (ls == null) { ls = new LightingSettings(); AssetDatabase.CreateAsset(ls, LightingPath); }
            ls.bakedGI = false;
            ls.realtimeGI = false;
            ls.autoGenerate = false;
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;
            Lightmapping.lightingDataAsset = null;
            RenderSettings.skybox = null;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(Ambient, Ambient, Ambient);
        }

        // ---- Step 4: target, radio, anchors, mixer -------------------------------------------------------

        public const string AudioDir = Folder + "/Audio";
        public const string MixerPath = AudioDir + "/HumPerFinalMixer.mixer";
        /// <summary>Anchor recordings from the GPL.Stimuli library (Design.md "Stimuli"; PO 2026-11-17: Freesound 348791 for the duck, the library's trumpet).</summary>
        public const string DuckClipId = "freesound_toy_ducks_quacking", TrumpetClipId = "openair_baroque_trumpet_fanfare";
        public static string DuckClipPath => GPL.Stimuli.StimulusLibrary.AssetPath(GPL.Stimuli.StimulusLibrary.Find(DuckClipId));
        public static string TrumpetClipPath => GPL.Stimuli.StimulusLibrary.AssetPath(GPL.Stimuli.StimulusLibrary.Find(TrumpetClipId));
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
            stimGo.transform.localPosition = new Vector3(0f, EyeLevelM, 3f);
            Spatial(stimGo, null, group);
            var placed = Get<PlacedSource>(stimGo);
            placed.Anchor = PlacedSource.Anchoring.Replaced;
            var pulse = Get<PulseTrain>(stimGo);

            // radio (box placeholder)
            var radio = Child(stimuli, "Radio");
            radio.transform.localPosition = new Vector3(0f, EyeLevelM, 3f);
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

            // the real models (PO 2026-11-18) replace the boxes; the PO's own Model children are kept
            loop.duckModelY = loop.trumpetModelY = float.NaN;
            if (HumPerFinalModels.Available)
            {
                var man = HumPerFinalModels.Load();
                if (man == null) return;
                HumPerFinalModels.ConfigureImports(man);
                var mats = HumPerFinalModels.Materials(man);
                bool duckOk = UseModel(loop.duck.transform, HumPerFinalModels.Duck, man, mats, false);
                bool trumpetOk = UseModel(loop.trumpet.transform, HumPerFinalModels.TrumpetPlayer, man, mats, false);
                UseModel(radio.transform, HumPerFinalModels.Radio, man, mats, true);
                if (duckOk) loop.duckModelY = man.duckSurfaceY;         // on the table top
                if (trumpetOk) loop.trumpetModelY = man.trumpetSurfaceY; // the player on the floor
            }
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

        /// <summary>An anchor: spatial source at <paramref name="az"/> on the 3 m circle (room-fixed) with a box child 'Model' (hidden until the loop starts).</summary>
        static PlacedSource Anchor(Transform parent, string name, float az, Vector3 size, Material mat, UnityEngine.Audio.AudioMixerGroup group)
        {
            var go = Child(parent, name);
            go.transform.localPosition = Ring.Position(new Vector3(0f, EyeLevelM, 0f), Quaternion.identity, 3f, az);
            go.transform.localRotation = Ring.FacingOrigin(go.transform.localPosition, new Vector3(0f, EyeLevelM, 0f));
            Spatial(go, null, group); // the leveled clip is made at run time (AnchorLoop)
            var placed = Get<PlacedSource>(go);
            placed.Anchor = PlacedSource.Anchoring.RoomFixed;
            Box(go.transform, "Model", size, mat).SetActive(false);
            return placed;
        }

        /// <summary>
        /// Replaces <paramref name="parent"/>'s 'Model' with <paramref name="file"/> when it is the box placeholder or an earlier copy
        /// of the same model; the PO's own model is kept (returns false then). The new 'Model' is active only when
        /// <paramref name="active"/> (the anchors' models are shown by AnchorLoop).
        /// </summary>
        static bool UseModel(Transform parent, string file, HumPerFinalModels.Manifest man, Dictionary<string, Material> mats, bool active)
        {
            var old = parent.Find("Model");
            if (old != null && !HumPerFinalModels.IsBox(old) && !HumPerFinalModels.IsOurs(old, file))
            {
                Debug.Log(Log + parent.name + "/Model is not the placeholder: kept as it is.");
                return false;
            }
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var holder = new GameObject("Model");
            holder.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(holder, "HumPerFinal model");
            if (HumPerFinalModels.Place(man, file, holder.transform, Path.GetFileNameWithoutExtension(file), mats, false) == null) return false;
            holder.SetActive(active);
            return true;
        }

        // ---- Step 5: onboarding slides (the PO's C05 illustrations, 2026-11-17) ------------------------------

        public const string SlidesFolder = Folder + "/Slides";
        public const string SlidesPath = SlidesFolder + "/Instructions.asset";

        /// <summary>
        /// The C05 onboarding slides (PO 2026-11-17): id, file stem (<c>Slides/&lt;en|zh&gt;-&lt;stem&gt;.jpg</c>, 2000 × 936), title
        /// English / 中文, and the label of the Back / Next buttons that lead to the slide (empty = the title). Slide 1 is the
        /// recenter slide (stage <c>recenter</c>; its buttons read "Back" / "上一頁"); 2-5 are the instruction deck. The last one
        /// announces the fixation dot; the dot itself is the stock <c>ready</c> stage (GPL.Protocol with the GPL.Homing gate).
        /// </summary>
        static readonly (string id, string stem, string en, string zh, string shortEn, string shortZh)[] SlideList =
        {
            ("recenter", "1-reposition", "Before we start", "開始之前", "Back", "上一頁"),
            ("procedure", "2-procedure", "Procedure", "實驗說明", "", ""),
            ("controllers", "3-controller", "Controllers", "操作說明", "", ""),
            ("practice", "4-practice", "Trials", "練習回合", "", ""),
            ("ready", "5-black-dot", "Ready?", "準備就緒？", "", ""),
        };

        /// <summary>The number of instruction slides (all but the recenter slide).</summary>
        public const int InstructionCount = 4;

        /// <summary>
        /// Step 5: the recenter slide on the chamber (slide 1) and the SlideSet <c>Slides/Instructions.asset</c> (slides 2-5) from the
        /// PO's JPGs, linked to the chamber. The deck is rewritten from <see cref="SlideList"/> every time (titles and images live
        /// here, not in the Inspector); the asset and its GUID stay.
        /// </summary>
        [MenuItem("Magic/HumPerFinal/Step 5 - Onboarding slides", priority = 114)]
        public static void SlidesStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var slides = new List<GPL.Protocol.SlideSet.Slide>();
            foreach (var s in SlideList)
            {
                var image = new Dictionary<string, Texture2D>();
                foreach (var lang in new[] { "en", "zh" })
                {
                    string path = SlidesFolder + "/" + lang + "-" + s.stem + ".jpg";
                    if (!File.Exists(path)) { Debug.LogError(Log + "Missing " + path); return; }
                    GPL.Protocol.Editor.SlideSetup.ImportForSlides(path);
                    image[lang] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (image[lang] == null) { Debug.LogError(Log + "Not a texture: " + path); return; }
                    if (image[lang].width != GPL.Protocol.Editor.SlideSetup.Width || image[lang].height != GPL.Protocol.Editor.SlideSetup.Height)
                        Debug.LogWarning(Log + path + " is " + image[lang].width + " × " + image[lang].height + ", slides are " +
                                         GPL.Protocol.Editor.SlideSetup.Width + " × " + GPL.Protocol.Editor.SlideSetup.Height + ".");
                }
                slides.Add(new GPL.Protocol.SlideSet.Slide
                {
                    id = s.id, titleEnglish = s.en, titleChinese = s.zh, shortEnglish = s.shortEn, shortChinese = s.shortZh,
                    imageEnglish = image["en"], imageChinese = image["zh"],
                });
            }
            var set = GPL.Protocol.Editor.SlideSetup.EnsureSet(SlidesPath, null);
            Undo.RecordObject(set, "HumPerFinal slides");
            set.slides = slides.GetRange(1, InstructionCount);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Undo.RecordObject(logic, "HumPerFinal slides");
            logic.recenterSlide = slides[0];
            logic.instructions = set;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 5: recenter slide and " + set.Count + " instruction slides linked. Save the scene.");
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
            if (GPL.Protocol.SlideSet.CountOf(logic.instructions) != InstructionCount) Problem("instruction slides not linked (Step 5)");
            if (logic.recenterSlide == null || logic.recenterSlide.imageEnglish == null) Problem("recenter slide has no illustration (Step 5)");
            if (logic.stimulus != null && logic.stimulus.GetComponent<AudioSource>().clip != null)
                Problem("stimulus still has a clip asset (the white noise is generated at run time)");
            foreach (var s in ReflectionMixer.Unrouted()) Problem(s.name + " is not routed to a mixer with the '" + ReflectionMixer.EffectName + "' effect (room would be dry)");
            var shoeboxes = Object.FindObjectsByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (shoeboxes.Length != 1) Problem(shoeboxes.Length + " shoebox components (need exactly 1)");
            else if (logic.room != null && (Mathf.Abs(shoeboxes[0].width - logic.room.width) > 1e-3f || Mathf.Abs(shoeboxes[0].depth - logic.room.depth) > 1e-3f || Mathf.Abs(shoeboxes[0].height - logic.room.height) > 1e-3f))
                Problem("shoebox size differs from the visual room");
            string spatializer = AudioSettings.GetSpatializerPluginName();
            if (spatializer != "Meta XR Audio") Problem("spatializer plugin is '" + spatializer + "', not 'Meta XR Audio' (Project Settings > Audio)");
            // the dining room and the models (PO 2026-11-18)
            if (logic.room != null)
            {
                var roomModel = logic.room.transform.Find("Model");
                if (roomModel == null || roomModel.GetComponentsInChildren<Renderer>(true).Length == 0) Problem("Environment/Room has no dining-room model (Step 3)");
                foreach (var r in HumPerFinalModels.MissingMaterials(roomModel)) Problem("room renderer '" + r + "' has a missing material (Step 3)");
            }
            if (a != null && HumPerFinalModels.Available)
            {
                var models = new (Transform model, string file)[]
                {
                    (a.DuckModel != null ? a.DuckModel.transform : null, HumPerFinalModels.Duck),
                    (a.TrumpetModel != null ? a.TrumpetModel.transform : null, HumPerFinalModels.TrumpetPlayer),
                    (logic.picture != null ? logic.picture.Find("Model") : null, HumPerFinalModels.Radio),
                };
                foreach (var (model, file) in models)
                {
                    if (HumPerFinalModels.IsBox(model)) Debug.LogWarning(Log + "Validate: " + file + " not used yet, a box is shown (Step 4).");
                    foreach (var r in HumPerFinalModels.MissingMaterials(model)) Problem(file + " renderer '" + r + "' has a missing material (Step 4)");
                }
                if (HumPerFinalModels.IsOurs(models[0].model, HumPerFinalModels.Duck) && float.IsNaN(a.duckModelY)) Problem("duck model height not set (Step 4)");
                if (HumPerFinalModels.IsOurs(models[1].model, HumPerFinalModels.TrumpetPlayer) && float.IsNaN(a.trumpetModelY)) Problem("trumpet player height not set (Step 4)");
            }
            int directional = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (l.type == LightType.Directional) directional++;
            if (directional != 1) Debug.LogWarning(Log + "Validate: " + directional + " directional lights (expected 1, the Prop Light of Step 3).");
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
                def.label = "C05";
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

        /// <summary>Headless (mirror project): Steps 1 to 5, save, Validate (an error exit when it finds a problem).</summary>
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
