using NUnit.Framework;
using UnityEngine;

namespace GPL.SpatialSources.Tests
{
    public class SpatialSourcesTests
    {
        // ---- Polar / Ring -----------------------------------------------------------------------------------

        [Test]
        public void Polar_FollowsTheLabConvention()
        {
            Polar.ToXZ(0, 2, out double x, out double z);
            Assert.AreEqual(0, x, 1e-12); Assert.AreEqual(2, z, 1e-12);       // ahead
            Polar.ToXZ(-90, 2, out x, out z);
            Assert.AreEqual(-2, x, 1e-12); Assert.AreEqual(0, z, 1e-12);      // left
            Polar.ToXZ(180, 2, out x, out z);
            Assert.AreEqual(0, x, 1e-12); Assert.AreEqual(-2, z, 1e-12);      // behind
        }

        [Test]
        public void Polar_AzimuthRoundTripsAndWraps()
        {
            foreach (double az in new[] { -37.5, -7.5, 0, 22.5, 60, 90, 179 })
            {
                Polar.ToXZ(az, 1.2, out double x, out double z);
                Assert.AreEqual(az, Polar.AzimuthDeg(x, z), 1e-9);
            }
            Assert.AreEqual(180, Polar.Wrap180(-180), 1e-12);
            Assert.AreEqual(-170, Polar.Wrap180(190), 1e-12);
            Assert.AreEqual(-20, Polar.DeltaDeg(170, -170), 1e-12);
            Assert.AreEqual(0, Polar.AzimuthDeg(0, 0));
        }

        [Test]
        public void Ring_PlacesAroundARotatedOrigin()
        {
            var origin = new Vector3(1f, 1.2f, -3f);
            var yaw = Quaternion.Euler(0f, 90f, 0f);   // facing +x
            Vector3 p = Ring.Position(origin, yaw, 2f, 0f);
            Assert.AreEqual(3f, p.x, 1e-5); Assert.AreEqual(1.2f, p.y, 1e-5); Assert.AreEqual(-3f, p.z, 1e-5);
            Vector3 left = Ring.Position(origin, yaw, 2f, -90f);
            Assert.AreEqual(1f, left.x, 1e-5); Assert.AreEqual(-1f, left.z, 1e-5); // left of +x is +z
            Assert.AreEqual(-90f, Ring.AzimuthOf(left, origin, yaw), 1e-3);
            Vector3 f = Ring.FacingOrigin(p, origin) * Vector3.forward;
            Assert.AreEqual(-1f, f.x, 1e-5);                                        // looks back at the origin
        }

        [Test]
        public void Ring_YawOfIgnoresPitch()
        {
            var q = Ring.YawOf(new Vector3(0f, -5f, 1f));
            Assert.AreEqual(0f, Quaternion.Angle(q, Quaternion.identity), 1e-3);
            Assert.AreEqual(Quaternion.identity, Ring.YawOf(Vector3.up));
        }

        // ---- PulseSchedule ----------------------------------------------------------------------------------

        [Test]
        public void Pulse_StartsEveryPeriod()
        {
            var p = new PulseSchedule(2, 1);
            Assert.IsTrue(p.Start());
            int starts = 1;
            for (int i = 0; i < 850; i++) if (p.Advance(0.01)) starts++;   // 8.5 s at 100 Hz
            Assert.AreEqual(3, starts);       // at 0, 3, 6 s
            Assert.AreEqual(8.5, p.Elapsed, 1e-6);
        }

        [Test]
        public void Pulse_IsOnOnlyDuringTheSound()
        {
            var p = new PulseSchedule(2, 1);
            p.Start();
            p.Advance(1.9); Assert.IsTrue(p.IsOn);
            p.Advance(0.2); Assert.IsFalse(p.IsOn);    // 2.1 s: gap
            Assert.IsTrue(p.Advance(1.0));             // 3.1 s: second pulse
            Assert.IsTrue(p.IsOn);
        }

        [Test]
        public void Pulse_PausedClockHoldsAndLongFramesDoNotStack()
        {
            var p = new PulseSchedule(2, 1);
            p.Start();
            for (int i = 0; i < 100; i++) Assert.IsFalse(p.Advance(0));  // Stop dialog: time scale 0
            Assert.AreEqual(0, p.Elapsed);
            Assert.IsTrue(p.Advance(10));                                 // one long frame
            Assert.AreEqual(4, p.PulsesStarted);                          // jumps to the pulse due at 9 s
            p.Stop();
            Assert.IsFalse(p.Advance(5));
            Assert.IsFalse(p.IsOn);
        }

        [Test]
        public void Pulse_ZeroGapIsContinuous()
        {
            var p = new PulseSchedule(2, 0);
            p.Start();
            Assert.IsFalse(p.Advance(1.99));
            Assert.IsTrue(p.Advance(0.02));
            Assert.Throws<System.ArgumentException>(() => new PulseSchedule(0, 1));
        }
    }
}
