using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Homing;
using GPL.Input;
using GPL.Logging;
using GPL.Params;
using GPL.Session;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.Chambers.Template
{
    /// <summary>
    /// The Template chamber (C00, Docs/Design.md). PB-07a: the parameters and log columns of the design and a placeholder run
    /// (one screen, A completes the session) so the chamber menu, sessions, Stop dialog and data files can be tested. The
    /// protocol (language, handover, practice, blocks of gate + press A, thanks) replaces the placeholder in PB-07b.
    /// </summary>
    public class TemplateChamber : ChamberBehaviour
    {
        public const string Id = "Template";

        static ParameterSchema _schema;
        static LogSchema _log;

        public static ParameterSchema CreateSchema()
        {
            var defs = new List<ParamDef>
            {
                ParamDef.Int("practice_trials", 2, 0, 10, "Practice trials", "", "Procedure"),
                ParamDef.Int("blocks", 2, 1, 10, "Blocks", "", "Procedure"),
                ParamDef.Int("trials_per_block", 4, 1, 50, "Trials per block", "", "Procedure"),
                ParamDef.Float("response_timeout_s", 5f, 1f, 30f, 1f, "Response timeout", "s", "Procedure").Describe("no press = timeout"),
                ParamDef.Float("iti_s", 1f, 0f, 5f, 0.1f, "Inter-trial interval", "s", "Procedure"),
                ParamDef.Choice("resume_from", "block_start", ResumeNames.All, "Resume from", "Resume").Describe("experiment stage after an interruption"),
            };
            defs.AddRange(HomingParams.GateDefs());
            return new ParameterSchema(defs);
        }

        public static LogSchema CreateLogSchema() =>
            new LogSchema("rt_s", "timed_out", "gate_s", "gate_breaks", "stop_dialog", ChamberMenu.VolumeColumn);

        public override ParameterSchema Schema => _schema ?? (_schema = CreateSchema());
        public override LogSchema LogSchema => _log ?? (_log = CreateLogSchema());

        public override string Describe(SessionRecord r) =>
            "started " + r.Created.ToString("MM-dd HH:mm") + ", " + ResumeNames.Name(r.ResumePolicy);

        RectTransform _screen;

        public override IEnumerator Run(ChamberRun run)
        {
            var rec = run.Record;
            if (!run.IsResume)
            {
                rec.EnterStage(0, ResumeNames.Parse(run.Values.GetText("resume_from")));
                run.Save();
            }
            BuildScreen(run);
            // wait for A (ignored while the Stop dialog is open)
            while (true)
            {
                if (!Freeze.Active && !StopDialog.IsOpen && run.Actions != null && run.Actions.Reader.Down(InputActions.Confirm)) break;
                yield return null;
            }
            HideScreen();
        }

        public override void Abort(ChamberRun run) => HideScreen();

        void BuildScreen(ChamberRun run)
        {
            var th = UiKit.Theme;
            _screen = UiKit.Window(run.Canvas, "Run Screen");
            string who = run.IsDebug ? "Debug run" : (run.IsResume ? "Resumed session " : "Session ") + run.Record.StudyId;
            UiKit.Text(_screen, "Title", who, th.titleSize, new Vector2(0f, 140f), new Vector2(ChamberMenu.Size.x - 96f, 80f), bold: true);
            UiKit.Text(_screen, "Info",
                       "Placeholder for the protocol (PB-07b).\nResume policy: " + run.Values.GetText("resume_from") +
                       "\n\nA: finish the session · Y: Stop dialog\nQuit the app here to test resume.",
                       th.bodySize, new Vector2(0f, -60f), new Vector2(ChamberMenu.Size.x - 96f, 300f), color: th.text2);
        }

        void HideScreen()
        {
            if (_screen != null) Destroy(_screen.gameObject);
            _screen = null;
        }
    }
}
