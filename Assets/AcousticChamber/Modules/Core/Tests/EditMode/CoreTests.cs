using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GPL.Core;
using NUnit.Framework;

namespace GPL.Core.Tests
{
    public class CoreTests
    {
        string _dir;

        [SetUp] public void SetUp() { _dir = Path.Combine(Path.GetTempPath(), "gpl_core_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_dir); }
        [TearDown] public void TearDown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

        // ---- StudyId ----
        [TestCase("1", "S0001")]
        [TestCase("42", "S0042")]
        [TestCase("0042", "S0042")]
        [TestCase("9999", "S9999")]
        public void StudyId_FormatsWithPrefixAndLeadingZeros(string digits, string expected) => Assert.AreEqual(expected, StudyId.FromDigits(digits));

        [TestCase("")]
        [TestCase("0")]
        [TestCase("0000")]
        [TestCase("12345")]
        [TestCase("12a")]
        [TestCase(null)]
        public void StudyId_RejectsInvalidDigits(string digits)
        {
            Assert.IsFalse(StudyId.IsValidDigits(digits));
            Assert.AreEqual("", StudyId.FromDigits(digits));
        }

        [Test]
        public void StudyId_RecognizesFormattedIds()
        {
            Assert.IsTrue(StudyId.IsFormatted("S0001"));
            Assert.IsFalse(StudyId.IsFormatted("S001"));
            Assert.IsFalse(StudyId.IsFormatted("S0000"));
            Assert.IsFalse(StudyId.IsFormatted("P0001"));
        }

        // ---- Ids ----
        [Test]
        public void Ids_SanitizeKeepsSafeCharacters()
        {
            Assert.AreEqual("Ab-1_2", Ids.Sanitize("Ab -1_2/.,"));
            Assert.AreEqual("Ab-12", Ids.SanitizeToken("Ab -1_2"));
            Assert.AreEqual("", Ids.Sanitize("中文"));
        }

        // ---- Csv ----
        [Test]
        public void Csv_EscapesOnlyWhenNeeded_AndRoundTrips()
        {
            Assert.AreEqual("plain", Csv.Escape("plain"));
            Assert.AreEqual("\"a,b\"", Csv.Escape("a,b"));
            Assert.AreEqual("\"say \"\"hi\"\"\"", Csv.Escape("say \"hi\""));
            var fields = new[] { "x", "a,b", "q\"q", "", "中文" };
            CollectionAssert.AreEqual(fields, Csv.SplitLine(Csv.Join(fields)));
        }

        // ---- Fmt ----
        [Test]
        public void Fmt_IsCultureInvariant()
        {
            var saved = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Assert.AreEqual("0.5", Fmt.F(0.5f));
                Assert.AreEqual("1.235", Fmt.F(1.23456));
                Assert.AreEqual("", Fmt.F(float.NaN));
                Assert.IsTrue(Fmt.TryFloat("0.25", out float f));
                Assert.AreEqual(0.25f, f);
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = saved; }
        }

        [Test]
        public void Fmt_TimestampRoundTrips()
        {
            var t = new DateTime(2026, 11, 5, 14, 3, 7, 123);
            Assert.AreEqual("2026-11-05T14:03:07.123", Fmt.Timestamp(t));
            Assert.IsTrue(Fmt.TryParseTimestamp(Fmt.Timestamp(t), out var back));
            Assert.AreEqual(t, back);
        }

        // ---- Rng ----
        [Test]
        public void Rng_IsDeterministicPerSeed_AndDiffersAcrossSeeds()
        {
            var r1 = new Rng(123); var r2 = new Rng(123); var r3 = new Rng(124);
            var s1 = Enumerable.Range(0, 20).Select(_ => r1.Next(1000)).ToArray();
            var s2 = Enumerable.Range(0, 20).Select(_ => r2.Next(1000)).ToArray();
            var s3 = Enumerable.Range(0, 20).Select(_ => r3.Next(1000)).ToArray();
            CollectionAssert.AreEqual(s1, s2);
            CollectionAssert.AreNotEqual(s1, s3);
        }

        [Test]
        public void Rng_KnownSequence_StaysTheSameAcrossVersions()
        {
            // If this fails, the generator changed: stored seeds would no longer rebuild the same trial plans.
            var r = new Rng(42);
            var seq = Enumerable.Range(0, 5).Select(_ => r.Next(100)).ToArray();
            var again = new Rng(42);
            CollectionAssert.AreEqual(seq, Enumerable.Range(0, 5).Select(_ => again.Next(100)).ToArray());
            CollectionAssert.AreEqual(KnownSeq42, seq, "sequence: " + string.Join(",", seq));
        }

        // Recorded from the first verified run (2026-11-05, Unity 6000.6.6f1 Mono). A change here means old seeds give different plans.
        static readonly int[] KnownSeq42 = { 42, 23, 59, 63, 2 };

        [Test]
        public void Rng_NextStaysInRange_AndCoversIt()
        {
            var r = new Rng(7);
            var seen = new HashSet<int>();
            for (int i = 0; i < 2000; i++) { int v = r.Next(6); Assert.That(v, Is.InRange(0, 5)); seen.Add(v); }
            Assert.AreEqual(6, seen.Count);
            for (int i = 0; i < 1000; i++) { double d = r.NextDouble(); Assert.That(d, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0)); }
        }

