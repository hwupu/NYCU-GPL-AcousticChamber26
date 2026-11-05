using System.Collections.Generic;

namespace GPL.Input
{
    /// <summary>
    /// Action names. <see cref="Stop"/> is the one reserved global action (Y opens the Stop dialog in every chamber).
    /// The others are standard names with default controls; a chamber declares only those it uses
    /// (<see cref="BindingTable.DeclareStandard"/>) or its own names (<see cref="BindingTable.Declare"/>).
    /// </summary>
    public static class InputActions
    {
        public const string Stop = "stop";

        public const string Confirm = "confirm";
        public const string Back = "back";
        public const string TriggerLeft = "trigger_left";
        public const string TriggerRight = "trigger_right";
        public const string GripLeft = "grip_left";
        public const string GripRight = "grip_right";
        public const string StickLeft = "stick_left";
        public const string StickRight = "stick_right";

        /// <summary>Global actions and their default controls.</summary>
        public static readonly IReadOnlyDictionary<string, Control> Globals = new Dictionary<string, Control>
        {
            { Stop, Control.Y },
        };

        /// <summary>Standard chamber actions and their default controls (X is left free).</summary>
        public static readonly IReadOnlyDictionary<string, Control> Standard = new Dictionary<string, Control>
        {
            { Confirm, Control.A },
            { Back, Control.B },
            { TriggerLeft, Control.TriggerLeft },
            { TriggerRight, Control.TriggerRight },
            { GripLeft, Control.GripLeft },
            { GripRight, Control.GripRight },
            { StickLeft, Control.StickLeft },
            { StickRight, Control.StickRight },
        };

        /// <summary>Default English description shown in menus ("Y (left): Stop").</summary>
        public static string Describe(string action)
        {
            switch (action)
            {
                case Stop: return "Stop";
                case Confirm: return "Confirm";
                case Back: return "Back";
                case TriggerLeft: return "Left trigger";
                case TriggerRight: return "Right trigger";
                case GripLeft: return "Left grip";
                case GripRight: return "Right grip";
                case StickLeft: return "Left stick";
                case StickRight: return "Right stick";
                default: return action;
            }
        }
    }
}
