using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GPL.Core;
using GPL.Logging;
using GPL.Protocol;
using GPL.Session;
using NUnit.Framework;

namespace GPL.Protocol.Tests
{
    public class ProtocolTests
    {
        /// <summary>The Template's plan: language, handover, practice 1x2, experiment 2x3, thanks.</summary>
        static ProtocolPlan Plan(ResumeFrom experiment, int practice = 2, int blocks = 2, int trials = 3) =>
            new ProtocolPlan()
                .Stage("language", StageFlags.Once)
                .Stage("handover", StageFlags.RepeatOnResume)
                .Blocks("practice", 1, practice, ResumeFrom.StageStart)
                .Blocks("experiment", blocks, trials, experiment)
                .Stage("thanks", StageFlags.CompletesSession);

        static SessionRecord NewRecord() => new SessionRecord { SessionId = "20261106_100000_Template_S0001", ChamberId = "Template", StudyId = "S0001" };

        static List<string> WalkAll(ProtocolWalker w)
        {
            var steps = new List<string>();
            while (w.Next()) steps.Add(w.Current.ToString());
            return steps;
        }

        [Test]
        public void Plan_SkipsEmptyStages_RejectsDuplicates_HashFollowsShape()
        {
            var p = Plan(ResumeFrom.BlockStart, practice: 0);
            Assert.AreEqual(new[] { "language", "handover", "experiment", "thanks" }, p.Stages.Select(s => s.Name).ToArray());
            Assert.Throws<ArgumentException>(() => new ProtocolPlan().Stage("a").Stage("a"));
            Assert.AreEqual(Plan(ResumeFrom.BlockStart).Hash(), Plan(ResumeFrom.BlockStart).Hash());
            Assert.AreNotEqual(Plan(ResumeFrom.BlockStart).Hash(), Plan(ResumeFrom.BlockStart, trials: 4).Hash());
            Assert.AreNotEqual(Plan(ResumeFrom.BlockStart).Hash(), Plan(ResumeFrom.NextTrial).Hash());
            Assert.Throws<ArgumentException>(() => new ProtocolWalker(new ProtocolPlan().Blocks("x", 1, 1, ResumeFrom.NextTrial, StageFlags.RepeatOnResume),
                                                                       NewRecord(), false));
        }

        [Test]
        public void FreshWalk_VisitsEveryStepInOrder_AndCompletes()
        {
            var rec = NewRecord();
            int saves = 0;
            var steps = WalkAll(new ProtocolWalker(Plan(ResumeFrom.BlockStart), rec, false, _ => saves++));
            var expected = new List<string>
            {
                "StageBegin language", "StageEnd language", "StageBegin handover", "StageEnd handover",
                "StageBegin practice", "BlockBegin practice b1a1", "Trial practice b1a1 t1", "Trial practice b1a1 t2", "BlockEnd practice b1a1", "StageEnd practice",
                "StageBegin experiment",
                "BlockBegin experiment b1a1", "Trial experiment b1a1 t1", "Trial experiment b1a1 t2", "Trial experiment b1a1 t3", "BlockEnd experiment b1a1",
                "BlockBegin experiment b2a1", "Trial experiment b2a1 t1", "Trial experiment b2a1 t2", "Trial experiment b2a1 t3", "BlockEnd experiment b2a1",
                "StageEnd experiment", "StageBegin thanks", "StageEnd thanks",
            };
            Assert.AreEqual(expected, steps);
            Assert.AreEqual(SessionStatus.Completed, rec.Status);
            Assert.AreEqual("1", rec.Custom[ProtocolWalker.DoneKey("language")]);
            Assert.Greater(saves, 20);
        }

        /// <summary>Runs a fresh walk, "kills the app" during step <paramref name="kill"/> (1-based) and returns the last saved record.</summary>
        static (SessionRecord saved, List<string> rows) RunUntilKill(ProtocolPlan plan, int kill)
        {
            string file = null;
            var rows = new List<string>();
            var w = new ProtocolWalker(plan, NewRecord(), false, r => file = r.Serialize());
            for (int i = 1; i <= kill && w.Next(); i++)
                if (i < kill && w.Current.Kind == StepKind.Trial) rows.Add(Row(w.Current));
            return (file == null ? null : SessionRecord.Parse(file), rows);
        }

