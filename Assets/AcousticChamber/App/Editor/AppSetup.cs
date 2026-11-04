using System.Collections.Generic;
using System.Linq;
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
        const string TestPanelName = "UI Test Panel";

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
            var canvas = UiSetup.CreateOverlayCanvas(ui, TestPanelName, UiShowcase.Size);
            canvas.AddComponent<UiShowcase>();
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            EnsureMainHallFirstInBuild();
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] UI test panel added under '" + GroupUI + "'. Save the scene, then Play (Quest Link) or build.");
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
    }
}
