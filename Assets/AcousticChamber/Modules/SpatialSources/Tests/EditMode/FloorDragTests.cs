using NUnit.Framework;

namespace GPL.SpatialSources.Tests
{
    public class FloorDragTests
    {
        [Test]
        public void DownwardRayHitsFloor()
        {
            // from 1.5 m height, 45° down, forward: hits the floor 1.5 m ahead
            Assert.IsTrue(FloorDrag.FloorPoint(0, 1.5, 0, 0, -1, 1, 0, 10, out double x, out double z));
            Assert.AreEqual(0, x, 1e-9);
            Assert.AreEqual(1.5, z, 1e-9);
        }

        [Test]
        public void RayAboveHorizonGoesToMaxRange()
        {
            Assert.IsFalse(FloorDrag.FloorPoint(0, 1.5, 0, 1, 0.2, 0, 0, 10, out double x, out double z));
            Assert.Greater(x, 9);
            Assert.AreEqual(0, z, 1e-9);
        }

        [Test]
        public void FarFloorHitBeyondRangeIsNotAHit()
        {
            Assert.IsFalse(FloorDrag.FloorPoint(0, 1.5, 0, 0, -0.01, 1, 0, 10, out _, out double z));
            Assert.AreEqual(10, z, 0.01);
        }

        [Test]
        public void ClampToRing()
        {
            double x = 0, z = 10;
            FloorDrag.ClampToRing(ref x, ref z, 0, 0, 0.5, 6);
            Assert.AreEqual(6, z, 1e-9);
            x = 0.1; z = 0;
            FloorDrag.ClampToRing(ref x, ref z, 0, 0, 0.5, 6);
            Assert.AreEqual(0.5, x, 1e-9);
            x = 2; z = 3;
            FloorDrag.ClampToRing(ref x, ref z, 1, 1, 0.5, 6);
            Assert.AreEqual(2, x); Assert.AreEqual(3, z);
            x = 1; z = 1;
            FloorDrag.ClampToRing(ref x, ref z, 1, 1, 0.5, 6);
            Assert.AreEqual(1, x); Assert.AreEqual(1.5, z, 1e-9);
        }

        [Test]
        public void RaySphere()
        {
            Assert.IsTrue(FloorDrag.RaySphere(0, 0, 0, 0, 0, 1, 0, 0, 5, 1, 10, out double d));
            Assert.AreEqual(4, d, 1e-9);
            Assert.IsFalse(FloorDrag.RaySphere(0, 0, 0, 0, 0, 1, 2, 0, 5, 1, 10, out _));
            Assert.IsFalse(FloorDrag.RaySphere(0, 0, 0, 0, 0, -1, 0, 0, 5, 1, 10, out _));   // behind
            Assert.IsFalse(FloorDrag.RaySphere(0, 0, 0, 0, 0, 1, 0, 0, 5, 1, 3, out _));     // too far
            Assert.IsTrue(FloorDrag.RaySphere(0, 0, 5, 0, 0, 1, 0, 0, 5, 1, 10, out d));      // inside
            Assert.AreEqual(0, d);
        }
    }
}
