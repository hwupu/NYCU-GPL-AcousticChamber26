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
    /// the dialog blocks the panel behind it.
    /// </summary>
    public class UiShowcase : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);

        TextMeshProUGUI _status;
        NumberPadEntry _pad;
        UiButton _motion;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var root = (RectTransform)transform;
            UiKit.Clear(root);
            root.sizeDelta = Size;

            UiKit.Background(root);
            // header: ‹ at the top left, title, DEBUG tag at the right
            float headerY = Size.y / 2f - 40f - th.iconButtonSize / 2f;
            UiKit.IconButton(root, "Back", UiKit.BackGlyph, new Vector2(-Size.x / 2f + 48f + th.iconButtonSize / 2f, headerY), () => SetStatus("Back"));
            float titleLeft = -Size.x / 2f + 48f + th.iconButtonSize + UiKit.IconTitleGap;
            UiKit.Text(root, "Title", "GPL.UI test panel", th.titleSize, new Vector2(titleLeft + 300f, headerY), new Vector2(600f, 80f),
                       bold: true, align: TextAlignmentOptions.Left);
            UiKit.DebugTag(root);

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
            UiKit.Button(root, "Debug", "Debug", UiStyle.Gray, new Vector2(rightX, y), new Vector2(w, 0f), () => SetStatus("Debug"));
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

            _status = UiKit.Text(root, "Status", "Point at a button and pull the trigger.", th.minimumSize, new Vector2(leftX, -Size.y / 2f + 34f),
                                 new Vector2(NumberPadEntry.Width, 36f), color: th.text2, align: TextAlignmentOptions.Left);
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
