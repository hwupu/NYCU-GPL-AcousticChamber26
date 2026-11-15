using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPL.Acoustics.Editor
{
    /// <summary>
    /// <c>Magic > Acoustics > Import RT60 Calibration...</c>: turns a calibration-sweep report (CSV pulled from the headset, with
    /// its TXT next to it) into an <see cref="Rt60CalibrationAsset"/> in <see cref="AssetFolder"/> and copies both report files to
    /// <see cref="ReportFolder"/> so the table stays traceable to its measurement. Re-importing the same platform and room size
    /// replaces the table.
    /// </summary>
    public static class Rt60CalibrationImport
    {
        public const string AssetFolder = "Assets/AcousticChamber/Calibration/Resources/" + Rt60CalibrationAsset.ResourcesFolder;
        public const string ReportFolder = "docs/calibration";

        [MenuItem("Magic/Acoustics/Import RT60 Calibration...", priority = 400)]
        static void ImportMenu()
        {
            string csv = EditorUtility.OpenFilePanel("RT60 calibration sweep (CSV)", "", "csv");
            if (string.IsNullOrEmpty(csv)) return;
            try
            {
                var asset = Import(csv);
                EditorUtility.DisplayDialog("RT60 calibration", "Imported " + asset.reflection.Length + " points for " + asset.platform + " " +
                    Rt60Protocol.RoomText(asset.roomWidth, asset.roomDepth, asset.roomHeight) + " m.\n\nCommit " + AssetPath(asset) +
                    " and " + ReportFolder + "/" + Path.GetFileNameWithoutExtension(csv) + ".*", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError("[GPL] RT60 calibration import failed: " + e.Message);
                EditorUtility.DisplayDialog("RT60 calibration", "Import failed:\n" + e.Message, "OK");
            }
        }

        /// <summary>Imports one sweep CSV; returns the saved asset. Throws when the CSV is not a usable sweep.</summary>
        public static Rt60CalibrationAsset Import(string csvPath)
        {
            string name = Path.GetFileName(csvPath);
            var cal = Rt60Calibration.FromSweepCsv(File.ReadAllText(csvPath), name);

            Directory.CreateDirectory(ReportFolder);
            File.Copy(csvPath, Path.Combine(ReportFolder, name), true);
            string txt = Path.ChangeExtension(csvPath, ".txt");
            if (File.Exists(txt)) File.Copy(txt, Path.Combine(ReportFolder, Path.GetFileName(txt)), true);
            else Debug.LogWarning("[GPL] RT60 calibration: " + Path.GetFileName(txt) + " not found next to the CSV; copy it to " + ReportFolder + " by hand.");

            Directory.CreateDirectory(AssetFolder);
            var asset = ScriptableObject.CreateInstance<Rt60CalibrationAsset>();
            asset.platform = cal.Platform;
            asset.protocol = cal.Protocol;
            asset.roomWidth = (float)cal.RoomWidth; asset.roomDepth = (float)cal.RoomDepth; asset.roomHeight = (float)cal.RoomHeight;
            asset.sourceReport = name;
            asset.importedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm", GPL.Core.Fmt.Inv);
            asset.reflection = new double[cal.Count];
            asset.tmidSeconds = new double[cal.Count];
            for (int i = 0; i < cal.Count; i++) { asset.reflection[i] = cal.ReflectionAt(i); asset.tmidSeconds[i] = cal.Rt60At(i); }

            string path = AssetPath(asset);
            var existing = AssetDatabase.LoadAssetAtPath<Rt60CalibrationAsset>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                existing.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(existing);
                asset = existing;
            }
            else AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[GPL] RT60 calibration imported: " + path + " from " + name + " (" + cal.Describe() + ")");
            if (cal.DroppedPoints.Count > 0)
                Debug.LogWarning("[GPL] RT60 calibration: extension points left out of the table: " + string.Join(", ", cal.DroppedPoints));
            return asset;
        }

        static string AssetPath(Rt60CalibrationAsset a) =>
            AssetFolder + "/RT60_" + a.platform + "_" + Rt60Protocol.RoomText(a.roomWidth, a.roomDepth, a.roomHeight).Replace('.', 'p') + ".asset";
    }
}
