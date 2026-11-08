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
    /// with the loss sounds quieter than the other. The levels are the HLSim noise-notch shape, labelled by the 4 kHz band: -20 / -30 / -40
    /// are the Mild / Moderate / Severe presets, -60 / -80 are Severe x 1.5 / x 2 (TestPanelLevels; docs/references/HearingLossLevels.md). The panel
    /// shows per ear the loudest block of the tone before and after the filters, and the filter bank's expected change at that
    /// frequency.
    /// </summary>
    public class HearingLossTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1100f, 760f);
        const float ToneDbfs = -20f, ToneSeconds = 1.5f;

        static readonly Ear[] Ears = { Ear.Both, Ear.Left, Ear.Right };
        static int[] LevelDb => TestPanelLevels.LevelDb;
        static int[] ToneHz => TestPanelLevels.ToneHz;

        OutputHearingLoss _output;
        TextMeshProUGUI _live, _status;
        readonly List<UiButton> _earButtons = new List<UiButton>(), _levelButtons = new List<UiButton>(), _toneButtons = new List<UiButton>();
        Ear _ear = Ear.Both;
        int _level; // index into LevelDb
        int _toneIndex = -1;
        float _inL, _inR, _outL, _outR;
        string _expL = "", _expR = "";

        HearingProfile Profile => HearingProfile.For(_ear, TestPanelLevels.Bands(LevelDb[_level]));

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            _output = OutputHearingLoss.Ensure();
            var actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (actions != null) actions.Use(new BindingTable());

            var th = UiKit.Theme;
            var panel = TestPanelKit.Panel(transform, Size, "GPL.HearingLoss test panel");
            var body = panel.Surface.Body;
            var statusLabel = panel.Surface.AddStatus("");
            _status = statusLabel;

            _earButtons.Clear(); _levelButtons.Clear(); _toneButtons.Clear();

            TestPanelKit.Text(body, "Hearing condition", "Hearing condition (loss on)", th.smallSize, th.text2);
            var ears = new List<string>();
            foreach (var ear in Ears) ears.Add(ear == Ear.Both ? "Both ears" : ear + " ear");
            UiKit.ChoiceGroup(body, "Ear", ears, 0, i => { _ear = Ears[i]; Apply(); }, out var earButtons);
            _earButtons.AddRange(earButtons);

            TestPanelKit.Text(body, "Hearing loss", "Hearing loss (NIOSH notch, label = 4 kHz band)", th.smallSize, th.text2);
            var levels = new List<string>();
            foreach (int db in LevelDb) levels.Add(DbLabel(db));
            UiKit.ChoiceGroup(body, "Level", levels, 0, i => { _level = i; Apply(); }, out var levelButtons);
            _levelButtons.AddRange(levelButtons);

            TestPanelKit.Text(body, "Pure tone", "Pure tone (both ears, " + ToneDbfs.ToString(CultureInfo.InvariantCulture) + " dBFS peak, " +
                              ToneSeconds.ToString(CultureInfo.InvariantCulture) + " s)", th.smallSize, th.text2);
            var tones = new List<string>();
            foreach (int hz in ToneHz) tones.Add(HzLabel(hz));
            UiKit.ChoiceGroup(body, "Tone", tones, -1, i => PlayTone(i), out var toneButtons);
            _toneButtons.AddRange(toneButtons);

            _live = TestPanelKit.Text(body, "Live", "", th.smallSize, th.text, flexHeight: true);
            if (_output == null) _status.text = "No AudioListener in the scene.";
            Apply();
        }

        void Apply()
        {
            if (_output != null) _output.SetProfile(Profile);
            UpdateExpected();
            UiKit.Select(_earButtons, i => Ears[i] == _ear);
            UiKit.Select(_levelButtons, i => i == _level);
            UiKit.Select(_toneButtons, i => i == _toneIndex);
            if (_status != null && _output != null) _status.text = _output.Note();
            Debug.Log("[GPL] HearingLoss test: " + DbLabel(LevelDb[_level]) + " = " + TestPanelLevels.Source(LevelDb[_level]) + " on " + _ear +
                      "; " + (_output != null ? _output.Note() : "no output"));
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
