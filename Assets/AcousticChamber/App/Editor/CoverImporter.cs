using UnityEditor;

namespace AcousticChamber.App.Editor
{
    /// <summary>
    /// Import settings of the Main Hall covers (<c>App/Covers/IMG-C-NN</c>, docs/AssetCredits.md): sRGB color texture, mipmaps
    /// (the card is seen small and at an angle in the headset), clamped, max 2048, no power-of-two rescale. Applies to every
    /// image dropped into the folder, so a new cover needs no setup step; a chamber picks one in its ChamberDefinition (Thumbnail).
    /// </summary>
    public class CoverImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/AcousticChamber/App/Covers/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.textureShape = TextureImporterShape.Texture2D; // a ChamberDefinition.thumbnail is a Texture2D (a cubemap import left it empty)
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = true;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.filterMode = UnityEngine.FilterMode.Trilinear;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
