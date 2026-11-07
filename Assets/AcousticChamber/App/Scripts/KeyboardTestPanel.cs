using GPL.Input;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Headset test panel for PB-14 (no study logic): opens the Quest system keyboard through <see cref="NativeKeyboardEntry"/> and
    /// shows the value it returns and a live diagnosis (status, focus losses, "not shown"). Build to the headset (the keyboard does not
    /// exist in the Editor or over Link). Test matrix: docs/references/Keyboard.md.
    /// </summary>
    public class KeyboardTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 640f);

        NativeKeyboardEntry _entry;
        TextMeshProUGUI _value, _diag;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (actions != null) actions.Use(new BindingTable());
            _entry = GetComponent<NativeKeyboardEntry>();
            if (_entry == null) _entry = gameObject.AddComponent<NativeKeyboardEntry>();
            _entry.Changed += v => Debug.Log("[GPL] Keyboard test: value '" + v + "'");

            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            var window = UiKit.Window(canvas);
            float width = Size.x - 96f;
            UiKit.Text(window, "Title", "System keyboard test (PB-14)", th.headingSize, new Vector2(0f, Size.y / 2f - 60f), new Vector2(width, 70f),
                       bold: true, align: TextAlignmentOptions.Left);
            UiKit.DebugTag(window);
            UiKit.Button(window, "Numbers", "Open number keyboard", UiStyle.Prominent, new Vector2(-230f, 150f), new Vector2(420f, 0f), () => Open(TouchScreenKeyboardType.NumberPad, 4));
            UiKit.Button(window, "Text", "Open text keyboard", UiStyle.Gray, new Vector2(230f, 150f), new Vector2(420f, 0f), () => Open(TouchScreenKeyboardType.Default, 32));
            UiKit.Button(window, "Close", "Close keyboard", UiStyle.Gray, new Vector2(-230f, 50f), new Vector2(420f, 0f), () => _entry.SetVisible(false));
            UiKit.Button(window, "Clear", "Clear value", UiStyle.Gray, new Vector2(230f, 50f), new Vector2(420f, 0f), () => _entry.Clear());
            _value = UiKit.Text(window, "Value", "", th.bodySize, new Vector2(0f, -60f), new Vector2(width, 60f), align: TextAlignmentOptions.Left);
            _diag = UiKit.Text(window, "Diagnosis", "", th.smallSize, new Vector2(0f, -190f), new Vector2(width, 160f), color: th.text2,
                               align: TextAlignmentOptions.TopLeft);
        }

        void Open(TouchScreenKeyboardType type, int max)
        {
            _entry.keyboardType = type;
            _entry.maxLength = max;
            _entry.Open();
        }

        void Update()
        {
            if (_entry == null || _value == null) return;
            _value.text = "Value: '" + _entry.Value + "'";
            _diag.text = "Supported: " + NativeKeyboardEntry.Supported + "\n" + _entry.Diagnosis();
        }
    }
}
