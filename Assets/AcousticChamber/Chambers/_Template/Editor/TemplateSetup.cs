using AcousticChamber.App;
using AcousticChamber.App.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AcousticChamber.Chambers.Template.Editor
{
    /// <summary>Magic steps of the Template chamber. Idempotent; they edit only the open Template scene.</summary>
    public static class TemplateSetup
    {
        public const string Folder = "Assets/AcousticChamber/Chambers/_Template";
        public const string ScenePath = Folder + "/Scenes/_Template.unity";
        public const string DefinitionPath = Folder + "/_Template.ChamberDefinition.asset";
        public const string DefaultParamsPath = Folder + "/Config/default.params";

        /// <summary>
        /// Step 1 (PB-07a): Config/default.params (written once from the schema) and the chamber menu with TemplateChamber in
        /// place of the stub panel.
        /// </summary>
        [MenuItem("Magic/Template/Step 1 - Chamber menu (open _Template)", priority = 100)]
        public static void ChamberMenuStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(DefinitionPath);
            if (def == null)
            {
                Debug.LogError("[Chamber:Template] " + DefinitionPath + " missing: run Magic > App > Step 4 - Template chamber stub.");
                return;
            }
            var text = ChamberSetup.EnsureDefaultParams(DefaultParamsPath,
                TemplateChamber.CreateSchema().Defaults().Serialize("Template (C00) preset Default. Design: Docs/Design.md"));
            if (ChamberSetup.AddChamberMenu<TemplateChamber>(def, text) == null) return;
            Debug.Log("[Chamber:Template] Step 1: chamber menu added. Save the scene.");
        }

        /// <summary>
        /// Step 2 (PB-07b): the Homing object (group Experiment: fixation dot with its URP Unlit material, recenter listener)
        /// and the links from TemplateChamber to it.
        /// </summary>
        [MenuItem("Magic/Template/Step 2 - Homing (fixation dot, recenter listener)", priority = 101)]
        public static void HomingStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Object.FindAnyObjectByType<TemplateChamber>(FindObjectsInactive.Include);
            if (logic == null)
            {
                Debug.LogError("[Chamber:Template] No TemplateChamber in the scene: run Step 1 first.");
                return;
            }
            var (cue, listener) = AppSetup.EnsureHoming();
            Undo.RecordObject(logic, "Template homing");
            logic.cue = cue;
            logic.listener = listener;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log("[Chamber:Template] Step 2: fixation dot and recenter listener added. Save the scene.");
        }

        /// <summary>Headless (mirror project): open the Template scene, run Steps 1 and 2, save.</summary>
        public static void BatchStep1()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ChamberMenuStep();
            HomingStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
