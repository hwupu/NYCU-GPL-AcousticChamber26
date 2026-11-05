using System.Collections.Generic;
using System.Linq;
using AcousticChamber.App;
using GPL.Input;
using GPL.UI;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AcousticChamber.App.Editor
{
    /// <summary>
    /// Magic menu steps of the app shell. Idempotent; they edit only the open scene and never touch the Meta Building
    /// Blocks added by hand.
    /// </summary>
    public static class AppSetup
    {
        public const string MainHallScene = "Assets/AcousticChamber/App/Scenes/MainHall.unity";
        const string FontsDir = "Assets/AcousticChamber/Settings/Fonts";
        const string ThemePath = "Assets/AcousticChamber/Settings/UiTheme.asset";
        const string LaserPath = "Assets/AcousticChamber/Settings/Materials/Laser.mat";
        const string GroupUI = "UI";
        const string GroupXR = "XR";
        const string TestPanelName = "UI Test Panel";
        const string InputTestPanelName = "Input Test Panel";
        const string BenchTestPanelName = "Debug Bench Test Panel";

        [MenuItem("Magic/App/Step 1 - UI theme and fonts", priority = 1)]
        public static UiTheme EnsureUiAssets()
        {
            var regular = UiSetup.EnsureFontAsset(FontsDir + "/NotoSansTC-Regular.otf", FontsDir + "/NotoSansTC-Regular SDF.asset");
            var bold = UiSetup.EnsureFontAsset(FontsDir + "/NotoSansTC-Bold.otf", FontsDir + "/NotoSansTC-Bold SDF.asset");
            var theme = UiSetup.EnsureTheme(ThemePath, regular, bold);
            UiSetup.EnsureUnlitMaterial(LaserPath, Color.white);
            Debug.Log("[GPL] App Step 1: UI theme " + ThemePath + " (fonts: " + (regular != null ? regular.name : "none") + ", " +
                      (bold != null ? bold.name : "none") + ")");
            return theme;
        }

        /// <summary>
        /// Adds the GPL.UI test panel (UiShowcase) to the open scene, with UiRoot and UiPointer, and puts the MainHall first in
        /// the build scene list. Temporary headset check for PB-05a; the Hall menu (PB-06) replaces it.
        /// </summary>
        [MenuItem("Magic/App/UI Test Panel (open scene)", priority = 50)]
        public static void AddUiTestPanel()
        {
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[GPL] Add the Camera Rig building block first.");
                return;
            }
            var theme = EnsureUiAssets();
            var ui = UiSetup.EnsureGroup(GroupUI);
            UiSetup.EnsureUiRoot(ui, theme);
            RemoveTestPanels(ui); // the test panels sit at the same spot
            var canvas = UiSetup.CreateOverlayCanvas(ui, TestPanelName, UiShowcase.Size);
            canvas.AddComponent<UiShowcase>();
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            EnsureMainHallFirstInBuild();
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] UI test panel added under '" + GroupUI + "'. Save the scene, then Play (Quest Link) or build.");
        }

        /// <summary>
        /// Adds the GPL.Input test panel (InputTestPanel + StopDialogHost) and a GlobalActions object to the open scene; it
        /// replaces the UI test panel. Temporary headset check for PB-05b; the Hall menu (PB-06) replaces it.
        /// </summary>
        [MenuItem("Magic/App/Input Test Panel (open scene)", priority = 51)]
        public static void AddInputTestPanel()
        {
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[GPL] Add the Camera Rig building block first.");
                return;
            }
            var theme = EnsureUiAssets();
            EnsureGlobalActions();
            var ui = UiSetup.EnsureGroup(GroupUI);
            UiSetup.EnsureUiRoot(ui, theme);
            RemoveTestPanels(ui);
            var canvas = UiSetup.CreateOverlayCanvas(ui, InputTestPanelName, InputTestPanel.Size);
            canvas.AddComponent<StopDialogHost>().canvas = canvas.transform;
            canvas.AddComponent<InputTestPanel>();
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            EnsureMainHallFirstInBuild();
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] Input test panel added under '" + GroupUI + "'. Save the scene, then Play (Quest Link) or build.");
        }

        /// <summary>One GlobalActions object (group XR) in the open scene.</summary>
        public static GlobalActions EnsureGlobalActions()
        {
            var existing = Object.FindAnyObjectByType<GlobalActions>(FindObjectsInactive.Include);
            if (existing != null) return existing;
            var xr = UiSetup.EnsureGroup(GroupXR);
            var go = new GameObject("Global Actions");
            go.transform.SetParent(xr, false);
            Undo.RegisterCreatedObjectUndo(go, "Global Actions");
            return go.AddComponent<GlobalActions>();
        }

        /// <summary>
        /// Adds the Debug bench test panel (BenchTestPanel: chamber-menu stand-in + Debug bench with a demo schema); it
        /// replaces the other test panels. Temporary headset check for PB-19; the template chamber (PB-07) replaces it.
        /// </summary>
        [MenuItem("Magic/App/Debug Bench Test Panel (open scene)", priority = 52)]
        public static void AddBenchTestPanel()
        {
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[GPL] Add the Camera Rig building block first.");
                return;
            }
            var theme = EnsureUiAssets();
            EnsureGlobalActions();
            var ui = UiSetup.EnsureGroup(GroupUI);
            UiSetup.EnsureUiRoot(ui, theme);
            RemoveTestPanels(ui);
            var canvas = UiSetup.CreateOverlayCanvas(ui, BenchTestPanelName, BenchTestPanel.Size);
            canvas.AddComponent<BenchTestPanel>();
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            EnsureMainHallFirstInBuild();
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] Debug bench test panel added under '" + GroupUI + "'. Save the scene, then Play (Quest Link) or build.");
        }

        static void RemoveTestPanels(Transform ui)
        {
            foreach (var n in new[] { TestPanelName, InputTestPanelName, BenchTestPanelName }) RemoveChild(ui, n);
        }

        static void RemoveChild(Transform parent, string childName)
        {
            var t = parent.Find(childName);
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }

        /// <summary>MainHall first in the build scene list; entries whose scene file no longer exists are removed.</summary>
        public static void EnsureMainHallFirstInBuild()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MainHallScene, true) };
            scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != MainHallScene && System.IO.File.Exists(s.path)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Headless (mirror project): open MainHall, add the test panel, save.</summary>
        public static void BatchUiTestPanelInMainHall()
        {
            EditorSceneManager.OpenScene(MainHallScene, OpenSceneMode.Single);
            AddUiTestPanel();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Headless (mirror project): open MainHall, add the input test panel, save.</summary>
        public static void BatchInputTestPanelInMainHall()
        {
            EditorSceneManager.OpenScene(MainHallScene, OpenSceneMode.Single);
            AddInputTestPanel();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
