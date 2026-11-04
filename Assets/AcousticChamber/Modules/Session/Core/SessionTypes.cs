namespace GPL.Session
{
    /// <summary>Life cycle of a real session. Only <see cref="InProgress"/> sessions can be resumed.</summary>
    public enum SessionStatus
    {
        /// <summary>Running, paused from the Stop dialog, or interrupted (app closed, headset off).</summary>
        InProgress,
        Completed,
        /// <summary>The participant withdrew (Stop dialog, Drop out).</summary>
        DroppedOut,
        /// <summary>The experimenter chose "Start new" for the same Study ID.</summary>
        Superseded,
    }

    /// <summary>Where an interrupted stage continues (ADR-0003). Set per stage (or block) by the protocol.</summary>
    public enum ResumeFrom
    {
        /// <summary>After the last finished trial of the block (HW2 behavior).</summary>
        NextTrial,
        /// <summary>From the first trial of the interrupted block, as a new attempt.</summary>
        BlockStart,
        /// <summary>From the first block of the interrupted stage; each block becomes a new attempt.</summary>
        StageStart,
        /// <summary>From the very beginning (same CSV); each block becomes a new attempt.</summary>
        SessionStart,
        /// <summary>The session cannot be continued; start a new one.</summary>
        NotResumable,
    }

    /// <summary>
    /// Position in the protocol tree session > stage > block > trial (all 0-based indices).
    /// <see cref="TrialsDone"/> = finished trials in the current block = index of the next trial.
    /// <see cref="Attempt"/> = attempt number of the current block (1 = first time); 0 = the block has not begun yet.
    /// </summary>
    public struct SessionCursor
    {
        public int Stage;
        public int Block;
        public int TrialsDone;
        public int Attempt;

        public SessionCursor(int stage, int block, int trialsDone, int attempt)
        {
            Stage = stage; Block = block; TrialsDone = trialsDone; Attempt = attempt;
        }

        public bool BlockBegun => Attempt > 0;

        public override string ToString() => $"stage {Stage}, block {Block}, {TrialsDone} trials done, attempt {Attempt}";
    }

    /// <summary>The result of resolving a resume: where to continue and whether the block must begin again (new attempt).</summary>
    public struct ResumePoint
    {
        public int Stage;
        public int Block;
        /// <summary>Index of the next trial to run in the block.</summary>
        public int NextTrial;
        /// <summary>True: call <see cref="SessionRecord.BeginBlock"/> (new attempt, trials from 0). False: continue the current attempt.</summary>
        public bool BeginBlock;
        public ResumeFrom Policy;
    }
}
