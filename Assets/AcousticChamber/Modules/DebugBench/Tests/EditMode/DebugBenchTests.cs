using System.Collections.Generic;
using System.IO;
using System.Linq;
using GPL.Params;
using GPL.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.DebugBench.Tests
{
    public class WorkingCopyTests
    {
        string _dir;
        PresetStore _store;

        static ParameterSchema Schema() => new ParameterSchema(
            ParamDef.Float("rt60_s", 0.7f, 0.3f, 5f, 0.1f, "RT60", "s"),
            ParamDef.Float("gain_db", 0f, -6f, 6f, 0.25f, "Gain", "dB"),
            ParamDef.Int("trials", 6, 1, 8, "Trials"),
            ParamDef.Bool("fixation", true, "Fixation"),
            ParamDef.Choice("hl", "none", new[] { "none", "mild", "severe" }, "Hearing loss"),
            ParamDef.Text("note", "x", "Note"));

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "gpl_bench_" + System.Guid.NewGuid().ToString("N"));
            var s = Schema();
            _store = new PresetStore(_dir, s, s.Defaults().Serialize());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void StepsStayOnTheGridAndInsideTheRange()
        {
            var w = new WorkingCopy(_store);
            Assert.IsTrue(w.Step("rt60_s", +1));
            Assert.AreEqual("0.8", w.Values.GetText("rt60_s"));
            for (int i = 0; i < 5; i++) w.Step("rt60_s", -1);
            Assert.AreEqual("0.3", w.Values.GetText("rt60_s"), "stops at the minimum");
            Assert.IsFalse(w.CanStep("rt60_s", -1));
            Assert.IsFalse(w.Step("rt60_s", -1));
            w.Step("gain_db", -1);
            Assert.AreEqual("-0.25", w.Values.GetText("gain_db"));
            for (int i = 0; i < 10; i++) w.Step("trials", +1);
            Assert.AreEqual(8, w.Values.GetInt("trials"));
        }

        [Test]
        public void BoolTogglesChoiceWrapsTextIsNotEditable()
        {
            var w = new WorkingCopy(_store);
            w.Step("fixation", +1);
            Assert.IsFalse(w.Values.GetBool("fixation"));
            w.Step("hl", -1);
            Assert.AreEqual("severe", w.Values.GetText("hl"), "wraps around");
            w.Step("hl", +1);
            Assert.AreEqual("none", w.Values.GetText("hl"));
            Assert.IsFalse(w.CanStep("note", +1));
        }

        [Test]
        public void EditsRevertSaveAndSaveAs()
        {
            var w = new WorkingCopy(_store);
            var changes = new List<string>();
            w.ParamChanged += (k, o, n) => changes.Add(k + ":" + o + "->" + n);
            Assert.AreEqual("Default", w.Source);
            Assert.IsFalse(w.CanSave, "nothing edited");
            w.Step("trials", +1);
            Assert.AreEqual("Default · 1 edited", w.Label);
            CollectionAssert.AreEqual(new[] { "trials:6->7" }, changes);
            Assert.IsFalse(w.CanSave, "Default is read-only");
            w.Revert();
            Assert.IsFalse(w.IsEdited);

            w.Step("trials", +1);
            string name = w.SaveAs();
            Assert.AreEqual("Preset-1", name);
            Assert.AreEqual("Preset-1", w.Source);
            Assert.IsFalse(w.IsEdited);
            Assert.AreEqual("Default", _store.ActiveName, "Save as does not make it active");
            Assert.AreEqual(7, _store.Load("Preset-1").GetInt("trials"));

            w.Step("trials", +1);
            Assert.IsTrue(w.CanSave);
            Assert.AreEqual("Preset-1: 7", "Preset-1: " + w.SourceValue("trials"));
            w.Save();
            Assert.AreEqual(8, _store.Load("Preset-1").GetInt("trials"));
            Assert.IsFalse(w.IsEdited);
        }

        [Test]
        public void FormatUsesTheStepsDecimalsAndTheUnit()
        {
            var s = Schema();
            Assert.AreEqual("0.7 s", WorkingCopy.Format(s.Get("rt60_s"), "0.7"));
            Assert.AreEqual("-0.25 dB", WorkingCopy.Format(s.Get("gain_db"), "-0.25"));
            Assert.AreEqual("6", WorkingCopy.Format(s.Get("trials"), "6"));
            Assert.AreEqual("On", WorkingCopy.Format(s.Get("fixation"), "true"));
            Assert.AreEqual(2, WorkingCopy.Decimals(s.Get("gain_db")));
        }

        [Test]
        public void BenchPagesBuildAndFollowTheWorkingCopy()
        {
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 700f);
                var window = UiKit.Window(canvas.transform);
                var pages = PageStack.On(window);
                pages.Push("Menu");
                var w = new WorkingCopy(_store);
                var bench = new DebugBench(pages, canvas.transform, w);
                bool ran = false;
                bench.RunRequested += () => ran = true;

                var debug = bench.OpenDebugPage();
                Button Find(Page p, string label) => p.GetComponentsInChildren<Button>(true).Single(b => UiKit.LabelOf(b).text == label);
                Find(debug, "Run experiment trials").onClick.Invoke();
                Assert.IsTrue(ran);
                var run = (RectTransform)Find(debug, "Run experiment trials").transform;
                var practice = (UiButton)Find(debug, "Practice trial");
                Assert.AreEqual(UiStyle.Borderless, practice.Style, "not the main action: fill on hover only");
                Assert.IsTrue(practice.InGroup);
                float gap = ((RectTransform)practice.transform).anchoredPosition.x - run.anchoredPosition.x - run.sizeDelta.x;
                Assert.AreEqual(UiKit.Theme.groupPadding, gap, 1e-3f, "buttons in a group are 8 u apart");

                Find(debug, "Working copy").onClick.Invoke();
                var parameters = pages.Top;
                Assert.AreEqual("Parameters", parameters.Title);
                var row = parameters.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Row Trials");
                var value = UiKit.RowText(row, "Value");
                Assert.AreEqual("6", value.text);
                row.Find("Plus").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual("7", value.text, "the row follows the working copy");
                Assert.AreEqual("Default: 6", UiKit.RowText(row, "Sub").text);
                Assert.IsTrue(Find(parameters, "Revert").interactable);
                Assert.IsFalse(Find(parameters, "Save to Default").interactable, "Default is read-only");

                Find(parameters, "Revert").onClick.Invoke();
                Assert.AreEqual("6", value.text);
                Assert.IsFalse(UiKit.RowText(row, "Sub").gameObject.activeSelf, "no secondary line when not edited and no description");

                var fixation = parameters.GetComponentsInChildren<UiButton>(true).Single(b => b.name == "Row Fixation");
                var sw = fixation.GetComponentInChildren<UiSwitch>();
                Assert.IsTrue(sw.On);
                fixation.onClick.Invoke();
                Assert.IsFalse(w.Values.GetBool("fixation"), "the whole row toggles the parameter");
                Assert.IsFalse(sw.On);
                Assert.AreEqual("Default: On", UiKit.RowText(fixation.transform, "Sub").text);

                parameters.BackButton.onClick.Invoke();
                Assert.AreEqual(debug, pages.Top);
            }
            finally { Object.DestroyImmediate(canvas); }
        }
    }
}
