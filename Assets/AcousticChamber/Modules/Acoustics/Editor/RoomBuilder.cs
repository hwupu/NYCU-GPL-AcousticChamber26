using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPL.Acoustics.Editor
{
    /// <summary>
    /// Builds a plain box room and its Meta XR Audio shoebox with the same size (HW2 Steps 1-2): white walls and ceiling, light-gray
    /// floor, URP Lit materials, two baked area lights, light probes, no directional light. The room is centered on the parent in x/z,
    /// floor at y = 0. Idempotent: children named "Room" and "Shoebox Acoustics" under the parent are rebuilt; nothing else is touched.
    /// </summary>
    public static class RoomBuilder
    {
        public const string RoomName = "Room";
        public const string ShoeboxName = "Shoebox Acoustics";

        public struct Options
        {
            public float Width, Depth, Height;
            public MetaXRAudioRoomAcousticProperties.MaterialPreset Material;
            /// <summary>Folder for the room materials (created if needed).</summary>
            public string MaterialsDir;
            public static Options Default(string materialsDir) => new Options
            {
                Width = 8.72f, Depth = 15f, Height = 3.5f,
                Material = MetaXRAudioRoomAcousticProperties.MaterialPreset.Concrete,
                MaterialsDir = materialsDir,
            };
        }

        /// <summary>Flat ambient light, no skybox or sun, baked GI only; the lighting settings asset is created at <paramref name="settingsPath"/>.
        /// Removes directional lights in the open scene; returns how many.</summary>
        public static int ApplyBakedLighting(string settingsPath, float ambient = 0.7f)
        {
            int removed = 0;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                Object.DestroyImmediate(light.gameObject);
                removed++;
            }
            RenderSettings.sun = null;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(ambient, ambient, ambient);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
            if (ls == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                ls = new LightingSettings();
                AssetDatabase.CreateAsset(ls, settingsPath);
            }
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.autoGenerate = false;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            ls.maxBounces = 4;
            ls.lightmapResolution = 20;
            ls.lightmapMaxSize = 1024;
            ls.directSampleCount = 64;
            ls.indirectSampleCount = 256;
            ls.environmentSampleCount = 128;
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return removed;
        }

        /// <summary>Visual room (with a <see cref="ShoeboxRoom"/> on it) + shoebox component under <paramref name="parent"/>.</summary>
        public static ShoeboxRoom Build(Transform parent, Options o)
        {
            float w = o.Width, h = o.Height, d = o.Depth, t = 0.1f;
            DestroyChild(parent, RoomName);
            DestroyChild(parent, ShoeboxName);

            var room = new GameObject(RoomName);
            room.transform.SetParent(parent, false);
            var info = room.AddComponent<ShoeboxRoom>();
            info.width = w; info.height = h; info.depth = d;

            Directory.CreateDirectory(o.MaterialsDir);
            var wallMat = Mat(o.MaterialsDir, "Room_Wall", new Color(0.95f, 0.95f, 0.95f));
            var ceilMat = Mat(o.MaterialsDir, "Room_Ceiling", new Color(0.95f, 0.95f, 0.95f));
            var floorMat = Mat(o.MaterialsDir, "Room_Floor", new Color(0.75f, 0.75f, 0.75f));

            Slab(room, "Floor", new Vector3(0, -t / 2, 0), new Vector3(w + 2 * t, t, d + 2 * t), floorMat);
            Slab(room, "Ceiling", new Vector3(0, h + t / 2, 0), new Vector3(w + 2 * t, t, d + 2 * t), ceilMat);
            Slab(room, "Wall_Left", new Vector3(-w / 2 - t / 2, h / 2, 0), new Vector3(t, h, d), wallMat);
            Slab(room, "Wall_Right", new Vector3(w / 2 + t / 2, h / 2, 0), new Vector3(t, h, d), wallMat);
            Slab(room, "Wall_Back", new Vector3(0, h / 2, -d / 2 - t / 2), new Vector3(w + 2 * t, h, t), wallMat);
            Slab(room, "Wall_Front", new Vector3(0, h / 2, d / 2 + t / 2), new Vector3(w + 2 * t, h, t), wallMat);

            var lighting = new GameObject("Lighting");
            lighting.transform.SetParent(room.transform, false);
            var areaSize = new Vector2(Mathf.Max(0.5f, w - 1f), Mathf.Max(0.5f, d - 1f));
            AreaLight(lighting.transform, "Ceiling Area Light", new Vector3(0f, h - 0.05f, 0f), 90f, areaSize, 2f);
            // the underside of the ceiling bakes black without it: a fainter light faces up
            AreaLight(lighting.transform, "Ceiling Fill Light", new Vector3(0f, h - 0.4f, 0f), -90f, areaSize, 1f);

            var probes = new GameObject("Light Probes").AddComponent<LightProbeGroup>();
            probes.transform.SetParent(lighting.transform, false);
            var positions = new List<Vector3>();
            foreach (float fx in new[] { -0.35f, 0f, 0.35f })
                foreach (float fz in new[] { -0.35f, 0f, 0.35f })
                    foreach (float y in new[] { 0.3f, 1.2f, 2.2f })
                        positions.Add(new Vector3(fx * w, Mathf.Min(y, h - 0.2f), fz * d));
            probes.probePositions = positions.ToArray();

            var shoeboxGo = new GameObject(ShoeboxName);
            shoeboxGo.transform.SetParent(parent, false);
            shoeboxGo.transform.localPosition = new Vector3(0f, h / 2f, 0f); // room center, mid-height
            var shoebox = shoeboxGo.AddComponent<MetaXRAudioRoomAcousticProperties>();
            shoebox.lockPositionToListener = false;
            shoebox.width = w; shoebox.height = h; shoebox.depth = d;
            shoebox.clutterFactor = 0f;
            shoebox.leftMaterial = shoebox.rightMaterial = shoebox.frontMaterial = shoebox.backMaterial =
                shoebox.ceilingMaterial = shoebox.floorMaterial = o.Material;

            if (Object.FindObjectsByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 1)
                Debug.LogWarning("[GPL] More than one MetaXRAudioRoomAcousticProperties in the scene; Meta allows only one.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[GPL] Room built: " + w + " x " + d + " m, height " + h + " m, shoebox material " + o.Material + ".");
            return info;
        }

        /// <summary>Saves the open scene and bakes its lightmaps (slow). False if the scene has no path or the bake failed.</summary>
        public static bool Bake()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) { Debug.LogWarning("[GPL] Save the scene first, then bake."); return false; }
            EditorSceneManager.SaveScene(scene);
            if (!Lightmapping.Bake()) { Debug.LogWarning("[GPL] Lightmap bake failed."); return false; }
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        static void AreaLight(Transform parent, string name, Vector3 pos, float pitch, Vector2 size, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(pitch, 0f, 0f));
            light.type = LightType.Rectangle;
            light.areaSize = size;
            light.lightmapBakeType = LightmapBakeType.Baked;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        static Material Mat(string dir, string name, Color color)
        {
            string path = dir.TrimEnd('/') + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Slab(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        static void DestroyChild(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name == name) Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
