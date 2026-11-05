using UnityEngine;

namespace GPL.Input
{
    /// <summary>
    /// Raw state of the physical controls for the current frame. <see cref="OvrControlSource"/> reads the Quest
    /// controllers; tests and desktop runs can plug in another source.
    /// </summary>
    public interface IControlSource
    {
        bool Down(Control c);
        bool Held(Control c);
        bool Up(Control c);
        /// <summary>0..1 for triggers and grips; 0/1 for buttons and stick clicks.</summary>
        float Value(Control c);
        /// <summary>Thumbstick position (-1..1); zero for other controls.</summary>
        Vector2 Stick(Control c);
    }
}
