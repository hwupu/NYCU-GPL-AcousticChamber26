using System.IO;
using AcousticChamber.App;
using AcousticChamber.App.Editor;
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

        /// <summary>Headless (mirror project): Steps 1 and 2, save.</summary>
        public static void BatchSteps()
        {
            ChamberStep();
            HomingStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
