using UnityEngine;

namespace GPL.Input
{
    /// <summary>
    /// Reads the Quest Touch controllers through <c>OVRInput</c> (needs an <c>OVRManager</c> in the scene, which the Camera
    /// Rig building block has). Works while <c>Time.timeScale</c> is 0, because OVRManager updates input every frame.
    /// The only place in the project that calls OVRInput for actions (the UI pointer reads the triggers itself).
    /// </summary>
    public sealed class OvrControlSource : IControlSource
    {
        public bool Down(Control c) => c != Control.None && OVRInput.GetDown(ButtonOf(c), ControllerOf(c));
        public bool Held(Control c) => c != Control.None && OVRInput.Get(ButtonOf(c), ControllerOf(c));
        public bool Up(Control c) => c != Control.None && OVRInput.GetUp(ButtonOf(c), ControllerOf(c));

        public float Value(Control c)
        {
            if (c == Control.None) return 0f;
            switch (Controls.KindOf(c))
            {
                case ControlKind.Analog:
                    var axis = c == Control.TriggerLeft || c == Control.TriggerRight
                        ? OVRInput.Axis1D.PrimaryIndexTrigger
                        : OVRInput.Axis1D.PrimaryHandTrigger;
                    return OVRInput.Get(axis, ControllerOf(c));
                default:
                    return Held(c) ? 1f : 0f;
            }
        }

        public Vector2 Stick(Control c) =>
            c != Control.None && Controls.KindOf(c) == ControlKind.Stick
                ? OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, ControllerOf(c))
                : Vector2.zero;

        /// <summary>With a single-controller mask, "One"/"Two" and "Primary*" refer to that controller (Y = Two on LTouch).</summary>
        static OVRInput.Button ButtonOf(Control c)
        {
            switch (c)
            {
                case Control.A:
                case Control.X:
                    return OVRInput.Button.One;
                case Control.B:
                case Control.Y:
                    return OVRInput.Button.Two;
                case Control.TriggerLeft:
                case Control.TriggerRight:
                    return OVRInput.Button.PrimaryIndexTrigger;
                case Control.GripLeft:
                case Control.GripRight:
                    return OVRInput.Button.PrimaryHandTrigger;
                case Control.StickLeft:
                case Control.StickRight:
                    return OVRInput.Button.PrimaryThumbstick;
                default:
                    return OVRInput.Button.None;
            }
        }

        static OVRInput.Controller ControllerOf(Control c) =>
            Controls.HandOf(c) == Hand.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
    }
}
