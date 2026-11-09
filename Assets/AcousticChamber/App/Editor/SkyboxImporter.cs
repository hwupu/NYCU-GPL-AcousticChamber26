using UnityEditor;
using UnityEngine;

namespace AcousticChamber.App.Editor
{
    /// <summary>
    /// Import settings of the sky panoramas in <c>Settings/Skyboxes</c> (Kenney Skyboxes, CC0; docs/AssetCredits.md): 4096 x 2048
    /// latitude-longitude images for the <c>Skybox/Panoramic</c> shader. sRGB, no mipmaps (a mip seam would show where the
    /// panorama wraps), repeat around (U), clamped at the poles (V), bilinear, max 4096, compressed.
    /// </summary>
    public class SkyboxImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/AcousticChamber/Settings/Skyboxes/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = false;
            ti.wrapModeU = TextureWrapMode.Repeat;
            ti.wrapModeV = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
