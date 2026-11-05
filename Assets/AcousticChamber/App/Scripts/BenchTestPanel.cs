using System;
using System.IO;
using System.Text;
using GPL.Core;
using GPL.DebugBench;
using GPL.Params;
using GPL.Session;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Headset test panel for PB-19 (no study logic): a chamber-menu-like root page with a demo parameter schema, and the
    /// Debug bench behind its Debug button. Presets are saved in <c>persistentDataPath/_BenchTest/presets</c>. Replaced by
    /// the template chamber (PB-07).
    /// </summary>
    public class BenchTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);
        const string ChamberId = "_BenchTest";

        DebugBench _bench;
        TextMeshProUGUI _preset;
        bool _stim, _bg, _sphere;

        /// <summary>A small schema with every control type.</summary>
        public static ParameterSchema DemoSchema() => new ParameterSchema(
            ParamDef.Float("rt60_s", 0.7f, 0.3f, 5f, 0.1f, "RT60", "s", "Acoustics").Describe("target reverberation time"),
            ParamDef.Float("source_distance_m", 2f, 0.5f, 5f, 0.25f, "Source distance", "m", "Acoustics"),
            ParamDef.Choice("hl.left", "none", new[] { "none", "mild", "moderate", "severe" }, "Hearing loss left", "Acoustics"),
            ParamDef.Int("trials_per_block", 6, 1, 50, "Trials per block", "", "Procedure"),
            ParamDef.Float("iti_s", 1.5f, 0.5f, 5f, 0.1f, "Inter-trial interval", "s", "Procedure"),
            ParamDef.Bool("show_fixation", true, "Fixation dot", "Procedure").Describe("shown between trials"));

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            var window = UiKit.Window(canvas);
            var pages = PageStack.On(window);
            pages.RootBack = () => Debug.Log("[GPL] BenchTestPanel: Back (would return to the Hall)");

            var schema = DemoSchema();
            string dataDir = Path.Combine(Application.persistentDataPath, ChamberId);
            string dir = Path.Combine(dataDir, "presets");
            var store = new PresetStore(dir, schema, schema.Defaults().Serialize("demo default.params"));
            var work = new WorkingCopy(store);
            work.ParamChanged += (k, o, n) => Debug.Log("[GPL] BenchTestPanel: param_change " + k + " " + o + " -> " + n);

            _bench = new DebugBench(pages, canvas, work)
            {
                ChamberSections = col =>
                {
                    col.Label("Test playback (not saved)");
                    var p = col.Group("Playback", 2);
                    UiKit.SwitchRow(p, 0, "Play stimulus", "2 s on, 1 s off, at the current position", _stim, on => { _stim = on; _bench.SetStatus(on ? "Stimulus playing (demo)." : "Stimulus stopped."); });
                    UiKit.SwitchRow(p, 1, "Play background", "loops while on", _bg, on => { _bg = on; _bench.SetStatus(on ? "Background playing (demo)." : "Background stopped."); });
                    col.Label("Display (Debug only, not saved)");
                    var d = col.Group("Display", 1);
                    UiKit.SwitchRow(d, 0, "Show audio sphere", "always, not only after the answer", _sphere, on => { _sphere = on; _bench.SetStatus(on ? "Audio sphere always visible." : "Audio sphere only after the answer."); });
                    col.Label("Test data (bench test only)");
                    col.Buttons("Test data", ("Create 3 demo data files", UiStyle.Borderless, () =>
                    {
                        CreateDemoFiles(dataDir);
                        _bench.RefreshDebugPage();
                        _bench.SetStatus("Created 3 demo data files in " + dataDir);
                    }));
                },
                Files = new DataFiles(dataDir, ChamberId),
            };
            _bench.RunRequested += () => Debug.Log("[GPL] BenchTestPanel: run with " + work.Label + " hash " + work.Values.Hash());

            // root page: a stand-in for the chamber menu
            var menu = pages.Push("C00 · Bench test", debugTag: true);
            var r = menu.Rect;
            float x = -Size.x / 2f + Page.EdgePad + 320f;
            _preset = UiKit.Text(r, "Preset", "", th.smallSize, new Vector2(x, 160f), new Vector2(640f, 40f), color: th.text2,
                                 align: TextAlignmentOptions.Left);
            UiKit.Button(r, "Start", "Start", UiStyle.Prominent, new Vector2(x - 110f, 70f), new Vector2(420f, 0f),
                         () => Debug.Log("[GPL] BenchTestPanel: Start would use the active preset " + store.ActiveName));
            UiKit.Button(r, "Debug", "Debug", UiStyle.Gray, new Vector2(x - 110f, -30f), new Vector2(420f, 0f), () => _bench.OpenDebugPage());
            menu.Returned += () => ShowPreset(store);
            ShowPreset(store);
        }

        /// <summary>
        /// Writes a completed session (S0001), an unfinished one (S0002) and a Debug run (CSV only) to try the Data files
        /// page. Fake content, not a real log format.
        /// </summary>
        public static void CreateDemoFiles(string dataDir)
        {
            Directory.CreateDirectory(dataDir);
            var now = DateTime.Now;
            var store = new SessionStore(dataDir);
            void Session(DateTime start, string studyId, SessionStatus status, int trials)
            {
                var r = new SessionRecord
                {
                    SessionId = DataFileNames.BaseName(start, ChamberId, studyId, false),
                    ChamberId = ChamberId, ChamberVersion = "0", StudyId = studyId, PresetName = "Default", Created = start,
                };
                r.EnterStage(0, ResumeFrom.NextTrial);
                r.Status = status;
                store.Save(r, now);
                File.WriteAllText(store.CsvPathFor(r.SessionId), DemoCsv(trials));
            }
            Session(now.AddMinutes(-30), "S0001", SessionStatus.Completed, 6);
            Session(now.AddMinutes(-20), "S0002", SessionStatus.InProgress, 3);
            File.WriteAllText(Path.Combine(dataDir, DataFileNames.BaseName(now.AddMinutes(-10), ChamberId, "", true) + DataFileNames.CsvExtension),
                              DemoCsv(2));
        }

        static string DemoCsv(int trials)
        {
            var sb = new StringBuilder("event,note\nsession_start,demo\n");
            for (int i = 0; i < trials; i++) sb.Append("trial,demo\n");
            return sb.ToString();
        }

        void ShowPreset(PresetStore store) => _preset.text = "Study ID S0001 · Preset: " + store.ActiveName;
    }
}
