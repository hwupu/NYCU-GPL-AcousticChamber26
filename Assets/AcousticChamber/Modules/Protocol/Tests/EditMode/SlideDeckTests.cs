using GPL.Protocol;
using GPL.Session;
using NUnit.Framework;

namespace GPL.Protocol.Tests
{
    public class SlideDeckTests
    {
        [Test]
        public void NextWalksToTheEndAndBackStopsAtTheFirst()
        {
            var d = new SlideDeck(3);
            Assert.IsTrue(d.IsFirst);
            Assert.IsFalse(d.Back());
            Assert.IsTrue(d.Next());
            Assert.IsTrue(d.Next());
            Assert.IsTrue(d.IsLast);
            Assert.IsTrue(d.Back());
            Assert.AreEqual(1, d.Index);
            Assert.IsTrue(d.Next());
            Assert.IsFalse(d.Next(), "Next on the last slide ends the deck");
            Assert.IsTrue(d.Done);
            Assert.IsFalse(d.Back(), "an ended deck stays ended");
        }

        [Test]
        public void EmptyDeckIsDoneAtOnce()
        {
            var d = new SlideDeck(0);
            Assert.IsTrue(d.Done);
            Assert.IsFalse(d.Next());
        }

        [Test]
        public void ResumeReopensTheStoredSlideClampedToTheDeck()
        {
            var rec = new SessionRecord();
            Assert.AreEqual(0, SlideDeck.Restore(rec, "instructions", 4), "nothing stored: first slide");
            SlideDeck.Store(rec, "instructions", 2);
            Assert.AreEqual("2", rec.Custom["slide.instructions"]);
            Assert.AreEqual(2, SlideDeck.Restore(rec, "instructions", 4));
            Assert.AreEqual(1, SlideDeck.Restore(rec, "instructions", 2), "deck got shorter: last slide");
            Assert.AreEqual(0, SlideDeck.Restore(rec, "other", 4), "keys are per stage");
            Assert.AreEqual(2, new SlideDeck(4, SlideDeck.Restore(rec, "instructions", 4)).Index);
        }

        [Test]
        public void StoredIndexSurvivesTheSessionFile()
        {
            var rec = new SessionRecord { SessionId = "20261107_100000_Template_S0001", ChamberId = "Template", StudyId = "S0001" };
            SlideDeck.Store(rec, "instructions", 1);
            var back = SessionRecord.Parse(rec.Serialize());
            Assert.AreEqual(1, SlideDeck.Restore(back, "instructions", 3));
        }

        [Test]
        public void StageIfLeavesSwitchedOffStagesOutAndKeepsTheOrder()
        {
            var plan = new ProtocolPlan()
                .StageIf(true, "handover", StageFlags.RepeatOnResume)
                .StageIf(false, "language", StageFlags.Once)
                .StageIf(true, "recenter", StageFlags.RepeatOnResume)
                .Blocks("experiment", 1, 2, ResumeFrom.BlockStart);
            CollectionAssert.AreEqual(new[] { "handover", "recenter", "experiment" }, System.Linq.Enumerable.Select(plan.Stages, s => s.Name));
            Assert.AreNotEqual(plan.Hash(), new ProtocolPlan().Stage("handover", StageFlags.RepeatOnResume).Stage("language", StageFlags.Once)
                .Stage("recenter", StageFlags.RepeatOnResume).Blocks("experiment", 1, 2, ResumeFrom.BlockStart).Hash(),
                "a switch changes the plan hash");
        }

        [Test]
        public void NoteNamesTheSlideItsPositionAndHow()
        {
            var d = new SlideDeck(5, 1);
            Assert.AreEqual("procedure 2/5 next", d.Note("procedure", SlideDeck.How.Next));
        }
    }
}
