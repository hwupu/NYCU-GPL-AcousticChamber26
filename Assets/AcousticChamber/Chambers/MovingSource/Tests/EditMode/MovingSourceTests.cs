using System;
using NUnit.Framework;

namespace AcousticChamber.Chambers.MovingSource.Tests
{
    public class MovingSourceTests
    {
        const double Eps = 1e-6;

        static double Dist(SourcePath p, double hx, double hz) => Math.Sqrt((p.X - hx) * (p.X - hx) + (p.Z - hz) * (p.Z - hz));

        [Test]
        public void CircleKeepsRadiusAndHeightAndTurns30DegreesPerSecond()
        {
            var p = new SourcePath();
            p.Start(PathKind.Circle, 1.0, 2.0, 40.0, 1.0, 1.4, 2.0 + 3.0); // 3 m straight ahead (world +z), 1.4 m high
            for (int i = 0; i < 100; i++) p.Advance(0.1, 1.0);
            Assert.AreEqual(3.0, Dist(p, 1.0, 2.0), Eps);
            Assert.AreEqual(1.4, p.Y, Eps);
            // 10 s x 30 deg = 300 deg clockwise from +z: sin(300) = -0.866, cos(300) = 0.5
            Assert.AreEqual(1.0 + 3.0 * Math.Sin(300 * Math.PI / 180), p.X, 1e-6);
            Assert.AreEqual(2.0 + 3.0 * Math.Cos(300 * Math.PI / 180), p.Z, 1e-6);
        }

        [Test]
        public void SpeedScalesTheAdvance()
        {
            var a = new SourcePath();
            var b = new SourcePath();
            a.Start(PathKind.Circle, 0, 0, 0, 0, 1.2, 2);
            b.Start(PathKind.Circle, 0, 0, 0, 0, 1.2, 2);
            a.Advance(1.0, 2.0);
            b.Advance(2.0, 1.0);
            Assert.AreEqual(a.X, b.X, Eps);
            Assert.AreEqual(a.Z, b.Z, Eps);
        }

        [Test]
        public void FlyByGoesLeftToRightTwoMetresAheadAndBack()
        {
            var p = new SourcePath();
            p.Start(PathKind.FlyBy, 0, 0, 0, 0, 1.2, 1); // head looks along +z
            Assert.AreEqual(-5.0, p.X, Eps);
            Assert.AreEqual(2.0, p.Z, Eps);
            p.Advance(10.0 / 3.0, 1.0); // 5 m at 1.5 m/s: the middle
            Assert.AreEqual(0.0, p.X, 1e-6);
            p.Advance(10.0 / 3.0, 1.0); // the right end
            Assert.AreEqual(5.0, p.X, 1e-6);
            p.Advance(10.0 / 3.0, 1.0); // back again
            Assert.AreEqual(0.0, p.X, 1e-6);
        }

        [Test]
        public void FlyByFollowsTheHeadYaw()
        {
            var p = new SourcePath();
            p.Start(PathKind.FlyBy, 0, 0, 90.0, 0, 1.2, 1); // head looks along +x: ahead is +x, its right is -z
            Assert.AreEqual(2.0, p.X, 1e-6);
            Assert.AreEqual(5.0, p.Z, 1e-6); // the left end: left of +x is +z
        }

        [Test]
        public void ApproachRunsBetweenSixAndHalfAMetre()
        {
            var p = new SourcePath();
            p.Start(PathKind.Approach, 0, 0, 0, 0, 1.2, 2); // azimuth 0
            Assert.AreEqual(6.0, Dist(p, 0, 0), Eps);
            double min = 99, max = 0;
            for (int i = 0; i < 400; i++)
            {
                p.Advance(0.05, 1.0);
                double d = Dist(p, 0, 0);
                min = Math.Min(min, d); max = Math.Max(max, d);
            }
            Assert.AreEqual(0.5, min, 0.05);
            Assert.AreEqual(6.0, max, 0.05);
        }

        [Test]
        public void StopEndsTheAdvance()
        {
            var p = new SourcePath();
            p.Start(PathKind.Circle, 0, 0, 0, 0, 1.2, 2);
            Assert.IsTrue(p.Advance(0.1, 1.0));
            p.Stop();
            Assert.IsFalse(p.Advance(0.1, 1.0));
            Assert.AreEqual(PathKind.None, p.Kind);
        }

        [Test]
        public void LimitsKeepDistanceAndHeight()
        {
            double x = 0, y = 1.2, z = 30;
            SourceLimits.Clamp(ref x, ref y, ref z, 0, 1.6, 0);
            Assert.AreEqual(10.0, Math.Sqrt(x * x + (y - 1.6) * (y - 1.6) + z * z), 1e-6);
            x = 0; y = 1.6; z = 0.1;
            SourceLimits.Clamp(ref x, ref y, ref z, 0, 1.6, 0);
            Assert.AreEqual(0.5, z, 1e-6);
            x = 3; y = -2; z = 3;
            SourceLimits.Clamp(ref x, ref y, ref z, 0, 1.6, 0);
            Assert.GreaterOrEqual(y, 0.1 - 1e-9);
        }
    }
}
