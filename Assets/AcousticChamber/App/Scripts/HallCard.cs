using GPL.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AcousticChamber.App
{
    /// <summary>
    /// Hover look of a Main Hall card (Storybook Screens/Main Hall, PO 2026-11-10): the card's <see cref="UiButton"/> grows to
    /// <see cref="HoverScale"/> (pressed <see cref="PressedScale"/>), and this raises the cover image's saturation to
    /// <see cref="HoverSaturation"/> with the button's timing (hover in 150 ms, out 350 ms, settle curve, unscaled time). The text
    /// does not change. The image uses its own <c>GPL/UI Saturation</c> material, destroyed with the card.
    /// </summary>
    public class HallCard : MonoBehaviour
    {
        public const float HoverScale = 1.03f, PressedScale = 1.015f, HoverSaturation = 1.25f;

        UiButton _button;
        RawImage _image;
        Material _material;
        bool _emphasized;
        float _from = 1f, _to = 1f, _start, _duration;

        /// <summary>Current saturation of the image (1 = unchanged).</summary>
        public float Saturation { get; private set; } = 1f;

        public static HallCard Attach(UiButton button, RawImage image)
        {
            button.HoverScale = HoverScale;
            button.PressedScale = PressedScale;
            var card = button.gameObject.AddComponent<HallCard>();
            card._button = button;
            if (image != null)
            {
                card._material = UiKit.SaturationMaterial();
                if (card._material != null) { image.material = card._material; card._image = image; }
            }
            return card;
        }

        void LateUpdate()
        {
            if (_button == null) return;
            bool on = _button.IsEmphasized;
            if (on != _emphasized)
            {
                _emphasized = on;
                _from = Saturation;
                _to = on ? HoverSaturation : 1f;
                _start = Time.unscaledTime;
                _duration = UiKit.Theme.Duration(on ? UiKit.Theme.hoverIn : UiKit.Theme.hoverOut);
            }
            if (Mathf.Approximately(Saturation, _to)) return;
            float p = GPL.UI.Motion.Progress(Time.unscaledTime - _start, _duration);
            Saturation = Mathf.LerpUnclamped(_from, _to, p);
            if (_material == null) return;
            _material.SetFloat(UiKit.SaturationId, Saturation);
            // inside the rounded cover (Mask) the image draws with a stencil copy of the material, made when it was first drawn
            var drawn = _image != null ? _image.materialForRendering : null;
            if (drawn != null && drawn != _material) drawn.SetFloat(UiKit.SaturationId, Saturation);
        }

        void OnDestroy()
        {
            if (_material == null) return;
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }
}
