using System.Collections.Generic;
using System.IO;
using System.Linq;
using GPL.Core;
using GPL.Params;
using GPL.Session;
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
            Assert.IsFalse(w.CustomExists);
            w.Step("trials", +1);
            Assert.AreEqual("Default · 1 edited", w.Label);
            CollectionAssert.AreEqual(new[] { "trials:6->7" }, changes);
            w.Revert();
            Assert.IsFalse(w.IsEdited, "Default active: Revert goes back to Default");

            // no Custom yet: Save as creates it and makes it active
            w.Step("trials", +1);
            Assert.AreEqual("Custom", w.SaveAs());
            Assert.AreEqual("Custom", w.Source);
            Assert.AreEqual("Custom", _store.ActiveName);
            Assert.IsFalse(w.IsEdited);
            Assert.AreEqual(7, _store.Load("Custom").GetInt("trials"));

            // Custom active: Revert goes back to Custom; Save as overwrites it
            w.Step("trials", +1);
            w.Revert();
            Assert.AreEqual(7, w.Values.GetInt("trials"));
            w.Step("trials", +1);
            w.SaveAs();
            Assert.AreEqual(8, _store.Load("Custom").GetInt("trials"));
            CollectionAssert.AreEqual(new[] { "Default", "Custom" }, _store.List());

            // Default active again: loads Default; Save as overwrites Custom and makes it active
            w.Activate("Default");
            Assert.AreEqual("Default", _store.ActiveName);
            Assert.AreEqual(6, w.Values.GetInt("trials"));
            w.Step("trials", -1);
            w.SaveAs();
            Assert.AreEqual(5, _store.Load("Custom").GetInt("trials"));
            Assert.AreEqual("Custom", _store.ActiveName);
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
        public void PracticeButtonFollowsAvailability_ChamberSectionsSitBetweenParametersAndData()
        {
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 700f);
                var pages = PageStack.On(UiKit.Window(canvas.transform));
                pages.Push("Menu");
                var bench = new DebugBench(pages, canvas.transform, new WorkingCopy(_store)) { PracticeAvailable = false };
                bool practiced = false, shown = false;
                bench.PracticeRequested += () => practiced = true;
                bench.ChamberSections = col => col.Buttons("Dot", ("Show dot", UiStyle.Borderless, () => shown = true));

                var debug = bench.OpenDebugPage();
                Button Find(string label) => debug.GetComponentsInChildren<Button>(true).Single(b => UiKit.LabelOf(b).text == label);
                Assert.IsFalse(Find("Practice trial").interactable, "no practice trial in the chamber: inactive");
                Find("Show dot").onClick.Invoke();
                Assert.IsTrue(shown);
                float y(string label) => ((RectTransform)Find(label).transform).position.y;
                Assert.Less(y("Show dot"), y("Playground"));
                Assert.Greater(y("Show dot"), y("Log files"));
                Assert.IsFalse(practiced);

                pages.Pop();
                bench.PracticeAvailable = true;
                debug = bench.OpenDebugPage();
                Assert.IsTrue(Find("Practice trial").interactable);
                Find("Practice trial").onClick.Invoke();
                Assert.IsTrue(practiced);
            }
            finally { Object.DestroyImmediate(canvas); }
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

                Find(debug, "Playground").onClick.Invoke();
                var parameters = pages.Top;
                Assert.AreEqual("Parameters", parameters.Title);
                var row = parameters.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Row Trials");
                var value = UiKit.RowText(row, "Value");
                Assert.AreEqual("6", value.text);
                var plus = row.Find("Plus").GetComponent<UiButton>();
                Assert.AreEqual(UiStyle.Borderless, plus.Style, "steppers: fill on hover only");
                Assert.IsTrue(plus.InGroup);
                plus.onClick.Invoke();
                Assert.AreEqual("7", value.text, "the row follows the working copy");
                Assert.AreEqual("Default: 6", UiKit.RowText(row, "Sub").text);
                Assert.IsTrue(Find(parameters, "Revert").interactable);
                Assert.IsTrue(Find(parameters, "Save as Custom").interactable);

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

        [Test]
        public void DeletingTheActiveCustomLoadsDefault()
        {
            var w = new WorkingCopy(_store);
            w.Step("trials", +1);
            w.SaveAs();
            w.Step("trials", +1);
            Assert.IsFalse(w.DeletePreset(PresetStore.DefaultName));
            Assert.IsTrue(w.DeletePreset("Custom"));
            Assert.AreEqual("Default", _store.ActiveName, "deleting the active preset makes Default active");
            Assert.AreEqual("Default", w.Source);
            Assert.IsFalse(w.IsEdited);
            Assert.AreEqual(6, w.Values.GetInt("trials"));
        }

        [Test]
        public void PresetsPageSwitchesTheActivePresetAndSaveAsOverwritesCustom()
        {
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 700f);
                var pages = PageStack.On(UiKit.Window(canvas.transform));
                pages.Push("Menu");
                var w = new WorkingCopy(_store);
                var bench = new DebugBench(pages, canvas.transform, w);
                UiButton Named(Component root, string name) => root.GetComponentsInChildren<UiButton>(true).Single(b => b.name == name);
                UiButton Labeled(Component root, string label) => root.GetComponentsInChildren<UiButton>(true).Single(b => UiKit.LabelOf(b).text == label);

                // Parameters: no Custom yet, Save as Custom creates it without asking and makes it active
                var parameters = bench.OpenParametersPage();
                var save = Labeled(parameters, DebugBench.SaveAsLabel);
                Assert.IsFalse(save.interactable, "nothing edited");
                parameters.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Row Trials").Find("Plus").GetComponent<UiButton>().onClick.Invoke();
                Assert.IsTrue(save.interactable);
                save.onClick.Invoke();
                Assert.IsNull(ModalDialog.Current, "creating Custom needs no confirmation");
                Assert.AreEqual("Custom", _store.ActiveName);
                Assert.AreEqual("Custom", w.Source);

                // Custom exists: Save as Custom asks before overwriting
                w.Step("trials", +1);
                save.onClick.Invoke();
                Assert.IsNotNull(ModalDialog.Current);
                Labeled(ModalDialog.Current, "Overwrite").onClick.Invoke();
                Assert.AreEqual(8, _store.Load("Custom").GetInt("trials"));
                parameters.BackButton.onClick.Invoke();

                // Presets: ✓ on Custom; Default's sheet has only Make active
                var presets = bench.OpenPresetsPage();
                UiButton RowOf(string name) => Named(pages.Top, "Row " + name);
                Assert.AreEqual(DebugBench.ActiveMark, UiKit.RowText(RowOf("Custom").transform, "Value").text);
                Assert.IsNull(UiKit.RowText(RowOf("Default").transform, "Value"), "not active: no mark");
                StringAssert.Contains("playground", UiKit.RowText(RowOf("Custom").transform, "Sub").text);
                RowOf("Default").onClick.Invoke();
                var sheet = Sheet.Top(canvas.transform);
                var actions = sheet.GetComponentsInChildren<UiButton>(true).Where(b => b.name.StartsWith("Row ")).Select(b => b.name).ToArray();
                CollectionAssert.AreEqual(new[] { "Row Make active" }, actions);

                // Make Default active with unsaved edits: confirm, then the working copy loads Default
                Assert.IsTrue(w.Step("trials", -1));
                sheet.Close();
                RowOf("Default").onClick.Invoke();
                Named(Sheet.Top(canvas.transform), "Row Make active").onClick.Invoke();
                Assert.IsNotNull(ModalDialog.Current, "unsaved edits: confirm first");
                Labeled(ModalDialog.Current, "Discard and switch").onClick.Invoke();
                Assert.AreEqual("Default", _store.ActiveName);
                Assert.AreEqual("Default", w.Source);
                Assert.AreEqual(6, w.Values.GetInt("trials"));
                Assert.AreEqual(DebugBench.ActiveMark, UiKit.RowText(RowOf("Default").transform, "Value").text, "the page is rebuilt");
                Assert.AreEqual(0, Sheet.Stack(canvas.transform).Count, "the sheets are closed");

                // Make Custom active without edits: no confirmation
                RowOf("Custom").onClick.Invoke();
                Named(Sheet.Top(canvas.transform), "Row Make active").onClick.Invoke();
                Assert.AreEqual("Custom", w.Source);
                Assert.AreEqual(8, w.Values.GetInt("trials"));

                // Delete Custom (active): confirm; Default becomes active
                RowOf("Custom").onClick.Invoke();
                var del = Named(Sheet.Top(canvas.transform), "Row Delete");
                Assert.AreEqual(UiStyle.DestructiveText, del.Style);
                del.onClick.Invoke();
                Labeled(ModalDialog.Current, "Delete").onClick.Invoke();
                Assert.IsFalse(_store.Exists("Custom"));
                Assert.AreEqual("Default", _store.ActiveName);
                Assert.AreEqual(presets, pages.Top);
                Assert.IsFalse(pages.Top.GetComponentsInChildren<UiButton>(true).Any(b => b.name == "Row Custom"));
                Assert.AreEqual(0, Sheet.Stack(canvas.transform).Count);
            }
            finally { Object.DestroyImmediate(canvas); }
        }

        [Test]
        public void DataPageDeletesOneOrAllAfterConfirming()
        {
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            string data = Path.Combine(_dir, "data");
            try
            {
                Directory.CreateDirectory(data);
                var t0 = new System.DateTime(2026, 11, 5, 10, 0, 0);
                var sessions = new SessionStore(data);
                foreach (var (min, id, status) in new[] { (0, "S0001", SessionStatus.Completed), (10, "S0002", SessionStatus.InProgress), (20, "S0003", SessionStatus.Completed) })
                {
                    var r = new SessionRecord { SessionId = DataFileNames.BaseName(t0.AddMinutes(min), "C00", id, false), ChamberId = "C00", StudyId = id, Status = status };
                    sessions.Save(r, t0);
                    File.WriteAllText(sessions.CsvPathFor(r.SessionId), "event,note\ntrial,\ntrial,\n");
                }
                File.WriteAllText(Path.Combine(data, DataFileNames.BaseName(t0.AddMinutes(30), "C00", "", true) + ".csv"), "event\ntrial,\n");
                Directory.CreateDirectory(Path.Combine(data, "presets"));
                File.WriteAllText(Path.Combine(data, "presets", "Custom.params"), "x");

                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 700f);
                var pages = PageStack.On(UiKit.Window(canvas.transform));
                pages.Push("Menu");
                var bench = new DebugBench(pages, canvas.transform, new WorkingCopy(_store)) { Files = new DataFiles(data, "C00") };
                UiButton Labeled(Component root, string label) => root.GetComponentsInChildren<UiButton>(true).Single(b => UiKit.LabelOf(b).text == label);
                RectTransform RowOf(string baseName) => pages.Top.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Row " + baseName);
                TextMeshProUGUI DialogText(string name) => ModalDialog.Current.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == name);

                var debug = bench.OpenDebugPage();
                var dataRow = debug.GetComponentsInChildren<UiButton>(true).Single(b => b.name == "Row Log files");
                Assert.AreEqual("4 files  ›", UiKit.RowText(dataRow.transform, "Value").text);
                dataRow.onClick.Invoke();
                var page = pages.Top;
                Assert.AreEqual("Log files", page.Title);
                string s2 = DataFileNames.BaseName(t0.AddMinutes(10), "C00", "S0002", false);
                Assert.AreEqual(s2, UiKit.RowText(RowOf(s2), "Label").text, "the file name is the row title");
                Assert.IsNull(UiKit.RowText(RowOf(s2), "Sub"), "one line: no description");
                var label = UiKit.RowText(RowOf(s2), "Label");
                Assert.AreEqual(UiKit.OpticalLift * label.fontSize, label.rectTransform.offsetMin.y, 0.01f,
                                "the file name is vertically centered (one line: optical lift only)");
                string dbg = DataFileNames.BaseName(t0.AddMinutes(30), "C00", "", true);
                Assert.AreEqual(dbg, UiKit.RowText(RowOf(dbg), "Label").text);

                // Delete one: confirm first (Cancel keeps it)
                var del = RowOf(s2).Find("Delete").GetComponent<UiButton>();
                Assert.AreEqual(UiStyle.DestructiveText, del.Style);
                del.onClick.Invoke();
                Assert.AreEqual("Delete S0002's session?", DialogText("Title").text);
                StringAssert.StartsWith("The CSV and session file are removed", DialogText("Message").text);
                StringAssert.Contains("can no longer resume", DialogText("Message").text);
                Labeled(ModalDialog.Current, "Cancel").onClick.Invoke();
                Assert.IsTrue(File.Exists(sessions.CsvPathFor(s2)));
                RowOf(s2).Find("Delete").GetComponent<UiButton>().onClick.Invoke();
                Labeled(ModalDialog.Current, "Delete").onClick.Invoke();
                Assert.IsFalse(File.Exists(sessions.CsvPathFor(s2)));
                Assert.IsFalse(File.Exists(sessions.PathFor(s2)));
                Assert.IsFalse(page.GetComponentsInChildren<RectTransform>(true).Any(t => t.name == "Row " + s2), "the page is rebuilt");

                // Delete all: one confirmation naming the count
                var all = Labeled(page, "Delete all");
                all.onClick.Invoke();
                Assert.AreEqual("Delete all 3 files?", DialogText("Title").text);
                Assert.AreEqual(3, bench.Files.List().Count, "nothing deleted before the confirmation");
                Labeled(ModalDialog.Current, "Delete 3 files").onClick.Invoke();
                Assert.IsNull(ModalDialog.Current);
                Assert.AreEqual(0, bench.Files.List().Count);
                Assert.IsTrue(File.Exists(Path.Combine(data, "presets", "Custom.params")), "presets are not data files");
                Assert.IsFalse(all.interactable, "nothing left to delete");

                page.BackButton.onClick.Invoke();
                Assert.AreEqual("none  ›", UiKit.RowText(dataRow.transform, "Value").text);
            }
            finally { Object.DestroyImmediate(canvas); }
        }
    }
}
