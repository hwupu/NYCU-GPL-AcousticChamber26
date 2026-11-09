using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    public static partial class UiKit
    {
        static readonly Dictionary<(Material, int, Color), Material> _shadowMaterials = new Dictionary<(Material, int, Color), Material>();

        /// <summary>
        /// A soft shadow around <paramref name="text"/>, like CSS <c>drop-shadow(0 0 blur color)</c> (Storybook Screens/Main Hall:
        /// text on the cover images). It is TextMeshPro's underlay (offset 0, dilate 0) on a copy of the font material, so other
        /// text keeps the plain material; copies are shared per font, softness and color. The SDF can only spread as far as the
        /// font atlas padding (9 texels at 90 pt sampling = 0.1 em), so the blur is capped there (5.6 u on 56 u text, 2.8 u on 28 u).
        /// The text's alpha fades the shadow with it.
        /// </summary>
        public static void SoftShadow(TMP_Text text, float blur = 3f, Color? color = null)
        {
            if (text == null || text.font == null) return;
            var baseMaterial = text.font.material;
            if (baseMaterial == null || !baseMaterial.HasProperty(ShaderUtilities.ID_UnderlaySoftness)) return;
            var c = color ?? new Color(0f, 0f, 0f, 0.2f);
            float softness = ShadowSoftness(text.font, text.fontSize, blur);
            var key = (baseMaterial, Mathf.RoundToInt(softness * 1000f), c);
            if (!_shadowMaterials.TryGetValue(key, out var m) || m == null)
            {
                m = new Material(baseMaterial) { name = baseMaterial.name + " (soft shadow)", hideFlags = HideFlags.DontSave };
                m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                m.SetColor(ShaderUtilities.ID_UnderlayColor, c);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
                ShaderUtilities.UpdateShaderRatios(m);
                _shadowMaterials[key] = m;
            }
            text.fontSharedMaterial = m;
        }

        /// <summary>Underlay softness (0-1, a share of the SDF spread) for a blur of <paramref name="blur"/> canvas units.</summary>
        public static float ShadowSoftness(TMP_FontAsset font, float fontSize, float blur)
        {
            float sampling = font.faceInfo.pointSize > 0 ? font.faceInfo.pointSize : 90f;
            float spread = font.atlasPadding * fontSize / sampling; // canvas units the SDF reaches beyond the glyph edge
            return spread > 0f ? Mathf.Clamp01(blur / spread) : 0f;
        }
    }
}
