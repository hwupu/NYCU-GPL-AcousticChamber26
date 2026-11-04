using System;
using System.Collections.Generic;
using System.IO;
using GPL.Params;
using NUnit.Framework;

namespace GPL.Params.Tests
{
    public class ParamsTests
    {
        string _dir;

        [SetUp] public void SetUp() { _dir = Path.Combine(Path.GetTempPath(), "gpl_params_" + Guid.NewGuid().ToString("N")); }
        [TearDown] public void TearDown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

        static ParameterSchema Schema() => new ParameterSchema(
            ParamDef.Float("rt60_s", 0.3f, 0.3f, 5f, 0.1f, "RT60", "s", "Acoustics"),
            ParamDef.Int("trials_per_block", 6, 1, 50, "Trials per block", group: "Protocol"),
            ParamDef.Bool("background_on", false, "Background", "Acoustics"),
            ParamDef.Choice("hl.left", "none", new[] { "none", "mild", "moderate", "severe" }, "Hearing loss left", "Hearing"),
            ParamDef.Text("note", "", "Note"));

        const string DefaultFile = "# default\nrt60_s=0.7\ntrials_per_block=6\nbackground_on=true\nhl.left=none\nnote=\n";

        [Test]
        public void Defaults_HaveEveryKeyInSchemaOrder()
        {
            var s = Schema();
            var d = s.Defaults();
            Assert.AreEqual(0.3f, d.GetFloat("rt60_s"));
            Assert.AreEqual(6, d.GetInt("trials_per_block"));
            Assert.IsFalse(d.GetBool("background_on"));
            Assert.AreEqual("none", d.GetText("hl.left"));
            CollectionAssert.AreEqual(new[] { "rt60_s", "trials_per_block", "background_on", "hl.left", "note" },
                                      new List<KeyValuePair<string, string>>(d.Pairs()).ConvertAll(p => p.Key));
        }

        [Test]
        public void Schema_RejectsBadDefinitions()
        {
            Assert.Throws<ArgumentException>(() => ParamDef.Float("Bad Key", 1, 0, 2, 0, "x"));
            Assert.Throws<ArgumentException>(() => ParamDef.Float("x", 3, 0, 2, 0, "x"));          // default out of range
            Assert.Throws<ArgumentException>(() => ParamDef.Choice("c", "z", new[] { "a" }, "c"));
            Assert.Throws<ArgumentException>(() => new ParameterSchema(ParamDef.Bool("a", true, "a"), ParamDef.Bool("a", false, "a")));
        }

        [Test]
        public void Set_ValidatesAndCanonicalizes()
        {
            var p = Schema().Defaults();
            p.Set("rt60_s", "2.20");
            Assert.AreEqual(2.2f, p.GetFloat("rt60_s"));
            Assert.AreEqual("2.2", p.GetText("rt60_s"));
            p.Set("background_on", "1");
            Assert.AreEqual("true", p.GetText("background_on"));
            Assert.Throws<ArgumentException>(() => p.Set("rt60_s", "9"));          // out of range: rejected, not clamped
            Assert.Throws<ArgumentException>(() => p.Set("trials_per_block", "2.5"));
            Assert.Throws<ArgumentException>(() => p.Set("hl.left", "total"));
            Assert.Throws<KeyNotFoundException>(() => p.Set("nope", "1"));
            Assert.IsFalse(p.TrySet("rt60_s", "abc", out string err));
            StringAssert.Contains("not a number", err);
        }

        [Test]
        public void Parse_RepairsOldPresets_AndReportsEveryRepair()
        {
            var warnings = new List<string>();
            var p = Schema().Parse("rt60_s=99\nold_param=1\ntrials_per_block=8\n", warnings);
            Assert.AreEqual(0.3f, p.GetFloat("rt60_s"));       // invalid -> default
            Assert.AreEqual(8, p.GetInt("trials_per_block"));  // kept
            Assert.IsFalse(p.GetBool("background_on"));        // missing -> default
            Assert.IsTrue(warnings.Exists(w => w.Contains("old_param")));
            Assert.IsTrue(warnings.Exists(w => w.Contains("rt60_s")));
            Assert.IsTrue(warnings.Exists(w => w.Contains("background_on")));
        }

        [Test]
        public void Serialize_ParseRoundTrip_AndHashFollowsValues()
        {
            var s = Schema();
            var a = s.Defaults();
            a.Set("note", "two\nlines, with comma");
            var b = s.Parse(a.Serialize("comment"), null);
            Assert.IsTrue(a.ValuesEqual(b));
            Assert.AreEqual(a.Hash(), b.Hash());
            b.Set("rt60_s", 1.5f);
            Assert.AreNotEqual(a.Hash(), b.Hash());
            CollectionAssert.AreEqual(new[] { "rt60_s" }, a.DiffKeys(b));
            var c = b.Clone();
            c.Set("rt60_s", 2f);
            Assert.AreEqual(1.5f, b.GetFloat("rt60_s"));
        }

        [Test]
        public void PresetStore_DefaultIsActiveUntilAnotherIsChosen()
        {
            var store = new PresetStore(_dir, Schema(), DefaultFile);
            Assert.AreEqual(PresetStore.DefaultName, store.ActiveName);
            Assert.AreEqual(0.7f, store.LoadActive().GetFloat("rt60_s"));
            CollectionAssert.AreEqual(new[] { "Default" }, store.List());
        }

        [Test]
        public void PresetStore_SaveActivateDelete()
        {
            var store = new PresetStore(_dir, Schema(), DefaultFile);
            var working = store.LoadActive();
            working.Set("rt60_s", 2.2f);
            store.Save("Pilot-A", working);
            Assert.AreEqual("Preset-1", store.NextFreeName());
            store.Save(store.NextFreeName(), working);
            CollectionAssert.AreEqual(new[] { "Default", "Pilot-A", "Preset-1" }, store.List());

            store.SetActive("Pilot-A");
            Assert.AreEqual("Pilot-A", store.ActiveName);
            Assert.AreEqual(2.2f, store.LoadActive().GetFloat("rt60_s"));

            // a fresh store object (app restart) sees the same active preset
            Assert.AreEqual("Pilot-A", new PresetStore(_dir, Schema(), DefaultFile).ActiveName);

            Assert.IsTrue(store.Delete("Pilot-A"));
            Assert.AreEqual(PresetStore.DefaultName, store.ActiveName);
            Assert.IsFalse(store.Delete(PresetStore.DefaultName));
            Assert.IsFalse(store.Delete("Missing"));
        }

        [Test]
        public void PresetStore_RejectsBadNames_AndDefaultOverwrite()
        {
            var store = new PresetStore(_dir, Schema(), DefaultFile);
            var p = store.LoadActive();
            Assert.Throws<ArgumentException>(() => store.Save("Default", p));
            Assert.Throws<ArgumentException>(() => store.Save("default", p));
            Assert.Throws<ArgumentException>(() => store.Save("bad name", p));
            Assert.Throws<ArgumentException>(() => store.Save("../escape", p));
            Assert.Throws<ArgumentException>(() => store.SetActive("Missing"));
            Assert.Throws<FileNotFoundException>(() => store.Load("Missing"));
        }

        [Test]
        public void PresetStore_ActiveFallsBackToDefault_WhenTheFileIsGone()
        {
            var store = new PresetStore(_dir, Schema(), DefaultFile);
            store.Save("X", store.LoadActive());
            store.SetActive("X");
            File.Delete(Path.Combine(_dir, "X" + PresetStore.Extension));
            Assert.AreEqual(PresetStore.DefaultName, store.ActiveName);
        }
    }
}
