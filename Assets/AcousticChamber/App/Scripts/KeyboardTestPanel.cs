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
            var body = TestPanelKit.Panel(transform, Size, "System keyboard test (PB-14)").Surface.Body;
            var r1 = TestPanelKit.Row(body, "Open");
            TestPanelKit.Wide(r1, "Numbers", "Open number keyboard", UiStyle.Prominent, () => Open(TouchScreenKeyboardType.NumberPad, 4));
            TestPanelKit.Wide(r1, "Text", "Open text keyboard", UiStyle.Gray, () => Open(TouchScreenKeyboardType.Default, 32));
            var r2 = TestPanelKit.Row(body, "Close");
            TestPanelKit.Wide(r2, "Close", "Close keyboard", UiStyle.Gray, () => _entry.SetVisible(false));
            TestPanelKit.Wide(r2, "Clear", "Clear value", UiStyle.Gray, () => _entry.Clear());
            _value = TestPanelKit.Text(body, "Value", "", th.bodySize);
            _diag = TestPanelKit.Text(body, "Diagnosis", "", th.smallSize, th.text2, flexHeight: true);
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
