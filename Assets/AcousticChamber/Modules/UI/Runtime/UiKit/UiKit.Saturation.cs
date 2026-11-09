using UnityEngine;

namespace GPL.UI
{
    public static partial class UiKit
    {
        /// <summary>Resources name of the shader <c>GPL/UI Saturation</c> (UI/Default + saturation; CSS <c>filter: saturate()</c>).</summary>
        public const string SaturationShaderResource = "GPLUISaturation";
        public static readonly int SaturationId = Shader.PropertyToID("_Saturation");

        /// <summary>
        /// A new material of <c>GPL/UI Saturation</c> for one graphic (each animates its own value; the owner destroys it), or
        /// null if the shader is missing (the graphic then keeps the default UI material, no saturation change).
        /// </summary>
        public static Material SaturationMaterial(float saturation = 1f)
        {
            var shader = Resources.Load<Shader>(SaturationShaderResource);
            if (shader == null)
            {
                Debug.LogWarning("[GPL] UiKit: shader Resources/" + SaturationShaderResource + " not found; no saturation effect.");
                return null;
            }
            var m = new Material(shader) { name = "GPL UI Saturation (instance)", hideFlags = HideFlags.DontSave };
            m.SetFloat(SaturationId, saturation);
            return m;
        }
    }
}
