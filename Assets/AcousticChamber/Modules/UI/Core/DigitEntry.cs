using System;

namespace GPL.UI
{
    /// <summary>
    /// Text model behind a number pad: digits only, limited length. The view formats it (e.g. Study ID "1" -> "S0001").
    /// From HW2's ParticipantIdField, without the Unity parts.
    /// </summary>
    public sealed class DigitEntry
    {
        public int MaxLength { get; }
        public string Value { get; private set; } = "";
        public bool IsEmpty => Value.Length == 0;
        public bool IsFull => Value.Length >= MaxLength;

        /// <summary>Raised after every change of <see cref="Value"/>.</summary>
        public event Action Changed;

        public DigitEntry(int maxLength)
        {
            if (maxLength < 1) throw new ArgumentOutOfRangeException(nameof(maxLength));
            MaxLength = maxLength;
        }

        /// <summary>Appends one ASCII digit; false (no change) if full or not a digit.</summary>
        public bool Append(char digit)
        {
            if (digit < '0' || digit > '9' || IsFull) return false;
            Value += digit;
            Changed?.Invoke();
            return true;
        }

        public bool Backspace()
        {
            if (IsEmpty) return false;
            Value = Value.Substring(0, Value.Length - 1);
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (IsEmpty) return;
            Value = "";
            Changed?.Invoke();
        }

        /// <summary>Sets the value from any text: keeps the digits, cut to <see cref="MaxLength"/>.</summary>
        public void Set(string text)
        {
            var digits = "";
            if (text != null)
                foreach (char c in text)
                    if (c >= '0' && c <= '9' && digits.Length < MaxLength) digits += c;
            if (digits == Value) return;
            Value = digits;
            Changed?.Invoke();
        }
    }
}
