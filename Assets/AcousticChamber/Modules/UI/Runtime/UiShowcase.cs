using GPL.Core;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// Headset test panel of GPL.UI (no study logic): title bar, DEBUG badge, Study ID number pad, language buttons,
    /// a localized line, one button per style and a Stop-style dialog. Builds itself on Start under its canvas.
    /// Check: both eyes sharp, ray hits and hover, 中文 glyphs, S0001 formatting, dialog blocks the panel behind it.
    /// </summary>
    public class UiShowcase : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);

        TextMeshProUGUI _status;
        NumberPadEntry _pad;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var root = (RectTransform)transform;
            UiKit.Clear(root);
            root.sizeDelta = Size;

            UiKit.Background(root);
            var bar = UiKit.Box(root, "Title Bar", new Vector2(0f, Size.y / 2f - 8f), new Vector2(Size.x, 16f), th.accent);
            bar.sprite = null;
            UiKit.Text(root, "Title", "GPL.UI test panel", th.titleSize, new Vector2(-Size.x / 2f + 40f + 300f, Size.y / 2f - 70f),
                       new Vector2(600f, 80f), bold: true, align: TextAlignmentOptions.Left);
            UiKit.Badge(root, "DEBUG Badge", "DEBUG", th.warning, new Vector2(150f, 50f)).rectTransform.anchoredPosition =
                new Vector2(-th.gap, -36f);

            // left column: Study ID
            const float leftX = -250f;
            UiKit.Text(root, "Study ID Label", "Study ID", th.smallSize, new Vector2(leftX, 210f), new Vector2(400f, 40f),
                       color: th.textSecondary, align: TextAlignmentOptions.Left);
            var pad = _pad = NumberPadEntry.Create(root, new Vector2(leftX, -50f), StudyId.MaxDigits, StudyId.FromDigits, "S____");
            pad.GetComponent<RectTransform>().localScale = Vector3.one * 0.95f;
            pad.Changed += v => SetStatus(v.Length == 0 ? "Study ID cleared" : "Study ID " + StudyId.FromDigits(v) + " (typed " + v + ")");
            UiKit.Text(root, "Preset Line", "Preset: Default", th.smallSize, new Vector2(leftX, -310f), new Vector2(400f, 40f),
                       color: th.textSecondary, align: TextAlignmentOptions.Left);

            // right column
            const float rightX = 240f, w = 420f;
            float y = 200f;
            UiKit.Button(root, "English", Loc.LabelOf(UiLanguage.English), UiStyle.Neutral, new Vector2(rightX - 110f, y), new Vector2(200f, 80f),
                         () => { Loc.Set(UiLanguage.English); SetStatus("Language: English"); });
            UiKit.Button(root, "Chinese", Loc.LabelOf(UiLanguage.Chinese), UiStyle.Neutral, new Vector2(rightX + 110f, y), new Vector2(200f, 80f),
                         () => { Loc.Set(UiLanguage.Chinese); SetStatus("Language: 中文"); });
            // the two language buttons are narrower than the minimum width on purpose (they sit side by side)
            foreach (var n in new[] { "English", "Chinese" }) ((RectTransform)root.Find(n)).sizeDelta = new Vector2(200f, 80f);

            y -= 85f;
            UiKit.LocText(root, "Localized Line", UiStrings.LookAhead, th.bodySize, new Vector2(rightX, y), new Vector2(w, 60f));
            y -= 90f;
            UiKit.Button(root, "Start", "Start", UiStyle.Primary, new Vector2(rightX, y), new Vector2(w, 80f), () => SetStatus("Start"), bold: true);
            y -= 95f;
            UiKit.Button(root, "Measure", "Debug action", UiStyle.Warning, new Vector2(rightX, y), new Vector2(w, 80f), () => SetStatus("Debug action"));
            y -= 95f;
            UiKit.Button(root, "Stop Dialog", "Stop dialog (Y)", UiStyle.Neutral, new Vector2(rightX, y), new Vector2(w, 80f), OpenStopDialog);
            var disabled = UiKit.Button(root, "Disabled", "Disabled", UiStyle.Danger, new Vector2(rightX, y - 95f), new Vector2(w, 80f));
            disabled.interactable = false;

            _status = UiKit.Text(root, "Status", "Point at a button and pull the trigger.", th.smallSize, new Vector2(0f, -Size.y / 2f + 30f),
                                 new Vector2(Size.x - 80f, 40f), color: th.textSecondary);
        }

        void OpenStopDialog()
        {
            string id = _pad != null && _pad.Value.Length > 0 ? StudyId.FromDigits(_pad.Value) : "";
            StopDialog.Show(transform, false, id, c => SetStatus("Stop dialog: " + c), freeze: false);
        }

        void SetStatus(string s)
        {
            if (_status != null) _status.text = s;
            Debug.Log("[GPL] UiShowcase: " + s);
        }
    }
}
