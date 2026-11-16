using AcousticChamber.App;
using AcousticChamber.App.Editor;
using GPL.Acoustics;
using GPL.Acoustics.Editor;
using GPL.UI;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AcousticChamber.App.Editor.ExperienceScene;

namespace AcousticChamber.Chambers.Reverberation.Editor
{
    /// <summary>
    /// Magic steps of the Reverberation chamber (C04, Docs/Design.md, Docs/SceneSetup.md). Idempotent; they edit only the
    /// Reverberation scene and never touch the Building Blocks.
    /// </summary>
    public static class ReverberationSetup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/Reverberation";
        public const string ScenePath = Folder + "/Scenes/Reverberation.unity";
        public const string DefinitionPath = Folder + "/Reverberation.ChamberDefinition.asset";
        public const string MaterialsDir = Folder + "/Materials";
        public const string MixerPath = Folder + "/Audio/ReverberationMixer.mixer";
        /// <summary>The Hall cover (the spare of the pool, docs/AssetCredits.md).</summary>
        public const string CoverPath = "Assets/AcousticChamber/App/Covers/IMG-C-03.jpeg";
        const string PanelName = "Experience Panel";
        const string Log = "[Chamber:Reverberation] ";

        /// <summary>Step 1: the ChamberDefinition (C04, cover IMG-C-03), the scene (a copy of MainHall), the panel with ReverberationExperience, Sync Chambers.</summary>
        [MenuItem("Magic/Reverberation/Step 1 - Chamber, scene and panel", priority = 140)]
        public static void ChamberStep()
        {
            var def = EnsureDefinition(DefinitionPath, ScenePath, ReverberationExperience.Id, "C04", "Reverberation",
                                       "Calibrate the reverberation of a room size on this headset: RT60 sweep and check.");
            if (def != null && def.thumbnail == null)
            {
                def.thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(CoverPath);
                EditorUtility.SetDirty(def);
                AssetDatabase.SaveAssets();
            }
            var logic = BuildPanelScene<ReverberationExperience>(Log, ScenePath, PanelName, ReverberationExperience.Size, PanelYawDeg, PanelPitchDeg);
            if (logic == null) return;
            Debug.Log(Log + "Step 1: panel added, scene saved, catalog synced.");
        }

        /// <summary>The panel hangs straight ahead (PO 2026-11-17; C01 / C02 hang 35° left), 8° below the eye line.</summary>
        const float PanelYawDeg = 0f, PanelPitchDeg = 8f;

