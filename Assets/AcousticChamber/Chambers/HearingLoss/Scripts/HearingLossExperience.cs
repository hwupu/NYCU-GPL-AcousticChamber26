using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.HearingLoss;
using GPL.Homing;
using GPL.Input;
using GPL.SpatialSources;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.Chambers.HearingLoss
{
    /// <summary>
    /// The HearingLoss experience chamber (Docs/Design.md, ADR-0006): no Study ID, no session, no log. A custom panel, laid out like
    /// the Hearing Loss Test Panel: hearing condition (both ears / left / right), hearing loss (HLSim presets, GPL.HearingLoss on the
    /// final output), pure tones and recordings, all played from the speaker (click to play, click again to pause). The speaker
    /// stands on the floor 2 m ahead and is dragged with the controller ray (trigger held). Y opens a Keep going / Back to Hall
    /// dialog. Lives on the panel's overlay canvas; English only.
    /// </summary>
    public class HearingLossExperience : MonoBehaviour
    {
        public const string Id = "HearingLoss";
        public const string Title = "C02 · Hearing Loss";
        const string Log = "[Chamber:HearingLoss] ";

        public static readonly Vector2 Size = new Vector2(1100f, 900f);
        public const float StartDistanceM = 2f;

        /// <summary>Pure-tone peak level: -20 dBFS (RMS -23 dBFS, about the -23 LUFS of the recordings at 1 kHz).</summary>
        public const float TonePeakDbfs = -20f;

        [Header("Set by Magic > HearingLoss steps")]
        public FloorDragSource speaker;
        public AudioSource source;
        public AudioClip[] clips = new AudioClip[0];
        public string[] clipNames = new string[0];

        ExperienceState _state;
        OVRCameraRig _rig;
        OutputHearingLoss _output;
        GlobalActions _actions;
        RectTransform _window;
        TextMeshProUGUI _status;
        AudioClip[] _tones;
        readonly List<UiButton> _soundButtons = new List<UiButton>(); // tones, then recordings
        readonly List<UiButton> _earButtons = new List<UiButton>();
        readonly List<UiButton> _levelButtons = new List<UiButton>();
        bool _leaving;

        IEnumerator Start()
        {
            if (speaker == null || source == null || clips == null || clips.Length == 0)
            {
                Debug.LogError(Log + "Speaker, source or clips not linked: run Magic > HearingLoss > Step 2.");
                yield break;
            }
            _state = new ExperienceState(clips.Length);
            _rig = HeadPose.FindRig();
            _output = OutputHearingLoss.Ensure();
            if (_output == null) Debug.LogError(Log + "No AudioListener in the scene: hearing loss is off.");

            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (_actions != null)
            {
                _actions.Use(new BindingTable()); // Stop (Y) only
                _actions.StopEnabled = true;
                _actions.StopRequested += OpenLeaveDialog;
            }

            _tones = new AudioClip[ExperienceState.ToneHz.Length];
            for (int i = 0; i < _tones.Length; i++) _tones[i] = MakeTone(ExperienceState.ToneHz[i]);
            source.loop = true;
            BuildPanel();
            Apply();

            // the speaker waits for a valid head pose, then stands 2 m ahead on the floor (room-fixed from then on)
            float waited = 0f;
            while (_rig != null && HeadPose.NotTrackedYet(_rig) && waited < 5f) { waited += Time.unscaledDeltaTime; yield return null; }
            speaker.center = _rig != null ? _rig.centerEyeAnchor : null;
            ResetSpeaker();
            Debug.Log(Log + "Ready: " + clips.Length + " sounds, speaker " + StartDistanceM + " m ahead.");
        }

        void OnDestroy()
        {
            if (_actions != null)
            {
                _actions.StopRequested -= OpenLeaveDialog;
                _actions.StopEnabled = false;
            }
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
            if (_tones != null) foreach (var t in _tones) if (t != null) Destroy(t);
        }

        /// <summary>
        /// One second of sine at <paramref name="hz"/> (whole cycles, so the loop is seamless), mono, peak <see cref="TonePeakDbfs"/>,
        /// at the output sample rate.
        /// </summary>
        static AudioClip MakeTone(int hz)
        {
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            float amp = Mathf.Pow(10f, TonePeakDbfs / 20f);
            var data = new float[rate];
            for (int i = 0; i < data.Length; i++) data[i] = (float)(amp * System.Math.Sin(2.0 * System.Math.PI * ((long)hz * i % rate) / rate));
            var clip = AudioClip.Create("Tone " + ExperienceState.ToneButton(hz), rate, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---- panel ---------------------------------------------------------------------------------------------------

        void BuildPanel()
        {
            var th = UiKit.Theme;
            ((RectTransform)transform).sizeDelta = Size; // scenes made before the 2026-11-08 layout have the old size
            _window = UiKit.Window(transform);
            float left = -Size.x / 2f + 48f, width = Size.x - 96f, top = Size.y / 2f;

            UiKit.Text(_window, "Title", Title, th.headingSize, new Vector2(0f, top - 60f), new Vector2(width, 70f), bold: true,
                       align: TextAlignmentOptions.Left);

            Section("Hearing condition (loss on)", top - 140f);
            _earButtons.Clear();
            for (int i = 0; i < ExperienceState.Ears.Length; i++)
            {
                var ear = ExperienceState.Ears[i];
                _earButtons.Add(Choice(_window, ExperienceState.EarButton(ear), i, ExperienceState.Ears.Length, top - 205f, () => { _state.SetEar(ear); Apply(); }));
            }

            Section("Hearing loss", top - 285f);
            _levelButtons.Clear();
            for (int i = 0; i < ExperienceState.Levels.Length; i++)
            {
                var level = ExperienceState.Levels[i];
                _levelButtons.Add(Choice(_window, ExperienceState.LevelButton(level), i, ExperienceState.Levels.Length, top - 350f, () => { _state.SetLevel(level); Apply(); }));
            }

            _soundButtons.Clear();
            Section("Pure tone", top - 430f);
            for (int i = 0; i < ExperienceState.ToneHz.Length; i++)
            {
                int k = i;
                _soundButtons.Add(Choice(_window, ExperienceState.ToneButton(ExperienceState.ToneHz[i]), i, ExperienceState.ToneHz.Length, top - 495f, () => ClickSound(k)));
            }

            Section("Audio recording", top - 575f);
            for (int i = 0; i < clips.Length; i++)
            {
                int k = ExperienceState.ToneHz.Length + i;
                string name = i < clipNames.Length && !string.IsNullOrEmpty(clipNames[i]) ? clipNames[i] : clips[i].name;
                _soundButtons.Add(Choice(_window, name, i, clips.Length, top - 640f, () => ClickSound(k)));
            }

            _status = UiKit.Text(_window, "Status", "", th.smallSize, new Vector2(0f, top - 730f), new Vector2(width, 80f), color: th.text2,
                                 align: TextAlignmentOptions.TopLeft);

            UiKit.Button(_window, "Reset Speaker", "Reset speaker", UiStyle.Gray, new Vector2(left + 160f, -top + 55f), new Vector2(320f, 0f), ResetSpeaker);
            UiKit.Button(_window, "Back to Hall", "Back to Hall", UiStyle.Gray, new Vector2(-left - 160f, -top + 55f), new Vector2(320f, 0f), BackToHall);

            void Section(string label, float y) =>
                UiKit.Text(_window, label, label, th.bodySize, new Vector2(0f, y), new Vector2(width, 50f), color: th.text2,
                           align: TextAlignmentOptions.Left);

            // the i-th of n equal buttons across the panel width at height y
            UiButton Choice(Transform parent, string label, int i, int n, float y, System.Action click)
            {
                float gap = 16f, w = (width - gap * (n - 1)) / n;
                var b = UiKit.Button(parent, label, label, UiStyle.Gray, new Vector2(left + w / 2f + i * (w + gap), y), new Vector2(w, 0f), click);
                ((RectTransform)b.transform).sizeDelta = new Vector2(w, ((RectTransform)b.transform).sizeDelta.y);
                return b;
            }
        }

        /// <summary>Click on a tone or recording: a new sound plays from the start; the playing one pauses; a paused one resumes.</summary>
        void ClickSound(int sound)
        {
            switch (_state.ClickSound(sound))
            {
                case SoundAction.PlayFromStart:
                    source.Stop();
                    source.clip = ClipOf(sound);
                    source.Play();
                    break;
                case SoundAction.Pause:
                    source.Pause();
                    break;
                case SoundAction.Resume:
                    source.UnPause();
                    break;
            }
            Apply();
        }

        AudioClip ClipOf(int sound) =>
            ExperienceState.IsTone(sound) ? _tones[sound] : clips[ExperienceState.RecordingIndex(sound)];

        string NameOf(int sound) => sound < 0 ? "nothing" : UiKit.LabelOf(_soundButtons[sound]).text;

        /// <summary>Pushes the state to the output and the panel. The sound playing is white; a paused one is gray again.</summary>
        void Apply()
        {
            if (_output != null) _output.SetProfile(_state.Profile);
            for (int i = 0; i < _soundButtons.Count; i++) _soundButtons[i].Style = i == _state.Sound && _state.Playing ? UiStyle.Prominent : UiStyle.Gray;
            for (int i = 0; i < _earButtons.Count; i++) _earButtons[i].Style = ExperienceState.Ears[i] == _state.Ear ? UiStyle.Prominent : UiStyle.Gray;
            for (int i = 0; i < _levelButtons.Count; i++) _levelButtons[i].Style = ExperienceState.Levels[i] == _state.Level ? UiStyle.Prominent : UiStyle.Gray;
            string playing = _state.Sound < 0 ? "Click a tone or a recording to play it; click it again to pause."
                           : (_state.Playing ? "Playing " : "Paused ") + NameOf(_state.Sound) + ".";
            if (_status != null) _status.text = _state.Status() + "  " + playing + "\nPoint at the speaker and hold the trigger to move it.";
            Debug.Log(Log + (_state.Playing ? "playing " : "paused ") + NameOf(_state.Sound) + "; " + _state.Status());
        }

        void ResetSpeaker()
        {
            if (speaker == null) return;
            speaker.PlaceInFront(_rig != null ? _rig.centerEyeAnchor : null, StartDistanceM);
        }

        // ---- leaving -------------------------------------------------------------------------------------------------

        void OpenLeaveDialog()
        {
            if (_leaving || StopDialog.IsOpen || ModalDialog.Current != null) return;
            if (_actions != null) _actions.Blocked = true;
            Freeze.Begin();
            _frozen = true;
            ModalDialog.Show(transform, "Leave Hearing Loss?", "Go back to the Main Hall, or keep going.",
                new DialogButton("Back to Hall", UiStyle.Borderless, () => { EndFreeze(); BackToHall(); }),
                new DialogButton("Keep going", UiStyle.Prominent, EndFreeze))
                .Abandoned += EndFreeze;
        }

        bool _frozen;

        void EndFreeze()
        {
            if (!_frozen) return;
            _frozen = false;
            Freeze.End();
            if (_actions != null) _actions.Blocked = false;
        }

        void BackToHall()
        {
            if (_leaving || AppNav.Busy) return;
            _leaving = true;
            source.Stop();
            var layer = _window != null ? _window.GetComponent<UiLayer>() : null;
            if (layer != null) layer.Show(false);
            if (!AppNav.BackToHall())
            {
                _leaving = false;
                if (layer != null) layer.Show(true);
            }
        }
    }
}
