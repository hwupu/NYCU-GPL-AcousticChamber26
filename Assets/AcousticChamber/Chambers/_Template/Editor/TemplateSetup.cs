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

        /// <summary>Headless (mirror project): open the Template scene, run Step 1, save.</summary>
        public static void BatchStep1()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ChamberMenuStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
