using System;
using System.Collections.Generic;
using System.Linq;

namespace GPL.Input
{
    /// <summary>One action bound to one control.</summary>
    public sealed class Binding
    {
        public string Action { get; }
        public Control Control { get; internal set; }
        public bool Global { get; }
        public string Description { get; }

        internal Binding(string action, Control control, bool global, string description)
        {
            Action = action;
            Control = control;
            Global = global;
            Description = description;
        }

        /// <summary>"Y (left): Stop".</summary>
        public override string ToString() => Controls.Label(Control) + ": " + Description;
    }

    /// <summary>
    /// Which control triggers which action, for one chamber. Starts with the global actions (Y = Stop). A control serves
    /// at most one action; an action name is declared once. A chamber may move a global action to another control only
    /// through <see cref="RemapGlobal"/> (declared in its ChamberDefinition; the menu then shows the binding).
    /// Every mistake throws <see cref="ArgumentException"/> at setup, not in the middle of a session.
    /// </summary>
    public sealed class BindingTable
    {
        readonly List<Binding> _bindings = new List<Binding>();

        /// <summary>A table with the global actions at their default controls.</summary>
        public BindingTable()
        {
            foreach (var g in InputActions.Globals)
                _bindings.Add(new Binding(g.Key, g.Value, true, InputActions.Describe(g.Key)));
        }

        public IReadOnlyList<Binding> All => _bindings;

        /// <summary>Declares a chamber action on a control.</summary>
        public BindingTable Declare(string action, Control control, string description = null)
        {
            if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("action name is empty");
            if (control == Control.None) throw new ArgumentException("action '" + action + "' needs a control");
            if (Find(action) != null) throw new ArgumentException("action '" + action + "' is declared twice");
            CheckFree(control, action);
            _bindings.Add(new Binding(action, control, false, description ?? InputActions.Describe(action)));
            return this;
        }

        /// <summary>Declares a standard action (<see cref="InputActions.Standard"/>) on its default control.</summary>
        public BindingTable DeclareStandard(string action, string description = null)
        {
            if (!InputActions.Standard.TryGetValue(action, out var control))
                throw new ArgumentException("'" + action + "' is not a standard action");
            return Declare(action, control, description);
        }

        /// <summary>Moves a global action to another control (explicit chamber decision).</summary>
        public BindingTable RemapGlobal(string action, Control control)
        {
            var b = Find(action);
            if (b == null || !b.Global) throw new ArgumentException("'" + action + "' is not a global action");
            if (control == Control.None) throw new ArgumentException("a global action cannot be unbound");
            if (b.Control == control) return this;
            CheckFree(control, action);
            b.Control = control;
            return this;
        }

        public bool Has(string action) => Find(action) != null;

        /// <summary>The control of <paramref name="action"/>; <see cref="Control.None"/> if not declared.</summary>
        public Control ControlOf(string action) => Find(action)?.Control ?? Control.None;

        /// <summary>The action on <paramref name="control"/>, or null.</summary>
        public string ActionOn(Control control) => _bindings.FirstOrDefault(b => b.Control == control)?.Action;

        /// <summary>Menu lines, globals first: "Y (left): Stop", "A (right): Confirm", ...</summary>
        public IEnumerable<string> Describe() => _bindings.OrderBy(b => b.Global ? 0 : 1).Select(b => b.ToString());

        Binding Find(string action) => _bindings.FirstOrDefault(b => b.Action == action);

        void CheckFree(Control control, string action)
        {
            var other = _bindings.FirstOrDefault(b => b.Control == control && b.Action != action);
            if (other != null)
                throw new ArgumentException(Controls.Label(control) + " is already bound to '" + other.Action + "'; cannot bind '" + action + "'");
        }
    }
}
