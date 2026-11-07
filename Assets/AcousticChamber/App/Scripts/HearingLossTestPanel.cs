using System.Collections.Generic;
using System.Globalization;
using GPL.HearingLoss;
using GPL.Input;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Headset test panel for PB-09 (no study logic): the pure-tone check of GPL.HearingLoss. Rows (PO 2026-11-08):
    /// hearing condition (loss on both ears / left / right), hearing loss (0 / -20 / -30 / -40 / -60 / -80 dB), pure tone
    /// (500 Hz, 1, 2, 4, 8 kHz). The tone is mixed into both ears of the final output *before* the hearing-loss filters, so the ear
    /// with the loss sounds quieter than the other. The levels are the NIOSH notch shape, labelled by the 4 kHz band: -20 / -30 / -40
    /// are the Mild / Moderate / Severe presets, -60 / -80 are Severe x 1.5 / x 2 (test panel only, not module presets). The panel
    /// shows per ear the loudest block of the tone before and after the filters, and the filter bank's expected change at that
    /// frequency.
    /// </summary>
    public class HearingLossTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1100f, 760f);
        const float ToneDbfs = -20f, ToneSeconds = 1.5f;

        static readonly Ear[] Ears = { Ear.Both, Ear.Left, Ear.Right };
        static readonly int[] LevelDb = { 0, -20, -30, -40, -60, -80 };
        static readonly int[] ToneHz = { 500, 1000, 2000, 4000, 8000 };

        //                                       125  250  500  1k   1.5k  2k   3k   4k   6k   8k
        static readonly int[] Severe60 = {  0,   0, -15, -30,  -38, -45, -45, -60, -45, -30 }; // Severe x 1.5 (-37.5 -> -38)
        static readonly int[] Severe80 = {  0,   0, -20, -40,  -50, -60, -60, -80, -60, -40 }; // Severe x 2

        OutputHearingLoss _output;
        RectTransform _window;
        TextMeshProUGUI _live, _status;
        readonly List<UiButton> _earButtons = new List<UiButton>(), _levelButtons = new List<UiButton>(), _toneButtons = new List<UiButton>();
        Ear _ear = Ear.Both;
        int _level; // index into LevelDb
        int _toneIndex = -1;
        float _inL, _inR, _outL, _outR;
        string _expL = "", _expR = "";

        /// <summary>The bands of a level: the NIOSH notch whose 4 kHz band is <paramref name="db"/>.</summary>
        public static int[] Bands(int db)
        {
            switch (db)
            {
                case 0: return HearingPresets.None;
                case -20: return HearingPresets.Mild;
                case -30: return HearingPresets.Moderate;
                case -40: return HearingPresets.Severe;
                case -60: return (int[])Severe60.Clone();
                case -80: return (int[])Severe80.Clone();
                default: throw new System.ArgumentOutOfRangeException(nameof(db), db, "Not a level on the test panel.");
            }
        }

        HearingProfile Profile => HearingProfile.For(_ear, Bands(LevelDb[_level]));

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

            _earButtons.Clear(); _levelButtons.Clear(); _toneButtons.Clear();

            Label("Hearing condition (loss on)", 240f);
            for (int i = 0; i < Ears.Length; i++)
            {
                var ear = Ears[i];
                _earButtons.Add(Choice(ear == Ear.Both ? "Both ears" : ear + " ear", i, Ears.Length, 175f, () => { _ear = ear; Apply(); }));
            }

            Label("Hearing loss (NIOSH notch, label = 4 kHz band)", 95f);
            for (int i = 0; i < LevelDb.Length; i++)
            {
                int k = i;
                _levelButtons.Add(Choice(DbLabel(LevelDb[i]), i, LevelDb.Length, 30f, () => { _level = k; Apply(); }));
            }

            Label("Pure tone (both ears, " + ToneDbfs.ToString(CultureInfo.InvariantCulture) + " dBFS peak, " +
                  ToneSeconds.ToString(CultureInfo.InvariantCulture) + " s)", -50f);
            for (int i = 0; i < ToneHz.Length; i++)
            {
                int k = i;
                _toneButtons.Add(Choice(HzLabel(ToneHz[i]), i, ToneHz.Length, -115f, () => PlayTone(k)));
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
            if (_output != null) _output.SetProfile(Profile);
            UpdateExpected();
            for (int i = 0; i < _earButtons.Count; i++) _earButtons[i].Style = Ears[i] == _ear ? UiStyle.Prominent : UiStyle.Gray;
            for (int i = 0; i < _levelButtons.Count; i++) _levelButtons[i].Style = i == _level ? UiStyle.Prominent : UiStyle.Gray;
            for (int i = 0; i < _toneButtons.Count; i++) _toneButtons[i].Style = i == _toneIndex ? UiStyle.Prominent : UiStyle.Gray;
            if (_status != null && _output != null) _status.text = _output.Note();
            Debug.Log("[GPL] HearingLoss test: " + (_output != null ? _output.Note() : "no output"));
        }

        void PlayTone(int index)
        {
            if (_output == null) return;
            _inL = _inR = _outL = _outR = -200f;
            _toneIndex = index;
            _output.PlayTestTone(Ear.Both, ToneHz[index], ToneDbfs, ToneSeconds);
            Apply();
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
            if (_toneIndex < 0) { _live.text = "Press a tone. It plays in both ears through the loss set above."; return; }
            _live.text = HzLabel(ToneHz[_toneIndex]) + " tone" + (_output.TestTonePlaying ? " (playing)" : "") + "\n" +
                         "Left ear   in " + Db(_inL) + "   out " + Db(_outL) + "   change " + Change(_inL, _outL) + "   expected " + _expL + "\n" +
                         "Right ear  in " + Db(_inR) + "   out " + Db(_outR) + "   change " + Change(_inR, _outR) + "   expected " + _expR;
        }

        void UpdateExpected()
        {
            if (_toneIndex < 0) return;
            var p = Profile;
            _expL = Expected(p.Left, ToneHz[_toneIndex]);
            _expR = Expected(p.Right, ToneHz[_toneIndex]);
        }

        // The filter bank's response at the tone frequency (what the measured change should be).
        static string Expected(int[] bands, int hz)
        {
            var bank = new EarFilterBank(AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000);
            bank.SetGains(bands);
            return bank.ResponseDb(hz).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " dB";
        }

        static string DbLabel(int db) => db == 0 ? "0 dB" : db + " dB";
        static string HzLabel(int hz) => hz < 1000 ? hz + " Hz" : (hz / 1000) + " kHz";
        static string Db(float v) => v <= -150f ? "silent" : v.ToString("0.0", CultureInfo.InvariantCulture) + " dBFS";
        static string Change(float a, float b) => a <= -150f ? "-" : (b - a).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " dB";

        void OnDestroy()
        {
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
        }
    }
}
