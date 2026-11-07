using System;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// <see cref="ITextEntry"/> over the platform keyboard (<see cref="TouchScreenKeyboard"/>; on Quest the system overlay keyboard).
    /// PB-14: in HW2 the keyboard reported "visible" but never appeared. Needed on Quest: "Requires System Keyboard" in the OVR
    /// project config (manifest feature <c>oculus.software.overlay_keyboard</c>). Suspected cause of the HW2 failure: the GameActivity
    /// application entry point (Unity issue "TouchScreenKeyboard.status stuck on Visible with GameActivity"; GameActivity also drops the
    /// input box above the keyboard). <see cref="Diagnosis"/> says what happened, for an on-screen line: a keyboard that is
    /// "visible" while the app never lost input focus was not shown. Not used by any chamber; the number pad stays the default.
    /// </summary>
    public class NativeKeyboardEntry : MonoBehaviour, ITextEntry
    {
        public TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.NumberPad;
        public int maxLength = 4;
        [Tooltip("Seconds after opening without a focus loss before the keyboard counts as 'not shown'.")]
        public float notShownAfterSec = 2f;

        TouchScreenKeyboard _kb;
        string _value = "";
        float _openedAt = -1f;
        bool _focusLostSinceOpen;
        int _focusLosses;

        public string Value => _value;
        public event Action<string> Changed;

        /// <summary>True on platforms with a keyboard API (Android / Quest; false in the Editor and on desktop).</summary>
        public static bool Supported => TouchScreenKeyboard.isSupported;

        public TouchScreenKeyboard.Status? Status => _kb != null ? _kb.status : (TouchScreenKeyboard.Status?)null;

        public void SetValue(string value)
        {
            value = Trim(value ?? "");
            if (_kb != null && _kb.active) _kb.text = value;
            Set(value);
        }

        public void Clear() => SetValue("");

        public void SetVisible(bool visible)
        {
            if (visible) Open();
            else if (_kb != null) { _kb.active = false; _kb = null; }
        }

        /// <summary>Opens the keyboard with the current value. Logs and returns false where there is no keyboard.</summary>
        public bool Open()
        {
            if (!Supported)
            {
                Debug.LogWarning("[GPL] NativeKeyboardEntry: TouchScreenKeyboard is not supported here (Editor / desktop / Link).");
                return false;
            }
            TouchScreenKeyboard.hideInput = false;
            _kb = TouchScreenKeyboard.Open(_value, keyboardType, false, false, false, false, "", maxLength);
            _openedAt = Time.unscaledTime;
            _focusLostSinceOpen = false;
            Debug.Log("[GPL] NativeKeyboardEntry: opened (" + keyboardType + "), status " + (_kb != null ? _kb.status.ToString() : "null"));
            return _kb != null;
        }

        void Update()
        {
            if (_kb == null) return;
            if (_kb.text != null && Trim(_kb.text) != _value) Set(Trim(_kb.text));
            if (_kb.status == TouchScreenKeyboard.Status.Done || _kb.status == TouchScreenKeyboard.Status.Canceled ||
                _kb.status == TouchScreenKeyboard.Status.LostFocus)
            {
                Debug.Log("[GPL] NativeKeyboardEntry: closed (" + _kb.status + "), value '" + _value + "'");
                _kb = null;
            }
        }

        // The system keyboard is an overlay owned by the OS: the app loses input focus while it shows.
        void OnApplicationFocus(bool focus)
        {
            if (focus || _kb == null) return;
            _focusLostSinceOpen = true;
            _focusLosses++;
        }

        /// <summary>
        /// One line for a test panel: supported, status, focus losses, and the verdict "not shown (status Visible, no focus loss)".
        /// </summary>
        public string Diagnosis()
        {
            if (!Supported) return "TouchScreenKeyboard not supported on this platform (Editor, desktop, Quest Link).";
            if (_openedAt < 0f) return "Not opened yet.";
            string s = "status " + (Status.HasValue ? Status.Value.ToString() : "closed") + ", focus losses " + _focusLosses;
            bool waiting = _kb != null && _kb.status == TouchScreenKeyboard.Status.Visible && !_focusLostSinceOpen;
            if (waiting && Time.unscaledTime - _openedAt > notShownAfterSec)
                s += " - NOT SHOWN: reports Visible but the app never lost focus (HW2 symptom; check Requires System Keyboard and the entry point)";
            return s;
        }

        void Set(string v)
        {
            if (v == _value) return;
            _value = v;
            Changed?.Invoke(v);
        }

        string Trim(string v) => maxLength > 0 && v.Length > maxLength ? v.Substring(0, maxLength) : v;

        void OnDisable() => SetVisible(false);
    }
}
