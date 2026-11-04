namespace GPL.Logging
{
    /// <summary>
    /// Names of the common events (Architecture section 10). Chambers add their own; keep names snake_case.
    /// </summary>
    public static class LogEvents
    {
        public const string SessionStart = "session_start";
        public const string SessionResume = "session_resume";
        public const string SessionPaused = "session_paused";
        public const string SessionSuperseded = "session_superseded";
        public const string SessionEnd = "session_end";
        public const string ParticipantDropout = "participant_dropout";
        public const string StageBegin = "stage_begin";
        public const string StageEnd = "stage_end";
        public const string BlockBegin = "block_begin";
        public const string BlockEnd = "block_end";
        public const string BlockRestart = "block_restart";
        public const string Trial = "trial";
        public const string Homing = "homing";
        public const string Recenter = "recenter";
        public const string Handover = "handover";
        public const string AppPaused = "app_paused";
        public const string AppResumed = "app_resumed";
        public const string Language = "language";
        public const string ParamChange = "param_change";
        public const string DebugBegin = "debug_begin";
        public const string DebugEnd = "debug_end";
    }
}
