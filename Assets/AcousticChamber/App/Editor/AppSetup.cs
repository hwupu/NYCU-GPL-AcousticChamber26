using System.Collections.Generic;
using System.Linq;
using AcousticChamber.App;
using GPL.Homing;
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
        const string HomingTestPanelName = "Homing Test Panel";
        const string HearingLossTestPanelName = "Hearing Loss Test Panel";
        const string KeyboardTestPanelName = "Keyboard Test Panel";
        const string GroupExperiment = "Experiment";
        const string HomingName = "Homing";
        const string FixationMatPath = "Assets/AcousticChamber/Settings/Materials/Fixation.mat";

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

        /// <summary>
        /// Adds the GPL.Homing test panel (HomingTestPanel) and one Homing object (group Experiment: FixationCue with the
        /// fixation material, RecenterListener); it replaces the other test panels. Temporary headset check for PB-05c.
        /// </summary>
        [MenuItem("Magic/App/Homing Test Panel (open scene)", priority = 53)]
        public static void AddHomingTestPanel()
        {
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[GPL] Add the Camera Rig building block first.");
                return;
            }
            var theme = EnsureUiAssets();
            EnsureGlobalActions();
            var (cue, listener) = EnsureHoming();
            var ui = UiSetup.EnsureGroup(GroupUI);
            UiSetup.EnsureUiRoot(ui, theme);
            RemoveTestPanels(ui);
            var canvas = UiSetup.CreateOverlayCanvas(ui, HomingTestPanelName, HomingTestPanel.Size);
            var panel = canvas.AddComponent<HomingTestPanel>();
            panel.cue = cue;
            panel.listener = listener;
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            EnsureMainHallFirstInBuild();
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] Homing test panel added under '" + GroupUI + "'. Save the scene, then Play (Quest Link) or build.");
        }

        /// <summary>
        /// Adds the GPL.HearingLoss test panel (HearingLossTestPanel: per-ear test tones through the loss, RMS before / after per
        /// ear); it replaces the other test panels. Headset check for PB-09; use it in the Sandbox scene.
        /// </summary>
        [MenuItem("Magic/App/Hearing Loss Test Panel (open scene)", priority = 54)]
        public static void AddHearingLossTestPanel()
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
            var canvas = UiSetup.CreateOverlayCanvas(ui, HearingLossTestPanelName, HearingLossTestPanel.Size);
            canvas.AddComponent<HearingLossTestPanel>();
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] Hearing loss test panel added under '" + GroupUI + "'. Save the scene, then Play (Quest Link) or build.");
        }

        /// <summary>Headless (mirror project): the hearing loss test panel in the Sandbox scene (created if missing), saved.</summary>
        public static void BatchHearingLossTestPanel()
        {
            HallSetup.CreateSandbox();
            EditorSceneManager.OpenScene(HallSetup.SandboxScene, OpenSceneMode.Single);
            AddHearingLossTestPanel();
            EditorSceneManager.SaveOpenScenes();
        }

        /// <summary>
        /// Adds the system keyboard test panel (KeyboardTestPanel + NativeKeyboardEntry); it replaces the other test panels. Headset
        /// check for PB-14 (build to the headset; the keyboard does not exist over Link). Test matrix: docs/references/Keyboard.md.
        /// </summary>
        [MenuItem("Magic/App/Keyboard Test Panel (open scene)", priority = 55)]
        public static void AddKeyboardTestPanel()
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
            var canvas = UiSetup.CreateOverlayCanvas(ui, KeyboardTestPanelName, KeyboardTestPanel.Size);
            canvas.AddComponent<NativeKeyboardEntry>();
            canvas.AddComponent<KeyboardTestPanel>();
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            UiSetup.MarkSceneDirty();
            Debug.Log("[GPL] Keyboard test panel added under '" + GroupUI + "'. Save the scene, then build to the headset (see docs/references/Keyboard.md).");
        }

        /// <summary>Headless (mirror project): the keyboard test panel in the Sandbox scene, saved.</summary>
        public static void BatchKeyboardTestPanel()
        {
            HallSetup.CreateSandbox();
            EditorSceneManager.OpenScene(HallSetup.SandboxScene, OpenSceneMode.Single);
            AddKeyboardTestPanel();
            EditorSceneManager.SaveOpenScenes();
        }

        /// <summary>One Homing object (group Experiment) with a FixationCue (URP Unlit #262626 material) and a RecenterListener.</summary>
        public static (FixationCue, RecenterListener) EnsureHoming()
        {
            var mat = UiSetup.EnsureUnlitMaterial(FixationMatPath, new Color32(0x26, 0x26, 0x26, 0xFF));
            var cue = Object.FindAnyObjectByType<FixationCue>(FindObjectsInactive.Include);
            var listener = Object.FindAnyObjectByType<RecenterListener>(FindObjectsInactive.Include);
            GameObject go = cue != null ? cue.gameObject : listener != null ? listener.gameObject : null;
            if (go == null)
            {
                go = new GameObject(HomingName);
                go.transform.SetParent(UiSetup.EnsureGroup(GroupExperiment), false);
                Undo.RegisterCreatedObjectUndo(go, HomingName);
            }
            if (cue == null) cue = Undo.AddComponent<FixationCue>(go);
            if (listener == null) listener = Undo.AddComponent<RecenterListener>(go);
            if (cue.material == null)
            {
                Undo.RecordObject(cue, "Fixation material");
                cue.material = mat;
            }
            return (cue, listener);
        }

        internal static void RemoveTestPanels(Transform ui)
        {
            foreach (var n in new[] { TestPanelName, InputTestPanelName, BenchTestPanelName, HomingTestPanelName, HearingLossTestPanelName, KeyboardTestPanelName }) RemoveChild(ui, n);
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

        /// <summary>Headless (mirror project): open MainHall, add the homing test panel, save.</summary>
        public static void BatchHomingTestPanelInMainHall()
        {
            EditorSceneManager.OpenScene(MainHallScene, OpenSceneMode.Single);
            AddHomingTestPanel();
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
