using System.Collections.Generic;
using GPL.Core;

namespace GPL.Logging
{
    /// <summary>Values of one row (column -> text) with culture-invariant typed setters: <c>new LogValues().Set("az_deg", 7.5f)</c>.</summary>
    public sealed class LogValues : Dictionary<string, string>
    {
        public LogValues Set(string column, string value) { this[column] = value ?? ""; return this; }
        public LogValues Set(string column, float value) { this[column] = Fmt.F(value); return this; }
        public LogValues Set(string column, double value) { this[column] = Fmt.F(value); return this; }
        public LogValues Set(string column, int value) { this[column] = Fmt.I(value); return this; }
        public LogValues Set(string column, bool value) { this[column] = value ? "1" : "0"; return this; }
        /// <summary>Null writes an empty field (e.g. "not measured").</summary>
        public LogValues Set(string column, float? value) { this[column] = value.HasValue ? Fmt.F(value.Value) : ""; return this; }
    }
}
