using System.Collections.Generic;
using GPL.HearingLoss;
using GPL.Input;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Headset test panel for PB-09 (no study logic): the per-ear test-tone check of GPL.HearingLoss. A sine tone is mixed into the
    /// left or the right channel of the final output *before* the hearing-loss filters; a preset is applied to both ears, the left or
    /// the right ear. Listen: the tone must be in the ear the button names, and quieter only when that ear has the loss. The panel
    /// shows the RMS before and after the filters per ear (the loudest block of the tone), e.g. a 4 kHz tone with -20 dB on the left:
    /// left in -23.0, out -43.0 dBFS; right silent.
    /// </summary>
    public class HearingLossTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 760f);
        const float ToneDbfs = -20f, ToneSeconds = 1.5f;

        static readonly HearingLossLevel[] Levels = { HearingLossLevel.None, HearingLossLevel.Mild, HearingLossLevel.Moderate, HearingLossLevel.Severe };
        static readonly Ear[] Ears = { Ear.Both, Ear.Left, Ear.Right };

        OutputHearingLoss _output;
        RectTransform _window;
        TextMeshProUGUI _live, _status;
        readonly List<UiButton> _earButtons = new List<UiButton>(), _levelButtons = new List<UiButton>();
        Ear _ear = Ear.Left;
        HearingLossLevel _level = HearingLossLevel.None;
        float _inL, _inR, _outL, _outR;
        string _tone = "";

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            _output = OutputHearingLoss.Ensure();
            var actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (actions != null) actions.Use(new BindingTable());

            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            _window = UiKit.Window(canvas);
            float left = -Size.x / 2f + 48f, width = Size.x - 96f;
            UiKit.Text(_window, "Title", "GPL.HearingLoss test panel", th.headingSize, new Vector2(0f, Size.y / 2f - 60f), new Vector2(width, 70f),
                       bold: true, align: TextAlignmentOptions.Left);
            UiKit.DebugTag(_window);

            Label("Tone (" + ToneDbfs + " dBFS peak, " + ToneSeconds + " s)", 240f);
            string[] tones = { "Left 1 kHz", "Right 1 kHz", "Left 4 kHz", "Right 4 kHz" };
            for (int i = 0; i < tones.Length; i++)
            {
                var ear = i % 2 == 0 ? Ear.Left : Ear.Right;
                float hz = i < 2 ? 1000f : 4000f;
                string name = tones[i];
                Choice(name, i, tones.Length, 175f, () => PlayTone(ear, hz, name));
            }

            Label("Loss on", 95f);
            for (int i = 0; i < Ears.Length; i++)
            {
                var ear = Ears[i];
                _earButtons.Add(Choice(ear == Ear.Both ? "Both ears" : ear + " ear", i, Ears.Length, 30f, () => { _ear = ear; Apply(); }));
            }

            Label("Level (NIOSH presets; deepest band at 4 kHz)", -50f);
            for (int i = 0; i < Levels.Length; i++)
            {
                var level = Levels[i];
                string label = level == HearingLossLevel.None ? "Normal" : (level == HearingLossLevel.Mild ? "-20 dB" : level == HearingLossLevel.Moderate ? "-30 dB" : "-40 dB");
                _levelButtons.Add(Choice(label, i, Levels.Length, -115f, () => { _level = level; Apply(); }));
            }

            _live = UiKit.Text(_window, "Live", "", th.smallSize, new Vector2(0f, -240f), new Vector2(width, 130f), align: TextAlignmentOptions.TopLeft);
            _status = UiKit.Text(_window, "Status", "", th.minimumSize, new Vector2(0f, -Size.y / 2f + 40f), new Vector2(width, 40f), color: th.text2,
                                 align: TextAlignmentOptions.Left);
            if (_output == null) _status.text = "No AudioListener in the scene.";
            Apply();

            void Label(string text, float y) =>
                UiKit.Text(_window, text, text, th.smallSize, new Vector2(0f, y), new Vector2(width, 40f), color: th.text2, align: TextAlignmentOptions.Left);

            UiButton Choice(string label, int i, int n, float y, System.Action click)
            {
                float gap = 16f, w = (width - gap * (n - 1)) / n;
                var b = UiKit.Button(_window, label, label, UiStyle.Gray, new Vector2(left + w / 2f + i * (w + gap), y), new Vector2(w, 0f), click);
                ((RectTransform)b.transform).sizeDelta = new Vector2(w, ((RectTransform)b.transform).sizeDelta.y);
                return b;
            }
        }

        void Apply()
        {
            if (_output != null) _output.SetProfile(HearingProfile.For(_ear, _level));
            for (int i = 0; i < _earButtons.Count; i++) _earButtons[i].Style = Ears[i] == _ear ? UiStyle.Prominent : UiStyle.Gray;
            for (int i = 0; i < _levelButtons.Count; i++) _levelButtons[i].Style = Levels[i] == _level ? UiStyle.Prominent : UiStyle.Gray;
            if (_status != null && _output != null) _status.text = _output.Note();
            Debug.Log("[GPL] HearingLoss test: " + (_output != null ? _output.Note() : "no output"));
        }

        void PlayTone(Ear ear, float hz, string name)
        {
            if (_output == null) return;
            _inL = _inR = _outL = _outR = -200f;
            _tone = name;
            _output.PlayTestTone(ear, hz, ToneDbfs, ToneSeconds);
        }

        void Update()
        {
            if (_output == null || _live == null) return;
            if (_output.TestTonePlaying)
            {
                _inL = Mathf.Max(_inL, _output.InputRmsDbfs(Ear.Left));
                _inR = Mathf.Max(_inR, _output.InputRmsDbfs(Ear.Right));
                _outL = Mathf.Max(_outL, _output.OutputRmsDbfs(Ear.Left));
                _outR = Mathf.Max(_outR, _output.OutputRmsDbfs(Ear.Right));
            }
            if (_tone.Length == 0) { _live.text = "Press a tone button. The tone goes through the loss set below."; return; }
            _live.text = _tone + (_output.TestTonePlaying ? " (playing)" : "") + "\n" +
                         "Left ear   in " + Db(_inL) + "   out " + Db(_outL) + "   change " + Change(_inL, _outL) + "\n" +
                         "Right ear  in " + Db(_inR) + "   out " + Db(_outR) + "   change " + Change(_inR, _outR);
        }

        static string Db(float v) => v <= -150f ? "silent" : v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dBFS";
        static string Change(float a, float b) => a <= -150f ? "-" : (b - a).ToString("+0.0;-0.0;0.0", System.Globalization.CultureInfo.InvariantCulture) + " dB";

        void OnDestroy()
        {
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
        }
    }
}
