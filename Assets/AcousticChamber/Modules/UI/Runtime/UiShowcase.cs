using GPL.Core;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// Headset test panel of GPL.UI (no study logic), design system v2: header with ‹ back and the DEBUG tag, Study ID
    /// number pad (borderless keys), one button per style, the Stop dialog, a motion switch and English / 中文 for the
    /// localization check. Builds itself on Start under its canvas.
    /// Check: both eyes sharp; hover settles in and out; pressed is the brightest; the 60 u hit region; S0001 formatting;
    /// the Stop dialog appears in place and pushes the panel back (94 %), its confirmation pushes both (88 %), only the
    /// top sheet takes the ray. Debug opens a page demo (Debug → Parameters with a × sheet, Data files with a delete
    /// confirmation): pages change in place, ‹ goes back.
    /// </summary>
    public class UiShowcase : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);

        TextMeshProUGUI _status;
        NumberPadEntry _pad;
        UiButton _motion;
        PageStack _pages;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            var window = UiKit.Window(canvas); // sheets (Stop dialog) push it back
            _pages = PageStack.On(window);
            _pages.RootBack = () => SetStatus("Back (would return to the Hall)");
            var root = _pages.Push("GPL.UI test panel", debugTag: true).Rect;

            // left column: Study ID
            const float leftX = -250f;
            UiKit.Text(root, "Study ID Label", "Study ID", th.smallSize, new Vector2(leftX, 175f), new Vector2(NumberPadEntry.Width, 40f),
                       color: th.text2, align: TextAlignmentOptions.Left);
            _pad = NumberPadEntry.Create(root, new Vector2(leftX, -70f), StudyId.MaxDigits, StudyId.FromDigits, "S____");
            _pad.Changed += v => SetStatus(v.Length == 0 ? "Study ID cleared" : "Study ID " + StudyId.FromDigits(v) + " (typed " + v + ")");

            // right column
            const float rightX = 240f, w = 420f;
            float y = 175f;
            UiKit.Text(root, "Preset Line", "Preset: Default", th.smallSize, new Vector2(rightX, y), new Vector2(w, 40f),
                       color: th.text2, align: TextAlignmentOptions.Left);
            y -= 60f;
            UiKit.Button(root, "Start", "Start", UiStyle.Prominent, new Vector2(rightX, y), new Vector2(w, 0f), () => SetStatus("Start"));
            y -= 100f;
            UiKit.Button(root, "Debug", "Debug", UiStyle.Gray, new Vector2(rightX, y), new Vector2(w, 0f), OpenDebugPage);
            y -= 100f;
            UiKit.Button(root, "Stop Dialog", "Stop dialog (Y)", UiStyle.Borderless, new Vector2(rightX, y), new Vector2(w, 0f), OpenStopDialog);
            y -= 100f;
            var english = UiKit.Button(root, "English", Loc.LabelOf(UiLanguage.English), UiStyle.Gray, new Vector2(rightX - 106f, y), new Vector2(0f, 0f),
                                       () => { Loc.Set(UiLanguage.English); SetStatus("Language: English"); });
            var chinese = UiKit.Button(root, "Chinese", Loc.LabelOf(UiLanguage.Chinese), UiStyle.Gray, new Vector2(rightX + 106f, y), new Vector2(0f, 0f),
                                       () => { Loc.Set(UiLanguage.Chinese); SetStatus("Language: 中文"); });
            // side by side: narrower than the minimum width on purpose
            ((RectTransform)english.transform).sizeDelta = ((RectTransform)chinese.transform).sizeDelta = new Vector2(208f, th.buttonHeight);
            y -= 100f;
            _motion = UiKit.Button(root, "Motion", "", UiStyle.Borderless, new Vector2(rightX - 106f, y), new Vector2(208f, 0f), ToggleMotion, th.smallSize);
            ((RectTransform)_motion.transform).sizeDelta = new Vector2(208f, th.buttonHeight);
            var disabled = UiKit.Button(root, "Disabled", "Delete", UiStyle.Destructive, new Vector2(rightX + 106f, y), new Vector2(208f, 0f));
            ((RectTransform)disabled.transform).sizeDelta = new Vector2(208f, th.buttonHeight);
            disabled.interactable = false;
            ShowMotion();

            _status = UiKit.Text(window, "Status", "Point at a button and pull the trigger.", th.minimumSize, new Vector2(leftX, -Size.y / 2f + 34f),
                                 new Vector2(NumberPadEntry.Width, 36f), color: th.text2, align: TextAlignmentOptions.Left);
        }

        // ---- page demo: Debug -> Parameters / Data files (layout only; the real bench is PB-19) ----------------

        static readonly float GroupWidth = Page.ContentWidth(Size.x); // aligned with the header edges (48 u from the window edge)

        void OpenDebugPage()
        {
            var page = _pages.Push("Debug").Rect;
            float top = Size.y / 2f - Page.HeaderHeight - 20f;
            var g = UiKit.Group(page, "Group", Vector2.zero, GroupWidth, 3);
            g.anchoredPosition = new Vector2(0f, top - g.sizeDelta.y / 2f);
            UiKit.Row(g, 0, "Parameters", "Default", OpenParametersPage);
            UiKit.Row(g, 1, "Presets", "2", () => SetStatus("Presets page: PB-19"));
            UiKit.Row(g, 2, "Data files", "3", OpenDataPage);
            SetStatus("Debug page (‹ goes back)");
        }

        void OpenParametersPage()
        {
            var page = _pages.Push("Parameters").Rect;
            float top = Size.y / 2f - Page.HeaderHeight - 20f;
            var g = UiKit.Group(page, "Group", Vector2.zero, GroupWidth, 2);
            g.anchoredPosition = new Vector2(0f, top - g.sizeDelta.y / 2f);
            UiKit.Row(g, 0, "Trials per block", "24", chevron: false);
            UiKit.Row(g, 1, "Inter-trial interval", "1.5 s", chevron: false);
            UiKit.Button(page, "Save As", "Save as…", UiStyle.Gray, new Vector2(0f, g.anchoredPosition.y - g.sizeDelta.y / 2f - 80f),
                         new Vector2(300f, 0f), OpenSaveAsSheet);
            SetStatus("Parameters page");
        }

        void OpenSaveAsSheet()
        {
            var th = UiKit.Theme;
            var sheet = Sheet.Open(transform, "Save As Sheet", Vector2.zero, new Vector2(820f, 360f));
            float y = 360f / 2f - Page.TopPad - th.iconButtonSize / 2f;
            float left = -820f / 2f + Page.EdgePad + th.iconButtonSize + UiKit.IconTitleGap;
            sheet.AddCloseButton(() => SetStatus("Save as: closed with ×"));
            UiKit.Text(sheet.Rect, "Title", "Save as", th.headingSize, new Vector2(left + 250f, y), new Vector2(500f, 80f), bold: true,
                       align: TextAlignmentOptions.MidlineLeft);
            UiKit.Text(sheet.Rect, "Message", "A sheet that may be left without a choice has × instead of Cancel.", th.smallSize,
                       new Vector2(0f, -10f), new Vector2(820f - 2f * Page.EdgePad, 80f), color: th.text2, align: TextAlignmentOptions.TopLeft);
            UiKit.Button(sheet.Rect, "Save", "Save", UiStyle.Prominent, new Vector2(820f / 2f - Page.EdgePad - 110f, -360f / 2f + 48f + 40f),
                         new Vector2(220f, 0f), () => { sheet.Close(); SetStatus("Save as: saved (demo)"); });
        }

        void OpenDataPage()
        {
            const int files = 12;
            var page = _pages.Push("Data files").Rect;
            var th = UiKit.Theme;

            // header action at the right: borderless, red text
            float headerY = Size.y / 2f - Page.TopPad - th.iconButtonSize / 2f;
            UiKit.Button(page, "Delete All", "Delete all", UiStyle.DestructiveText, new Vector2(Size.x / 2f - Page.EdgePad - 120f, headerY),
                         new Vector2(240f, 0f), () =>
                             ModalDialog.Show(transform, "Delete all " + files + " files?", "The files are removed from this headset.",
                                 new DialogButton("Cancel", UiStyle.Borderless, () => SetStatus("Delete all: cancelled")),
                                 new DialogButton("Delete", UiStyle.Destructive, () => SetStatus("Delete all: deleted (demo)"))));

            // borderless scroll list from below the header to above the status line
            float top = Size.y / 2f - Page.HeaderHeight - 8f, bottom = -Size.y / 2f + 64f;
            var list = ScrollList.Create(page, "Files", new Vector2(0f, (top + bottom) / 2f), new Vector2(GroupWidth + 2f * 20f, top - bottom));
            var g = UiKit.Group(list.Content, "Group", Vector2.zero, GroupWidth, files);
            ScrollList.TopAnchored(g, 0f);
            for (int i = 0; i < files; i++)
            {
                string name = "20261105_" + (9 + i).ToString("00") + "1500_C04_S" + (i + 1).ToString("0000") + ".csv";
                UiKit.Row(g, i, name, null, () => SetStatus("Clicked " + name), chevron: false);
            }
            list.ContentHeight = g.sizeDelta.y;
            SetStatus("Data files: drag with the trigger or use the right thumbstick");
        }

        void OpenStopDialog()
        {
            string id = _pad != null && _pad.Value.Length > 0 ? StudyId.FromDigits(_pad.Value) : "";
            StopDialog.Show(transform, false, id, c => SetStatus("Stop dialog: " + c), freeze: false);
        }

        void ToggleMotion()
        {
            UiKit.Theme.reduceMotion = !UiKit.Theme.reduceMotion;
            ShowMotion();
            SetStatus(UiKit.Theme.reduceMotion ? "Reduce motion: every transition instant" : "Motion: settle (150 / 350 / 250 ms)");
        }

        void ShowMotion()
        {
            if (_motion != null) _motion.Label.text = UiKit.Theme.reduceMotion ? "Motion: off" : "Motion: on";
        }

        void SetStatus(string s)
        {
            if (_status != null) _status.text = s;
            Debug.Log("[GPL] UiShowcase: " + s);
        }
    }
}
