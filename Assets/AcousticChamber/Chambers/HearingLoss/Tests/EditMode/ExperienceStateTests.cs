using System;
using GPL.HearingLoss;
using NUnit.Framework;

namespace AcousticChamber.Chambers.HearingLoss.Tests
{
    public class ExperienceStateTests
    {
        [Test]
        public void StartsPausedNormalBothEarsFirstClip()
        {
            var s = new ExperienceState(4);
            Assert.AreEqual(0, s.ClipIndex);
            Assert.IsFalse(s.Playing);
            Assert.AreEqual(Ear.Both, s.Ear);
            Assert.AreEqual(HearingLossLevel.None, s.Level);
            Assert.AreEqual(HearingProfile.Normal, s.Profile);
            Assert.AreEqual("Both ears: normal hearing.", s.Status());
        }

        [Test]
        public void LeftMildLeavesRightNormal()
        {
            var s = new ExperienceState(4);
            s.SetEar(Ear.Left);
            s.SetLevel(HearingLossLevel.Mild);
            CollectionAssert.AreEqual(HearingPresets.Mild, s.Profile.Left);
            CollectionAssert.AreEqual(new int[10], s.Profile.Right);
            Assert.AreEqual("Left ear: Mild, -20 dB at 4 kHz. Right ear: normal.", s.Status());
            s.SetEar(Ear.Right);
            Assert.AreEqual("Left ear: normal. Right ear: Mild, -20 dB at 4 kHz.", s.Status());
            s.SetEar(Ear.Both);
            s.SetLevel(HearingLossLevel.Severe);
            Assert.AreEqual("Both ears: Severe, -40 dB at 4 kHz.", s.Status());
        }

        [Test]
        public void ButtonLabelsMatchPresetDepth()
        {
            Assert.AreEqual("Normal", ExperienceState.LevelButton(HearingLossLevel.None));
            Assert.AreEqual("-20 dB", ExperienceState.LevelButton(HearingLossLevel.Mild));
            Assert.AreEqual("-30 dB", ExperienceState.LevelButton(HearingLossLevel.Moderate));
            Assert.AreEqual("-40 dB", ExperienceState.LevelButton(HearingLossLevel.Severe));
        }

        [Test]
        public void ChangedOnlyOnRealChange()
        {
            var s = new ExperienceState(2);
            int n = 0;
            s.Changed += () => n++;
            Assert.IsFalse(s.SelectClip(0));
            Assert.IsTrue(s.SelectClip(1));
            s.SetPlaying(false);
            s.TogglePlaying();
            s.SetEar(Ear.Both);
            s.SetLevel(HearingLossLevel.None);
            Assert.AreEqual(2, n);
            Assert.IsTrue(s.Playing);
        }

        [Test]
        public void RejectsBadInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceState(0));
            var s = new ExperienceState(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => s.SelectClip(2));
            Assert.Throws<ArgumentException>(() => s.SetLevel(HearingLossLevel.Predicted));
        }
    }
}
