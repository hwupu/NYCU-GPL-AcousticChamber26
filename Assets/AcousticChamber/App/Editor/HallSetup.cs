using System.Collections.Generic;
using System.IO;
using System.Linq;
using GPL.UI;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AcousticChamber.App.Editor
{
    /// <summary>
    /// Magic steps of the Main Hall (PB-06a): the Sandbox scene for the module test panels, the Hall in MainHall, a stub
    /// Template chamber, and Sync Chambers (catalog + build scene list). Idempotent; the rig and other Building Blocks are
    /// never edited (new scenes are copies of MainHall, rig included).
    /// </summary>
    public static class HallSetup
    {
        public const string SandboxScene = "Assets/AcousticChamber/App/Scenes/Sandbox.unity";
        public const string CatalogPath = "Assets/AcousticChamber/App/ChamberCatalog.asset";
        public const string ChambersRoot = "Assets/AcousticChamber/Chambers";
        const string TemplateFolder = ChambersRoot + "/_Template";
        const string TemplateId = "Template";
        const string HallName = "Hall";
        const string StubName = "Chamber Stub";
        const string FadeName = "Scene Fade";
        const string LaserPath = "Assets/AcousticChamber/Settings/Materials/Laser.mat";

        /// <summary>Copies MainHall (with its test panel) to Sandbox.unity, once. The Sandbox is not in the build.</summary>
        [MenuItem("Magic/App/Step 2 - Sandbox scene (copy of MainHall)", priority = 2)]
        public static void CreateSandbox()
        {
            if (File.Exists(SandboxScene))
            {
                Debug.Log("[GPL] Sandbox exists: " + SandboxScene + ". Open it to use the test panels (Magic > App > ... Test Panel).");
                return;
            }
            if (!SaveIfDirty()) return;
            if (!AssetDatabase.CopyAsset(AppSetup.MainHallScene, SandboxScene))
            {
                Debug.LogError("[GPL] Could not copy " + AppSetup.MainHallScene + " to " + SandboxScene);
                return;
            }
            Debug.Log("[GPL] App Step 2: " + SandboxScene + " created (a copy of MainHall). It holds the module test panels; it is not in the build: open it and Play over Link.");
        }

        /// <summary>
        /// In the open MainHall: removes the test panels and adds the Hall (HallMenu on an overlay canvas, the catalog) and the
        /// scene fade.
        /// </summary>
        [MenuItem("Magic/App/Step 3 - Main Hall (open MainHall)", priority = 3)]
        public static void SetUpHall()
        {
            if (EditorSceneManager.GetActiveScene().path != AppSetup.MainHallScene)
            {
                Debug.LogError("[GPL] Open " + AppSetup.MainHallScene + " first.");
                return;
            }
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[GPL] Add the Camera Rig building block first.");
                return;
            }
            var theme = AppSetup.EnsureUiAssets();
            var catalog = EnsureCatalog();
            var ui = UiSetup.EnsureGroup("UI");
            UiSetup.EnsureUiRoot(ui, theme);
            AppSetup.RemoveTestPanels(ui);
            var canvas = UiSetup.CreateOverlayCanvas(ui, HallName, HallMenu.Size);
            canvas.AddComponent<HallMenu>().catalog = catalog;
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            EnsureSceneFade();
            AppSetup.EnsureMainHallFirstInBuild();
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] App Step 3: Main Hall set up (" + catalog.chambers.Count + " chambers in the catalog). Save the scene.");
        }

        /// <summary>
        /// The Template chamber as a stub (PB-06a): folder <c>Chambers/_Template</c>, its ChamberDefinition, and its scene
        /// (a copy of MainHall: same rig) with only a ‹ back to the Hall. Then Sync Chambers. The Template scene is left open.
        /// </summary>
        [MenuItem("Magic/App/Step 4 - Template chamber stub", priority = 4)]
        public static void CreateTemplateStub()
        {
            if (!SaveIfDirty()) return;
            string scenes = TemplateFolder + "/Scenes";
            UiSetup.EnsureFolder(scenes);
            string scenePath = scenes + "/_Template.unity";

            string defPath = TemplateFolder + "/_Template.ChamberDefinition.asset";
            var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(defPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ChamberDefinition>();
                def.id = TemplateId;
                def.label = "C00";
                def.title = "Template";
                def.description = "Minimal chamber to copy (stub until PB-07).";
                AssetDatabase.CreateAsset(def, defPath);
            }
            if (def.scenePath != scenePath)
            {
                def.scenePath = scenePath;
                EditorUtility.SetDirty(def);
            }

            if (!File.Exists(scenePath) && !AssetDatabase.CopyAsset(AppSetup.MainHallScene, scenePath))
            {
                Debug.LogError("[GPL] Could not copy " + AppSetup.MainHallScene + " to " + scenePath);
                return;
            }
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[GPL] " + scenePath + " has no Camera Rig (MainHall had none when it was copied). Delete the scene and run this step again.");
                return;
            }
            var theme = AppSetup.EnsureUiAssets();
            var ui = UiSetup.EnsureGroup("UI");
            UiSetup.EnsureUiRoot(ui, theme);
            AppSetup.RemoveTestPanels(ui);
            var hall = ui.Find(HallName);
            if (hall != null) Object.DestroyImmediate(hall.gameObject);
            var canvas = UiSetup.CreateOverlayCanvas(ui, StubName, ChamberStubPanel.Size);
            canvas.AddComponent<ChamberStubPanel>().chamber = def;
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            EnsureSceneFade();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            SyncChambers();
            // The Template scene stays open: switching scenes twice in one step left a destroyed OVRCameraRig's
            // before-render callback registered (the rig runs in edit mode).
            Debug.Log("[GPL] App Step 4: Template stub at " + TemplateFolder + " (scene " + scenePath + ", saved and open).");
        }

        /// <summary>
        /// Collects every ChamberDefinition under Chambers/ into the catalog (label, then ID), and sets the build scene list to
        /// MainHall + the chamber scenes. Chambers that break the rules are left out with an error.
        /// </summary>
        [MenuItem("Magic/App/Sync Chambers", priority = 20)]
        public static ChamberCatalog SyncChambers()
        {
            var catalog = EnsureCatalog();
            var found = new List<ChamberDefinition>();
            var ids = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(ChamberDefinition)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(path);
                if (def == null) continue;
                string dir = Path.GetDirectoryName(path).Replace('\\', '/');
                string problem = Path.GetDirectoryName(dir).Replace('\\', '/') != ChambersRoot
                    ? "it must be directly in a chamber folder under " + ChambersRoot
                    : ChamberRules.Problem(def.id, Path.GetFileName(dir));
                if (problem == null && !File.Exists(def.scenePath)) problem = "scene not found: '" + def.scenePath + "'";
                if (problem == null && !ids.Add(def.id)) problem = "the id '" + def.id + "' is used twice";
                if (problem != null)
                {
                    Debug.LogError("[GPL] Sync Chambers: " + path + " left out: " + problem);
                    continue;
                }
                found.Add(def);
            }
            found.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.label ?? "", b.label ?? "");
                return c != 0 ? c : string.CompareOrdinal(a.id, b.id);
            });
            catalog.chambers = found;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(AppSetup.MainHallScene, true) };
            scenes.AddRange(found.Select(d => new EditorBuildSettingsScene(d.scenePath, true)));
            var dropped = EditorBuildSettings.scenes.Select(s => s.path).Where(p => scenes.All(s => s.path != p)).ToList();
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[GPL] Sync Chambers: " + found.Count + " chambers (" + string.Join(", ", found.Select(d => d.MenuTitle)) + ")" +
                      (dropped.Count > 0 ? "; removed from the build: " + string.Join(", ", dropped) : ""));
            return catalog;
        }

        public static ChamberCatalog EnsureCatalog()
        {
            var c = AssetDatabase.LoadAssetAtPath<ChamberCatalog>(CatalogPath);
            if (c != null) return c;
            c = ScriptableObject.CreateInstance<ChamberCatalog>();
            AssetDatabase.CreateAsset(c, CatalogPath);
            AssetDatabase.SaveAssets();
            return c;
        }

        /// <summary>One SceneFade object (group XR) in the open scene.</summary>
        public static SceneFade EnsureSceneFade()
        {
            var existing = Object.FindAnyObjectByType<SceneFade>(FindObjectsInactive.Include);
            if (existing != null) return existing;
            var go = new GameObject(FadeName);
            go.transform.SetParent(UiSetup.EnsureGroup("XR"), false);
            Undo.RegisterCreatedObjectUndo(go, FadeName);
            return go.AddComponent<SceneFade>();
        }

        /// <summary>Saves modified open scenes (asks in the Editor; saves silently in batch mode). False if the user cancels.</summary>
        static bool SaveIfDirty()
        {
            if (Application.isBatchMode)
            {
                EditorSceneManager.SaveOpenScenes();
                return true;
            }
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        /// <summary>Headless (mirror project): Sandbox, Hall in MainHall, Template stub, Sync; all saved.</summary>
        public static void BatchHall()
        {
            CreateSandbox();
            EditorSceneManager.OpenScene(AppSetup.MainHallScene, OpenSceneMode.Single);
            SetUpHall();
            EditorSceneManager.SaveOpenScenes();
            CreateTemplateStub();
            AssetDatabase.SaveAssets();
        }
    }
}
