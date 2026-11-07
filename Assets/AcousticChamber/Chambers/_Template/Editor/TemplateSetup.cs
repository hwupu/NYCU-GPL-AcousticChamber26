using AcousticChamber.App;
using AcousticChamber.App.Editor;
using GPL.Protocol;
using GPL.Protocol.Editor;
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

        public const string SlidesFolder = Folder + "/Slides";
        public const string SlidesPath = SlidesFolder + "/Instructions.asset";
        const string Placeholder1 = "Placeholder-1.png", Placeholder2 = "Placeholder-2.png";
        static readonly Color32 Fill1 = new Color32(0x3A, 0x3A, 0x40, 0xFF), Fill2 = new Color32(0x4A, 0x4A, 0x52, 0xFF);

        /// <summary>
        /// Step 3 (PB-20a): two gray placeholder slides (2000 x 936 px, a thin frame marks the 48 u safe margin) in
        /// Slides/, the SlideSet Slides/Instructions.asset, and the link from TemplateChamber to it. Existing images and an
        /// existing SlideSet are kept (replace the PNGs with real illustrations, edit the asset for titles).
        /// </summary>
        [MenuItem("Magic/Template/Step 3 - Instruction slides (placeholders)", priority = 102)]
        public static void SlidesStep()
        {
            if (!ChamberSetup.IsOpen(ScenePath)) return;
            var logic = Object.FindAnyObjectByType<TemplateChamber>(FindObjectsInactive.Include);
            if (logic == null)
            {
                Debug.LogError("[Chamber:Template] No TemplateChamber in the scene: run Step 1 first.");
                return;
            }
            SlideSetup.EnsureFolder(SlidesFolder);

            // existing placeholders of an older size are redrawn at 2000 x 936 (real illustrations have other names)
            string p1 = SlidesFolder + "/" + Placeholder1, p2 = SlidesFolder + "/" + Placeholder2;
            if (System.IO.File.Exists(p1)) SlideSetup.Placeholder(p1, Fill1);
            if (System.IO.File.Exists(p2)) SlideSetup.Placeholder(p2, Fill2);
            var set = SlideSetup.EnsureSet(SlidesPath, () => new[]
            {
                new SlideSet.Slide
                {
                    id = "placeholder_1", titleEnglish = "Placeholder slide 1", titleChinese = "示範投影片 1", shortEnglish = "Slide 1", shortChinese = "投影片 1",
                    imageEnglish = SlideSetup.Placeholder(p1, Fill1),
                },
                new SlideSet.Slide
                {
                    id = "placeholder_2", titleEnglish = "Placeholder slide 2", titleChinese = "示範投影片 2", shortEnglish = "Slide 2", shortChinese = "投影片 2",
                    imageEnglish = SlideSetup.Placeholder(p2, Fill2),
                },
            });

            Undo.RecordObject(logic, "Template slides");
            logic.instructions = set;
            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
            Debug.Log("[Chamber:Template] Step 3: instruction slides linked (" + set.Count + " slides). Save the scene.");
        }

        /// <summary>Headless (mirror project): open the Template scene, run Steps 1 to 3, save.</summary>
        public static void BatchStep1()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ChamberMenuStep();
            HomingStep();
            SlidesStep();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
