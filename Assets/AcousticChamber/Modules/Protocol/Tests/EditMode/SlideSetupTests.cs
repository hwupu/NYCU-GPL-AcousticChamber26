using System.IO;
using GPL.Protocol.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GPL.Protocol.Tests
{
    public class SlideSetupTests
    {
        string _dir;

        [SetUp] public void SetUp() { _dir = Path.Combine(Path.GetTempPath(), "GPL.SlideSetupTests." + System.Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_dir); }
        [TearDown] public void TearDown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

        [Test]
        public void PngSizeReadsTheHeader()
        {
            var tex = new Texture2D(37, 11, TextureFormat.RGBA32, false);
            string path = Path.Combine(_dir, "a.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Assert.AreEqual((37, 11), SlideSetup.PngSize(path));
        }

        [Test]
        public void PngSizeIsZeroForMissingShortOrOtherFiles()
        {
            Assert.AreEqual((0, 0), SlideSetup.PngSize(Path.Combine(_dir, "missing.png")));
            string shortFile = Path.Combine(_dir, "short.png");
            File.WriteAllBytes(shortFile, new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' });
            Assert.AreEqual((0, 0), SlideSetup.PngSize(shortFile));
            string other = Path.Combine(_dir, "other.png");
            File.WriteAllBytes(other, new byte[64]);
            Assert.AreEqual((0, 0), SlideSetup.PngSize(other));
        }

        [Test]
        public void PlaceholderFrameSitsJustInsideTheSafeMargin()
        {
            var fill = new Color32(1, 2, 3, 255);
            var px = SlideSetup.PlaceholderPixels(fill);
            Assert.AreEqual(SlideSetup.Width * SlideSetup.Height, px.Length);
            Color32 At(int x, int y) => px[y * SlideSetup.Width + x];
            int m = SlideSetup.SafeMarginPx;
            Assert.AreEqual(fill, At(0, 0));
            Assert.AreEqual(fill, At(m - 1, m + 10), "outside the margin");
            Assert.AreNotEqual(fill, At(m, m + 10), "left frame line");
            Assert.AreNotEqual(fill, At(m + 3, m + 10));
            Assert.AreEqual(fill, At(m + 4, m + 10), "inside the frame");
            Assert.AreNotEqual(fill, At(SlideSetup.Width - m - 1, SlideSetup.Height - m - 1), "top right corner of the frame");
            Assert.AreEqual(fill, At(SlideSetup.Width / 2, SlideSetup.Height / 2));
        }
    }
}
