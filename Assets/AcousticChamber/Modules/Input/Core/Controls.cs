using System;

namespace GPL.Input
{
    /// <summary>
    /// Physical controls of the two Quest Touch controllers that actions can be bound to. The Meta button and the menu
    /// button belong to the system and are not listed.
    /// </summary>
    public enum Control
    {
        None = 0,
        A,              // right controller, lower face button
        B,              // right controller, upper face button
        X,              // left controller, lower face button
        Y,              // left controller, upper face button
        TriggerLeft,    // index trigger (button + 0..1 value)
        TriggerRight,
        GripLeft,       // hand trigger (button + 0..1 value)
        GripRight,
        StickLeft,      // thumbstick (click + 2D value)
        StickRight,
    }

    public enum Hand { Left, Right }

    /// <summary>What a control can deliver besides pressed / released.</summary>
    public enum ControlKind
    {
        Button,     // A, B, X, Y
        Analog,     // triggers and grips: pressed + value 0..1
        Stick,      // thumbsticks: click + 2D value
    }

    public static class Controls
    {
        public static Hand HandOf(Control c)
        {
            switch (c)
            {
                case Control.X:
                case Control.Y:
                case Control.TriggerLeft:
                case Control.GripLeft:
                case Control.StickLeft:
                    return Hand.Left;
                case Control.None:
                    throw new ArgumentException("Control.None has no hand");
                default:
                    return Hand.Right;
            }
        }

        public static ControlKind KindOf(Control c)
        {
            switch (c)
            {
                case Control.TriggerLeft:
                case Control.TriggerRight:
                case Control.GripLeft:
                case Control.GripRight:
                    return ControlKind.Analog;
                case Control.StickLeft:
                case Control.StickRight:
                    return ControlKind.Stick;
                default:
                    return ControlKind.Button;
            }
        }

        /// <summary>Label for menus and docs, e.g. "Y (left)", "Right trigger".</summary>
        public static string Label(Control c)
        {
            switch (c)
            {
                case Control.A: return "A (right)";
                case Control.B: return "B (right)";
                case Control.X: return "X (left)";
                case Control.Y: return "Y (left)";
                case Control.TriggerLeft: return "Left trigger";
                case Control.TriggerRight: return "Right trigger";
                case Control.GripLeft: return "Left grip";
                case Control.GripRight: return "Right grip";
                case Control.StickLeft: return "Left stick";
                case Control.StickRight: return "Right stick";
                default: return "none";
            }
        }
    }
}
