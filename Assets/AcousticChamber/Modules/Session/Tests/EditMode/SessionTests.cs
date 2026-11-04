using System;
using System.Collections.Generic;
using System.IO;
using GPL.Core;
using GPL.Session;
using NUnit.Framework;

namespace GPL.Session.Tests
{
    public class SessionTests
    {
        string _dir;
        static readonly DateTime T0 = new DateTime(2026, 11, 5, 10, 0, 0);

        [SetUp] public void SetUp() { _dir = Path.Combine(Path.GetTempPath(), "gpl_session_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_dir); }
        [TearDown] public void TearDown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

        static SessionRecord NewRecord(string studyId, DateTime start, ResumeFrom policy = ResumeFrom.NextTrial)
        {
            var r = new SessionRecord
            {
                SessionId = DataFileNames.BaseName(start, "HumPerHW2", studyId, false),
                ChamberId = "HumPerHW2", ChamberVersion = "1", StudyId = studyId, Seed = 1234, Language = "zh",
                PresetName = "Default", ParamsHash = "abcd1234", PlanHash = "plan0001", Created = start,
            };
            r.Params.Add(new KeyValuePair<string, string>("rt60_s", "0.7"));
            r.EnterStage(0, policy);
            return r;
        }

        /// <summary>Runs <paramref name="trials"/> finished trials in the current block (begins it if needed).</summary>
        static void Run(SessionRecord r, int trials)
        {
            if (!r.Cursor.BlockBegun) r.BeginBlock();
            for (int i = 0; i < trials; i++) r.TrialFinished();
        }

        [Test]
        public void Progress_BlocksAndStagesMoveTheCursor()
        {
            var r = NewRecord("S0001", T0);
            Assert.AreEqual(1, r.BeginBlock());
            r.TrialFinished(); r.TrialFinished();
            Assert.AreEqual(new SessionCursor(0, 0, 2, 1), r.Cursor);
            r.BlockFinished();
            Assert.AreEqual(new SessionCursor(0, 1, 0, 0), r.Cursor);
            r.StageFinished();
            r.EnterStage(1, ResumeFrom.BlockStart);
            Assert.AreEqual(new SessionCursor(1, 0, 0, 0), r.Cursor);
            Assert.AreEqual(ResumeFrom.BlockStart, r.ResumePolicy);
            Assert.Throws<InvalidOperationException>(() => r.TrialFinished());
        }

        [Test]
        public void Resume_NextTrial_ContinuesSameAttempt()
        {
            var r = NewRecord("S0001", T0, ResumeFrom.NextTrial);
            Run(r, 3);
            var p = r.ResolveResume();
            Assert.IsFalse(p.BeginBlock);
            Assert.AreEqual(3, p.NextTrial);
            r.ApplyResume(p);
            Assert.AreEqual(new SessionCursor(0, 0, 3, 1), r.Cursor);
        }

        [Test]
        public void Resume_NextTrial_BeforeBlockBegun_BeginsBlockAsFirstAttempt()
        {
            var r = NewRecord("S0001", T0, ResumeFrom.NextTrial);
            Run(r, 6); r.BlockFinished();
            var p = r.ResolveResume();
            Assert.IsTrue(p.BeginBlock);
            r.ApplyResume(p);
            Assert.AreEqual(1, r.BeginBlock());
            Assert.AreEqual(1, r.Cursor.Block);
        }

        [Test]
        public void Resume_BlockStart_RedoesBlockAsNewAttempt()
        {
            // interrupted at block 2 (index 1) after 3 trials; BlockStart -> block 2 trial 1, attempt 2
            var r = NewRecord("S0001", T0, ResumeFrom.BlockStart);
            Run(r, 6); r.BlockFinished();
            Run(r, 3);
            var p = r.ResolveResume();
            Assert.AreEqual(0, p.Stage); Assert.AreEqual(1, p.Block); Assert.AreEqual(0, p.NextTrial); Assert.IsTrue(p.BeginBlock);
            r.ApplyResume(p);
            Assert.AreEqual(2, r.BeginBlock());
            Assert.AreEqual(new SessionCursor(0, 1, 0, 2), r.Cursor);
            Assert.AreEqual(1, r.AttemptsOf(0, 0));
        }

        [Test]
        public void Resume_StageStart_RedoesEveryBlockOfTheStage()
        {
            var r = NewRecord("S0001", T0, ResumeFrom.StageStart);
            Run(r, 6); r.BlockFinished();
            Run(r, 2);
            r.ApplyResume(r.ResolveResume());
            Assert.AreEqual(new SessionCursor(0, 0, 0, 0), r.Cursor);
            Assert.AreEqual(2, r.BeginBlock());     // block 1 again: attempt 2
            Run(r, 6); r.BlockFinished();
            Assert.AreEqual(2, r.BeginBlock());     // block 2 again: attempt 2
        }

        [Test]
        public void Resume_SessionStart_GoesBackToStageZero()
        {
            var r = NewRecord("S0001", T0, ResumeFrom.NextTrial);
            Run(r, 2); r.StageFinished();
            r.EnterStage(1, ResumeFrom.SessionStart);
            Run(r, 1);
            r.ApplyResume(r.ResolveResume());
            Assert.AreEqual(new SessionCursor(0, 0, 0, 0), r.Cursor);
            Assert.AreEqual(2, r.BeginBlock());
        }

        [Test]
        public void Resume_NotPossible_ForNotResumableOrClosedSessions()
        {
            var r = NewRecord("S0001", T0, ResumeFrom.NotResumable);
            Assert.IsFalse(r.CanResume);
            Assert.Throws<InvalidOperationException>(() => r.ResolveResume());
            var done = NewRecord("S0002", T0);
            done.Status = SessionStatus.DroppedOut;
            Assert.IsFalse(done.CanResume);
        }

        [Test]
        public void Record_SerializeParseRoundTrip()
        {
            var r = NewRecord("S0001", T0, ResumeFrom.BlockStart);
            Run(r, 6); r.BlockFinished(); Run(r, 3);
            r.Custom["staircase.level"] = "12.5";
            r.Custom["note"] = "multi\nline";
            r.Updated = T0.AddMinutes(5);
            var b = SessionRecord.Parse(r.Serialize());
            Assert.AreEqual(r.SessionId, b.SessionId);
            Assert.AreEqual("S0001", b.StudyId);
            Assert.AreEqual(1234, b.Seed);
            Assert.AreEqual("zh", b.Language);
            Assert.AreEqual("plan0001", b.PlanHash);
            Assert.AreEqual(r.Cursor, b.Cursor);
            Assert.AreEqual(ResumeFrom.BlockStart, b.ResumePolicy);
            Assert.AreEqual(1, b.AttemptsOf(0, 0));
            Assert.AreEqual(1, b.AttemptsOf(0, 1));
            Assert.AreEqual(T0, b.Created);
            Assert.AreEqual(T0.AddMinutes(5), b.Updated);
            CollectionAssert.AreEqual(r.Params, b.Params);
            Assert.AreEqual("12.5", b.Custom["staircase.level"]);
            Assert.AreEqual("multi\nline", b.Custom["note"]);
            b.ApplyResume(b.ResolveResume());
            Assert.AreEqual(2, b.BeginBlock());
        }

        [Test]
        public void Store_ListsUnfinishedPerStudyId_NewestFirst()
        {
            var store = new SessionStore(_dir);
            var a = NewRecord("S0001", T0);
            var b = NewRecord("S0002", T0.AddMinutes(1));
            var c = NewRecord("S0001", T0.AddMinutes(2));
            var d = NewRecord("S0003", T0.AddMinutes(3)); d.Status = SessionStatus.Completed;
            foreach (var r in new[] { a, b, c, d }) store.Save(r, T0.AddHours(1));

            Assert.AreEqual(4, store.ListAll().Count);
            var unfinished = store.ListUnfinished();
            CollectionAssert.AreEqual(new[] { c.SessionId, b.SessionId, a.SessionId }, unfinished.ConvertAll(r => r.SessionId));
            Assert.AreEqual(c.SessionId, store.FindUnfinished("S0001").SessionId);
            Assert.IsNull(store.FindUnfinished("S0003"));
            Assert.AreEqual(T0.AddHours(1), store.Load(a.SessionId).Updated);
        }

        [Test]
        public void DataFiles_PairsCsvAndSession_CountsTrials_DeletesTogether()
        {
            var store = new SessionStore(_dir);
            var a = NewRecord("S0001", T0);
            store.Save(a, T0);
            File.WriteAllText(store.CsvPathFor(a.SessionId), "event,note\nsession_start,\ntrial,\ntrial,\n");
            string debugCsv = Path.Combine(_dir, DataFileNames.BaseName(T0.AddMinutes(5), "HumPerHW2", "", true) + ".csv");
            File.WriteAllText(debugCsv, "event,note\ntrial,\n");
            string otherChamber = Path.Combine(_dir, DataFileNames.BaseName(T0, "Other", "S0001", false) + ".csv");
            File.WriteAllText(otherChamber, "x");
            Directory.CreateDirectory(Path.Combine(_dir, "presets"));
            File.WriteAllText(Path.Combine(_dir, "presets", "Pilot.params"), "x=1");
            File.WriteAllText(Path.Combine(_dir, "readme.txt"), "keep");

            var files = new DataFiles(_dir, "HumPerHW2");
            var list = files.List();
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list[0].IsDebug);                     // newest first
            Assert.IsNull(list[0].SessionPath);
            Assert.AreEqual(1, list[0].TrialRows);
            Assert.AreEqual("S0001", list[1].StudyId);
            Assert.AreEqual(SessionStatus.InProgress, list[1].Status);
            Assert.AreEqual(2, list[1].TrialRows);
            Assert.IsNotNull(list[1].CsvPath);
            Assert.IsNotNull(list[1].SessionPath);

            Assert.AreEqual(2, files.Delete(list[1]));
            Assert.IsFalse(File.Exists(store.PathFor(a.SessionId)));
            Assert.IsFalse(File.Exists(store.CsvPathFor(a.SessionId)));
            Assert.AreEqual(0, store.ListUnfinished().Count);

            Assert.AreEqual(1, files.DeleteAll());
            Assert.AreEqual(0, files.List().Count);
            Assert.IsTrue(File.Exists(otherChamber));
            Assert.IsTrue(File.Exists(Path.Combine(_dir, "presets", "Pilot.params")));
            Assert.IsTrue(File.Exists(Path.Combine(_dir, "readme.txt")));
        }

        [Test]
        public void DataFiles_RefusesEntriesOfAnotherChamber()
        {
            var files = new DataFiles(_dir, "HumPerHW2");
            Assert.Throws<ArgumentException>(() => files.Delete(new DataEntry { BaseName = "20261105_100000_Other_S0001" }));
            Assert.Throws<ArgumentException>(() => files.Delete(new DataEntry { BaseName = "../../evil" }));
        }
    }
}
