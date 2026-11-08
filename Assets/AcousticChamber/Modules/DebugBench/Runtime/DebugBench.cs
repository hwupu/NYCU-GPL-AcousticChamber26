using System;
using System.Collections.Generic;
using GPL.Params;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace GPL.DebugBench
{
    /// <summary>
    /// The Debug bench of a chamber (ADR-0002, DesignSystem section 7): pages pushed onto the chamber's
    /// <see cref="PageStack"/>.
    /// <list type="bullet">
    /// <item><b>Debug</b>: Run (Run experiment trials with the playground, Practice trial), Parameters (working copy ›,
    /// Presets ›), the chamber's own sections (<see cref="ChamberSections"/>: test playback, display switches, room ...),
    /// Data files ›, and a status line.</item>
    /// <item><b>Parameters</b>: the playground of the active preset, one row per parameter (− / +, switches for Bool),
    /// Revert in the header (back to the active preset), Save as Custom (creates Custom, or overwrites it after a
    /// confirmation; Custom becomes active).</item>
    /// <item><b>Presets</b> (DebugBench.Presets.cs): ✓ active; tap → sheet: Make active (loads it), Delete (Custom).</item>
    /// <item><b>Data files</b> (DebugBench.Data.cs, needs <see cref="Files"/>): Delete per row (confirms), Delete all
    /// (confirms once, names the count).</item>
    /// </list>
    /// No DEBUG tag on these pages. The bench never runs anything itself: it raises <see cref="RunRequested"/> /
    /// <see cref="PracticeRequested"/>; the chamber runs with <see cref="Work"/>.<c>Values</c>. <see cref="WorkingCopy.ParamChanged"/>
    /// is for the host to report (the chamber menu writes it to the console; edits reach the CSV as the Debug run's preset and params_hash).
    /// </summary>
    public sealed partial class DebugBench
    {
        readonly UiSurface _pages;
        readonly Transform _canvas;
        TextMeshProUGUI _status, _workValue, _presetsValue;

        public WorkingCopy Work { get; }

        /// <summary>Run experiment trials (real protocol, working copy, <c>_DEBUG</c> log).</summary>
        public event Action RunRequested;
        /// <summary>One practice trial (a Debug run with the working copy, <c>_DEBUG</c> log).</summary>
        public event Action PracticeRequested;

        /// <summary>False: the Practice trial button is inactive (the chamber has no practice trial). Read when the Debug page opens.</summary>
        public bool PracticeAvailable = true;

        /// <summary>The chamber's own sections on the Debug page, between Parameters and Data (labels + groups on the UiColumn).</summary>
        public Action<UiColumn> ChamberSections;

        /// <param name="pages">The chamber panel's surface (the chamber menu is its root page).</param>
        /// <param name="canvas">The canvas sheets open on (Save as, confirmations).</param>
        public DebugBench(UiSurface pages, Transform canvas, WorkingCopy work)
        {
            _pages = pages ?? throw new ArgumentNullException(nameof(pages));
            _canvas = canvas;
            Work = work ?? throw new ArgumentNullException(nameof(work));
        }

        public void SetStatus(string text)
        {
            if (_status != null) _status.text = text;
            if (!string.IsNullOrEmpty(text)) Debug.Log("[GPL] DebugBench: " + text);
        }

        // ---- Debug page ----------------------------------------------------------------------------------------

        public UiPage OpenDebugPage()
        {
            var page = _pages.Push("Config");
            _status = page.AddStatus("");
            var col = UiColumn.In(page);

            col.Label("Run");
            var run = col.Buttons("Run",
                ("Run experiment trials", UiStyle.Prominent, () => { SetStatus("Run experiment trials with " + Work.Label + " (_DEBUG log)"); RunRequested?.Invoke(); }),
                ("Practice trial", UiStyle.Borderless, () => { SetStatus("Practice trial with " + Work.Label + " (_DEBUG log)"); PracticeRequested?.Invoke(); }));
            run[1].interactable = PracticeAvailable;

            col.Label("Parameters");
            var g = col.Group("Parameters");
            var work = UiKit.Row(g, "Row Playground", "Playground", "Parameters of the Playground runs", Work.Label, null, "chevron-right", () => OpenParametersPage());
            _workValue = UiKit.RowValue(work);
            var presets = UiKit.Row(g, "Row Presets", "Presets", "real sessions use the active one", "Active: " + Work.Store.ActiveName, null, "chevron-right",
                                    () => OpenPresetsPage());
            _presetsValue = UiKit.RowValue(presets);

            ChamberSections?.Invoke(col);

            col.Label("Data");
            var d = col.Group("Data");
            var data = UiKit.Row(d, "Row Log files", "Log files", "this chamber's CSV and session files", DataSummary(), null, "chevron-right",
                                 () => OpenDataPage());
            data.GetComponent<UiButton>().interactable = Files != null;
            _dataValue = UiKit.RowValue(data);

            // empty until something happens (PO 2026-11-07: no "Ready."); success and failure notes stay
            SetStatus(Work.Warnings.Count > 0 ? Work.Warnings.Count + " repairs when loading " + Work.Source + " (see the log)" : "");

            page.Returned += RefreshDebugPage;
            return page;
        }

        /// <summary>Updates the Debug page's row values (working copy, active preset, data count).</summary>
        public void RefreshDebugPage()
        {
            if (_workValue != null) _workValue.text = Work.Label;
            if (_presetsValue != null) _presetsValue.text = "Active: " + Work.Store.ActiveName;
            if (_dataValue != null) _dataValue.text = DataSummary();
        }

        // ---- Parameters page -----------------------------------------------------------------------------------

        sealed class ParamRow
        {
            public ParamDef Def;
            public RectTransform Row;
            public TextMeshProUGUI Value;
            public TextMeshProUGUI Subtitle;
            public UiButton Minus, Plus;
            public UiSwitch Switch;
        }

        public UiPage OpenParametersPage()
        {
            var th = UiKit.Theme;
            var page = _pages.Push("Parameters");
            var rows = new List<ParamRow>();
            UiButton revert = page.AddHeaderAction("Revert", UiStyle.Borderless, () => Work.Revert());

            var col = UiColumn.In(page);
            var intro = col.Label(IntroText());

            // one group per ParamDef.Group, in schema order
            var groups = new List<(string name, List<ParamDef> defs)>();
            foreach (var d in Work.Schema.Defs)
            {
                int gi = groups.FindIndex(x => x.name == d.Group);
                if (gi < 0) groups.Add((d.Group, new List<ParamDef> { d }));
                else groups[gi].defs.Add(d);
            }
            foreach (var (name, defs) in groups)
            {
                if (groups.Count > 1 || name.Length > 0) col.Label(name.Length > 0 ? name : "Other");
                var g = col.Group("Group " + name);
                for (int i = 0; i < defs.Count; i++) rows.Add(BuildParamRow(g, defs[i]));
            }

            var save = col.Buttons("Save", (SaveAsLabel, UiStyle.Borderless, () => SaveAsCustom()))[0];
            var note = col.Label(SaveNote());

            void Refresh()
            {
                if (page == null) return;
                intro.text = IntroText();
                revert.interactable = Work.IsEdited;
                save.interactable = Work.IsEdited;
                note.text = SaveNote();
                foreach (var r in rows)
                {
                    string raw = Work.Values.GetText(r.Def.Key);
                    bool edited = Work.IsKeyEdited(r.Def.Key);
                    if (r.Switch != null) r.Switch.On = raw == "true";
                    if (r.Value != null)
                    {
                        r.Value.text = WorkingCopy.Format(r.Def, raw);
                        r.Value.color = edited ? th.text : th.text2;
                    }
                    string sub = edited ? Work.Source + ": " + WorkingCopy.Format(r.Def, Work.SourceValue(r.Def.Key)) : r.Def.Description;
                    r.Subtitle.text = sub;
                    r.Subtitle.gameObject.SetActive(!string.IsNullOrEmpty(sub)); // no secondary line when there is nothing to say
                    if (r.Minus != null) r.Minus.interactable = Work.CanStep(r.Def.Key, -1);
                    if (r.Plus != null) r.Plus.interactable = Work.CanStep(r.Def.Key, +1);
                }
                RefreshDebugPage();
            }

            Work.Changed += Refresh;
            page.gameObject.AddComponent<OnDestroyHook>().Destroyed += () => Work.Changed -= Refresh;
            Refresh();
            return page;
        }

        ParamRow BuildParamRow(RectTransform g, ParamDef d)
        {
            var th = UiKit.Theme;
            var r = new ParamRow { Def = d };
            if (d.Type == ParamType.Bool)
            {
                // the whole row toggles, like every switch row
                r.Switch = UiKit.SwitchRow(g, "Row " + d.Label, d.Label, "-", Work.Values.GetBool(d.Key), _ => Work.Step(d.Key, +1));
                r.Row = (RectTransform)r.Switch.transform.parent;
                r.Subtitle = UiKit.RowSubtitle(r.Row);
                return r;
            }
            // an info row: title and description at the left, the value, and (not for text) a - / + pair at the right
            var row = UiKit.Row(g, "Row " + d.Label, d.Label, "-", "");
            r.Row = row;
            r.Value = UiKit.RowValue(row);
            r.Subtitle = UiKit.RowSubtitle(row);
            if (d.Type != ParamType.Text)
            {
                var pair = UiKit.Node(row, "Stepper");
                UiKit.H(pair.gameObject, th.inset, null, TextAnchor.MiddleCenter);
                // borderless in-group steppers: fill on hover only (DesignSystem section 4)
                r.Minus = UiKit.Button(pair, "Minus", null, UiStyle.Borderless, "minus", small: true, onClick: () => Work.Step(d.Key, -1));
                r.Plus = UiKit.Button(pair, "Plus", null, UiStyle.Borderless, "plus", small: true, onClick: () => Work.Step(d.Key, +1));
                r.Plus.InGroup = r.Minus.InGroup = true;
            }
            return r;
        }

        string IntroText() =>
            "Playground: a copy of the active preset, " + Work.Source + (Work.IsEdited ? " · " + Work.EditedKeys.Count + " edited" : "") +
            ". Playground runs use it; real sessions use " + Work.Source + " as saved. Revert goes back to it.";

        public static readonly string SaveAsLabel = "Save as " + WorkingCopy.CustomName;

        string SaveNote() =>
            Work.SourceIsReadOnly
                ? "Default is read-only (Config/default.params). " + SaveAsLabel + (Work.CustomExists ? " overwrites Custom" : " creates Custom") +
                  " and makes it active."
                : SaveAsLabel + " overwrites Custom; new real sessions use it.";

        /// <summary>Writes Custom (creates it, or overwrites it after a confirmation) and makes it active.</summary>
        void SaveAsCustom()
        {
            void Save()
            {
                string name = Work.SaveAs();
                SetStatus("Saved " + name + "; it is active (new real sessions use it)");
            }
            if (!Work.CustomExists)
            {
                Save();
                return;
            }
            ModalDialog.Show(_canvas, "Overwrite Custom?",
                "Custom gets the playground's values and " + (Work.Source == WorkingCopy.CustomName ? "stays" : "becomes") +
                " active: new real sessions use them. Unfinished sessions keep their own snapshot.",
                new DialogButton("Cancel", UiStyle.Borderless),
                new DialogButton("Overwrite", UiStyle.Prominent, Save));
        }

    }

    /// <summary>Raises <see cref="Destroyed"/> when its GameObject is destroyed (unhooks page listeners).</summary>
    public sealed class OnDestroyHook : MonoBehaviour
    {
        public event Action Destroyed;
        void OnDestroy() => Destroyed?.Invoke();
    }
}
