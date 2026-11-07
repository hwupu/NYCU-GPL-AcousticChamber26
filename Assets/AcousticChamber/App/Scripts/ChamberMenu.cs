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
        const float LineStep = 48f, VolumePollSec = 0.25f;
        /// <summary>Height of the status line band at the bottom of the menu (status text centered 34 u above the edge).</summary>
        const float StatusBand = 54f;

        public ChamberDefinition chamber;
        public ChamberBehaviour logic;

        RectTransform _canvas, _window;
        TextMeshProUGUI _volume, _preset, _unfinished, _status;
        float _linesY, _volumePoll;
        int _target = -1, _shownVolume = int.MinValue;
        NumberPadEntry _pad;
        PageStack _pages;
        PresetStore _presets;
        SessionStore _sessions;
        DebugBench _bench;
        GlobalActions _actions;
        StopDialogHost _stopHost;
        ChamberRun _run;
        Coroutine _runCo;

        string DataDir => Path.Combine(Application.persistentDataPath, chamber.id);

        void Start() => Build();

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

            _window = UiKit.Window(_canvas);
            _pages = PageStack.On(_window);
            _pages.RootBack = BackToHall;
            _bench = new DebugBench(_pages, _canvas, work)
            {
                Files = new DataFiles(DataDir, chamber.id),
                PracticeAvailable = logic.HasPracticeTrial,
            };
            _bench.ChamberSections = col => logic.DebugSections(col, _bench);
            _bench.RunRequested += () => StartDebugRun(false);
            _bench.PracticeRequested += () => StartDebugRun(true);

            var menu = _pages.Push(chamber.MenuTitle);
            BuildMenu(menu.Rect);
            menu.Returned += Refresh;
            Refresh();
            if (AppNav.TakeRoutedAtLaunch())
                SetStatus("Opened at launch: this chamber has unfinished sessions. Enter the Study ID, then Start.");
        }

        // ---- menu page -----------------------------------------------------------------------------------------

        void BuildMenu(RectTransform r)
        {
            var th = UiKit.Theme;
            // the number pad and the button column share one top edge; that group (the pad's height) is centered
            // vertically between the header and the status band at the bottom (PO 2026-11-07)
            float header = Size.y / 2f - Page.HeaderHeight, statusTop = -Size.y / 2f + StatusBand;
            float top = Mathf.Min(header - th.gap, (header + statusTop) / 2f + NumberPadEntry.Height / 2f);
            float left = -Size.x / 2f + Page.EdgePad;
            float padX = left + NumberPadEntry.Width / 2f;
            float padY = top - NumberPadEntry.Height / 2f;
            _pad = NumberPadEntry.Create(r, new Vector2(padX, padY), StudyId.MaxDigits,
                                         d => StudyId.IsValidDigits(d) ? StudyId.FromDigits(d) : d, "Study ID");

            float colLeft = left + NumberPadEntry.Width + 2f * th.gap;
            float colRight = Size.x / 2f - Page.EdgePad;
            float colW = colRight - colLeft, colX = (colLeft + colRight) / 2f;
            float y = top;
            UiKit.Button(r, "Start", "Start", UiStyle.Prominent, new Vector2(colX, y - th.buttonHeight / 2f), new Vector2(colW, 0f), OnStart);
            y -= th.buttonHeight + th.gap;
            UiKit.Button(r, "Debug", "Debug", UiStyle.Gray, new Vector2(colX, y - th.buttonHeight / 2f), new Vector2(colW, 0f),
                         () => _bench.OpenDebugPage());
            y -= th.buttonHeight + 2f * th.gap;
            // status lines under the buttons, stacked; an empty line takes no space
            _linesY = y - 20f;
            _volume = Line(r, "Volume", colX, _linesY, colW);
            _preset = Line(r, "Preset", colX, _linesY, colW);
            _unfinished = Line(r, "Unfinished", colX, _linesY, colW);

            _status = UiKit.Text(r, "Status", "", th.smallSize, new Vector2(0f, -Size.y / 2f + 34f), new Vector2(Page.ContentWidth(Size.x), 40f),
                                 color: th.text2);
        }

        static TextMeshProUGUI Line(RectTransform r, string name, float x, float y, float w)
        {
            var t = UiKit.Text(r, name, "", UiKit.Theme.smallSize, new Vector2(x, y), new Vector2(w, 40f), color: UiKit.Theme.text2,
                               align: TextAlignmentOptions.Left);
            UiKit.SingleLine(t); // a 40 u box is shorter than one 28 u line (41 u): TMP would hide it
            return t;
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
            _preset.text = custom ? "Preset: " + active : "";
            var ids = new List<string>();
            foreach (var rec in _sessions.ListUnfinished())
                if (!ids.Contains(rec.StudyId)) ids.Add(rec.StudyId);
            ids.Sort(StringComparer.Ordinal);
            _unfinished.text = ids.Count > 0 ? "Unfinished: " + string.Join(", ", ids) : "";
            _target = logic.Schema.Contains(HeadsetVolume.TargetKey) ? _presets.LoadActive(new List<string>()).GetInt(HeadsetVolume.TargetKey) : -1;
            _shownVolume = int.MinValue;
            ShowVolume();
            StackLines();
        }

        void StackLines()
        {
            float y = _linesY;
            foreach (var t in new[] { _volume, _preset, _unfinished })
            {
                var p = t.rectTransform.anchoredPosition;
                t.rectTransform.anchoredPosition = new Vector2(p.x, y);
                if (!string.IsNullOrEmpty(t.text)) y -= LineStep;
            }
        }

        /// <summary>Reads the volume and updates the line when it changed.</summary>
        void ShowVolume()
        {
            if (_volume == null) return;
            int v = HeadsetVolume.ReadOrUnknown();
            if (v == _shownVolume) return;
            _shownVolume = v;
            _volume.text = VolumeStatus.Line(v, _target);
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
            var layer = _window.GetComponent<UiLayer>();
            if (layer != null) layer.Show(on);
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
            var size = new Vector2(860f, 640f);
            var sheet = Sheet.Open(_canvas, "Volume Step", Vector2.zero, size);
            var r = sheet.Rect;
            sheet.AddCloseButton(() => SetStatus("Start cancelled."));
            float d = th.iconButtonSize;
            float titleLeft = -size.x / 2f + Page.EdgePad + d + UiKit.IconTitleGap;
            float titleY = size.y / 2f - Page.TopPad - d / 2f;
            float titleW = size.x / 2f - Page.EdgePad - titleLeft;
            UiKit.Text(r, "Title", "Headset volume step", th.titleSize, new Vector2(titleLeft + titleW / 2f, titleY), new Vector2(titleW, d),
                       bold: true, align: TextAlignmentOptions.MidlineLeft);
            float padX = -size.x / 2f + Page.EdgePad + NumberPadEntry.Width / 2f;
            float padY = titleY - d / 2f - th.gap - NumberPadEntry.Height / 2f;
            var pad = NumberPadEntry.Create(r, new Vector2(padX, padY), 2, null, "0–" + MaxVolumeStep);

            float colLeft = padX + NumberPadEntry.Width / 2f + 2f * th.gap, colRight = size.x / 2f - Page.EdgePad;
            float colX = (colLeft + colRight) / 2f, colW = colRight - colLeft;
            UiKit.Text(r, "Info", "Enter the headset volume step (0–" + MaxVolumeStep + ") before handing the headset over. It is logged with the session.",
                       th.smallSize, new Vector2(colX, padY + 60f), new Vector2(colW, 200f), color: th.text2, align: TextAlignmentOptions.TopLeft);
            UiButton ok = null;
            ok = UiKit.Button(r, "OK", "OK", UiStyle.Prominent, new Vector2(colX, padY - NumberPadEntry.Height / 2f + th.buttonHeight / 2f),
                              new Vector2(colW, 0f), () =>
                              {
                                  if (!TryVolume(pad.Value, out int v)) return;
                                  sheet.Close();
                                  then(v);
                              });
            ok.interactable = false;
            pad.Changed += s => ok.interactable = TryVolume(s, out _);
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
