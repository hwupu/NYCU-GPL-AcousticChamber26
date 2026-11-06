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
    }
}
