using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GPL.UI.Editor
{
    /// <summary>
    /// Reusable Editor setup functions of GPL.UI, called by project/chamber setup steps (Magic menu). All are idempotent:
    /// they return the existing asset/object when it is already there. They never touch objects they did not create.
    /// </summary>
    public static class UiSetup
    {
        /// <summary>
        /// A dynamic SDF TextMeshPro font asset for <paramref name="fontPath"/> (.ttf/.otf), saved at
        /// <paramref name="assetPath"/> with its atlas texture and material as sub-assets. Dynamic: glyphs (any 中文
        /// character) are added at run time, so nothing is pre-baked; the source font is included in the build.
        /// </summary>
        public static TMP_FontAsset EnsureFontAsset(string fontPath, string assetPath, int samplingPointSize = 90, int atlasSize = 2048)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError("[GPL] UiSetup: font not found at " + fontPath);
                return null;
            }

            var fa = TMP_FontAsset.CreateFontAsset(font, samplingPointSize, 9, GlyphRenderMode.SDFAA, atlasSize, atlasSize,
                                                   AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            if (fa == null)
            {
                Debug.LogError("[GPL] UiSetup: could not create a font asset from " + fontPath + " (Include Font Data on?).");
                return null;
            }
            fa.name = Path.GetFileNameWithoutExtension(assetPath);
            EnsureFolder(Path.GetDirectoryName(assetPath));
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            Debug.Log("[GPL] UiSetup: created dynamic font asset " + assetPath);
            return fa;
        }

        /// <summary>A <see cref="UiTheme"/> asset with the default tokens and the given fonts (fonts are updated if missing).</summary>
        public static UiTheme EnsureTheme(string assetPath, TMP_FontAsset regular, TMP_FontAsset bold)
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(assetPath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiTheme>();
                theme.version = UiTheme.CurrentVersion;
                EnsureFolder(Path.GetDirectoryName(assetPath));
                AssetDatabase.CreateAsset(theme, assetPath);
            }
            else if (theme.version < UiTheme.CurrentVersion)
            {
                Debug.Log("[GPL] UiTheme " + assetPath + ": tokens v" + theme.version + " -> v" + UiTheme.CurrentVersion + " (reset to the design-system defaults, fonts kept).");
                theme.ResetTokens();
            }
            if (theme.regular == null) theme.regular = regular;
            if (theme.bold == null) theme.bold = bold;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        /// <summary>A white URP Unlit material asset for the laser (single-pass stereo safe).</summary>
        public static Material EnsureUnlitMaterial(string assetPath, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("[GPL] UiSetup: URP Unlit shader not found.");
                return null;
            }
            mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(assetPath) };
            mat.SetColor("_BaseColor", color);
            EnsureFolder(Path.GetDirectoryName(assetPath));
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        /// <summary>Names a layer in the TagManager if it is still unnamed (keeps an existing name, with a warning).</summary>
        public static void EnsureLayerName(int layer, string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var element = tagManager.FindProperty("layers").GetArrayElementAtIndex(layer);
            if (element.stringValue == layerName) return;
            if (!string.IsNullOrEmpty(element.stringValue))
            {
                Debug.LogWarning("[GPL] Layer " + layer + " is already named '" + element.stringValue + "'; using it.");
                return;
            }
            element.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A root object with identity transform (hierarchy group such as "UI").</summary>
        public static Transform EnsureGroup(string groupName)
        {
            foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == groupName) return go.transform;
            var g = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(g, "Create " + groupName);
            return g.transform;
        }

        /// <summary>The scene's UiRoot (created under <paramref name="parent"/> if missing), with the theme assigned.</summary>
        public static UiRoot EnsureUiRoot(Transform parent, UiTheme theme)
        {
            var root = Object.FindAnyObjectByType<UiRoot>(FindObjectsInactive.Include);
            if (root == null)
            {
                var go = new GameObject("UI Root");
                go.transform.SetParent(parent, false);
                root = go.AddComponent<UiRoot>();
            }
            root.theme = theme;
            EditorUtility.SetDirty(root);
            return root;
        }

        /// <summary>
        /// A world-space canvas shown through an OVROverlayCanvas (sharp text in the headset), placed by
        /// <see cref="OverlayPanel"/>. The source canvas is on the hidden layer 30, which is removed from every camera in the
        /// scene. An existing object with the same name under <paramref name="parent"/> is replaced.
        /// </summary>
        public static GameObject CreateOverlayCanvas(Transform parent, string objectName, Vector2 size, float distanceM = 2f,
                                                     float pitchDownDeg = 8f)
        {
            EnsureLayerName(OverlayPanel.HiddenCanvasLayer, "HiddenOverlayCanvas");
            var old = parent.Find(objectName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var go = new GameObject(objectName, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * (OverlayPanel.ScalePerMeter * distanceM);
            rt.position = new Vector3(0f, 1.2f, distanceM); // editor preview; OverlayPanel moves it at run time

            var panel = go.AddComponent<OverlayPanel>();
            panel.distanceM = distanceM;
            panel.pitchDownDeg = pitchDownDeg;

            var overlay = go.AddComponent<OVROverlayCanvas>();
            overlay.SetCanvasLayer(OverlayPanel.HiddenCanvasLayer, true);
            HideCanvasLayersFromAllCameras();
            Undo.RegisterCreatedObjectUndo(go, "Create " + objectName);
            return go;
        }

        public static void HideCanvasLayersFromAllCameras()
        {
            int mask = (1 << OverlayPanel.HiddenCanvasLayer) | (1 << OverlayPanel.OverlayRenderLayer);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if ((cam.cullingMask & mask) == 0) continue;
                // OVROverlayCanvas creates its own hidden camera at run time; scene cameras are eye cameras.
                Undo.RecordObject(cam, "Hide canvas layers");
                cam.cullingMask &= ~mask;
            }
        }

        /// <summary>The scene's UiPointer (created under <paramref name="parent"/> if missing).</summary>
        public static UiPointer EnsurePointer(Transform parent, Material laser)
        {
            var p = Object.FindAnyObjectByType<UiPointer>(FindObjectsInactive.Include);
            if (p == null)
            {
                var go = new GameObject("UI Pointer");
                go.transform.SetParent(parent, false);
                p = go.AddComponent<UiPointer>();
                Undo.RegisterCreatedObjectUndo(go, "Create UI Pointer");
            }
            p.laserMaterial = laser;
            EditorUtility.SetDirty(p);
            return p;
        }

        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static void MarkSceneDirty() => EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
