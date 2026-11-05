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
    /// <item><b>Parameters</b>: the working copy, one row per parameter (− / +, switches for Bool), Revert in the header,
    /// Save to &lt;preset&gt; (confirm) and Save as… (a sheet; the name is the next free "Preset-N").</item>
    /// </list>
    /// No DEBUG tag on these pages. The bench never runs anything itself: it raises <see cref="RunRequested"/> /
    /// <see cref="PracticeRequested"/>; the chamber runs with <see cref="Work"/>.<c>Values</c> and logs
    /// <see cref="WorkingCopy.ParamChanged"/> as <c>param_change</c>.
    /// </summary>
    public sealed class DebugBench
    {
        public const float StatusHeight = 56f;

        readonly PageStack _pages;
        readonly Transform _canvas;
        TextMeshProUGUI _status, _workValue, _presetsValue;

        public WorkingCopy Work { get; }

        /// <summary>Run experiment trials (real protocol, working copy, <c>_DEBUG</c> log).</summary>
        public event Action RunRequested;
        public event Action PracticeRequested;

        /// <summary>Presets › (built in PB-19b; until then the bench shows a status line).</summary>
        public event Action PresetsRequested;

        /// <summary>Data files › (PB-19c).</summary>
        public event Action DataRequested;

        /// <summary>The chamber's own sections on the Debug page, between Parameters and Data (labels + groups on the column).</summary>
        public Action<Column> ChamberSections;

        /// <summary>Value shown on the Data files row (e.g. "12 files").</summary>
        public Func<string> DataSummary;

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
            Debug.Log("[GPL] DebugBench: " + text);
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
            var presets = UiKit.Row(g, 1, "Presets", "Active: " + Work.Store.ActiveName, () =>
            {
                if (PresetsRequested != null) PresetsRequested();
                else SetStatus("Presets page: next step (PB-19b)");
            }, sub: "real sessions use the active one");
            _presetsValue = UiKit.RowText(presets.transform, "Value");

            ChamberSections?.Invoke(col);

            col.Label("Data");
            var d = col.Group("Data", 1);
            UiKit.Row(d, 0, "Data files", DataSummary?.Invoke(), () =>
            {
                if (DataRequested != null) DataRequested();
                else SetStatus("Data files page: PB-19c");
            });
            col.Finish();

            var th = UiKit.Theme;
            var size = page.Rect.rect.size;
            _status = UiKit.Text(page.Rect, "Status", "", th.smallSize, new Vector2(0f, -size.y / 2f + StatusHeight / 2f),
                                 new Vector2(Page.ContentWidth(size.x) - 2f * UiKit.RowTextPad, StatusHeight), color: th.text2,
                                 align: TextAlignmentOptions.MidlineLeft);
            _status.textWrappingMode = TextWrappingModes.NoWrap;
            _status.overflowMode = TextOverflowModes.Ellipsis;
            SetStatus(Work.Warnings.Count > 0 ? Work.Warnings.Count + " repairs when loading " + Work.Source + " (see the log)" : "Ready.");

            page.Returned += RefreshDebugPage;
            return page;
        }

        void RefreshDebugPage()
        {
            if (_workValue != null) _workValue.text = Work.Label + "  ›";
            if (_presetsValue != null) _presetsValue.text = "Active: " + Work.Store.ActiveName + "  ›";
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

            var buttons = col.Buttons("Save",
                (SaveLabel(), UiStyle.Borderless, () => ConfirmSave()),
                ("Save as…", UiStyle.Borderless, () => OpenSaveAsSheet()));
            var readOnly = col.Label("\"Default\" is read-only (Config/default.params); use Save as.");
            col.Finish();

            void Refresh()
            {
                if (page == null) return;
                intro.text = IntroText();
                revert.interactable = Work.IsEdited;
                UiKit.LabelOf(buttons[0]).text = SaveLabel();
                buttons[0].interactable = Work.CanSave;
                readOnly.gameObject.SetActive(Work.SourceIsReadOnly);
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
                    r.Plus = UiKit.SmallButton(row, "Plus", "+", new Vector2(plusX, 0f), stepW, () => Work.Step(d.Key, +1));
                    r.Minus = UiKit.SmallButton(row, "Minus", UiKit.MinusGlyph, new Vector2(plusX - stepW - th.inset, 0f), stepW,
                                                () => Work.Step(d.Key, -1));
                    break;
            }
            return r;
        }

        string IntroText() =>
            "Working copy of \"" + Work.Source + "\"" + (Work.IsEdited ? " · " + Work.EditedKeys.Count + " edited" : "") +
            ". Debug runs use it; real sessions use the active preset (" + Work.Store.ActiveName + ").";

        string SaveLabel() => "Save to " + Work.Source;

        void ConfirmSave()
        {
            bool active = Work.Source == Work.Store.ActiveName;
            ModalDialog.Show(_canvas, "Overwrite " + Work.Source + "?",
                active ? Work.Source + " is active: new real sessions will use the new values. Running and unfinished sessions keep their own snapshot."
                       : "The preset " + Work.Source + " gets the working copy's values.",
                new DialogButton("Cancel", UiStyle.Borderless),
                new DialogButton("Overwrite", UiStyle.Prominent, () =>
                {
                    Work.Save();
                    SetStatus("Saved " + Work.Source);
                }));
        }

        void OpenSaveAsSheet()
        {
            var th = UiKit.Theme;
            var size = new Vector2(820f, 400f);
            string name = Work.NextName;
            var sheet = Sheet.Open(_canvas, "Save As", Vector2.zero, size);
            sheet.AddCloseButton();
            float y = size.y / 2f - Page.TopPad - th.iconButtonSize / 2f;
            float left = -size.x / 2f + Page.EdgePad + th.iconButtonSize + UiKit.IconTitleGap;
            float w = size.x / 2f - Page.EdgePad - left;
            UiKit.Text(sheet.Rect, "Title", "Save as a new preset", th.headingSize, new Vector2(left + w / 2f, y), new Vector2(w, th.iconButtonSize),
                       bold: true, align: TextAlignmentOptions.MidlineLeft);
            UiKit.Text(sheet.Rect, "Message", "Name: " + name + ". It is not active until you make it active in Presets.", 32,
                       new Vector2(0f, y - th.iconButtonSize / 2f - 70f), new Vector2(size.x - 2f * Page.EdgePad, 100f), color: th.text2,
                       align: TextAlignmentOptions.TopLeft);
            float bw = th.buttonMinWidth;
            UiKit.Button(sheet.Rect, "Save", "Save", UiStyle.Prominent,
                         new Vector2(size.x / 2f - Page.EdgePad - bw / 2f, -size.y / 2f + Page.EdgePad + th.buttonHeight / 2f), new Vector2(bw, 0f), () =>
                         {
                             sheet.Close();
                             string saved = Work.SaveAs(name);
                             SetStatus("Saved " + saved + " (not active)");
                         });
        }
    }

    /// <summary>Raises <see cref="Destroyed"/> when its GameObject is destroyed (unhooks page listeners).</summary>
    public sealed class OnDestroyHook : MonoBehaviour
    {
        public event Action Destroyed;
        void OnDestroy() => Destroyed?.Invoke();
    }
}
