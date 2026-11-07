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
    /// The HearingLoss experience chamber (Docs/Design.md, ADR-0006): no Study ID, no session, no log. A custom panel picks the
    /// sound, plays / pauses it, and sets a NIOSH notch preset on both ears, the left or the right ear (GPL.HearingLoss on the final
    /// output). The speaker stands on the floor 2 m ahead and is dragged with the controller ray (trigger held). Y opens a
    /// Keep going / Back to Hall dialog. Lives on the panel's overlay canvas; English only.
    /// </summary>
    public class HearingLossExperience : MonoBehaviour
    {
        public const string Id = "HearingLoss";
        public const string Title = "C02 · Hearing Loss";
        const string Log = "[Chamber:HearingLoss] ";

        public static readonly Vector2 Size = new Vector2(1000f, 860f);
        public const float StartDistanceM = 2f;

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
        UiButton _play;
        TextMeshProUGUI _status;
        readonly List<UiButton> _clipButtons = new List<UiButton>();
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

            source.clip = clips[0];
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
        }

        // ---- panel ---------------------------------------------------------------------------------------------------

        void BuildPanel()
        {
            var th = UiKit.Theme;
            _window = UiKit.Window(transform);
            float left = -Size.x / 2f + 48f, width = Size.x - 96f;

            UiKit.Text(_window, "Title", Title, th.headingSize, new Vector2(0f, 370f), new Vector2(width, 70f), bold: true,
                       align: TextAlignmentOptions.Left);

            Section("Sound", 290f);
            _clipButtons.Clear();
            for (int i = 0; i < clips.Length; i++)
            {
                int k = i;
                string name = i < clipNames.Length && !string.IsNullOrEmpty(clipNames[i]) ? clipNames[i] : clips[i].name;
                _clipButtons.Add(Choice(_window, name, i, clips.Length, 220f, () => SelectClip(k)));
            }
            _play = UiKit.Button(_window, "Play", "Play", UiStyle.Prominent, new Vector2(left + 150f, 130f), new Vector2(300f, 0f), TogglePlay);

            Section("Ear", 40f);
            _earButtons.Clear();
            for (int i = 0; i < ExperienceState.Ears.Length; i++)
            {
                var ear = ExperienceState.Ears[i];
                _earButtons.Add(Choice(_window, ExperienceState.EarButton(ear), i, ExperienceState.Ears.Length, -30f, () => { _state.SetEar(ear); Apply(); }));
            }

            Section("Hearing loss", -120f);
            _levelButtons.Clear();
            for (int i = 0; i < ExperienceState.Levels.Length; i++)
            {
                var level = ExperienceState.Levels[i];
                _levelButtons.Add(Choice(_window, ExperienceState.LevelButton(level), i, ExperienceState.Levels.Length, -190f, () => { _state.SetLevel(level); Apply(); }));
            }

            _status = UiKit.Text(_window, "Status", "", th.smallSize, new Vector2(0f, -280f), new Vector2(width, 80f), color: th.text2,
                                 align: TextAlignmentOptions.TopLeft);

            UiKit.Button(_window, "Reset Speaker", "Reset speaker", UiStyle.Gray, new Vector2(left + 160f, -365f), new Vector2(320f, 0f), ResetSpeaker);
            UiKit.Button(_window, "Back to Hall", "Back to Hall", UiStyle.Gray, new Vector2(-left - 160f, -365f), new Vector2(320f, 0f), BackToHall);

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

        void SelectClip(int index)
        {
            if (!_state.SelectClip(index)) return;
            source.Stop();
            source.clip = clips[index];
            if (_state.Playing) source.Play();
            Apply();
        }

        void TogglePlay()
        {
            _state.TogglePlaying();
            if (_state.Playing)
            {
                if (source.time > 0f && !source.isPlaying) source.UnPause();
                else source.Play();
            }
            else source.Pause();
            Apply();
        }

        /// <summary>Pushes the state to the output and the panel.</summary>
        void Apply()
        {
            if (_output != null) _output.SetProfile(_state.Profile);
            for (int i = 0; i < _clipButtons.Count; i++) _clipButtons[i].Style = i == _state.ClipIndex ? UiStyle.Prominent : UiStyle.Gray;
            for (int i = 0; i < _earButtons.Count; i++) _earButtons[i].Style = ExperienceState.Ears[i] == _state.Ear ? UiStyle.Prominent : UiStyle.Gray;
            for (int i = 0; i < _levelButtons.Count; i++) _levelButtons[i].Style = ExperienceState.Levels[i] == _state.Level ? UiStyle.Prominent : UiStyle.Gray;
            if (_play != null) UiKit.LabelOf(_play).text = _state.Playing ? "Pause" : "Play";
            if (_status != null) _status.text = _state.Status() + "\nPoint at the speaker and hold the trigger to move it.";
            Debug.Log(Log + (_state.Playing ? "playing " : "paused ") + clips[_state.ClipIndex].name + "; " + _state.Status());
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
