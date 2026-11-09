using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AcousticChamber.App;
using GPL.HearingLoss;
using GPL.Homing;
using GPL.Input;
using GPL.SpatialSources;
using GPL.Stimuli;
using GPL.UI;
using UnityEngine;

namespace AcousticChamber.Chambers.HearingLoss
{
    /// <summary>
    /// The HearingLoss experience chamber (Docs/Design.md, ADR-0006): no Study ID, no session, no log. A custom panel, laid out like
    /// the Hearing Loss Test Panel: hearing condition (both ears / left / right), hearing loss (HLSim presets, GPL.HearingLoss on the
    /// final output) and the sounds in a 4-column grid (GPL.Stimuli: generated noises and the library recordings; no HW2 clips, PO
    /// 2026-11-10), played from the speaker (click to play, click again to pause). Config (bottom right) opens the hearing loss test
    /// (HearingLossTestView) with its tones played from the speaker (PO 2026-11-10). The speaker stands on the floor 2 m ahead and is dragged with the controller ray (trigger held). Y opens a
    /// Keep going / Back to Hall dialog. Lives on the panel's overlay canvas; English only.
    /// </summary>
    public class HearingLossExperience : MonoBehaviour
    {
        public const string Id = "HearingLoss";
        public const string Title = "C03 · Hearing Loss";
        const string Log = "[Chamber:HearingLoss] ";

        public static readonly Vector2 Size = new Vector2(1100f, 900f);
        public const float StartDistanceM = 2f;

        /// <summary>Library recordings are leveled at load to this RMS (the noises' level; the files stay unchanged; PO 2026-11-10).</summary>
        public const double LibraryLevelDbfs = -26.0;

        /// <summary>Silence after each library recording: looped, the sound repeats once per (length + 1 s) (PO 2026-11-10).</summary>
        public const double LibraryGapS = 1.0;

        /// <summary>Columns of the sound grid (PO 2026-11-10).</summary>
        public const int SoundColumns = 4;

        /// <summary>The generated noises, first in the sound grid (GPL.Stimuli, looped): label, recipe.</summary>
        public static readonly (string label, StimulusRecipe recipe)[] Generated =
        {
            ("White noise", StandardRecipes.WhiteLoop),
            ("Pink noise", StandardRecipes.PinkLoop),
        };

        [Header("Set by Magic > HearingLoss steps")]
        public FloorDragSource speaker;
        public AudioSource source;
        [Tooltip("Second source on the speaker for the Config page's test tones (not looped), so the paused sound keeps its place.")]
        public AudioSource toneSource;
        [Tooltip("GPL.Stimuli library recordings after the generated noises: leveled to -26 dBFS RMS, 1 s gap, looped (may be empty).")]
        public AudioClip[] libraryClips = new AudioClip[0];
        public string[] libraryNames = new string[0];
        public string[] libraryIds = new string[0];

        ExperienceState _state;
        OVRCameraRig _rig;
        OutputHearingLoss _output;
        GlobalActions _actions;
        UiPanel _panel;
        readonly List<AudioClip> _recordings = new List<AudioClip>();     // generated noises, leveled library copies
        readonly Dictionary<int, AudioClip> _testTones = new Dictionary<int, AudioClip>(); // Config page, by Hz (made on first use)
        readonly List<AudioClip> _generated = new List<AudioClip>();      // destroyed on leaving
        readonly List<UiButton> _soundButtons = new List<UiButton>();
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
            for (int i = 0; libraryClips != null && i < libraryClips.Length; i++)
            {
                if (libraryClips[i] == null) continue;
                string label = NameAt(libraryNames, i, libraryClips[i]);
                string id = libraryIds != null && i < libraryIds.Length ? libraryIds[i] : libraryClips[i].name;
                var copy = StimulusClips.Leveled(libraryClips[i], LibraryLevelDbfs, out double gainDb, label, LibraryGapS);
                _generated.Add(copy);
                _recordings.Add(copy);
                Debug.Log(Log + label + ": recording=" + id + " rms_dbfs=" + LibraryLevelDbfs.ToString(CultureInfo.InvariantCulture) +
                          " gain_db=" + gainDb.ToString("0.00", CultureInfo.InvariantCulture) + " gap_s=" + LibraryGapS.ToString(CultureInfo.InvariantCulture));
            }
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
            Debug.Log(Log + "Ready: " + _recordings.Count + " sounds, speaker " + StartDistanceM + " m ahead.");
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
            foreach (var c in _testTones.Values) StimulusClips.Release(c);
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

            // the sounds: a heading above a full-width 4-column grid, so the labels fit (PO 2026-11-10)
            var heading = UiKit.Label(body, "Sounds", "Sounds", th.bodySize, th.text2);
            UiKit.Size(heading.transform.parent.gameObject, prefW: 0f, flexW: 1f);
            var names = SoundNames();
            UiKit.ChoiceGroup(body, "Sound Grid", names, -1, ClickSound, SoundColumns, out var soundButtons);
            _soundButtons.AddRange(soundButtons);
            _soundNames.AddRange(names);

