using System;
using System.Collections.Generic;
using System.IO;
using GPL.Core;
using GPL.Logging;
using NUnit.Framework;

namespace GPL.Logging.Tests
{
    public class LoggingTests
    {
        string _dir;
        ManualClock _clock;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "gpl_log_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _clock = new ManualClock(new DateTime(2026, 11, 5, 10, 0, 0));
        }

        [TearDown] public void TearDown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

        static LogSchema Schema() => new LogSchema("visual_az_deg", "audio_final_az_deg", "signed_error_deg");

        static LogContext Ctx() => new LogContext
        {
            SessionId = "20261105_100000_HumPerHW2_S0001", Chamber = "HumPerHW2", ChamberVersion = "1", StudyId = "S0001",
            Mode = LogModes.Experiment, Preset = "Default", ParamsHash = "abcd1234",
        };

        [Test]
        public void Schema_CommonColumnsFirst_ChamberColumns_NoteLast()
        {
            var s = Schema();
            Assert.AreEqual("event", s.Columns[0]);
            Assert.AreEqual("note", s.Columns[s.Columns.Count - 1]);
            Assert.AreEqual(LogSchema.CommonColumns.Count + 3 + 1, s.Columns.Count);
            Assert.AreEqual(LogSchema.CommonColumns.Count, s.IndexOf("visual_az_deg"));
            StringAssert.StartsWith("event,timestamp_local,elapsed_s,session_id,chamber,chamber_version,study_id,mode,preset,params_hash,stage,block,condition,trial,attempt,visual_az_deg", s.Header);
        }

        [Test]
        public void Schema_RejectsDuplicatesAndBadNames()
        {
            Assert.Throws<ArgumentException>(() => new LogSchema("trial"));
            Assert.Throws<ArgumentException>(() => new LogSchema("note"));
            Assert.Throws<ArgumentException>(() => new LogSchema("a", "a"));
            Assert.Throws<ArgumentException>(() => new LogSchema("Bad Column"));
        }

        [Test]
        public void Write_FillsContext_ValuesAndElapsed_FlushesAndMirrors()
        {
            var sw = new StringWriter();
            var mirrored = new List<string>();
            var log = new EventLog(sw, _clock, Schema(), Ctx(), mirror: mirrored.Add);
            log.Context.Stage = "experiment"; log.Context.Block = "2"; log.Context.Condition = "noisy"; log.Context.Attempt = 1; log.Context.Trial = 3;
            _clock.AdvanceSeconds(1.5);
            log.Write(LogEvents.Trial, new LogValues().Set("visual_az_deg", 7.5f).Set("audio_final_az_deg", 9.25f).Set("signed_error_deg", 1.75f), "a,b");

            var lines = sw.ToString().TrimEnd().Split('\n');
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual(2, mirrored.Count);
            var f = Csv.SplitLine(lines[1].TrimEnd('\r'));
            var s = Schema();
            Assert.AreEqual("trial", f[s.IndexOf("event")]);
            Assert.AreEqual("2026-11-05T10:00:01.500", f[s.IndexOf("timestamp_local")]);
            Assert.AreEqual("1.5", f[s.IndexOf("elapsed_s")]);
            Assert.AreEqual("S0001", f[s.IndexOf("study_id")]);
            Assert.AreEqual("EXPERIMENT", f[s.IndexOf("mode")]);
            Assert.AreEqual("abcd1234", f[s.IndexOf("params_hash")]);
            Assert.AreEqual("noisy", f[s.IndexOf("condition")]);
            Assert.AreEqual("3", f[s.IndexOf("trial")]);
            Assert.AreEqual("1", f[s.IndexOf("attempt")]);
            Assert.AreEqual("7.5", f[s.IndexOf("visual_az_deg")]);
            Assert.AreEqual("1.75", f[s.IndexOf("signed_error_deg")]);
            Assert.AreEqual("a,b", f[s.IndexOf("note")]);
        }

        [Test]
        public void Write_RejectsUndeclaredAndAutomaticColumns_AllowsPositionOverride()
        {
            var sw = new StringWriter();
            var log = new EventLog(sw, _clock, Schema(), Ctx());
            Assert.Throws<ArgumentException>(() => log.Write("trial", new LogValues().Set("typo_column", 1)));
            Assert.Throws<ArgumentException>(() => log.Write("trial", new LogValues().Set("timestamp_local", "x")));
            log.Write("trial", new LogValues().Set("condition", "practice").Set("trial", 1));
            var f = Csv.SplitLine(sw.ToString().TrimEnd().Split('\n')[1].TrimEnd('\r'));
            Assert.AreEqual("practice", f[Schema().IndexOf("condition")]);
        }

        [Test]
        public void Open_CreatesWithHeader_AppendsWithoutSecondHeader_RefusesOtherLayout()
        {
            string path = Path.Combine(_dir, "data", "s.csv");
            using (var log = EventLog.Open(path, _clock, Schema(), Ctx())) log.Write(LogEvents.SessionStart, "first");
            using (var log = EventLog.Open(path, _clock, Schema(), Ctx())) log.Write(LogEvents.SessionResume, "second");
            var lines = File.ReadAllLines(path);
            Assert.AreEqual(3, lines.Length);
            Assert.AreEqual(Schema().Header, lines[0]);
            StringAssert.StartsWith("session_start,", lines[1]);
            StringAssert.StartsWith("session_resume,", lines[2]);

            Assert.Throws<InvalidOperationException>(() => EventLog.Open(path, _clock, new LogSchema("other"), Ctx()));
        }

        [Test]
        public void Open_RowIsOnDiskBeforeDispose()
        {
            string path = Path.Combine(_dir, "live.csv");
            var log = EventLog.Open(path, _clock, Schema(), Ctx());
            log.Write(LogEvents.SessionStart);
            string text;
            using (var r = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) text = r.ReadToEnd();
            StringAssert.Contains("session_start,", text);
            log.Dispose();
        }
    }
}
