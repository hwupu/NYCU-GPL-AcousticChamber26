using System;
using System.Collections.Generic;
using System.Text;
using GPL.Core;
using GPL.Session;

namespace GPL.Protocol
{
    /// <summary>How a stage behaves around a resume and at the end of a session.</summary>
    [Flags]
    public enum StageFlags
    {
        None = 0,
        /// <summary>Runs once per session: skipped (also on a <c>SessionStart</c> resume) once it has ended (e.g. language).</summary>
        Once = 1,
        /// <summary>Runs again after every resume before the resume point, without moving the cursor (e.g. handover).</summary>
        RepeatOnResume = 2,
        /// <summary>Entering it completes the session (status Completed); a kill after this point is not resumable (e.g. thanks).</summary>
        CompletesSession = 4,
    }

    /// <summary>One stage of a protocol: a plain stage (no blocks; the chamber runs its body) or a stage of blocks of trials.</summary>
    public sealed class StageSpec
    {
        /// <summary>The <c>stage</c> column value, e.g. <c>practice</c>.</summary>
        public string Name { get; }
        /// <summary>Trials of each block; empty = a plain stage.</summary>
        public IReadOnlyList<int> TrialsPerBlock { get; }
        /// <summary>Resume policy while the cursor is in this stage.</summary>
        public ResumeFrom Policy { get; }
        public StageFlags Flags { get; }

        public StageSpec(string name, IReadOnlyList<int> trialsPerBlock, ResumeFrom policy, StageFlags flags)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Stage name required.", nameof(name));
            Name = name;
            var t = new List<int>(trialsPerBlock ?? Array.Empty<int>());
            foreach (int n in t)
                if (n < 1) throw new ArgumentException($"Stage '{name}': every block needs at least one trial.");
            TrialsPerBlock = t;
            Policy = policy;
            Flags = flags;
        }

        public int Blocks => TrialsPerBlock.Count;
        public bool HasBlocks => Blocks > 0;
        public bool Has(StageFlags f) => (Flags & f) == f;
    }

    /// <summary>
    /// The stage list of a session (protocol tree: session > stage > block > trial). Built by the chamber from the session's
    /// parameter snapshot, so a resumed session gets the same plan; <see cref="Hash"/> is stored as the session's
    /// <c>plan_hash</c> and a different hash blocks the resume.
    /// </summary>
    public sealed class ProtocolPlan
    {
        readonly List<StageSpec> _stages = new List<StageSpec>();

        public IReadOnlyList<StageSpec> Stages => _stages;

        /// <summary>
        /// One stage of one block with one trial (PB-21b: the Debug bench's Practice trial). Walked like any plan, it writes
        /// <c>stage_begin</c>, <c>block_begin</c>, the chamber's trial row, <c>block_end</c>, <c>stage_end</c>.
        /// </summary>
        public static ProtocolPlan SingleTrial(string stage) => new ProtocolPlan().Blocks(stage, 1, 1, ResumeFrom.StageStart);

        /// <summary>A plain stage (the chamber runs its body between <c>StageBegin</c> and <c>StageEnd</c>).</summary>
        public ProtocolPlan Stage(string name, StageFlags flags = StageFlags.None, ResumeFrom policy = ResumeFrom.StageStart) =>
            Add(new StageSpec(name, null, policy, flags));

        /// <summary>
        /// A plain stage only when <paramref name="on"/> (PB-20b: onboarding stages switched by preset booleans such as
        /// <c>onboarding.language</c>). The order of the calls is the order of the stages.
        /// </summary>
        public ProtocolPlan StageIf(bool on, string name, StageFlags flags = StageFlags.None, ResumeFrom policy = ResumeFrom.StageStart) =>
            on ? Stage(name, flags, policy) : this;

        /// <summary>A stage of <paramref name="blocks"/> blocks of <paramref name="trialsPerBlock"/> trials. Zero blocks or trials: left out.</summary>
        public ProtocolPlan Blocks(string name, int blocks, int trialsPerBlock, ResumeFrom policy, StageFlags flags = StageFlags.None)
        {
            if (blocks <= 0 || trialsPerBlock <= 0) return this;
            var t = new int[blocks];
            for (int i = 0; i < blocks; i++) t[i] = trialsPerBlock;
            return Add(new StageSpec(name, t, policy, flags));
        }

        /// <summary>A stage whose blocks have different trial counts.</summary>
        public ProtocolPlan Blocks(string name, IReadOnlyList<int> trialsPerBlock, ResumeFrom policy, StageFlags flags = StageFlags.None) =>
            trialsPerBlock == null || trialsPerBlock.Count == 0 ? this : Add(new StageSpec(name, trialsPerBlock, policy, flags));

        public ProtocolPlan Add(StageSpec s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            foreach (var o in _stages)
                if (o.Name == s.Name) throw new ArgumentException($"Stage '{s.Name}' appears twice.");
            _stages.Add(s);
            return this;
        }

        public int IndexOf(string name)
        {
            for (int i = 0; i < _stages.Count; i++)
                if (_stages[i].Name == name) return i;
            return -1;
        }

        /// <summary>One line per stage: <c>practice:1x2:StageStart:None</c>.</summary>
        public string Describe()
        {
            var sb = new StringBuilder();
            foreach (var s in _stages)
            {
                sb.Append(s.Name).Append(':');
                for (int b = 0; b < s.Blocks; b++) sb.Append(b == 0 ? "" : ",").Append(Fmt.I(s.TrialsPerBlock[b]));
                sb.Append(':').Append(s.Policy).Append(':').Append(s.Flags).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Short hash of <see cref="Describe"/> (the session's <c>plan_hash</c>).</summary>
        public string Hash() => Hashing.Short(Describe());
    }
}
