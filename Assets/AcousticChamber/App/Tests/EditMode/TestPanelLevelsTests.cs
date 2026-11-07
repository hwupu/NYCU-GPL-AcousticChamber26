using GPL.HearingLoss;
using NUnit.Framework;

namespace AcousticChamber.App.Tests
{
    /// <summary>
    /// Keeps docs/references/HearingLossLevels.md true: band tables and the filter response at the test-panel tones (48 kHz).
    /// If a value here changes, update the doc.
    /// </summary>
    public class TestPanelLevelsTests
    {
        [Test]
        public void FourKilohertzBandEqualsLabel()
        {
            foreach (int db in TestPanelLevels.LevelDb)
                Assert.AreEqual(db, TestPanelLevels.Bands(db)[7], "level " + db); // band 7 = 4000 Hz
            Assert.AreEqual(4000, ANSIS344.BandHz[7]);
        }

        [Test]
        public void ExtrapolatedLevelsAreSevereScaled()
        {
            var severe = HearingPresets.Severe;
            var x15 = TestPanelLevels.Bands(-60);
            var x2 = TestPanelLevels.Bands(-80);
            for (int i = 0; i < severe.Length; i++)
            {
                Assert.AreEqual(severe[i] * 2, x2[i], "x2 band " + i);
                Assert.LessOrEqual(System.Math.Abs(severe[i] * 1.5 - x15[i]), 0.5, "x1.5 band " + i);
            }
        }

        [Test]
        public void HlsimLevelsAreThePresets()
        {
            CollectionAssert.AreEqual(HearingPresets.None, TestPanelLevels.Bands(0));
            CollectionAssert.AreEqual(HearingPresets.Mild, TestPanelLevels.Bands(-20));
            CollectionAssert.AreEqual(HearingPresets.Moderate, TestPanelLevels.Bands(-30));
            CollectionAssert.AreEqual(HearingPresets.Severe, TestPanelLevels.Bands(-40));
        }

        // Rows: levels -20, -30, -40, -60, -80. Columns: 500, 1000, 2000, 4000, 8000 Hz. dB, as in the doc's response table.
        static readonly double[,] Response =
        {
            {   0.0,  -0.1, -10.4,  -20.4,  -0.3 },
            { -10.1, -10.5, -21.3,  -31.3, -11.0 },
            { -10.4, -21.5, -33.9,  -43.7, -22.9 },
            { -16.7, -37.0, -60.5,  -73.1, -42.2 },
            { -28.2, -63.6, -97.8, -111.4, -71.1 },
        };

        [Test]
        public void ResponseAtTonesMatchesDoc()
        {
            for (int r = 0; r < 5; r++)
            {
                int db = TestPanelLevels.LevelDb[r + 1];
                var bank = new EarFilterBank(48000);
                bank.SetGains(TestPanelLevels.Bands(db));
                for (int c = 0; c < TestPanelLevels.ToneHz.Length; c++)
                    Assert.AreEqual(Response[r, c], bank.ResponseDb(TestPanelLevels.ToneHz[c]), 0.05,
                                    "level " + db + " at " + TestPanelLevels.ToneHz[c] + " Hz");
            }
        }
    }
}
