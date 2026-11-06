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
    /// <item><b>Debug</b>: Run (Run experiment trials with the working copy, Practice trial), Parameters (working copy ›,
    /// Presets ›), the chamber's own sections (<see cref="ChamberSections"/>: test playback, display switches, room ...),
    /// Data files ›, and a status line.</item>
    /// <item><b>Parameters</b>: the working copy of the active preset, one row per parameter (− / +, switches for Bool),
    /// Revert in the header (back to the active preset), Save as Custom (creates Custom, or overwrites it after a
    /// confirmation; Custom becomes active).</item>
    /// <item><b>Presets</b> (DebugBench.Presets.cs): ✓ active; tap → sheet: Make active (loads it), Delete (Custom).</item>
    /// <item><b>Data files</b> (DebugBench.Data.cs, needs <see cref="Files"/>): Delete per row (confirms), Delete all
    /// (confirms once, names the count).</item>
    /// </list>
    /// No DEBUG tag on these pages. The bench never runs anything itself: it raises <see cref="RunRequested"/> /
    /// <see cref="PracticeRequested"/>; the chamber runs with <see cref="Work"/>.<c>Values</c> and logs
    /// <see cref="WorkingCopy.ParamChanged"/> as <c>param_change</c>.
    /// </summary>
    public sealed partial class DebugBench
    {
        /// <summary>Space reserved for the status line at the bottom of the Debug page (the line is centered in it).</summary>
        public const float StatusHeight = 88f;

        readonly PageStack _pages;
        readonly Transform _canvas;
        TextMeshProUGUI _status, _workValue, _presetsValue;

        public WorkingCopy Work { get; }

        /// <summary>Run experiment trials (real protocol, working copy, <c>_DEBUG</c> log).</summary>
        public event Action RunRequested;
        public event Action PracticeRequested;

        /// <summary>The chamber's own sections on the Debug page, between Parameters and Data (labels + groups on the column).</summary>
        public Action<Column> ChamberSections;

        /// <param name="pages">The chamber panel's page stack (the chamber menu is its root page).</param>
        /// <param name="canvas">The canvas sheets open on (Save as, confirmations).</param>
        public DebugBench(PageStack pages, Transform canvas, WorkingCopy work)
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

        public Page OpenDebugPage()
        {
            var page = _pages.Push("Debug");
            var col = Column.In(page, StatusHeight);

            col.Label("Run");
            col.Buttons("Run",
                ("Run experiment trials", UiStyle.Prominent, () => { SetStatus("Run experiment trials with " + Work.Label + " (_DEBUG log)"); RunRequested?.Invoke(); }),
                ("Practice trial", UiStyle.Borderless, () => { SetStatus("Practice trial"); PracticeRequested?.Invoke(); }));

            col.Label("Parameters");
            var g = col.Group("Parameters", 2);
            var work = UiKit.Row(g, 0, "Working copy", Work.Label, () => OpenParametersPage(), sub: "used by Debug runs");
            _workValue = UiKit.RowText(work.transform, "Value");
            var presets = UiKit.Row(g, 1, "Presets", "Active: " + Work.Store.ActiveName, () => OpenPresetsPage(), sub: "real sessions use the active one");
            _presetsValue = UiKit.RowText(presets.transform, "Value");

            ChamberSections?.Invoke(col);

            col.Label("Data");
            var d = col.Group("Data", 1);
            var data = UiKit.Row(d, 0, "Data files", DataSummary(), () => OpenDataPage(), sub: "this chamber's CSV and session files");
            data.interactable = Files != null;
            _dataValue = UiKit.RowText(data.transform, "Value");
            col.Finish();

            var th = UiKit.Theme;
            var size = page.Rect.rect.size;
            _status = UiKit.Text(page.Rect, "Status", "", th.smallSize, new Vector2(0f, -size.y / 2f + StatusHeight / 2f),
                                 new Vector2(Page.ContentWidth(size.x) - 2f * UiKit.RowTextPad, StatusHeight), color: th.text2,
                                 align: TextAlignmentOptions.Left);
            UiKit.SingleLine(_status);
            // empty until something happens (PO 2026-11-07: no "Ready."); success and failure notes stay
            SetStatus(Work.Warnings.Count > 0 ? Work.Warnings.Count + " repairs when loading " + Work.Source + " (see the log)" : "");

            page.Returned += RefreshDebugPage;
            return page;
        }

        /// <summary>Updates the Debug page's row values (working copy, active preset, data count).</summary>
        public void RefreshDebugPage()
        {
            if (_workValue != null) _workValue.text = Work.Label + "  ›";
            if (_presetsValue != null) _presetsValue.text = "Active: " + Work.Store.ActiveName + "  ›";
            if (_dataValue != null) _dataValue.text = DataSummary() + "  ›";
        }

        // ---- Parameters page -----------------------------------------------------------------------------------

        sealed class ParamRow
        {
            public ParamDef Def;
            public RectTransform Row;
            public TextMeshProUGUI Value;
            public UiButton Minus, Plus;
            public UiSwitch Switch;
        }

        public Page OpenParametersPage()
        {
            var th = UiKit.Theme;
            var page = _pages.Push("Parameters");
            var rows = new List<ParamRow>();
            UiButton revert = page.AddHeaderAction("Revert", UiStyle.Borderless, () => Work.Revert());

            var col = Column.In(page);
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
                var g = col.Group("Group " + name, defs.Count);
                for (int i = 0; i < defs.Count; i++) rows.Add(BuildParamRow(g, i, defs[i]));
            }

            var save = col.Buttons("Save", (SaveAsLabel, UiStyle.Borderless, () => SaveAsCustom()))[0];
            var note = col.Label(SaveNote());
            col.Finish();

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
                    UiKit.SetRowSub(r.Row, edited ? Work.Source + ": " + WorkingCopy.Format(r.Def, Work.SourceValue(r.Def.Key)) : r.Def.Description);
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

        ParamRow BuildParamRow(RectTransform g, int index, ParamDef d)
        {
            var th = UiKit.Theme;
            var r = new ParamRow { Def = d };
            if (d.Type == ParamType.Bool)
            {
                // the whole row toggles, like every switch row
                r.Switch = UiKit.SwitchRow(g, index, d.Label, "-", Work.Values.GetBool(d.Key), _ => Work.Step(d.Key, +1));
                r.Row = (RectTransform)r.Switch.transform.parent;
                return r;
            }
            const float stepW = 80f;
            float controls = d.Type == ParamType.Text ? UiKit.RowTextPad : 2f * stepW + th.inset + 16f + 24f;
            var row = UiKit.InfoRow(g, index, d.Label, "-", "", controls);
            r.Row = row;
            r.Value = UiKit.RowText(row, "Value");
            float right = row.sizeDelta.x / 2f;
            switch (d.Type)
            {
                case ParamType.Text:
                    break;
                default:
                    float plusX = right - 24f - stepW / 2f;
                    // borderless in-group steppers: fill on hover only (DesignSystem section 4)
                    r.Plus = UiKit.SmallButton(row, "Plus", "+", new Vector2(plusX, 0f), stepW, () => Work.Step(d.Key, +1), UiStyle.Borderless);
                    r.Minus = UiKit.SmallButton(row, "Minus", UiKit.MinusGlyph, new Vector2(plusX - stepW - th.inset, 0f), stepW,
                                                () => Work.Step(d.Key, -1), UiStyle.Borderless);
                    r.Plus.InGroup = r.Minus.InGroup = true;
                    break;
            }
            return r;
        }

        string IntroText() =>
            "Working copy of the active preset, " + Work.Source + (Work.IsEdited ? " · " + Work.EditedKeys.Count + " edited" : "") +
            ". Debug runs use it; real sessions use " + Work.Source + " as saved. Revert goes back to it.";

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
                "Custom gets the working copy's values and " + (Work.Source == WorkingCopy.CustomName ? "stays" : "becomes") +
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
