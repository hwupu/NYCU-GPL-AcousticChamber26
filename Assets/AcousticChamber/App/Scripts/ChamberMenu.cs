using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GPL.Core;
using GPL.DebugBench;
using GPL.Input;
using GPL.Logging;
using GPL.Params;
using GPL.Session;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// The chamber menu every chamber starts at (Architecture section 6, DesignSystem section 7): ‹ back to the Hall, the Study
    /// ID number pad, Start, Debug (the Debug bench), and under them "Audio level: 10 (target 8)", "Preset: X" (not for Default) and
    /// "Unfinished: S0005, ..." (managed on Debug > Data files). Start with an ID that has an unfinished session asks Resume / Start new / Back; then the headset volume
    /// step; then the chamber's <see cref="ChamberBehaviour.Run"/>. The menu owns the session files, the start and end rows
    /// and the Stop dialog (Y): Drop out ends a real session, Back to the chamber menu ends a Debug run.
    /// </summary>
    public class ChamberMenu : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);
        public const string VolumeColumn = GPL.Protocol.OnboardingStages.VolumeColumn;
        public const int MaxVolumeStep = 15;
        /// <summary>
        /// False (PB-20b): Start reads the headset volume (<see cref="HeadsetVolume"/>) and the menu shows it against the
        /// chamber's target. True: Start asks for the step on a number pad first (the PB-07a sheet, kept for setups where the
        /// volume cannot be read).
        /// </summary>
        public static bool AskVolumeStep = false;
        const float VolumePollSec = 0.25f;

        public ChamberDefinition chamber;
        public ChamberBehaviour logic;

        RectTransform _canvas;
        UiPanel _panel;
        TextMeshProUGUI _volume, _preset, _unfinished, _status;
        float _volumePoll;
        int _target = -1, _shownVolume = int.MinValue;
        NumberPadEntry _pad;
        UiSurface _pages;
        PresetStore _presets;
        SessionStore _sessions;
        DebugBench _bench;
        GlobalActions _actions;
        StopDialogHost _stopHost;
        ChamberRun _run;
        Coroutine _runCo;

        string DataDir => Path.Combine(Application.persistentDataPath, chamber.id);

        void Start() => Build();

        OverlayPanel _overlay;
        bool _movable;

        /// <summary>The participant can move the menu (handle below it) except while a run is on (PO 2026-11-08).</summary>
        void LateUpdate()
        {
            if (_overlay == null) _overlay = GetComponent<OverlayPanel>();
            bool movable = _run == null;
            if (_overlay == null || movable == _movable) return;
            _movable = movable;
            _overlay.Movable = movable;
        }

        public void Build()
        {
            UiStrings.EnsureRegistered();
            Loc.Set(UiLanguage.English); // menus are English; only a run switches to the participant's language
            if (chamber == null || logic == null)
            {
                Debug.LogError("[GPL] ChamberMenu: chamber definition or chamber logic missing; run the chamber's setup step.");
                return;
            }
            _canvas = (RectTransform)transform;
            UiKit.Clear(_canvas);
            _canvas.sizeDelta = Size;

            _sessions = new SessionStore(DataDir);
            _presets = new PresetStore(Path.Combine(DataDir, "presets"), logic.Schema, logic.DefaultParamsText);
            var work = new WorkingCopy(_presets);
            work.ParamChanged += (k, o, n) => Debug.Log("[Chamber:" + chamber.id + "] param_change " + k + " " + o + " -> " + n);

            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (_actions != null)
            {
                var table = new BindingTable();
                table.DeclareStandard(InputActions.Confirm);
                table.DeclareStandard(InputActions.Back);
                logic.DeclareActions(table);
                _actions.Use(table);
                _actions.StopEnabled = false;
            }
            else Debug.LogError("[GPL] ChamberMenu: no GlobalActions in the scene; Y and A will not work.");
            _stopHost = GetComponent<StopDialogHost>();
            if (_stopHost == null) _stopHost = gameObject.AddComponent<StopDialogHost>();
            _stopHost.canvas = _canvas;
            _stopHost.Chosen -= OnStopChoice;
            _stopHost.Chosen += OnStopChoice;

            _panel = UiPanel.Create(_canvas, "Menu", Size);
            _pages = _panel.Surface;
            _pages.RootBack = BackToHall;
            UiNavigationBar.Create(_pages, chamber.MenuTitle, UiNavigationBar.Leading.Back);
            _bench = new DebugBench(_pages, _canvas, work)
            {
                Files = new DataFiles(DataDir, chamber.id),
                PracticeAvailable = logic.HasPracticeTrial,
            };
            _bench.ChamberSections = col => logic.DebugSections(col, _bench);
            _bench.RunRequested += () => StartDebugRun(false);
            _bench.PracticeRequested += () => StartDebugRun(true);

            var menu = _pages.Root;
            BuildMenu(menu);
            menu.Returned += Refresh;
            menu.Returned += logic.DebugLeft; // only the Debug pages lie above the menu page
            Refresh();
            if (AppNav.TakeRoutedAtLaunch())
                SetStatus("Opened at launch: this chamber has unfinished sessions. Enter the Study ID, then Start.");
        }

        // ---- menu page -----------------------------------------------------------------------------------------

        /// <summary>
        /// The menu page: the number pad (520 u) at the left, the button column at the right (Start, Debug, then the status
        /// lines: volume, preset, unfinished; an empty line takes no space), and the status line at the bottom.
        /// </summary>
        void BuildMenu(UiPage page)
        {
            var th = UiKit.Theme;
            _status = page.AddStatus("");
            var row = UiLay.Node(page.Body, "Content");
            UiLay.H(row.gameObject, th.gap, null, TextAnchor.UpperLeft);
            UiLay.Size(row.gameObject, flexW: 1f, flexH: 1f);

            _pad = NumberPadEntry.CreateLayout(row, "Number Pad", StudyId.MaxDigits,
                                               d => StudyId.IsValidDigits(d) ? StudyId.FromDigits(d) : d, "Study ID");

            // the number pad takes 2/5 of the width, the button column 3/5 (the 20 u gap is not shared)
            UiLay.Size(_pad.gameObject, minW: 0f, prefW: 0f, flexW: 2f);
            var col = UiLay.Node(row, "Column");
            UiLay.V(col.gameObject, th.gap, null, TextAnchor.UpperLeft);
            UiLay.Size(col.gameObject, minW: 0f, prefW: 0f, flexW: 3f);
            var start = UiLay.Button(col, "Start", "Start", UiStyle.Prominent, minWidth: false, onClick: OnStart);
            var debug = UiLay.Button(col, "Debug", "Debug", UiStyle.Gray, minWidth: false, onClick: () => _bench.OpenDebugPage());
            foreach (var b in new[] { start, debug })
                UiLay.Size(b.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f);

            var lines = UiLay.Node(col, "Lines");
            UiLay.V(lines.gameObject, 7f, UiLay.Pad(0, 20, 0, 0), TextAnchor.UpperLeft);
            UiLay.Size(lines.gameObject, prefW: 0f, flexW: 1f);
            _volume = Line(lines, "Volume");
            _preset = Line(lines, "Preset");
            _unfinished = Line(lines, "Unfinished");
        }

        static TextMeshProUGUI Line(RectTransform parent, string name)
        {
            var t = UiKit.Text(parent, name, "", UiKit.Theme.smallSize, Vector2.zero, Vector2.zero, color: UiKit.Theme.text2,
                               align: TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.Normal;
            UiLay.Size(t.gameObject, prefW: 0f, flexW: 1f);
            t.gameObject.SetActive(false);
            return t;
        }

        /// <summary>Sets a status line; an empty line is hidden and takes no space.</summary>
        static void SetLine(TextMeshProUGUI line, string text)
        {
            line.text = text;
            line.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// The lines under the buttons: "Audio level: 10 (target 8)" (PB-20b; red when they
        /// differ; the target is the active preset's <c>volume.target_step</c>, if the chamber has one), "Preset:
        /// X" (nothing for the preset Default) and "Unfinished: S0005, S0012" (nothing when none). Unfinished sessions
        /// are managed on Debug > Data files.
        /// </summary>
        void Refresh()
        {
            if (_preset == null) return;
            string active = _presets.ActiveName;
            bool custom = !string.Equals(active, PresetStore.DefaultName, StringComparison.OrdinalIgnoreCase);
            SetLine(_preset, custom ? "Preset: " + active : "");
            var ids = new List<string>();
            foreach (var rec in _sessions.ListUnfinished())
                if (!ids.Contains(rec.StudyId)) ids.Add(rec.StudyId);
            ids.Sort(StringComparer.Ordinal);
            SetLine(_unfinished, ids.Count > 0 ? "Unfinished: " + string.Join(", ", ids) : "");
            _target = logic.Schema.Contains(HeadsetVolume.TargetKey) ? _presets.LoadActive(new List<string>()).GetInt(HeadsetVolume.TargetKey) : -1;
            _shownVolume = int.MinValue;
            ShowVolume();
        }

        /// <summary>Reads the volume and updates the line when it changed.</summary>
        void ShowVolume()
        {
            if (_volume == null) return;
            int v = HeadsetVolume.ReadOrUnknown();
            if (v == _shownVolume) return;
            _shownVolume = v;
            SetLine(_volume, VolumeStatus.Line(v, _target));
            _volume.color = VolumeStatus.Off(v, _target) ? UiKit.Theme.redText : UiKit.Theme.text2;
        }

        void Update()
        {
            if (_volume == null || _run != null) return;
            _volumePoll -= Time.unscaledDeltaTime;
            if (_volumePoll > 0f) return;
            _volumePoll = VolumePollSec;
            ShowVolume();
        }

        void SetStatus(string s)
        {
            if (_status != null) _status.text = s;
            Debug.Log("[Chamber:" + chamber.id + "] " + s);
        }

        void BackToHall()
        {
            if (_run != null || AppNav.Busy) return;
            ShowWindow(false);
            if (!AppNav.BackToHall()) ShowWindow(true);
        }

        void ShowWindow(bool on)
        {
            _panel.Shown = on;
        }

        // ---- Start / Resume / Start new ------------------------------------------------------------------------

        void OnStart()
        {
            if (_run != null) return;
            if (!StudyId.IsValidDigits(_pad.Value))
            {
                SetStatus("Enter a Study ID (1 to 4 digits).");
                return;
            }
            string id = StudyId.FromDigits(_pad.Value);
            var old = _sessions.FindUnfinished(id);
            if (old == null)
            {
                WithVolume(v => BeginNew(id, v, null));
                return;
            }
            ModalDialog.Show(_canvas, id + " has an unfinished session",
                             logic.Describe(old) + ", started " + old.Created.ToString("yyyy-MM-dd HH:mm") + ".\nResume continues it. Start new closes it (its data are kept) and starts again.",
                             new DialogButton("Back", UiStyle.Borderless),
                             new DialogButton("Start new", UiStyle.Gray, () => WithVolume(v => BeginNew(id, v, old))),
                             new DialogButton("Resume", UiStyle.Prominent, () => WithVolume(v => BeginResume(old, v))));
        }

        /// <summary>The volume step for the start row: read from the headset (-1 where it cannot be read), or asked (<see cref="AskVolumeStep"/>).</summary>
        void WithVolume(Action<int> then)
        {
            if (AskVolumeStep) AskVolume(then);
            else then(HeadsetVolume.ReadOrUnknown());
        }

        /// <summary>A sheet with a number pad for the headset volume step (0..15); OK calls <paramref name="then"/>.</summary>
        void AskVolume(Action<int> then)
        {
            var th = UiKit.Theme;
            var sheet = UiSheet.Create(_canvas, "Volume Step", "Headset volume step", 860f, () => SetStatus("Start cancelled."));
            var row = UiLay.Node(sheet.Body, "Content");
            UiLay.H(row.gameObject, th.gap, null, TextAnchor.UpperLeft);
            UiLay.Size(row.gameObject, flexW: 1f);
            var pad = NumberPadEntry.CreateLayout(row, "Number Pad", 2, null, "0–" + MaxVolumeStep);

            var col = UiLay.Node(row, "Column");
            UiLay.V(col.gameObject, th.gap, null, TextAnchor.UpperLeft);
            UiLay.Size(col.gameObject, prefW: 0f, flexW: 1f);
            var info = UiKit.Text(col, "Info", "Enter the headset volume step (0–" + MaxVolumeStep + ") before handing the headset over. It is logged with the session.",
                                  th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
            UiLay.Size(info.gameObject, prefW: 0f, flexW: 1f);
            UiButton ok = null;
            ok = UiLay.Button(col, "OK", "OK", UiStyle.Prominent, minWidth: false, onClick: () =>
            {
                if (!TryVolume(pad.Value, out int v)) return;
                sheet.Close();
                then(v);
            });
            UiLay.Size(ok.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f);
            ok.interactable = false;
            pad.Changed += s => ok.interactable = TryVolume(s, out _);
            sheet.Show();
        }

        static bool TryVolume(string digits, out int v) =>
            int.TryParse(digits, System.Globalization.NumberStyles.None, Fmt.Inv, out v) && v >= 0 && v <= MaxVolumeStep;

        void BeginNew(string studyId, int volume, SessionRecord superseded)
        {
            try
            {
                var warnings = new List<string>();
                var values = _presets.LoadActive(warnings);
                foreach (var w in warnings) Debug.LogWarning("[Chamber:" + chamber.id + "] preset " + _presets.ActiveName + ": " + w);
                int seed = Environment.TickCount & 0x7fffffff;
                var rec = SessionLauncher.NewSession(_sessions, chamber.id, chamber.version, Application.version, studyId, _presets.ActiveName,
                                                     values, seed, DateTime.Now);
                if (superseded != null) SessionLauncher.Supersede(_sessions, superseded, rec.SessionId, logic.LogSchema, SystemClock.Instance);
                var log = OpenLog(_sessions.CsvPathFor(rec.SessionId), SessionLauncher.ContextOf(rec));
                log.Write(LogEvents.SessionStart, VolumeValues(log, volume), StartNote(volume, log));
                Launch(new ChamberRun { Record = rec, Store = _sessions, Values = values, Log = log, VolumeStep = volume });
            }
            catch (Exception e) { Fail("Could not start the session", e); }
        }

        void BeginResume(SessionRecord rec, int volume)
        {
            try
            {
                var warnings = new List<string>();
                var values = SessionLauncher.Snapshot(rec, logic.Schema, warnings);
                foreach (var w in warnings) Debug.LogWarning("[Chamber:" + chamber.id + "] snapshot of " + rec.SessionId + ": " + w);
                string problem = logic.ResumeProblem(rec, values);
                if (problem != null)
                {
                    SetStatus("Cannot resume " + rec.StudyId + ": " + problem);
                    return;
                }
                var log = OpenLog(_sessions.CsvPathFor(rec.SessionId), SessionLauncher.ContextOf(rec));
                log.Write(LogEvents.SessionResume, VolumeValues(log, volume), "resume_from=" + ResumeNames.Name(rec.ResumePolicy) + " cursor=" +
                          rec.Cursor.Stage + "/" + rec.Cursor.Block + "/" + rec.Cursor.TrialsDone + "/" + rec.Cursor.Attempt + StartNote(volume, log, true));
                Launch(new ChamberRun { Record = rec, Store = _sessions, Values = values, Log = log, VolumeStep = volume, IsResume = true });
            }
            catch (Exception e) { Fail("Could not resume " + rec.SessionId, e); }
        }

        /// <summary>A Debug run with the playground: the chamber's protocol, or one practice trial (<paramref name="practice"/>).</summary>
        void StartDebugRun(bool practice)
        {
            if (_run != null) return;
            if (practice && !logic.HasPracticeTrial) return;
            try
            {
                var values = _bench.Work.Values.Clone();
                string id = SessionLauncher.DebugRunId(chamber.id, DateTime.Now);
                var rec = new SessionRecord { SessionId = id, ChamberId = chamber.id, ChamberVersion = chamber.version, PresetName = _bench.Work.Label,
                                              ParamsHash = values.Hash(), Created = DateTime.Now };
                var ctx = new LogContext { SessionId = id, Chamber = chamber.id, ChamberVersion = chamber.version, Mode = LogModes.Debug,
                                           Preset = _bench.Work.Label, ParamsHash = rec.ParamsHash };
                var log = OpenLog(Path.Combine(DataDir, id + DataFileNames.CsvExtension), ctx);
                log.Write(LogEvents.DebugBegin, (practice ? "practice trial, " : "") + "app " + Application.version + ", " + SystemInfo.deviceModel);
                Launch(new ChamberRun { Record = rec, Values = values, Log = log, IsDebug = true, IsPractice = practice });
            }
            catch (Exception e) { Fail("Could not start the Debug run", e); }
        }

        EventLog OpenLog(string path, LogContext ctx) =>
            EventLog.Open(path, SystemClock.Instance, logic.LogSchema, ctx, line => Debug.Log("[Chamber:" + chamber.id + "] " + line));

        static LogValues VolumeValues(EventLog log, int volume) =>
            log.Schema.Has(VolumeColumn) ? new LogValues().Set(VolumeColumn, volume >= 0 ? Fmt.I(volume) : "") : null;

        static string StartNote(int volume, EventLog log, bool continued = false)
        {
            string s = (continued ? " " : "") + "app " + Application.version + ", " + SystemInfo.deviceModel;
            if (volume < 0) s += ", volume not readable";
            return log.Schema.Has(VolumeColumn) || volume < 0 ? s : s + ", volume step " + volume;
        }

        void Fail(string what, Exception e)
        {
            SetStatus(what + ": " + e.Message);
            Debug.LogException(e);
        }

        // ---- the run -------------------------------------------------------------------------------------------

        void Launch(ChamberRun run)
        {
            run.Canvas = _canvas;
            run.Actions = _actions;
            _run = run;
            ShowWindow(false);
            _stopHost.DebugRun = run.IsDebug;
            _stopHost.StudyId = run.Record.StudyId;
            if (_actions != null) _actions.StopEnabled = true;
            SetStatus((run.IsPractice ? "Practice trial " : run.IsDebug ? "Debug run " : run.IsResume ? "Resumed " : "Started ") + run.Record.SessionId);
            _runCo = StartCoroutine(RunToEnd(run));
        }

        IEnumerator RunToEnd(ChamberRun run)
        {
            yield return run.IsPractice ? logic.PracticeTrial(run) : logic.Run(run);
            End(run, null);
        }

        void OnStopChoice(StopChoice choice)
        {
            var run = _run;
            if (run == null || choice == StopChoice.KeepGoing) return;
            run.Stopped = true;
            if (_runCo != null) StopCoroutine(_runCo);
            try { logic.Abort(run); }
            catch (Exception e) { Debug.LogException(e); }
            End(run, choice);
        }

        /// <summary>Writes the end rows and the final session status, closes the log and shows the menu again.</summary>
        void End(ChamberRun run, StopChoice? stopped)
        {
            if (_run != run) return;
            _run = null;
            _runCo = null;
            if (_actions != null) _actions.StopEnabled = false;
            try
            {
                if (run.IsDebug)
                    run.Log.Write(LogEvents.DebugEnd, stopped.HasValue ? "back to the chamber menu" : "completed");
                else if (stopped == StopChoice.DropOut)
                {
                    run.Log.Write(LogEvents.ParticipantDropout, "Stop dialog");
                    run.Record.Status = SessionStatus.DroppedOut;
                    run.Save();
                    run.Log.Write(LogEvents.SessionEnd, "dropped out");
                }
                else
                {
                    run.Record.Status = SessionStatus.Completed;
                    run.Save();
                    run.Log.Write(LogEvents.SessionEnd, "completed");
                }
            }
            catch (Exception e) { Debug.LogException(e); }
            finally { run.Log.Dispose(); }
            SetStatus(run.Record.SessionId + (run.IsDebug ? " Debug run ended." : stopped.HasValue ? ": dropped out." : ": completed."));
            // the participant's language belongs to the run (onboarding, slides, thanks); menus are English (PB-20b)
            Loc.Set(UiLanguage.English);
            if (!run.IsDebug) _pad.Clear();
            Refresh();
            _bench.RefreshDebugPage();
            ShowWindow(true);
        }

        void OnApplicationPause(bool paused)
        {
            if (_run == null) return;
            try { _run.Log.Write(paused ? LogEvents.AppPaused : LogEvents.AppResumed, ""); }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnDestroy()
        {
            if (_run == null) return;
            try { _run.Log.Dispose(); }
            catch (Exception) { }
            _run = null;
        }
    }
}
