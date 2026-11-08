using NUnit.Framework;
using UnityEngine;

namespace GPL.UI.Tests
{
    /// <summary>The move handle is a rounded rectangle (120 x 8 u, 12 u below the surface) that grows when hovered.</summary>
    public class UiGrabberTests
    {
        [Test]
        public void PillIsARoundedRectangleOfTheGivenSize()
        {
            var mesh = new Mesh();
            UiGrabber.BuildPill(mesh, UiGrabber.PillLength, UiGrabber.PillThickness);
            Assert.AreEqual(120f, mesh.bounds.size.x, 0.01f);
            Assert.AreEqual(8f, mesh.bounds.size.y, 0.01f);
            Assert.AreEqual(0f, mesh.bounds.center.x, 0.01f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void ThicknessGrowsOnHoverAndTheGapIsTwelve()
        {
            Assert.AreEqual(UiGrabber.PillThickness, UiGrabber.ThicknessAt(0f), 1e-4f);
            Assert.AreEqual(UiGrabber.PillThicknessHover, UiGrabber.ThicknessAt(1f), 1e-4f);
            Assert.Greater(UiGrabber.PillThicknessHover, UiGrabber.PillThickness);
            Assert.AreEqual(16f, UiGrabber.EdgeGap, 1e-4f);
            Assert.AreEqual(220f, UiGrabber.LengthAt(1f), 1e-4f);
            Assert.AreEqual(16f, UiGrabber.ThicknessAt(1f), 1e-4f);
        }
    }
}
