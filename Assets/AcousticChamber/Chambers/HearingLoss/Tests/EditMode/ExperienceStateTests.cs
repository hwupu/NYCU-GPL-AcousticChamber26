using System;
using GPL.HearingLoss;
using NUnit.Framework;

namespace AcousticChamber.Chambers.HearingLoss.Tests
{
    public class ExperienceStateTests
    {
        static readonly int[] Zero = new int[10];

        [Test]
        public void StartsWithNothingPlayingBothEarsZeroDb()
        {
            var s = new ExperienceState(4);
            Assert.AreEqual(-1, s.Sound);
            Assert.IsFalse(s.Playing);
            Assert.AreEqual(Ear.Both, s.Ear);
            Assert.AreEqual(HearingLossLevel.None, s.Level);
            Assert.AreEqual(HearingProfile.Normal, s.Profile);
            Assert.AreEqual(4, s.SoundCount);
            Assert.AreEqual("Left ear 0 dB, right ear 0 dB (at 4 kHz).", s.Status());
        }

        [Test]
        public void ClickPlaysPausesResumesAndSwitches()
        {
            var s = new ExperienceState(4);
            Assert.AreEqual(SoundAction.PlayFromStart, s.ClickSound(3)); // fourth recording
            Assert.IsTrue(s.Playing);
            Assert.AreEqual(SoundAction.Pause, s.ClickSound(3));
            Assert.IsFalse(s.Playing);
            Assert.AreEqual(SoundAction.Resume, s.ClickSound(3));
            Assert.IsTrue(s.Playing);
            Assert.AreEqual(SoundAction.PlayFromStart, s.ClickSound(1)); // second recording
            Assert.AreEqual(1, s.Sound);
            Assert.IsTrue(s.Playing);
            s.ClickSound(1);
            Assert.AreEqual(SoundAction.PlayFromStart, s.ClickSound(0)); // another sound while paused starts it
            Assert.IsTrue(s.Playing);
            s.Stop();
            Assert.IsFalse(s.Playing);
            Assert.AreEqual(0, s.Sound);
        }

        [Test]
        public void RightLossLeavesLeftAtZero()
        {
            var s = new ExperienceState(4);
            s.SetEar(Ear.Right);
            CollectionAssert.AreEqual(Zero, s.Profile.Left);
            CollectionAssert.AreEqual(HearingPresets.Mild, s.Profile.Right);
            Assert.AreEqual("Left ear 0 dB, right ear -20 dB (at 4 kHz).", s.Status());
            s.SetEar(Ear.Left);
            Assert.AreEqual("Left ear -20 dB, right ear 0 dB (at 4 kHz).", s.Status());
            s.SetEar(Ear.Both);
            s.SetLevel(HearingLossLevel.Severe);
            Assert.AreEqual("Left ear -40 dB, right ear -40 dB (at 4 kHz).", s.Status());
        }

        [Test]
        public void ChoosingAnEarAtZeroSetsMinus20AndKeepsAnExistingLoss()
        {
            var s = new ExperienceState(4);
            s.SetEar(Ear.Both); // already selected, but at 0 dB
            Assert.AreEqual(HearingLossLevel.Mild, s.Level);
            s.SetLevel(HearingLossLevel.Moderate);
            s.SetEar(Ear.Left);
            Assert.AreEqual(HearingLossLevel.Moderate, s.Level);
            s.SetLevel(HearingLossLevel.None);
            Assert.AreEqual(Ear.Left, s.Ear);
            Assert.AreEqual(HearingProfile.Normal, s.Profile);
        }

        [Test]
        public void LevelsAreHlsimOnly()
        {
            CollectionAssert.AreEqual(new[] { "0 dB", "-20 dB", "-30 dB", "-40 dB" },
                Array.ConvertAll(ExperienceState.Levels, ExperienceState.LevelButton));
        }

        [Test]
        public void ChangedOnlyOnRealChange()
        {
            var s = new ExperienceState(2);
            int n = 0;
            s.Changed += () => n++;
            s.Stop();
            s.SetLevel(HearingLossLevel.None);
            Assert.AreEqual(0, n);
            s.SetEar(Ear.Left); // ear and level change together: one event
            Assert.AreEqual(1, n);
            s.ClickSound(0);
            Assert.AreEqual(2, n);
        }

        [Test]
        public void RejectsBadInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceState(-1));
            var s = new ExperienceState(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => s.ClickSound(7));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.ClickSound(-1));
            Assert.Throws<ArgumentException>(() => s.SetLevel(HearingLossLevel.Predicted));
        }
    }
}