        /// <summary>Hot Fix (2026-11-17): centers the existing panel without rebuilding it (Step 1 would drop the Step 2 links).</summary>
        [MenuItem("Magic/Reverberation/Hot Fix - Center panel", priority = 160)]
        public static void CenterPanel()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var panel = logic.GetComponent<OverlayPanel>();
            if (panel == null) { Debug.LogError(Log + "The panel is not an overlay canvas (Step 1)."); return; }
            if (Mathf.Approximately(panel.yawOffsetDeg, PanelYawDeg) && Mathf.Approximately(panel.pitchDownDeg, PanelPitchDeg))
            {
                Debug.Log(Log + "Hot Fix: the panel is already centered.");
                return;
            }
            Undo.RecordObject(panel, "Center panel");
            panel.yawOffsetDeg = PanelYawDeg;
            panel.pitchDownDeg = PanelPitchDeg;
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Debug.Log(Log + "Hot Fix: panel centered (yaw 0°). Save the scene.");
        }

        /// <summary>
        /// Step 2: the floor, the Reflection mixer (the RT60 probe is routed to it), Environment/Shoebox Acoustics (open space, locked to
        /// the listener, clutter 0; the panel sets its size) and Audio/Reverb; links them to the panel.
        /// </summary>
        [MenuItem("Magic/Reverberation/Step 2 - Floor, mixer and room", priority = 141)]
        public static void RoomStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Logic();
            if (logic == null) return;
            var group = ReflectionMixer.Ensure(MixerPath);
            if (group == null) return;

            EnsureFloor(MaterialsDir);
            var audio = UiSetup.EnsureGroup("Audio");
            var shoeboxGo = Child(UiSetup.EnsureGroup("Environment"), "Shoebox Acoustics");
            var shoebox = Get<MetaXRAudioRoomAcousticProperties>(shoeboxGo);
            shoebox.lockPositionToListener = true;
            shoebox.width = Rt60Protocol.OpenSpaceWidth; shoebox.depth = Rt60Protocol.OpenSpaceDepth; shoebox.height = Rt60Protocol.OpenSpaceHeight;
            shoebox.clutterFactor = 0f;
            shoebox.leftMaterial = shoebox.rightMaterial = shoebox.frontMaterial = shoebox.backMaterial =
                shoebox.ceilingMaterial = shoebox.floorMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.Concrete;
            EditorUtility.SetDirty(shoebox);
            var reverb = Get<ReverbTuner>(Child(audio, "Reverb"));
            reverb.room = shoebox;
            reverb.roomSize = null; // open space: the panel sets the shoebox size
            EditorUtility.SetDirty(reverb);

            Undo.RecordObject(logic, "Reverberation links");
            logic.reverb = reverb;
            logic.output = group;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log(Log + "Step 2: floor, mixer and room linked. Save the scene.");
        }

        /// <summary>Checks the scene against Design.md.</summary>
        [MenuItem("Magic/Reverberation/Validate Setup", priority = 150)]
        public static int ValidateSetup()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return -1;
            int problems = 0;
            void Problem(string m) { Debug.LogError(Log + "Validate: " + m); problems++; }
            var logic = Object.FindAnyObjectByType<ReverberationExperience>(FindObjectsInactive.Include);
            if (logic == null) { Problem("no ReverberationExperience (Step 1)"); return problems; }
            var overlay = logic.GetComponent<OverlayPanel>();
            if (overlay == null) Problem("the panel is not an overlay canvas (Step 1)");
            else if (!Mathf.Approximately(overlay.yawOffsetDeg, PanelYawDeg)) Problem("the panel is not centered (Hot Fix - Center panel)");
            if (Object.FindAnyObjectByType<GPL.Input.GlobalActions>(FindObjectsInactive.Include) == null) Problem("no Global Actions (Step 1)");
            if (Object.FindAnyObjectByType<UiPointer>(FindObjectsInactive.Include) == null) Problem("no UI Pointer (Step 1)");
            if (logic.reverb == null || logic.reverb.room == null) Problem("reverb or shoebox not linked (Step 2)");
            else if (logic.reverb.roomSize != null) Problem("the reverb follows a visible room (ShoeboxRoom); C04 is open space (Step 2)");
            if (logic.output == null) Problem("mixer group not linked (Step 2)");
            else if (logic.output.audioMixer == null || AssetDatabase.GetAssetPath(logic.output.audioMixer) != MixerPath) Problem("mixer group is not " + MixerPath + " (Step 2)");
            foreach (var s in ReflectionMixer.Unrouted())
                Problem(s.name + " is not routed to a mixer with the '" + ReflectionMixer.EffectName + "' effect (room would be dry)");
            if (Object.FindObjectsByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include).Length != 1)
                Problem("the scene needs exactly one shoebox (Meta allows one)");
            else if (!Object.FindAnyObjectByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include).lockPositionToListener)
                Problem("the shoebox must be locked to the listener (Step 2)");
            string spatializer = AudioSettings.GetSpatializerPluginName();
            if (spatializer != "Meta XR Audio") Problem("spatializer plugin is '" + spatializer + "', not 'Meta XR Audio' (Project Settings > Audio)");
            Debug.Log(Log + "Validate: " + (problems == 0 ? "OK." : problems + " problem(s)."));
            return problems;
        }

        /// <summary>Headless (mirror project): Steps 1 and 2, save, Validate (an error exit when it finds a problem).</summary>
        public static void BatchSteps()
        {
            ChamberStep();
            RoomStep();
            CenterPanel();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            if (ValidateSetup() != 0) EditorApplication.Exit(1);
        }

        static ReverberationExperience Logic()
        {
            var logic = Object.FindAnyObjectByType<ReverberationExperience>(FindObjectsInactive.Include);
            if (logic == null) Debug.LogError(Log + "No ReverberationExperience in the scene: run Step 1 first.");
            return logic;
        }
    }
}