            var toolbar = surface.AddToolbar();
            UiKit.Button(toolbar, "Reset Speaker", "Reset speaker", UiStyle.Gray, onClick: ResetSpeaker);
            var spacer = UiKit.Node(toolbar, "Spacer");
            UiKit.Size(spacer.gameObject, flexW: 1f);
            UiKit.Button(toolbar, "Config", "Config", UiStyle.Gray, iconLeading: "settings-horizontal", onClick: OpenConfig);
            surface.Root.Returned += () => // back from Config: the test tone stops, the chamber's own hearing loss again
            {
                if (toneSource != null) toneSource.Stop();
                Apply();
            };
        }

        /// <summary>
        /// Config: the hearing loss test (the Sandbox test panel's view), its tones from the speaker (PO 2026-11-10). The sound pauses;
        /// the test sets its own loss.
        /// </summary>
        void OpenConfig()
        {
            if (_panel == null || _panel.Surface.Count > 1) return;
            if (_state != null && _state.Playing)
            {
                source.Pause();
                _state.Stop();
            }
            Apply();
            var page = _panel.Surface.Push("Hearing loss test");
            var view = toneSource != null ? HearingLossTestView.Create(page.Body, null, PlayTestTone, () => toneSource.isPlaying)
                                          : HearingLossTestView.Create(page.Body);
            if (toneSource == null) Debug.LogError(Log + "No test tone source on the speaker (run Magic > HearingLoss > Step 2): tones play in the head.");
            view.ResetOnDestroy = false; // Returned re-applies the chamber's profile
            Debug.Log(Log + "Config: hearing loss test opened.");
        }

        /// <summary>
        /// A test tone from the speaker: <paramref name="hz"/> for <paramref name="seconds"/>, peak <paramref name="peakDbfs"/> (RMS 3.01 dB
        /// lower), 10 ms cos² ramps (GPL.Stimuli), on the speaker's second source.
        /// </summary>
        void PlayTestTone(int hz, float peakDbfs, float seconds)
        {
            if (!_testTones.TryGetValue(hz, out var clip))
            {
                var recipe = StimulusRecipe.Tone(hz, seconds, rampS: 0.010, levelDbfs: peakDbfs - 20.0 * System.Math.Log10(System.Math.Sqrt(2.0)));
                _testTones[hz] = clip = StimulusClips.CreateAtOutputRate(recipe, "Test tone " + hz + " Hz");
            }
            toneSource.Stop();
            toneSource.loop = false;
            toneSource.clip = clip;
            toneSource.Play();
            Debug.Log(Log + "Config: test tone " + hz + " Hz from the speaker, peak " + peakDbfs.ToString(CultureInfo.InvariantCulture) + " dBFS, " +
                      seconds.ToString(CultureInfo.InvariantCulture) + " s.");
        }

        /// <summary>Click on a sound: a new sound plays from the start; the playing one pauses; a paused one resumes.</summary>
        void ClickSound(int sound)
        {
            switch (_state.ClickSound(sound))
            {
                case SoundAction.PlayFromStart:
                    source.Stop();
                    source.clip = _recordings[sound];
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

        /// <summary>The labels of the sound grid: the generated noises, then the library recordings (the order of Start).</summary>
        List<string> SoundNames()
        {
            var names = new List<string>();
            foreach (var g in Generated) names.Add(g.label);
            for (int i = 0; libraryClips != null && i < libraryClips.Length; i++)
                if (libraryClips[i] != null) names.Add(NameAt(libraryNames, i, libraryClips[i]));
            return names;
        }

        static string NameAt(string[] names, int i, AudioClip clip) =>
            names != null && i < names.Length && !string.IsNullOrEmpty(names[i]) ? names[i] : clip.name;

        string NameOf(int sound) => sound < 0 ? "nothing" : _soundNames[sound];

        /// <summary>Pushes the state to the output and the panel. The sound playing is Selected; a paused one is normal again.</summary>
        void Apply()
        {
            if (_state == null) return;
            if (_output != null) _output.SetProfile(_state.Profile);
            UiKit.Select(_soundButtons, i => i == _state.Sound && _state.Playing);
            UiKit.Select(_earButtons, i => ExperienceState.Ears[i] == _state.Ear);
            UiKit.Select(_levelButtons, i => ExperienceState.Levels[i] == _state.Level);
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
            if (toneSource != null) toneSource.Stop();
            if (_panel != null) _panel.Shown = false;
            if (!AppNav.BackToHall())
            {
                _leaving = false;
                if (_panel != null) _panel.Shown = true;
            }
        }
    }
}
