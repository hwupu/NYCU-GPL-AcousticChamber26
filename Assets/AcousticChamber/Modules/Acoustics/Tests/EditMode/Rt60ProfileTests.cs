using System.Linq;
using NUnit.Framework;

namespace GPL.Acoustics.Tests
{
    /// <summary>PB-27: target RT60 profiles (sweep range rule) and the time-left estimate of the run dialog.</summary>
    public class Rt60ProfileTests
    {
        static readonly double[] Grid = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };
        static double TenTimes(double r) => r * 10.0; // a made-up table: RT60 = 10 x coefficient

        [Test]
        public void Profiles_AreThePoDefaults()
        {
            CollectionAssert.AreEqual(new[] { "Experience", "HW2", "Short", "Long" }, Rt60Profiles.All.Select(p => p.Name).ToArray());
            CollectionAssert.AreEqual(new[] { 0.7, 2.2 }, Rt60Profiles.Experience.Targets);
            CollectionAssert.AreEqual(new[] { 0.3, 0.7, 2.2 }, Rt60Profiles.Hw2.Targets);
            CollectionAssert.AreEqual(new[] { 0.3, 0.5, 0.7, 1.0 }, Rt60Profiles.Short.Targets);
            CollectionAssert.AreEqual(new[] { 1.5, 2.2, 3.0, 4.0 }, Rt60Profiles.Long.Targets);
            Assert.IsTrue(Rt60Profiles.Experience.FullSweep);
            Assert.IsTrue(Rt60Profiles.Hw2.FullSweep);
            Assert.IsFalse(Rt60Profiles.Short.FullSweep);
            Assert.IsFalse(Rt60Profiles.Long.FullSweep);
        }

        [Test]
        public void FullSweep_IsTheWholeGrid()
        {
            CollectionAssert.AreEqual(Rt60Protocol.SweepReflections, Rt60Profiles.Experience.SweepFor(TenTimes));
            CollectionAssert.AreEqual(Grid, new Rt60Profile("x", new[] { 3.0 }, true).SweepFor(TenTimes, Grid));
        }

        [Test]
        public void PartialSweep_RangePlusOneNeighbourEachSide()
        {
            // targets 3, 4 s -> range 2.1 .. 5.2 s -> 0.3, 0.4, 0.5 inside, plus 0.2 and 0.6
            var p = new Rt60Profile("x", new[] { 4.0, 3.0 }, false);
            CollectionAssert.AreEqual(new[] { 0.2, 0.3, 0.4, 0.5, 0.6 }, p.SweepFor(TenTimes, Grid));
        }

        [Test]
        public void PartialSweep_AtTheGridEdge_KeepsAtLeastFourPoints()
        {
            // target 9 s -> range 6.3 .. 11.7 s -> 0.7, 0.8, 0.9 inside, neighbour 0.6; already 4
            CollectionAssert.AreEqual(new[] { 0.6, 0.7, 0.8, 0.9 }, new Rt60Profile("x", new[] { 9.0 }, false).SweepFor(TenTimes, Grid));
            // target 1 s -> range 0.7 .. 1.3 s -> 0.1 inside, neighbour 0.2 -> widened to 4
            CollectionAssert.AreEqual(new[] { 0.1, 0.2, 0.3, 0.4 }, new Rt60Profile("x", new[] { 1.0 }, false).SweepFor(TenTimes, Grid));
        }

        [Test]
        public void PartialSweep_NothingInRange_UsesTheNearestPoint()
        {
            // target 30 s is far above the table (9 s max): the top of the grid
            CollectionAssert.AreEqual(new[] { 0.6, 0.7, 0.8, 0.9 }, new Rt60Profile("x", new[] { 30.0 }, false).SweepFor(TenTimes, Grid));
        }

        [Test]
        public void PartialSweep_OnTheHw2Table_ShortAndLongAreSubsetsOfTheGrid()
        {
            var t = Rt60Calibration.MetaXrAudioShoebox;
            double vs = RoomMath.VolumeOverSurface(12, 12, 3.5);
            var shortSweep = Rt60Profiles.Short.SweepFor(r => t.Rt60ForReflection(r, vs));
            var longSweep = Rt60Profiles.Long.SweepFor(r => t.Rt60ForReflection(r, vs));
            Assert.Less(shortSweep.Length, Rt60Protocol.SweepReflections.Length);
            Assert.Less(longSweep.Length, Rt60Protocol.SweepReflections.Length);
            Assert.AreEqual(0.20, shortSweep.First(), 1e-9, "short times start at the lowest extension point");
            Assert.AreEqual(0.90, shortSweep.Last(), 1e-9);
            Assert.AreEqual(12, shortSweep.Length, "Design.md of C04 quotes 0.20-0.90, 12 points");
            Assert.AreEqual(0.85, longSweep.First(), 1e-9);
            Assert.AreEqual(0.99, longSweep.Last(), 1e-9, "4 s needs the top of the grid");
            Assert.AreEqual(12, longSweep.Length, "Design.md of C04 quotes 0.85-0.99, 12 points");
            foreach (double r in shortSweep.Concat(longSweep)) Assert.Contains(r, Rt60Protocol.SweepReflections);
        }

        [Test]
        public void TargetName_AndTargetsText()
        {
            Assert.AreEqual("Target 0.7 s", Rt60Profile.TargetName(0.7));
            Assert.AreEqual("0.7, 2.2 s", Rt60Profiles.Experience.TargetsText());
        }

        // ---- time left ---------------------------------------------------------------------------------------

        [Test]
        public void Eta_BeforeTheFirstRecording_IsThePlan()
        {
            var eta = new Rt60Eta(120);
            Assert.AreEqual(0.0, eta.Fraction, 1e-9);
            Assert.AreEqual(120.0, eta.SecondsLeft(0), 1e-9);
            Assert.AreEqual(110.0, eta.SecondsLeft(10), 1e-9, "the clock runs down between updates");
        }

        [Test]
        public void Eta_CorrectsByTheMeasuredSpeed_WithinLimits()
        {
            var eta = new Rt60Eta(100);
            eta.Update(50, 60); // 1.2 x slower than planned
            Assert.AreEqual(0.5, eta.Fraction, 1e-9);
            Assert.AreEqual(60.0, eta.SecondsLeft(60), 1e-9);
            eta.Update(50, 500); // 10 x slower: clamped to MaxRatio
            Assert.AreEqual(50 * Rt60Eta.MaxRatio, eta.SecondsLeft(500), 1e-9);
            eta.Update(100, 520);
            Assert.AreEqual(0.0, eta.SecondsLeft(520), 1e-9);
            Assert.AreEqual(1.0, eta.Fraction, 1e-9);
        }

        [Test]
        public void Eta_Text()
        {
            Assert.AreEqual("Almost done", Rt60Eta.Text(3));
            Assert.AreEqual("About 55 s left", Rt60Eta.Text(52));
            Assert.AreEqual("About 1 min left", Rt60Eta.Text(60));
            Assert.AreEqual("About 2 min 20 s left", Rt60Eta.Text(131));
            Assert.AreEqual("about 6 min", Rt60Eta.Duration(355));
            Assert.AreEqual("about 50 s", Rt60Eta.Duration(47));
        }
    }
}
