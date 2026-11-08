using System;
using NUnit.Framework;

namespace AcousticChamber.Chambers.HiddenSource.Tests
{
    public class HiddenRoundTests
    {
        const double Eps = 1e-6;

        [Test]
        public void DrawStaysInTheRangeAndIsSeedable()
        {
            var a = new Random(7);
            var b = new Random(7);
            for (int i = 0; i < 2000; i++)
            {
                var s = HiddenRound.Draw(a);
                Assert.That(s.DistanceM, Is.InRange(1.0, 6.0));
                Assert.That(s.AzimuthDeg, Is.InRange(-180.0, 180.0));
                var t = HiddenRound.Draw(b);
                Assert.AreEqual(s.AzimuthDeg, t.AzimuthDeg);
                Assert.AreEqual(s.DistanceM, t.DistanceM);
            }
        }

        [Test]
        public void WorldAndSpotRoundTripWithHeadOffsetAndYaw()
        {
            var spot = new Spot(-40, 3.5);
            HiddenRound.ToWorld(spot, 1.0, -2.0, 30.0, out double x, out double z);
            var back = HiddenRound.FromWorld(x, z, 1.0, -2.0, 30.0);
            Assert.AreEqual(spot.AzimuthDeg, back.AzimuthDeg, Eps);
            Assert.AreEqual(spot.DistanceM, back.DistanceM, Eps);
        }

        [Test]
        public void AzimuthZeroIsAheadAndNegativeIsLeft()
        {
            HiddenRound.ToWorld(new Spot(0, 2), 0, 0, 0, out double x, out double z);
            Assert.AreEqual(0, x, Eps);
            Assert.AreEqual(2, z, Eps);
            HiddenRound.ToWorld(new Spot(-90, 2), 0, 0, 0, out x, out z);
            Assert.AreEqual(-2, x, Eps); // left of a head looking along +z
            Assert.AreEqual(0, z, Eps);
        }

        [Test]
        public void ErrorsAreSignedGuessMinusSource()
        {
            var r = new RoundResult(new Spot(-40, 3.5), new Spot(-28, 2.7));
            Assert.AreEqual(12, r.AzimuthErrorDeg, Eps);
            Assert.AreEqual(-0.8, r.DistanceErrorM, Eps);
            Assert.AreEqual("Azimuth error +12° (source -40°, guess -28°). Distance error -0.8 m (source 3.5 m, guess 2.7 m).", r.Describe());
        }

        [Test]
        public void AzimuthErrorTakesTheShortWayAroundTheBack()
        {
            var r = new RoundResult(new Spot(170, 2), new Spot(-170, 2));
            Assert.AreEqual(20, r.AzimuthErrorDeg, Eps);
            r = new RoundResult(new Spot(-170, 2), new Spot(170, 2));
            Assert.AreEqual(-20, r.AzimuthErrorDeg, Eps);
        }

        [Test]
        public void ZeroErrorHasNoMinusSign()
        {
            var r = new RoundResult(new Spot(10, 2), new Spot(10, 2));
            StringAssert.Contains("Azimuth error +0°", r.Describe());
            StringAssert.Contains("Distance error +0.0 m", r.Describe());
        }
    }
}