        static string Row(ProtocolStep s) => s.Stage.Name + "/" + (s.Block + 1) + "/" + s.Attempt + "/" + (s.Trial + 1);

        [Test]
        public void Resume_NextTrial_ContinuesTheBlock()
        {
            // kill during the 3rd trial of experiment block 2 (2 trials done)
            var plan = Plan(ResumeFrom.NextTrial);
            var (rec, _) = RunUntilKill(plan, 20);
            Assert.AreEqual(2, rec.Cursor.TrialsDone);
            var steps = WalkAll(new ProtocolWalker(plan, rec, true));
            Assert.AreEqual(new[] { "StageBegin handover replay", "StageEnd handover replay", "StageBegin experiment resumed",
                                    "BlockBegin experiment b2a1 resumed continued", "Trial experiment b2a1 t3", "BlockEnd experiment b2a1",
                                    "StageEnd experiment", "StageBegin thanks", "StageEnd thanks" }, steps);
            Assert.AreEqual(SessionStatus.Completed, rec.Status);
        }

        [Test]
        public void Resume_BlockStart_RedoesTheBlockAsAttempt2()
        {
            var plan = Plan(ResumeFrom.BlockStart);
            var (rec, _) = RunUntilKill(plan, 20);
            var steps = WalkAll(new ProtocolWalker(plan, rec, true));
            Assert.AreEqual("BlockBegin experiment b2a2 resumed", steps[3]);
            Assert.AreEqual("Trial experiment b2a2 t1", steps[4]);
            Assert.AreEqual(3, steps.Count(s => s.StartsWith("Trial experiment b2a2")));
        }

        [Test]
        public void Resume_StageStart_RedoesEveryBlockOfTheStage()
        {
            var plan = Plan(ResumeFrom.StageStart);
            var (rec, _) = RunUntilKill(plan, 20);
            var steps = WalkAll(new ProtocolWalker(plan, rec, true));
            Assert.AreEqual("BlockBegin experiment b1a2 resumed", steps[3]);
            Assert.IsTrue(steps.Contains("BlockBegin experiment b2a2"));
            Assert.IsFalse(steps.Any(s => s.Contains("practice")));
        }

        [Test]
        public void Resume_SessionStart_SkipsTheLanguage_RunsTheHandoverOnce()
        {
            var plan = Plan(ResumeFrom.SessionStart);
            var (rec, _) = RunUntilKill(plan, 20);
            var steps = WalkAll(new ProtocolWalker(plan, rec, true));
            Assert.AreEqual("StageBegin handover", steps[0]);
            Assert.AreEqual(1, steps.Count(s => s.StartsWith("StageBegin handover")));
            Assert.IsFalse(steps.Any(s => s.Contains("language")));
            Assert.IsTrue(steps.Contains("BlockBegin practice b1a2"));
            Assert.IsTrue(steps.Contains("BlockBegin experiment b1a2"));
        }

        [Test]
        public void Resume_DuringTheLanguage_AsksForItAgain()
        {
            var plan = Plan(ResumeFrom.BlockStart);
            var (rec, _) = RunUntilKill(plan, 1);
            var steps = WalkAll(new ProtocolWalker(plan, rec, true));
            Assert.AreEqual("StageBegin language resumed", steps[0]);
            Assert.AreEqual("StageBegin handover", steps[2]);
        }

        [Test]
        public void NotResumable_DuringTheExperimentOnly()
        {
            var plan = Plan(ResumeFrom.NotResumable);
            Assert.IsTrue(RunUntilKill(plan, 8).saved.CanResume, "practice uses StageStart");
            Assert.IsFalse(RunUntilKill(plan, 13).saved.CanResume, "experiment");
            Assert.IsFalse(RunUntilKill(plan, 23).saved.CanResume, "thanks: completed");
        }

