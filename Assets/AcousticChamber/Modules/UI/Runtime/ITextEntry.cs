using System;

namespace GPL.UI
{
    /// <summary>
    /// Swappable text input (ADR-0003): on-canvas number pad, letter keypad, or the Quest system keyboard. Screens only use
    /// this interface, so a chamber can switch the entry method without changing its menus.
    /// </summary>
    public interface ITextEntry
    {
        /// <summary>The raw value (for the number pad: the digits, e.g. "1"; the screen formats it, e.g. "S0001").</summary>
        string Value { get; }

        /// <summary>Raised after every change of <see cref="Value"/>.</summary>
        event Action<string> Changed;

        void SetValue(string value);
        void Clear();

        /// <summary>Shows or hides the entry UI (a native keyboard opens or closes).</summary>
        void SetVisible(bool visible);
    }
}
