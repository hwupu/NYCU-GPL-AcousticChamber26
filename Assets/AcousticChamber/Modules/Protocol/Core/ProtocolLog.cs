using GPL.Core;
using GPL.Logging;

namespace GPL.Protocol
{
    /// <summary>
    /// Writes the structure rows of a protocol walk and keeps the log's position columns (stage, block, trial, attempt) in
    /// step, so the chamber writes only its own rows (<c>trial</c>, <c>homing</c>, ...). Rows:
    /// <c>stage_begin</c> / <c>stage_end</c> (note <c>again after resume</c> for a replayed stage, <c>resumed</c> at the resume
    /// point), <c>block_begin</c> (attempt 1, or a block continued after a resume: note <c>continued at trial n</c>),
    /// <c>block_restart</c> (instead of <c>block_begin</c> for attempt 2 and later), <c>block_end</c>. Trial steps write no row;
    /// they set the trial column (1-based).
    /// </summary>
    public static class ProtocolLog
    {
        public static void Write(EventLog log, ProtocolStep step)
        {
            var c = log.Context;
            switch (step.Kind)
            {
                case StepKind.StageBegin:
                    c.ClearPosition();
                    c.Stage = step.Stage.Name;
                    log.Write(LogEvents.StageBegin, step.Replay ? "again after resume" : step.Resumed ? "resumed" : "");
                    break;
                case StepKind.BlockBegin:
                    c.Block = Fmt.I(step.Block + 1);
                    c.Attempt = step.Attempt;
                    c.Trial = null;
                    string what = "block " + (step.Block + 1) + " of " + step.Blocks + ", " + step.Trials + " trials";
                    if (step.Continued) log.Write(LogEvents.BlockBegin, what + ", continued at trial " + (step.Trial + 1));
                    else if (step.Attempt > 1) log.Write(LogEvents.BlockRestart, what + ", attempt " + step.Attempt);
                    else log.Write(LogEvents.BlockBegin, what);
                    break;
                case StepKind.Trial:
                    c.Block = Fmt.I(step.Block + 1);
                    c.Attempt = step.Attempt;
                    c.Trial = step.Trial + 1;
                    break;
                case StepKind.BlockEnd:
                    c.Trial = null;
                    log.Write(LogEvents.BlockEnd, "");
                    break;
                case StepKind.StageEnd:
                    c.Block = "";
                    c.Attempt = null;
                    c.Trial = null;
                    log.Write(LogEvents.StageEnd, step.Replay ? "again after resume" : "");
                    break;
            }
        }
    }
}
