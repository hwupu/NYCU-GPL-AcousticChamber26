using System;
using System.Collections.Generic;
using System.Linq;

namespace GPL.Input
{
    /// <summary>One action bound to one control, and optionally to the other hand's matching control (<see cref="Mirror"/>).</summary>
    public sealed class Binding
    {
        public string Action { get; }
        public Control Control { get; internal set; }
        /// <summary>A second control doing the same (Confirm: X mirrors A); <see cref="Control.None"/> if none.</summary>
        public Control Mirror { get; }
        public bool Global { get; }
        public string Description { get; }

        internal Binding(string action, Control control, bool global, string description, Control mirror = Control.None)
        {
            Action = action;
            Control = control;
            Mirror = mirror;
            Global = global;
            Description = description;
        }

        /// <summary>True when <paramref name="c"/> is this binding's control or its mirror.</summary>
        public bool Uses(Control c) => c != Control.None && (Control == c || Mirror == c);

        /// <summary>"Y (left): Stop", "A (right) / X (left): Confirm".</summary>
        public override string ToString() =>
            Controls.Label(Control) + (Mirror != Control.None ? " / " + Controls.Label(Mirror) : "") + ": " + Description;
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
        public BindingTable Declare(string action, Control control, string description = null) =>
            Declare(action, control, Control.None, description);

        /// <summary>Declares a chamber action on a control and a mirror control (the other hand's matching button).</summary>
        public BindingTable Declare(string action, Control control, Control mirror, string description = null)
        {
            if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("action name is empty");
            if (control == Control.None) throw new ArgumentException("action '" + action + "' needs a control");
            if (mirror == control) throw new ArgumentException("action '" + action + "': the mirror is the control itself");
            if (Find(action) != null) throw new ArgumentException("action '" + action + "' is declared twice");
            CheckFree(control, action);
            if (mirror != Control.None) CheckFree(mirror, action);
            _bindings.Add(new Binding(action, control, false, description ?? InputActions.Describe(action), mirror));
            return this;
        }

        /// <summary>
        /// Declares a standard action (<see cref="InputActions.Standard"/>) on its default control, with its mirror
        /// (<see cref="InputActions.StandardMirror"/>: Confirm on A and X).
        /// </summary>
        public BindingTable DeclareStandard(string action, string description = null)
        {
            if (!InputActions.Standard.TryGetValue(action, out var control))
                throw new ArgumentException("'" + action + "' is not a standard action");
            InputActions.StandardMirror.TryGetValue(action, out var mirror);
            return Declare(action, control, mirror, description);
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

        /// <summary>The mirror control of <paramref name="action"/>; <see cref="Control.None"/> if it has none or is not declared.</summary>
        public Control MirrorOf(string action) => Find(action)?.Mirror ?? Control.None;

        /// <summary>The action on <paramref name="control"/> (as its control or its mirror), or null.</summary>
        public string ActionOn(Control control) => _bindings.FirstOrDefault(b => b.Uses(control))?.Action;

        /// <summary>Menu lines, globals first: "Y (left): Stop", "A (right) / X (left): Confirm", ...</summary>
        public IEnumerable<string> Describe() => _bindings.OrderBy(b => b.Global ? 0 : 1).Select(b => b.ToString());

        Binding Find(string action) => _bindings.FirstOrDefault(b => b.Action == action);

        void CheckFree(Control control, string action)
        {
            var other = _bindings.FirstOrDefault(b => b.Uses(control) && b.Action != action);
            if (other != null)
                throw new ArgumentException(Controls.Label(control) + " is already bound to '" + other.Action + "'; cannot bind '" + action + "'");
        }
    }
}