        /// <summary>
        /// The kill-the-app test in pure form: for every policy and every step, kill, resume from the saved file and finish.
        /// Then every block's highest attempt holds each trial exactly once and the session is completed.
        /// </summary>
        [Test]
        public void KillAtAnyStep_ResumeCompletesEveryBlock(
            [Values(ResumeFrom.NextTrial, ResumeFrom.BlockStart, ResumeFrom.StageStart, ResumeFrom.SessionStart)] ResumeFrom policy)
        {
            var plan = Plan(policy);
            int total = WalkAll(new ProtocolWalker(plan, NewRecord(), false)).Count;
            for (int kill = 1; kill <= total; kill++)
            {
                var (rec, rows) = RunUntilKill(plan, kill);
                if (!rec.CanResume)
                {
                    Assert.AreEqual(SessionStatus.Completed, rec.Status, "kill " + kill);
                    continue;
                }
                // round trip through the file again, as the app does
                rec = SessionRecord.Parse(rec.Serialize());
                var w = new ProtocolWalker(plan, rec, true);
                while (w.Next())
                    if (w.Current.Kind == StepKind.Trial) rows.Add(Row(w.Current));
                Assert.AreEqual(SessionStatus.Completed, rec.Status, "kill " + kill);

                foreach (var stage in plan.Stages.Where(s => s.HasBlocks))
                    for (int b = 1; b <= stage.Blocks; b++)
                    {
                        var mine = rows.Where(r => r.StartsWith(stage.Name + "/" + b + "/")).Select(r => r.Split('/')).ToList();
                        Assert.IsNotEmpty(mine, $"kill {kill}: {stage.Name} block {b} never ran");
                        int top = mine.Max(r => int.Parse(r[2]));
                        var trials = mine.Where(r => int.Parse(r[2]) == top).Select(r => int.Parse(r[3])).OrderBy(t => t).ToArray();
                        Assert.AreEqual(Enumerable.Range(1, stage.TrialsPerBlock[b - 1]).ToArray(), trials, $"kill {kill}: {stage.Name} block {b}");
                    }
            }
        }

        [Test]
        public void Log_WritesStructureRows_AndPositionColumns()
        {
            var sw = new StringWriter();
            var ctx = new LogContext { SessionId = "s", Chamber = "Template" };
            var log = new EventLog(sw, new ManualClock(new DateTime(2026, 11, 6, 10, 0, 0)), new LogSchema("rt_s"), ctx);
            var plan = Plan(ResumeFrom.BlockStart);

            var (rec, _) = RunUntilKill(plan, 20);
            var w = new ProtocolWalker(plan, rec, true);
            while (w.Next())
            {
                ProtocolLog.Write(log, w.Current);
                if (w.Current.Kind == StepKind.Trial) log.Write(LogEvents.Trial, new LogValues().Set("rt_s", 0.5f));
            }
            var lines = sw.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToList();
            var header = Csv.SplitLine(lines[0]).ToArray();
            int ev = Array.IndexOf(header, "event"), st = Array.IndexOf(header, "stage"), bl = Array.IndexOf(header, "block"),
                tr = Array.IndexOf(header, "trial"), at = Array.IndexOf(header, "attempt"), note = Array.IndexOf(header, "note");
            var rows = lines.Skip(1).Select(l => Csv.SplitLine(l).ToArray()).ToList();

            Assert.AreEqual("stage_begin", rows[0][ev]);
            Assert.AreEqual("handover", rows[0][st]);
            Assert.AreEqual("again after resume", rows[0][note]);
            var restart = rows.First(r => r[ev] == "block_restart");
            Assert.AreEqual("experiment", restart[st]);
            Assert.AreEqual("2", restart[bl]);
            Assert.AreEqual("2", restart[at]);
            var trial = rows.First(r => r[ev] == "trial");
            Assert.AreEqual("1", trial[tr]);
            Assert.AreEqual("2", trial[at]);
            var end = rows.First(r => r[ev] == "stage_end" && r[st] == "experiment");
            Assert.AreEqual("", end[bl]);
            Assert.AreEqual("", end[at]);
        }
    }
}
