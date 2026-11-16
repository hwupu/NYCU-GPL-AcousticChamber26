using System;
using System.IO;
using GPL.Acoustics;
using GPL.Core;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// The RT60 run dialog (docs/design Screens/RT60 Run Dialog, PB-27): runs an <see cref="Rt60Plan"/> with a modal
    /// <see cref="ModalDialog"/> on the chamber's canvas instead of status lines. While it runs: "Measuring RT60" / "Calibrating
    /// RT60", the step and "Stay still and quiet.", a <see cref="UiProgressBar"/> with the time left, and a borderless Cancel (the run
    /// stops, no report files are kept, the dialog closes). When it ends: a calibration shows only "Calibration completed" and the
    /// file to import; a measurement shows T_mid per condition and the report name; a refused or failed run shows the reason; each with
    /// Close. The chamber panel behind takes no clicks while the dialog is open.
    /// </summary>
    public class Rt60RunDialog : MonoBehaviour
    {
        const string StayStill = "Stay still and quiet.";

        Rt60Runner _runner;
        ModalDialog _dialog;
        UiProgressBar _bar;
        string _step;
        bool _cancelledByUser, _silent;
        Action<Rt60RunResult> _done;
        Func<string, bool> _outsideTable;
        Transform _canvas;

        /// <summary>True from <see cref="Run"/> until the run has ended (the result dialog may still be open).</summary>
        public bool Running => _runner != null && _runner.Running;

        /// <summary>
        /// Starts <paramref name="plan"/> and opens the progress dialog on <paramref name="canvas"/> (the chamber panel's overlay canvas).
        /// <paramref name="done"/> gets the result once (also after a cancel); <paramref name="outsideTable"/> marks result lines whose
        /// target lies outside the calibration table.
        /// </summary>
        public static Rt60RunDialog Run(Transform canvas, Rt60Plan plan, Action<Rt60RunResult> done = null, Func<string, bool> outsideTable = null)
        {
            var host = canvas.gameObject.AddComponent<Rt60RunDialog>();
            host._canvas = canvas;
            host._done = done;
            host._outsideTable = outsideTable;
            bool sweep = plan.Kind == Rt60Report.KindSweep;
            host._dialog = ModalDialog.Show(canvas, sweep ? "Calibrating RT60" : "Measuring RT60", Message("Starting."),
                d => host._bar = UiKit.ProgressBar(d.Slot, "Progress", 0f, Rt60Eta.Text(plan.EstimatedSeconds())),
                new DialogButton("Cancel", UiStyle.Borderless, host.CancelByUser));
            host._runner = Rt60Runner.Begin(plan, null, host.OnDone);
            return host;
        }

        /// <summary>Leaving the chamber: stop the run without any further dialog (the canvas is going away).</summary>
        public void Stop()
        {
            _silent = true;
            if (_runner != null && _runner.Running) _runner.Cancel();
            if (_dialog != null) _dialog.Close();
        }

        void CancelByUser()
        {
            _cancelledByUser = true;
            if (_runner != null && _runner.Running) _runner.Cancel();
        }

        static string Message(string step) => step + " " + StayStill;

        void Update()
        {
            if (_runner == null || !_runner.Running || _dialog == null || _bar == null) return;
            _bar.Value = _runner.Fraction;
            _bar.Caption = Rt60Eta.Text(_runner.SecondsLeft);
            string step = _runner.Step;
            if (step == _step || string.IsNullOrEmpty(step)) return;
            _step = step;
            var m = _dialog.Dialog.MessageText;
            if (m != null)
            {
                m.text = Message(step);
                _dialog.Dialog.Fit(); // the step may take another line
            }
        }

        void OnDone(Rt60RunResult result)
        {
            try { _done?.Invoke(result); }
            catch (Exception e) { Debug.LogException(e); }
            if (_silent || this == null || _canvas == null) { Finish(); return; }
            if (_dialog != null && !_dialog.Dialog.Panel.IsClosing) _dialog.Close();
            _dialog = null;
            if (!_cancelledByUser) ShowResult(result);
            Finish();
        }

        void Finish()
        {
            _runner = null;
            if (this != null) Destroy(this);
        }

        void ShowResult(Rt60RunResult r)
        {
            bool sweep = r.Report != null && r.Report.Kind == Rt60Report.KindSweep;
            var close = new DialogButton("Close", UiStyle.Prominent);
            if (!r.Completed)
            {
                ModalDialog.Show(_canvas, sweep ? "Calibration not completed" : "RT60 not measured",
                                 Sentence(r.Error) + " No report was written.", close);
                return;
            }
            if (sweep)
            {
                // main protocol: import in the Editor and build; alternative (PO 2026-11-17): use it on this headset now
                var canvas = _canvas;
                string csv = r.CsvPath;
                ModalDialog.Show(_canvas, "Calibration completed",
                                 "Import " + Path.GetFileName(csv) + " in the Editor: Magic > Acoustics > Import RT60 Calibration.",
                                 new DialogButton("Close", UiStyle.Borderless),
                                 new DialogButton("Use on this headset", UiStyle.Prominent, () => UseOnHeadset(canvas, csv)));
                return;
            }
            var lines = r.ResultLines(_outsideTable);
            ModalDialog.Show(_canvas, "RT60 measured", null, d =>
            {
                var th = UiKit.Theme;
                var text = UiKit.Text(d.Slot, "Result", string.Join("\n", lines), th.smallSize, Vector2.zero, Vector2.zero,
                                      color: th.text, align: TextAlignmentOptions.TopLeft);
                UiKit.Size(text.gameObject, prefW: 0f, flexW: 1f);
                var report = UiKit.Text(d.Slot, "Report", "Report: " + Path.GetFileName(r.TxtPath), th.smallSize, Vector2.zero, Vector2.zero,
                                        color: th.text2, align: TextAlignmentOptions.TopLeft);
                UiKit.Size(report.gameObject, prefW: 0f, flexW: 1f);
            }, close);
        }

        /// <summary>
        /// "Use on this headset" (PB-27 alternative): the sweep becomes this headset's table for its size, read by every chamber when
        /// it loads (no rebuild); an imported table of the same size still wins. Static: the run dialog component is gone by now.
        /// </summary>
        static void UseOnHeadset(Transform canvas, string csvPath)
        {
            if (canvas == null) return;
            var close = new DialogButton("Close", UiStyle.Prominent);
            try
            {
                var cal = Rt60HeadsetTables.Use(csvPath);
                string size = Fmt.F(cal.RoomWidth, 2) + " × " + Fmt.F(cal.RoomDepth, 2) + " × " + Fmt.F(cal.RoomHeight, 2) + " m";
                ModalDialog.Show(canvas, "Used on this headset",
                                 "The chambers with a " + size + " room use this calibration on this headset now. Import " +
                                 Path.GetFileName(csvPath) + " in the Editor before the study.", close);
            }
            catch (Exception e)
            {
                Debug.LogError("[GPL] RT60: not used on this headset: " + e);
                ModalDialog.Show(canvas, "Not used", Sentence(e.Message) + " The headset keeps its previous calibration.", close);
            }
        }

        /// <summary>
        /// The RT60 line under a chamber's status (C01, C02): the warning in the debug color while uncalibrated, the softer headset-table
        /// note in text2 (PO 2026-11-17), or hidden when an imported table is in use.
        /// </summary>
        public static void ShowPanelNote(TextMeshProUGUI line, ReverbTuner reverb)
        {
            if (line == null) return;
            bool warning = true;
            string note = reverb != null ? reverb.PanelNote(out warning) : ReverbTuner.UncalibratedWarning;
            line.gameObject.SetActive(note != null);
            if (note == null) return;
            line.text = note;
            line.color = warning ? UiKit.Theme.debug : UiKit.Theme.text2;
        }

        static string Sentence(string s)
        {
            if (string.IsNullOrEmpty(s)) return "The run stopped.";
            s = char.ToUpperInvariant(s[0]) + s.Substring(1);
            return s.EndsWith(".") ? s : s + ".";
        }

        void OnDestroy()
        {
            // the canvas went away (scene change) while running: stop quietly
            if (_runner != null && _runner.Running) { _silent = true; _runner.Cancel(); }
        }
    }
}
