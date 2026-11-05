using System.Collections.Generic;
using UnityEngine;

namespace GPL.Input
{
    /// <summary>
    /// Reads actions (not buttons) through a <see cref="BindingTable"/>: chamber code asks <c>Down("confirm")</c> and never
    /// calls OVRInput. Reading an action the table does not declare logs one error and returns "not pressed".
    /// </summary>
    public sealed class ActionReader
    {
        readonly HashSet<string> _reported = new HashSet<string>();

        public BindingTable Bindings { get; }
        public IControlSource Source { get; set; }

        public ActionReader(BindingTable bindings, IControlSource source)
        {
            Bindings = bindings;
            Source = source;
        }

        public bool Down(string action) { var c = ControlOf(action); return c != Control.None && Source.Down(c); }
        public bool Held(string action) { var c = ControlOf(action); return c != Control.None && Source.Held(c); }
        public bool Up(string action) { var c = ControlOf(action); return c != Control.None && Source.Up(c); }
        public float Value(string action) { var c = ControlOf(action); return c != Control.None ? Source.Value(c) : 0f; }
        public Vector2 Stick(string action) { var c = ControlOf(action); return c != Control.None ? Source.Stick(c) : Vector2.zero; }

        Control ControlOf(string action)
        {
            var c = Bindings.ControlOf(action);
            if (c == Control.None && _reported.Add(action))
                Debug.LogError("[GPL] Input: action '" + action + "' is not declared in the binding table.");
            return c;
        }
    }
}
