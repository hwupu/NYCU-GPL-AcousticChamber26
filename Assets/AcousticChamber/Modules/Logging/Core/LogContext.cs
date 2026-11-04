namespace GPL.Logging
{
    public static class LogModes
    {
        public const string Experiment = "EXPERIMENT";
        public const string Debug = "DEBUG";
    }

    /// <summary>
    /// Values repeated on every row. The session part is fixed when the log opens; the position part (stage, block, condition,
    /// trial, attempt) is updated by the protocol as it moves on, so trial code only writes its own measurements.
    /// </summary>
    public sealed class LogContext
    {
        // session (fixed)
        public string SessionId = "";
        public string Chamber = "";
        public string ChamberVersion = "";
        public string StudyId = "";
        public string Mode = LogModes.Experiment;
        public string Preset = "";
        public string ParamsHash = "";

        // position (changes)
        public string Stage = "";
        public string Block = "";
        public string Condition = "";
        /// <summary>1-based trial number in the block, or null outside trials.</summary>
        public int? Trial;
        /// <summary>1-based attempt of the current block (ADR-0003), or null outside blocks.</summary>
        public int? Attempt;

        /// <summary>Clears the position (between stages).</summary>
        public void ClearPosition()
        {
            Stage = ""; Block = ""; Condition = ""; Trial = null; Attempt = null;
        }
    }
}
