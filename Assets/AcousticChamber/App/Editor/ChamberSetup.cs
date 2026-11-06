using System.IO;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AcousticChamber.App.Editor
{
    /// <summary>
    /// Building blocks for a chamber's own Magic steps (<c>Magic > &lt;ChamberId&gt; > Step N</c>): the default parameter file
    /// and the chamber menu canvas with the chamber's <see cref="ChamberBehaviour"/>. Edits only the open scene.
    /// </summary>
    public static class ChamberSetup
    {
        public const string MenuName = "Chamber Menu";
        const string StubName = "Chamber Stub";
        const string LaserPath = "Assets/AcousticChamber/Settings/Materials/Laser.mat";

        /// <summary>True if the open scene is <paramref name="scenePath"/> (logs an error otherwise).</summary>
        public static bool IsOpen(string scenePath)
        {
            if (EditorSceneManager.GetActiveScene().path == scenePath) return true;
            Debug.LogError("[GPL] Open " + scenePath + " first.");
            return false;
        }

        /// <summary>
        /// Writes <paramref name="path"/> (the preset "Default") from <paramref name="defaultsText"/> if it does not exist yet;
        /// an existing file is never overwritten (it may hold tuned values). Returns the imported TextAsset.
        /// </summary>
        public static TextAsset EnsureDefaultParams(string path, string defaultsText)
        {
            if (!File.Exists(path))
            {
                UiSetup.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
                File.WriteAllText(path, defaultsText);
                AssetDatabase.ImportAsset(path);
                Debug.Log("[GPL] Wrote " + path + " (schema defaults).");
            }
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        /// <summary>
        /// The chamber menu in the open chamber scene: replaces the stub panel and any older menu with an overlay canvas holding
        /// <see cref="ChamberMenu"/> and <typeparamref name="T"/>; adds GlobalActions, the pointer and the scene fade.
        /// </summary>
        public static T AddChamberMenu<T>(ChamberDefinition def, TextAsset defaultParams) where T : ChamberBehaviour
        {
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[GPL] The scene has no Camera Rig building block.");
                return null;
            }
            var theme = AppSetup.EnsureUiAssets();
            AppSetup.EnsureGlobalActions();
            var ui = UiSetup.EnsureGroup("UI");
            UiSetup.EnsureUiRoot(ui, theme);
            AppSetup.RemoveTestPanels(ui);
            var stub = ui.Find(StubName);
            if (stub != null) Undo.DestroyObjectImmediate(stub.gameObject);
            var canvas = UiSetup.CreateOverlayCanvas(ui, MenuName, ChamberMenu.Size);
            var logic = canvas.AddComponent<T>();
            logic.defaultParams = defaultParams;
            var menu = canvas.AddComponent<ChamberMenu>();
            menu.chamber = def;
            menu.logic = logic;
            canvas.AddComponent<StopDialogHost>().canvas = canvas.transform;
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            HallSetup.EnsureSceneFade();
            UiSetup.MarkSceneDirty();
            return logic;
        }
    }
}