        [Test]
        public void Rng_ShuffleIsAPermutation()
        {
            var list = Enumerable.Range(0, 10).ToList();
            new Rng(3).Shuffle(list);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 10), list);
        }

        // ---- KeyValueText ----
        [Test]
        public void KeyValueText_RoundTripsWithEscapes_IgnoresComments_KeepsOrder()
        {
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("b", "line1\nline2"),
                new KeyValuePair<string, string>("a", "back\\slash = ok"),
                new KeyValuePair<string, string>("empty", ""),
            };
            string text = KeyValueText.Serialize(pairs, "header\nsecond");
            StringAssert.StartsWith("# header\n# second\n", text);
            var back = KeyValueText.Parse(text);
            CollectionAssert.AreEqual(pairs, back);
            var map = KeyValueText.ParseToDictionary("x=1\n# c=2\n\ny = 3 \nx=4\nbroken line\n");
            Assert.AreEqual("4", map["x"]);
            Assert.AreEqual("3", map["y"]);
            Assert.IsFalse(map.ContainsKey("c"));
        }

        // ---- Hashing ----
        [Test]
        public void Hashing_IsStableAndShort()
        {
            Assert.AreEqual("cbf29ce4", Hashing.Short(""));   // FNV-1a 64 offset basis
            Assert.AreEqual(8, Hashing.Short("abc").Length);
            Assert.AreNotEqual(Hashing.Short("a=1"), Hashing.Short("a=2"));
        }

        // ---- AtomicFile ----
        [Test]
        public void AtomicFile_WritesReplacesAndRecoversFromTemp()
        {
            string p = Path.Combine(_dir, "sub", "state.txt");
            AtomicFile.WriteAllText(p, "one");
            AtomicFile.WriteAllText(p, "two");
            Assert.AreEqual("two", AtomicFile.ReadAllText(p));
            Assert.IsFalse(File.Exists(p + AtomicFile.TempSuffix));

            // power-off between delete and move: only the temp file is left
            File.Delete(p);
            File.WriteAllText(p + AtomicFile.TempSuffix, "three");
            Assert.IsTrue(AtomicFile.Exists(p));
            Assert.AreEqual("three", AtomicFile.ReadAllText(p));
            Assert.AreEqual(1, AtomicFile.Delete(p));
            Assert.IsNull(AtomicFile.ReadAllText(p));
        }

        // ---- DataFileNames ----
        [Test]
        public void DataFileNames_BuildAndParse()
        {
            var t = new DateTime(2026, 11, 5, 9, 8, 7);
            Assert.AreEqual("20261105_090807_HumPerHW2_S0001", DataFileNames.BaseName(t, "HumPerHW2", "S0001", false));
            Assert.AreEqual("20261105_090807_HumPerHW2_S0001_DEBUG", DataFileNames.BaseName(t, "HumPerHW2", "S0001", true));
            Assert.AreEqual("20261105_090807_HumPerHW2_DEBUG", DataFileNames.BaseName(t, "HumPerHW2", "", true));
            Assert.AreEqual("20261105_090807_HumPer", DataFileNames.BaseName(t, "Hum_Per", "", false));   // '_' would break parsing

            Assert.IsTrue(DataFileNames.TryParse("20261105_090807_HumPerHW2_S0001_DEBUG", out var start, out var ch, out var id, out var dbg));
            Assert.AreEqual(t, start); Assert.AreEqual("HumPerHW2", ch); Assert.AreEqual("S0001", id); Assert.IsTrue(dbg);
            Assert.IsTrue(DataFileNames.TryParse("20261105_090807_HumPerHW2", out _, out ch, out id, out dbg));
            Assert.AreEqual("", id); Assert.IsFalse(dbg);
            Assert.IsFalse(DataFileNames.TryParse("notes", out _, out _, out _, out _));
            Assert.IsFalse(DataFileNames.TryParse("20261105_090807_A_S1_DEBUG_extra", out _, out _, out _, out _));
            Assert.Throws<ArgumentException>(() => DataFileNames.BaseName(t, "__", "", false));
        }
    }
}
