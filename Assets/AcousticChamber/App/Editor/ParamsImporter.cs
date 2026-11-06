using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AcousticChamber.App.Editor
{
    /// <summary>Imports <c>.params</c> files (key=value presets, e.g. a chamber's <c>Config/default.params</c>) as TextAssets.</summary>
    [ScriptedImporter(1, "params")]
    public class ParamsImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("text", text);
            ctx.SetMainObject(text);
        }
    }
}
