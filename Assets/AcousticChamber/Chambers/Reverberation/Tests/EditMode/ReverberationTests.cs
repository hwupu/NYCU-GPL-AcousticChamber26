using GPL.Acoustics;
using NUnit.Framework;

namespace AcousticChamber.Chambers.Reverberation.Tests
{
    /// <summary>C04 Reverberation (PB-27): the Room row (presets, Custom, stepper limits and rounding).</summary>
    public class ReverberationTests
    {
        [Test]
        public void Presets_AreTheChambersRooms()
        {
            CollectionAssert.AreEqual(new[] { "Open space", "HW2 room", "Custom" }, RoomSizes.Labels());
            var open = RoomSizes.Presets[0].size;
            Assert.AreEqual(Rt60Protocol.OpenSpaceWidth, open.Width, 1e-9);
            Assert.AreEqual(Rt60Protocol.OpenSpaceDepth, open.Depth, 1e-9);
            Assert.AreEqual(Rt60Protocol.OpenSpaceHeight, open.Height, 1e-9);
            Assert.AreEqual("8.72 × 15 × 3.5 m", RoomSizes.Presets[1].size.ToString());
        }

        [Test]
        public void PresetIndex_MatchesTo1cm_ElseCustom()
        {
            Assert.AreEqual(0, RoomSizes.PresetIndex(new RoomSize(12, 12, 3.5)));
            Assert.AreEqual(1, RoomSizes.PresetIndex(new RoomSize(8.72, 15.004, 3.5)));
            Assert.AreEqual(-1, RoomSizes.PresetIndex(new RoomSize(8.82, 15, 3.5)));
        }

        [Test]
        public void Clamp_LimitsAndRoundsTo1cm()
        {
            var s = RoomSizes.Clamp(new RoomSize(8.72 + 0.1, 0.5, 12));
            Assert.AreEqual(8.82, s.Width, 1e-9);
            Assert.AreEqual(RoomSizes.MinDepth, s.Depth, 1e-9);
            Assert.AreEqual(RoomSizes.MaxHeight, s.Height, 1e-9);
            Assert.AreEqual(RoomSizes.MaxWidth, RoomSizes.Clamp(new RoomSize(31, 5, 3)).Width, 1e-9);
        }

        [Test]
        public void SizeText()
        {
            Assert.AreEqual("12 × 12 × 3.5 m", new RoomSize(12, 12, 3.5).ToString());
            Assert.AreEqual("6.1", RoomSizes.M(6.1000001));
        }
    }
}
