using NUnit.Framework;

namespace GPL.Acoustics.Tests
{
    /// <summary>PB-27 alternative (PO 2026-11-17): imported tables win over headset tables; headset file names and source names.</summary>
    public class Rt60TableChoiceTests
    {
        static Rt60Calibration Table(string source, string platform, double w, double d, double h) =>
            new Rt60Calibration(new[] { 0.5, 0.9 }, new[] { 0.3, 1.3 }, w, d, h, source, platform, Rt60Calibration.ProtocolIso);

        [Test]
        public void Imported_WinsOverHeadset_ForTheSameSize()
        {
            var imported = new[] { Table("imported", "Android", 12, 12, 3.5) };
            var headset = new[] { Table("headset", "Android", 12, 12, 3.5) };
            var c = Rt60TableChoice.Pick(imported, headset, "Android", 12, 12, 3.5, out var source);
            Assert.AreEqual("imported", c.Source);
            Assert.AreEqual(Rt60TableSource.Imported, source);
        }

        [Test]
        public void Headset_FillsASizeWithoutImportedTable()
        {
            var imported = new[] { Table("imported", "Android", 12, 12, 3.5) };
            var headset = new[] { Table("headset", "Android", 6, 8, 3) };
            var c = Rt60TableChoice.Pick(imported, headset, "Android", 6, 8, 3, out var source);
            Assert.AreEqual("headset", c.Source);
            Assert.AreEqual(Rt60TableSource.Headset, source);
        }

        [Test]
        public void OtherPlatformOrSize_IsNone()
        {
            var headset = new[] { Table("headset", "Android", 6, 8, 3) };
            Assert.IsNull(Rt60TableChoice.Pick(null, headset, "Windows", 6, 8, 3, out var s1));
            Assert.AreEqual(Rt60TableSource.None, s1);
            Assert.IsNull(Rt60TableChoice.Pick(null, headset, "Android", 6, 8, 3.1, out var s2));
            Assert.AreEqual(Rt60TableSource.None, s2);
        }

        [Test]
        public void HeadsetFileName_MatchesTheImportedAssetStem()
        {
            Assert.AreEqual("RT60_Android_12x12x3p5.csv", Rt60TableChoice.HeadsetFileName("Android", 12, 12, 3.5));
            Assert.AreEqual("RT60_Android_8p72x15x3p5.csv", Rt60TableChoice.HeadsetFileName("Android", 8.72, 15, 3.5));
        }

        [Test]
        public void SourceName_IsTheReportId()
        {
            string csv = "report_id,kind\nRT60_Reverberation_20261117-101500,sweep\n";
            Assert.AreEqual("RT60_Reverberation_20261117-101500.csv", Rt60TableChoice.SourceName(csv));
            Assert.AreEqual("", Rt60TableChoice.SourceName("report_id,kind\n"));
        }
    }
}
