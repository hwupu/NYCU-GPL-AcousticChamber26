using System;
using GPL.HearingLoss;
using NUnit.Framework;

namespace AcousticChamber.Chambers.HearingLoss.Tests
{
    public class ExperienceStateTests
    {
        static readonly int[] Zero = new int[10];

        [Test]
        public void StartsPausedNormalHearingFirstClip()
        {
            var s = new ExperienceState(4);
            Assert.AreEqual(0, s.ClipIndex);
            Assert.IsFalse(s.Playing);
            Assert.AreEqual(LossOn.None, s.Ear);
            Assert.AreEqual(HearingLossLevel.None, s.Level);
            Assert.AreEqual(HearingProfile.Normal, s.Profile);
            Assert.AreEqual("Left ear 0 dB, right ear 0 dB (at 4 kHz).", s.Status());
        }

        [Test]
        public void RightLossLeavesLeftAtZero()
        {
            var s = new ExperienceState(4);
            s.SetEar(LossOn.Right);
            CollectionAssert.AreEqual(Zero, s.Profile.Left);
            CollectionAssert.AreEqual(HearingPresets.Mild, s.Profile.Right);
            Assert.AreEqual("Left ear 0 dB, right ear -20 dB (at 4 kHz).", s.Status());
            s.SetEar(LossOn.Left);
            CollectionAssert.AreEqual(HearingPresets.Mild, s.Profile.Left);
            CollectionAssert.AreEqual(Zero, s.Profile.Right);
            Assert.AreEqual("Left ear -20 dB, right ear 0 dB (at 4 kHz).", s.Status());
            s.SetEar(LossOn.Both);
            s.SetLevel(HearingLossLevel.Severe);
            Assert.AreEqual("Left ear -40 dB, right ear -40 dB (at 4 kHz).", s.Status());
        }

        [Test]
        public void ChoosingAnEarAtZeroSetsMinus20()
        {
            var s = new ExperienceState(4);
            s.SetEar(LossOn.Left);
            Assert.AreEqual(HearingLossLevel.Mild, s.Level);
        }

        [Test]
        public void ChoosingAnEarKeepsAnExistingLoss()
        {
            var s = new ExperienceState(4);
            s.SetEar(LossOn.Left);
            s.SetLevel(HearingLossLevel.Moderate);
            s.SetEar(LossOn.Right);
            Assert.AreEqual(HearingLossLevel.Moderate, s.Level);
            Assert.AreEqual(LossOn.Right, s.Ear);
        }

        [Test]
        public void NormalHearingSetsZeroAndZeroSetsNormalHearing()
        {
            var s = new ExperienceState(4);
            s.SetEar(LossOn.Right);
            s.SetLevel(HearingLossLevel.Severe);
            s.SetEar(LossOn.None);
            Assert.AreEqual(HearingLossLevel.None, s.Level);
            Assert.AreEqual(HearingProfile.Normal, s.Profile);

            s.SetEar(LossOn.Left);
            s.SetLevel(HearingLossLevel.None);
            Assert.AreEqual(LossOn.None, s.Ear);
            Assert.AreEqual(HearingProfile.Normal, s.Profile);
        }

        [Test]
        public void LossWhileNormalHearingGoesOnBothEars()
        {
            var s = new ExperienceState(4);
            s.SetLevel(HearingLossLevel.Moderate);
            Assert.AreEqual(LossOn.Both, s.Ear);
            CollectionAssert.AreEqual(HearingPresets.Moderate, s.Profile.Left);
            CollectionAssert.AreEqual(HearingPresets.Moderate, s.Profile.Right);
        }

        [Test]
        public void ButtonLabels()
        {
            Assert.AreEqual("0 dB", ExperienceState.LevelButton(HearingLossLevel.None));
            Assert.AreEqual("-20 dB", ExperienceState.LevelButton(HearingLossLevel.Mild));
            Assert.AreEqual("-30 dB", ExperienceState.LevelButton(HearingLossLevel.Moderate));
            Assert.AreEqual("-40 dB", ExperienceState.LevelButton(HearingLossLevel.Severe));
            Assert.AreEqual("Normal hearing", ExperienceState.EarButton(LossOn.None));
            Assert.AreEqual("Both ears", ExperienceState.EarButton(LossOn.Both));
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
            s.SetEar(LossOn.None);
            s.SetLevel(HearingLossLevel.None);
            Assert.AreEqual(2, n);
            Assert.IsTrue(s.Playing);
            s.SetEar(LossOn.Left); // ear and level change together: one event
            Assert.AreEqual(3, n);
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
