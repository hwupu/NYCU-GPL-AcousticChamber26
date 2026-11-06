using System;
using System.Collections.Generic;
using GPL.Session;

namespace GPL.Protocol
{
    public enum StepKind { StageBegin, BlockBegin, Trial, BlockEnd, StageEnd }

    /// <summary>One step of a protocol walk. Indices are 0-based; the log shows block and trial 1-based.</summary>
    public sealed class ProtocolStep
    {
        public StepKind Kind { get; internal set; }
        public int StageIndex { get; internal set; }
        public StageSpec Stage { get; internal set; }
        public int Block { get; internal set; }
        public int Trial { get; internal set; }
        /// <summary>Attempt of the block (1 = first time); 0 outside blocks.</summary>
        public int Attempt { get; internal set; }
        /// <summary>A <see cref="StageFlags.RepeatOnResume"/> stage run again after a resume; the cursor does not move.</summary>
        public bool Replay { get; internal set; }
        /// <summary>The stage or block where a resumed session continues.</summary>
        public bool Resumed { get; internal set; }
        /// <summary>BlockBegin of a block continued in the same attempt (<c>NextTrial</c> resume); <see cref="Trial"/> = first trial run.</summary>
        public bool Continued { get; internal set; }

        public int Blocks => Stage.Blocks;
        public int Trials => Stage.HasBlocks && Block < Stage.Blocks ? Stage.TrialsPerBlock[Block] : 0;
        public bool LastBlock => Block == Stage.Blocks - 1;
        public bool LastTrial => Trial == Trials - 1;

        public override string ToString() =>
            Kind + " " + Stage.Name + (Kind == StepKind.StageBegin || Kind == StepKind.StageEnd ? "" : " b" + (Block + 1) + "a" + Attempt) +
            (Kind == StepKind.Trial ? " t" + (Trial + 1) : "") + (Replay ? " replay" : "") + (Resumed ? " resumed" : "") + (Continued ? " continued" : "");
    }

    /// <summary>
    /// Walks a <see cref="ProtocolPlan"/> step by step and keeps the session cursor in step (generalized from HW2's
    /// ExperimentFlow loop). Pure: the chamber runs each step's content (screens, trials) between two <see cref="Next"/>
    /// calls. <see cref="Next"/> first completes the previous step (a trial counts only now, after the chamber wrote its
    /// row), moves the cursor, calls <c>save</c> after every change of the record, and returns the next step.
    /// On a resume (<see cref="SessionRecord.ResolveResume"/>) it first replays the <see cref="StageFlags.RepeatOnResume"/>
    /// stages before the resume point, skips <see cref="StageFlags.Once"/> stages that already ended, and starts a block
    /// again as a new attempt unless the policy continues it (<c>NextTrial</c>).
    /// </summary>
    public sealed class ProtocolWalker
    {
        /// <summary>Custom key of a <see cref="StageFlags.Once"/> stage that has ended: <c>stage_done.&lt;name&gt;</c> = 1.</summary>
        public static string DoneKey(string stage) => "stage_done." + stage;

        readonly ProtocolPlan _plan;
        readonly SessionRecord _rec;
        readonly Action<SessionRecord> _save;
        readonly bool _resume;
        readonly IEnumerator<ProtocolStep> _steps;

        public ProtocolStep Current { get; private set; }
        public bool Finished { get; private set; }
        public ProtocolPlan Plan => _plan;
        public SessionRecord Record => _rec;

        /// <param name="resume">Continue the record at its resume point (it must be resumable); false = start at the first stage.</param>
        /// <param name="save">Called after every change of the record (write the session file); null = nothing.</param>
        public ProtocolWalker(ProtocolPlan plan, SessionRecord record, bool resume, Action<SessionRecord> save = null)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _rec = record ?? throw new ArgumentNullException(nameof(record));
            foreach (var s in plan.Stages)
                if (s.Has(StageFlags.RepeatOnResume) && s.HasBlocks)
                    throw new ArgumentException($"Stage '{s.Name}': only a plain stage can repeat on resume.");
            _resume = resume;
            _save = save;
            _steps = Walk().GetEnumerator();
        }

        /// <summary>Completes the current step and moves to the next one. False when the protocol has ended.</summary>
        public bool Next()
        {
            if (Finished) return false;
            if (_steps.MoveNext())
            {
                Current = _steps.Current;
                return true;
            }
            Current = null;
            Finished = true;
            return false;
        }

        void Save() => _save?.Invoke(_rec);

        bool Done(StageSpec s) => _rec.Custom.TryGetValue(DoneKey(s.Name), out var v) && v == "1";

        static ProtocolStep Step(StepKind k, int si, StageSpec s, int block = 0, int trial = 0, int attempt = 0) =>
            new ProtocolStep { Kind = k, StageIndex = si, Stage = s, Block = block, Trial = trial, Attempt = attempt };

        IEnumerable<ProtocolStep> Walk()
        {
            var stages = _plan.Stages;
            int startStage = 0, startBlock = 0, startTrial = 0;
            bool beginBlock = true;

            if (_resume)
            {
                var p = _rec.ResolveResume();
                _rec.ApplyResume(p);
                Save();
                startStage = p.Stage;
                startBlock = p.Block;
                startTrial = p.NextTrial;
                beginBlock = p.BeginBlock;
                for (int i = 0; i < startStage && i < stages.Count; i++)
                {
                    if (!stages[i].Has(StageFlags.RepeatOnResume)) continue;
                    var b = Step(StepKind.StageBegin, i, stages[i]);
                    b.Replay = true;
                    yield return b;
                    var e = Step(StepKind.StageEnd, i, stages[i]);
                    e.Replay = true;
                    yield return e;
                }
            }

            for (int si = startStage; si < stages.Count; si++)
            {
                var spec = stages[si];
                bool atResume = _resume && si == startStage;
                if (spec.Has(StageFlags.Once) && Done(spec)) continue;

                if (!atResume) _rec.EnterStage(si, spec.Policy);
                if (spec.Has(StageFlags.CompletesSession)) _rec.Status = SessionStatus.Completed;
                Save();
                var sb = Step(StepKind.StageBegin, si, spec);
                sb.Resumed = atResume;
                yield return sb;

                int b0 = atResume ? startBlock : 0;
                for (int b = b0; b < spec.Blocks; b++)
                {
                    bool first = atResume && b == b0;
                    int t0 = 0, attempt;
                    bool continued = first && !beginBlock && _rec.Cursor.BlockBegun;
                    if (continued)
                    {
                        t0 = startTrial;
                        attempt = _rec.Cursor.Attempt;
                    }
                    else
                    {
                        attempt = _rec.BeginBlock();
                        Save();
                    }
                    var bb = Step(StepKind.BlockBegin, si, spec, b, t0, attempt);
                    bb.Resumed = first;
                    bb.Continued = continued;
                    yield return bb;

                    int n = spec.TrialsPerBlock[b];
                    for (int t = t0; t < n; t++)
                    {
                        yield return Step(StepKind.Trial, si, spec, b, t, attempt);
                        _rec.TrialFinished();
                        Save();
                    }
                    _rec.BlockFinished();
                    Save();
                    yield return Step(StepKind.BlockEnd, si, spec, b, n - 1, attempt);
                }

                if (spec.Has(StageFlags.Once)) _rec.Custom[DoneKey(spec.Name)] = "1";
                _rec.StageFinished();
                Save();
                yield return Step(StepKind.StageEnd, si, spec);
            }
        }
    }
}
