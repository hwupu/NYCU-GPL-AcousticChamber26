using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.HearingLoss;
using GPL.Homing;
using GPL.Input;
using GPL.SpatialSources;
using GPL.Stimuli;
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
        public const string Title = "C03 · Hearing Loss";
        const string Log = "[Chamber:HearingLoss] ";

        public static readonly Vector2 Size = new Vector2(1100f, 900f);
        public const float StartDistanceM = 2f;

        /// <summary>Pure-tone peak level: -20 dBFS (RMS -23 dBFS, about the -23 LUFS of the recordings at 1 kHz).</summary>
        public const float TonePeakDbfs = -20f;

        /// <summary>The generated noises, first in the "Audio recording" row (GPL.Stimuli, looped): label, recipe.</summary>
        public static readonly (string label, StimulusRecipe recipe)[] Generated =
        {
            ("White noise", StandardRecipes.WhiteLoop),
            ("Pink noise", StandardRecipes.PinkLoop),
        };

        /// <summary>
        /// One second of sine at <paramref name="hz"/> (whole cycles, so the loop is seamless; no ramps), peak <see cref="TonePeakDbfs"/>
        /// (RMS 3.01 dB lower).
        /// </summary>
        public static StimulusRecipe ToneRecipe(int hz) =>
            StimulusRecipe.Tone(hz, 1.0, rampS: 0, levelDbfs: TonePeakDbfs - 20.0 * System.Math.Log10(System.Math.Sqrt(2.0)));

        [Header("Set by Magic > HearingLoss steps")]
        public FloorDragSource speaker;
        public AudioSource source;
        [Tooltip("Recordings after the generated noises (may be empty).")]
        public AudioClip[] clips = new AudioClip[0];
        public string[] clipNames = new string[0];

        ExperienceState _state;
        OVRCameraRig _rig;
        OutputHearingLoss _output;
        GlobalActions _actions;
        UiPanel _panel;
        TextMeshProUGUI _status;
        AudioClip[] _tones;
        readonly List<AudioClip> _recordings = new List<AudioClip>();     // generated noises, then the recording assets
        readonly List<AudioClip> _generated = new List<AudioClip>();      // destroyed on leaving
        readonly List<UiButton> _soundButtons = new List<UiButton>(); // tones, then recordings
        readonly List<string> _soundNames = new List<string>();
        readonly List<UiButton> _earButtons = new List<UiButton>();
        readonly List<UiButton> _levelButtons = new List<UiButton>();
        bool _leaving;

        IEnumerator Start()
        {
            if (speaker == null || source == null)
            {
                Debug.LogError(Log + "Speaker or source not linked: run Magic > HearingLoss > Step 2.");
                yield break;
            }
            foreach (var g in Generated)
            {
                var clip = StimulusClips.CreateAtOutputRate(g.recipe, g.label);
                _generated.Add(clip);
                _recordings.Add(clip);
            }
            if (clips != null) foreach (var c in clips) if (c != null) _recordings.Add(c);
            _state = new ExperienceState(_recordings.Count);
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
            for (int i = 0; i < _tones.Length; i++)
                _generated.Add(_tones[i] = StimulusClips.CreateAtOutputRate(ToneRecipe(ExperienceState.ToneHz[i]),
                                                                            "Tone " + ExperienceState.ToneButton(ExperienceState.ToneHz[i])));
            source.loop = true;
            BuildPanel();
            var overlay = GetComponent<OverlayPanel>();
            if (overlay != null) overlay.Movable = true;
            Apply();

            // the speaker waits for a valid head pose, then stands 2 m ahead on the floor (room-fixed from then on)
            float waited = 0f;
            while (_rig != null && HeadPose.NotTrackedYet(_rig) && waited < 5f) { waited += Time.unscaledDeltaTime; yield return null; }
            speaker.center = _rig != null ? _rig.centerEyeAnchor : null;
            ResetSpeaker();
            Debug.Log(Log + "Ready: " + _tones.Length + " tones, " + _recordings.Count + " recordings, speaker " + StartDistanceM + " m ahead.");
        }

        void OnDestroy()
        {
            if (_actions != null)
            {
                _actions.StopRequested -= OpenLeaveDialog;
                _actions.StopEnabled = false;
            }
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
            foreach (var c in _generated) StimulusClips.Release(c);
        }

        // ---- panel ---------------------------------------------------------------------------------------------------

        void BuildPanel()
        {
            var th = UiKit.Theme;
            ((RectTransform)transform).sizeDelta = Size; // scenes made before the 2026-11-08 layout have the old size
            _panel = UiPanel.Create(transform, "Panel", Size);
            var surface = _panel.Surface;
            surface.RootBack = BackToHall;
            UiNavigationBar.Create(surface, Title, UiNavigationBar.Leading.Back);
            var body = surface.Body;
            body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 12f;
            _earButtons.Clear();
            _levelButtons.Clear();
            _soundButtons.Clear();
            _soundNames.Clear();

            var ears = new List<string>();
            foreach (var ear in ExperienceState.Ears) ears.Add(ExperienceState.EarButton(ear));
            var earRow = UiKit.LabeledRow(body, "Hearing condition", "Ear", 160f);
            UiKit.ChoiceGroup(earRow, "Ear", ears, 0, i => { _state.SetEar(ExperienceState.Ears[i]); Apply(); }, out var earButtons);
            _earButtons.AddRange(earButtons);

            var levels = new List<string>();
            foreach (var level in ExperienceState.Levels) levels.Add(ExperienceState.LevelButton(level));
            var levelRow = UiKit.LabeledRow(body, "Hearing loss", "Loss", 160f);
            UiKit.ChoiceGroup(levelRow, "Level", levels, 0, i => { _state.SetLevel(ExperienceState.Levels[i]); Apply(); }, out var levelButtons);
            _levelButtons.AddRange(levelButtons);

            var tones = new List<string>();
            foreach (var hz in ExperienceState.ToneHz) tones.Add(ExperienceState.ToneButton(hz));
            var toneRow = UiKit.LabeledRow(body, "Pure tone", "Tone", 160f);
            UiKit.ChoiceGroup(toneRow, "Tone", tones, -1, i => ClickSound(i), out var toneButtons);
            _soundButtons.AddRange(toneButtons);
            _soundNames.AddRange(tones);

            var recs = RecordingNames();
            var recRow = UiKit.LabeledRow(body, "Audio recording", "Recording", 160f);
            UiKit.ChoiceGroup(recRow, "Recording", recs, -1, i => ClickSound(ExperienceState.ToneHz.Length + i), out var recButtons);
            _soundButtons.AddRange(recButtons);
            _soundNames.AddRange(recs);

            _status = UiKit.Text(body, "Status", "", th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
            UiKit.Size(_status.gameObject, prefW: 0f, flexW: 1f);

            var toolbar = surface.AddToolbar();
            UiKit.Button(toolbar, "Reset Speaker", "Reset speaker", UiStyle.Gray, onClick: ResetSpeaker);
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
            ExperienceState.IsTone(sound) ? _tones[sound] : _recordings[ExperienceState.RecordingIndex(sound)];

        /// <summary>The labels of the "Audio recording" row: the generated noises, then the recordings (same order as the clips built in Start).</summary>
        List<string> RecordingNames()
        {
            var names = new List<string>();
            foreach (var g in Generated) names.Add(g.label);
            for (int i = 0; i < (clips != null ? clips.Length : 0); i++)
                if (clips[i] != null) names.Add(i < clipNames.Length && !string.IsNullOrEmpty(clipNames[i]) ? clipNames[i] : clips[i].name);
            return names;
        }

        string NameOf(int sound) => sound < 0 ? "nothing" : _soundNames[sound];

        /// <summary>Pushes the state to the output and the panel. The sound playing is Selected; a paused one is normal again.</summary>
        void Apply()
        {
            if (_output != null) _output.SetProfile(_state.Profile);
            UiKit.Select(_soundButtons, i => i == _state.Sound && _state.Playing);
            UiKit.Select(_earButtons, i => ExperienceState.Ears[i] == _state.Ear);
            UiKit.Select(_levelButtons, i => ExperienceState.Levels[i] == _state.Level);
            string playing = _state.Sound < 0 ? "Click a tone or a recording to play it; click it again to pause."
                           : (_state.Playing ? "Playing " : "Paused ") + NameOf(_state.Sound) + ".";
            if (_status != null) _status.text = _state.Status() + "  " + playing + "\nPoint at the speaker and hold the trigger to move it.";
            Debug.Log(Log + (_state.Playing ? "playing " : "paused ") + NameOf(_state.Sound) + "; " + _state.Status());
        }

        void ResetSpeaker()
        {
            if (speaker == null) return;
            // 0 deg az = the rig's forward; the panel is to the left, so the head often looks away from it
            if (_rig != null) speaker.PlaceAhead(_rig.centerEyeAnchor.position, _rig.transform.eulerAngles.y, StartDistanceM);
            else speaker.PlaceInFront(null, StartDistanceM);
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
            if (_panel != null) _panel.Shown = false;
            if (!AppNav.BackToHall())
            {
                _leaving = false;
                if (_panel != null) _panel.Shown = true;
            }
        }
    }
}
