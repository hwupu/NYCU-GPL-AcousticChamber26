using System.IO;
using GPL.UI;
using GPL.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AcousticChamber.App.Editor
{
    /// <summary>
    /// Scene-building helpers shared by the experience chambers' Magic steps (ADR-0006): the chamber definition, the scene (a copy
    /// of MainHall) with its overlay panel, the grid floor, URP material assets. Idempotent; they touch only the open chamber scene
    /// and never the Building Blocks. Same recipe as the HearingLoss chamber's setup.
    /// </summary>
    public static class ExperienceScene
    {
        const string LaserPath = "Assets/AcousticChamber/Settings/Materials/Laser.mat";
        public const float FloorSizeM = 30f;

        public static ChamberDefinition EnsureDefinition(string defPath, string scenePath, string id, string label, string title, string description)
        {
            var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(defPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ChamberDefinition>();
                def.id = id;
                def.label = label;
                def.title = title;
                def.description = description;
                def.version = "0.1";
                AssetDatabase.CreateAsset(def, defPath);
            }
            if (def.scenePath != scenePath)
            {
                def.scenePath = scenePath;
                EditorUtility.SetDirty(def);
                AssetDatabase.SaveAssets();
            }
            return def;
        }

        /// <summary>
        /// Step 1 of an experience chamber: the scene (created as a copy of MainHall, opened), group UI with the pointer and scene fade,
        /// the overlay canvas <paramref name="panelName"/> with a <typeparamref name="T"/> on it, Global Actions; saves and syncs the
        /// catalog. Returns the logic component, or null (with an error) when the scene could not be made.
        /// </summary>
        public static T BuildPanelScene<T>(string log, string scenePath, string panelName, Vector2 size, float yawOffsetDeg, float pitchDownDeg)
            where T : MonoBehaviour
        {
            if (!File.Exists(scenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
                UiSetup.EnsureFolder(Path.GetDirectoryName(scenePath).Replace('\\', '/'));
                if (!AssetDatabase.CopyAsset(AppSetup.MainHallScene, scenePath))
                {
                    Debug.LogError(log + "Could not copy " + AppSetup.MainHallScene + " to " + scenePath);
                    return null;
                }
                Debug.Log(log + "Scene created from MainHall: " + scenePath);
            }
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            if (Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError(log + "The scene has no Camera Rig building block.");
                return null;
            }

            var theme = AppSetup.EnsureUiAssets();
            AppSetup.EnsureGlobalActions();
            var ui = UiSetup.EnsureGroup("UI");
            UiSetup.EnsureUiRoot(ui, theme);
            var hall = ui.Find("Hall");
            if (hall != null) Object.DestroyImmediate(hall.gameObject);

            var canvas = UiSetup.CreateOverlayCanvas(ui, panelName, size);
            var panel = canvas.GetComponent<OverlayPanel>();
            panel.yawOffsetDeg = yawOffsetDeg;
            panel.pitchDownDeg = pitchDownDeg;
            var logic = canvas.AddComponent<T>();
            UiSetup.EnsurePointer(ui, AssetDatabase.LoadAssetAtPath<Material>(LaserPath));
            HallSetup.EnsureSceneFade();
            UiSetup.MarkSceneDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            HallSetup.SyncChambers();
            return logic;
        }

        /// <summary>Environment/Floor: a 30 x 30 m quad with a 1 m grid (URP Lit), at y = 0.</summary>
        public static void EnsureFloor(string materialsDir)
        {
            var env = UiSetup.EnsureGroup("Environment");
            var floor = env.Find("Floor") != null ? env.Find("Floor").gameObject : null;
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                floor.name = "Floor";
                floor.transform.SetParent(env, false);
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
            }
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(FloorSizeM, FloorSizeM, 1f);
            floor.GetComponent<Renderer>().sharedMaterial = FloorMaterial(materialsDir);
        }

        public static GameObject Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        public static T Get<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>(); // Unity null when missing in the Editor: no ??
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>A primitive without collider under <paramref name="parent"/> (reused by name).</summary>
        public static Renderer Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t != null) go = t.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.transform.SetParent(parent, false);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return r;
        }

        public static Material LitMat(string materialsDir, string name, Color color)
        {
            UiSetup.EnsureFolder(materialsDir);
            string path = materialsDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>URP Unlit material asset (single-pass stereo safe; never Sprites/Default).</summary>
        public static Material UnlitMat(string materialsDir, string name, Color color)
        {
            UiSetup.EnsureFolder(materialsDir);
            return UiSetup.EnsureUnlitMaterial(materialsDir + "/" + name + ".mat", color);
        }

        static Material FloorMaterial(string materialsDir)
        {
            string texPath = materialsDir + "/FloorGrid.png";
            UiSetup.EnsureFolder(materialsDir);
            if (!File.Exists(texPath))
            {
                const int N = 256, Line = 3;
                var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
                var fill = new Color(0.30f, 0.30f, 0.33f);
                var line = new Color(0.45f, 0.45f, 0.50f);
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                        px[y * N + x] = x < Line || y < Line ? line : fill;
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (imp != null)
                {
                    imp.wrapMode = TextureWrapMode.Repeat;
                    imp.anisoLevel = 8;
                    imp.SaveAndReimport();
                }
            }
            var m = LitMat(materialsDir, "Floor", Color.white);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetTextureScale("_BaseMap", new Vector2(FloorSizeM, FloorSizeM));
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
