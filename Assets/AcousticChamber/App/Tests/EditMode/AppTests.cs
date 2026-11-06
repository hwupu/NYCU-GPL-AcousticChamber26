using System.IO;
using NUnit.Framework;

namespace AcousticChamber.App.Tests
{
    public class AppTests
    {
        [Test]
        public void AppState_RoundTrip_AndMissingFileIsEmpty()
        {
            string dir = Path.Combine(Path.GetTempPath(), "gpl_appstate_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, AppState.FileName);
                Assert.AreEqual("", AppState.Load(path).LastChamber);
                new AppState { LastChamber = "HumPerHW2" }.Save(path);
                Assert.AreEqual("HumPerHW2", AppState.Load(path).LastChamber);
                File.WriteAllText(path, "garbage without equals\n");
                Assert.AreEqual("", AppState.Load(path).LastChamber);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void HallGrid_Geometry()
        {
            var g = new HallGrid(904f, 3);
            Assert.AreEqual(450f, g.CardWidth, 1e-4);
            Assert.AreEqual(442f, g.CoverWidth, 1e-4);
            Assert.AreEqual(248.625f, g.CoverHeight, 1e-4);
            Assert.AreEqual(4f + 248.625f + 72f, g.CardHeight, 1e-4);
            Assert.AreEqual(2, g.Rows);
            Assert.AreEqual(2f * g.CardHeight + 8f, g.TotalHeight, 1e-4);
            Assert.AreEqual(-227f, g.X(0), 1e-4);
            Assert.AreEqual(227f, g.X(1), 1e-4);
            Assert.AreEqual(-227f, g.X(2), 1e-4);
            Assert.AreEqual(0f, g.Top(1), 1e-4);
            Assert.AreEqual(g.CardHeight + 8f, g.Top(2), 1e-4);
            Assert.AreEqual(g.CardHeight / 2f - 4f - g.CoverHeight / 2f, g.CoverCenterY, 1e-4);
            Assert.AreEqual(0f, new HallGrid(904f, 0).TotalHeight);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new HallGrid(0f, 1));
        }

        [Test]
        public void SessionLauncher_NewSupersedeSnapshot()
        {
            string dir = Path.Combine(Path.GetTempPath(), "gpl_launch_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var schema = new GPL.Params.ParameterSchema(
                    GPL.Params.ParamDef.Int("blocks", 2, 1, 10, "Blocks"),
                    GPL.Params.ParamDef.Choice("resume_from", "block_start", ResumeNames.All, "Resume from"));
                var values = schema.Defaults();
                values.Set("blocks", 3);
                var store = new GPL.Session.SessionStore(dir);
                var clock = new GPL.Core.ManualClock(new System.DateTime(2026, 11, 6, 10, 0, 0));
                var logSchema = new GPL.Logging.LogSchema("rt_s");

                var a = SessionLauncher.NewSession(store, "Template", "0.1", "0.1.0", "S0001", "Default", values, 42, clock.Now);
                Assert.AreEqual("20261106_100000_Template_S0001", a.SessionId);
                Assert.AreEqual(a.SessionId, store.FindUnfinished("S0001").SessionId);
                using (var log = GPL.Logging.EventLog.Open(store.CsvPathFor(a.SessionId), clock, logSchema, SessionLauncher.ContextOf(a)))
                    log.Write(GPL.Logging.LogEvents.SessionStart, "test");

                clock.AdvanceSeconds(60);
                var b = SessionLauncher.NewSession(store, "Template", "0.1", "0.1.0", "S0001", "Default", values, 43, clock.Now);
                SessionLauncher.Supersede(store, a, b.SessionId, logSchema, clock);
                Assert.AreEqual(GPL.Session.SessionStatus.Superseded, store.Load(a.SessionId).Status);
                Assert.AreEqual(b.SessionId, store.FindUnfinished("S0001").SessionId);
                var lines = File.ReadAllLines(store.CsvPathFor(a.SessionId));
                Assert.AreEqual(3, lines.Length);
                StringAssert.StartsWith("session_superseded,", lines[2]);
                StringAssert.Contains("by " + b.SessionId, lines[2]);

                var snap = SessionLauncher.Snapshot(store.Load(b.SessionId), schema);
                Assert.AreEqual(3, snap.GetInt("blocks"));
                Assert.AreEqual(values.Hash(), snap.Hash());
                Assert.Throws<System.ArgumentException>(() =>
                    SessionLauncher.NewSession(store, "Template", "0.1", "", "1", "Default", values, 1, clock.Now));
                StringAssert.EndsWith("_Template_DEBUG", SessionLauncher.DebugRunId("Template", clock.Now));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void ResumeNames_RoundTrip()
        {
            foreach (var n in ResumeNames.All) Assert.AreEqual(n, ResumeNames.Name(ResumeNames.Parse(n)));
            Assert.AreEqual(GPL.Session.ResumeFrom.BlockStart, ResumeNames.Parse("block_start"));
            Assert.Throws<System.ArgumentException>(() => ResumeNames.Parse("BlockStart"));
        }

        [Test]
        public void ChamberRules_IdAndFolder()
        {
            Assert.IsNull(ChamberRules.Problem("HumPerHW2", "HumPerHW2"));
            Assert.IsNull(ChamberRules.Problem("Template", "_Template"));
            Assert.IsNull(ChamberRules.Problem("Moving-Source", "Moving-Source"));
            StringAssert.Contains("empty", ChamberRules.Problem("", "x"));
            StringAssert.Contains("only letters", ChamberRules.Problem("_Template", "_Template"));
            StringAssert.Contains("only letters", ChamberRules.Problem("Hum Per", "Hum Per"));
            StringAssert.Contains("folder", ChamberRules.Problem("Template", "Templates"));
            Assert.AreEqual("C04 · HumPerHW2", ChamberRules.MenuTitle("C04", "HumPerHW2"));
            Assert.AreEqual("Template", ChamberRules.MenuTitle("", "Template"));
        }

        [Test]
        public void SessionRouter_OpensTheLastChamberOnlyWithUnfinishedSessions()
        {
            var ids = new[] { "Template", "HumPerHW2" };
            System.Func<string, int> counts = id => id == "Template" ? 2 : 0;
            Assert.AreEqual("Template", SessionRouter.Route("Template", ids, counts));
            Assert.IsNull(SessionRouter.Route("HumPerHW2", ids, counts), "no unfinished sessions");
            Assert.IsNull(SessionRouter.Route("Gone", ids, _ => 5), "not in the Hall");
            Assert.IsNull(SessionRouter.Route("", ids, counts), "no last chamber");
            Assert.IsNull(SessionRouter.Route("Template", ids, _ => throw new IOException("unreadable")), "unreadable = 0");
        }
    }
}
