using System;
using System.Collections.Generic;
using System.IO;
using GPL.UI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AcousticChamber.Chambers.HumPerFinal.Editor
{
    /// <summary>
    /// The dining-room models from the PO's C05.blend (Docs/SceneSetup.md "Models"): <c>Models/Room.fbx</c>, <c>Duck.fbx</c>,
    /// <c>TrumpetPlayer.fbx</c>, <c>Radio.fbx</c>, their textures and <c>Models/Models.json</c>, all written by the Blender export
    /// script. The manifest lists every object's material slots and every material (Unlit = the room's baked textures, Lit = the
    /// props, Shadow = the soft contact shadows) and the surface heights of the anchors. Each FBX carries two empties,
    /// <c>Front</c> (toward the participant) and <c>Up</c>, so the models are aligned without relying on FBX axis conventions;
    /// a model's origin is its anchor point (Room: the participant's seat on the floor).
    /// </summary>
    public static class HumPerFinalModels
    {
        public const string Dir = HumPerFinalSetup.Folder + "/Models";
        public const string ManifestPath = Dir + "/Models.json";
        public const string MaterialsDir = HumPerFinalSetup.Folder + "/Materials/Models";
        public const string Room = "Room.fbx", Duck = "Duck.fbx", TrumpetPlayer = "TrumpetPlayer.fbx", Radio = "Radio.fbx";
        /// <summary>Texture size caps (the room bakes are 3072 px): colour maps and normal maps.</summary>
        const int MaxTexture = 2048, MaxNormal = 1024;
        const float MarkerDistanceM = 0.5f;
        const string Log = "[Chamber:HumPerFinal] ";

        [Serializable] public class Manifest
        {
            public float duckSurfaceY, trumpetSurfaceY, bellAboveFloorM, duckHeightM;
            public Model[] models;
            public Mat[] materials;
        }
        [Serializable] public class Model { public string file; public Obj[] objects; }
        [Serializable] public class Obj { public string name; public string[] materials; }
        [Serializable] public class Mat
        {
            public string name, shader, baseMap, normalMap, emissionMap;
            public float[] color;
            public float metallic, smoothness;
        }

        public static bool Available => File.Exists(ManifestPath);

        public static Manifest Load()
        {
            if (!Available) { Debug.LogError(Log + "Missing " + ManifestPath + " (export the models from C05.blend first)."); return null; }
            var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (m == null || m.models == null || m.materials == null) { Debug.LogError(Log + ManifestPath + " is not a model manifest."); return null; }
            return m;
        }

        // ---- import settings ------------------------------------------------------------------------------------

        /// <summary>Models: no materials, cameras, lights or animation. Textures: capped size, ASTC on Android, normal maps typed. Reimports what changed.</summary>
        public static void ConfigureImports(Manifest m)
        {
            foreach (var model in m.models)
            {
                var mi = AssetImporter.GetAtPath(Dir + "/" + model.file) as ModelImporter;
                if (mi == null) { Debug.LogError(Log + "Missing model " + Dir + "/" + model.file); continue; }
                bool changed = mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.importCameras || mi.importLights ||
                               mi.importAnimation || mi.animationType != ModelImporterAnimationType.None || mi.isReadable ||
                               !mi.useFileScale || Math.Abs(mi.globalScale - 1f) > 1e-6f;
                if (!changed) continue;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importCameras = false;
                mi.importLights = false;
                mi.importAnimation = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.isReadable = false;
                mi.useFileScale = true;
                mi.globalScale = 1f;
                mi.SaveAndReimport();
            }
            foreach (var mat in m.materials)
            {
                Texture(mat.baseMap, false, mat.shader == "shadow");
                Texture(mat.normalMap, true, false);
                Texture(mat.emissionMap, false, false);
            }
        }

        static void Texture(string rel, bool normal, bool alpha)
        {
            if (string.IsNullOrEmpty(rel)) return;
            string path = Dir + "/" + rel;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogError(Log + "Missing texture " + path); return; }
            int max = normal ? MaxNormal : MaxTexture;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            var android = ti.GetPlatformTextureSettings("Android");
            bool changed = ti.textureType != type || ti.maxTextureSize != max || !ti.mipmapEnabled ||
                           (!normal && ti.sRGBTexture != true) || ti.alphaIsTransparency != alpha ||
                           !android.overridden || android.maxTextureSize != max || android.format != TextureImporterFormat.ASTC_6x6;
            if (!changed) return;
            ti.textureType = type;
            ti.maxTextureSize = max;
            ti.mipmapEnabled = true;
            if (!normal) ti.sRGBTexture = true;
            ti.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = alpha;
            android.overridden = true;
            android.maxTextureSize = max;
            android.format = TextureImporterFormat.ASTC_6x6;
            ti.SetPlatformTextureSettings(android);
            ti.SaveAndReimport();
        }

        // ---- materials --------------------------------------------------------------------------------------------

        /// <summary>One material asset per manifest entry in <see cref="MaterialsDir"/> (created or updated; the GUID stays).</summary>
        public static Dictionary<string, Material> Materials(Manifest m)
        {
            UiSetup.EnsureFolder(MaterialsDir);
            var all = new Dictionary<string, Material>();
            foreach (var e in m.materials)
            {
                string shaderName = e.shader == "lit" ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit";
                var shader = Shader.Find(shaderName);
                if (shader == null) { Debug.LogError(Log + "Shader not found: " + shaderName); continue; }
                string path = MaterialsDir + "/" + e.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                else if (mat.shader != shader) mat.shader = shader;

                var c = e.color != null && e.color.Length >= 3 ? new Color(e.color[0], e.color[1], e.color[2]).gamma : Color.white; // Blender linear -> sRGB
                mat.SetTexture("_BaseMap", Tex(e.baseMap));
                switch (e.shader)
                {
                    case "unlit": // double-sided: Blender shows back faces (window slats, chair shells), URP culls them by default
                        mat.SetColor("_BaseColor", Color.white);
                        Opaque(mat);
                        mat.SetFloat("_Cull", (float)CullMode.Off);
                        break;
                    case "shadow": // black, the texture's alpha is the shadow
                        mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 1f));
                        Transparent(mat);
                        break;
                    default: // lit
                        mat.SetColor("_BaseColor", string.IsNullOrEmpty(e.baseMap) ? c : Color.white);
                        mat.SetFloat("_Metallic", e.metallic);
                        mat.SetFloat("_Smoothness", e.smoothness);
                        var normal = Tex(e.normalMap);
                        mat.SetTexture("_BumpMap", normal);
                        if (normal != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
                        var emission = Tex(e.emissionMap);
                        mat.SetTexture("_EmissionMap", emission);
                        mat.SetColor("_EmissionColor", emission != null ? Color.white : Color.black);
                        if (emission != null) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                        Opaque(mat);
                        break;
                }
                EditorUtility.SetDirty(mat);
                all[e.name] = mat;
            }
            AssetDatabase.SaveAssets();
            return all;
        }

        static Texture2D Tex(string rel) => string.IsNullOrEmpty(rel) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/" + rel);

        static void Opaque(Material mat)
        {
            mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = -1;
        }

        static void Transparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f); // alpha
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        // ---- instances --------------------------------------------------------------------------------------------

        /// <summary>
        /// An unpacked instance of <paramref name="file"/> under <paramref name="parent"/> named <paramref name="name"/>: aligned by its
        /// Front / Up empties (front = parent +z, up = +y; the empties are removed), its origin on the parent's origin, the manifest's
        /// materials, no shadows. Null when something is missing (logged).
        /// </summary>
        public static GameObject Place(Manifest m, string file, Transform parent, string name, Dictionary<string, Material> mats, bool isStatic)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "/" + file);
            var model = Array.Find(m.models, x => x.file == file);
            if (asset == null || model == null) { Debug.LogError(Log + "Model " + file + " missing (file or manifest entry)."); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var front = go.transform.Find("Front");
            var up = go.transform.Find("Up");
            if (front == null || up == null) { Debug.LogError(Log + file + " has no Front / Up empties (re-export it)."); return go; }
            Vector3 f = front.localPosition, u = up.localPosition;
            float scale = MarkerDistanceM / Mathf.Max(f.magnitude, 1e-6f);
            if (Mathf.Abs(scale - 1f) > 0.01f) Debug.LogWarning(Log + file + " imported at " + (1f / scale).ToString("0.###") + " × size; corrected to metres.");
            go.transform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(f, u));
            go.transform.localScale = Vector3.one * scale;
            Object.DestroyImmediate(front.gameObject);
            Object.DestroyImmediate(up.gameObject);

            foreach (var o in model.objects)
            {
                var t = Find(go.transform, o.name);
                var r = t != null ? t.GetComponent<Renderer>() : null;
                if (r == null) { Debug.LogError(Log + file + ": no renderer '" + o.name + "'."); continue; }
                var slots = new Material[o.materials.Length];
                for (int i = 0; i < slots.Length; i++)
                    if (!mats.TryGetValue(o.materials[i], out slots[i])) Debug.LogError(Log + file + "/" + o.name + ": material '" + o.materials[i] + "' not in the manifest.");
                r.sharedMaterials = slots;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            if (isStatic)
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            Undo.RegisterCreatedObjectUndo(go, "Place " + file);
            return go;
        }

        /// <summary>A model instance placed by <see cref="Place"/> (not a placeholder box or the PO's own model).</summary>
        public static bool IsOurs(Transform model, string file) =>
            model != null && model.GetComponent<MeshFilter>() == null && model.Find(Path.GetFileNameWithoutExtension(file)) != null;

        /// <summary>The setup's box placeholder (a cube primitive).</summary>
        public static bool IsBox(Transform model)
        {
            var mf = model != null ? model.GetComponent<MeshFilter>() : null;
            return mf != null && mf.sharedMesh != null && mf.sharedMesh.name == "Cube";
        }

        /// <summary>
        /// Renderers under <paramref name="root"/> with a slot that is empty or not one of the manifest's materials (the importer's
        /// default material counts as missing: it means the object was not found by name).
        /// </summary>
        public static List<string> MissingMaterials(Transform root)
        {
            var bad = new List<string>();
            if (root == null) return bad;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                foreach (var mat in r.sharedMaterials)
                    if (mat == null || !AssetDatabase.GetAssetPath(mat).StartsWith(MaterialsDir + "/", StringComparison.Ordinal)) { bad.Add(r.name); break; }
            return bad;
        }

        /// <summary>Depth-first search below <paramref name="root"/> (not the root itself: Duck.fbx's root and its mesh share a name).</summary>
        static Transform Find(Transform root, string name)
        {
            foreach (Transform c in root)
            {
                if (c.name == name) return c;
                var t = Find(c, name);
                if (t != null) return t;
            }
            return null;
        }
    }
}
