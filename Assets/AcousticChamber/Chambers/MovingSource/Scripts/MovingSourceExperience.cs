using System.Collections;
using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Acoustics;
using GPL.HearingLoss;
using GPL.Homing;
using GPL.Input;
using GPL.SpatialSources;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.Chambers.MovingSource
{
    /// <summary>
    /// The MovingSource experience chamber (Docs/Design.md, ADR-0006): no Study ID, no session, no log. A source hovers in the room; the
    /// visitor grabs it with the grip (<see cref="GrabSource"/>) or lets it fly a scripted path (<see cref="SourcePath"/>) at a chosen
    /// speed, and hears the direction, distance and echoes change. The panel picks the sound, the room (Dry / Room 0.7 s / Hall 2.2 s),
    /// the distance cue (natural / flat) and the hearing condition (Normal, Left / Right -20 / -40 dB). Y opens Keep going / Back to Hall.
    /// English only.
    /// </summary>
    public class MovingSourceExperience : MonoBehaviour
    {
        public const string Id = "MovingSource";
        public const string Title = "C01 · Moving Source";
        const string Log = "[Chamber:MovingSource] ";

        public static readonly Vector2 Size = new Vector2(1100f, 900f);
        public const float StartDistanceM = 2f, StartHeightM = 1.2f;

        [Header("Set by Magic > MovingSource steps")]
        public Transform sourceRoot;
        public GrabSource grab;
        public AudioSource source;
        public AudioClip[] clips = new AudioClip[0];
        public string[] clipNames = new string[0];
        public ReverbTuner reverb;

        RoomChoice _room = RoomChoice.Dry;
        bool _natural = true;
        HearingCondition _condition = HearingCondition.Normal;
        int _sound = -1;
        bool _playing;
        int _speedIndex = 2;
        readonly SourcePath _path = new SourcePath();
        readonly List<UiButton> _soundButtons = new List<UiButton>(), _roomButtons = new List<UiButton>(), _cueButtons = new List<UiButton>(),
                                _hearingButtons = new List<UiButton>(), _levelButtons = new List<UiButton>(), _pathButtons = new List<UiButton>(), _speedButtons = new List<UiButton>();
        static readonly PathKind[] PathButtons = { PathKind.Circle, PathKind.FlyBy, PathKind.Approach };

        OVRCameraRig _rig;
        OutputHearingLoss _output;
        GlobalActions _actions;
        ExperienceExit _exit;
        RectTransform _window;
        TextMeshProUGUI _status;

        IEnumerator Start()
        {
            if (sourceRoot == null || grab == null || source == null || clips == null || clips.Length == 0)
            {
                Debug.LogError(Log + "Source, grab or clips not linked: run Magic > MovingSource > Step 2.");
                yield break;
            }
            _rig = HeadPose.FindRig();
            _output = OutputHearingLoss.Ensure();
            if (_output == null) Debug.LogError(Log + "No AudioListener in the scene: hearing loss is off.");

            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (_actions != null)
            {
                var table = new BindingTable();
                table.DeclareStandard(InputActions.GripLeft);
                table.DeclareStandard(InputActions.GripRight);
                _actions.Use(table);
            }

            source.loop = true;
            BuildPanel();
            var overlay = GetComponent<OverlayPanel>();
            if (overlay != null) overlay.Movable = true;
            _exit = new ExperienceExit(transform, _window, _actions, "Moving Source", Cleanup);
            grab.Init(_rig, _actions);
            grab.Grabbed += () => { _path.Stop(); Show(); };
            Apply();

            // the source waits for a valid head pose, then hovers 2 m ahead
            float waited = 0f;
            while (_rig != null && HeadPose.NotTrackedYet(_rig) && waited < 5f) { waited += Time.unscaledDeltaTime; yield return null; }
            ResetSource();
            Debug.Log(Log + "Ready: " + clips.Length + " sounds.");
        }

        void OnDestroy()
        {
            _exit?.Dispose();
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
        }

        void Cleanup()
        {
            if (source != null) source.Stop();
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
        }

        void Update()
        {
            if (_path.Kind == PathKind.None || grab == null || grab.Held) return;
            _path.Advance(Time.deltaTime, SourcePath.SpeedSteps[_speedIndex]);
            sourceRoot.position = new Vector3((float)_path.X, (float)_path.Y, (float)_path.Z);
        }

        // ---- panel ---------------------------------------------------------------------------------------------------

        void BuildPanel()
        {
            var th = UiKit.Theme;
            ((RectTransform)transform).sizeDelta = Size;
            _window = UiKit.Window(transform);
            float left = -Size.x / 2f + 48f, width = Size.x - 96f, top = Size.y / 2f;
            const float labelW = 200f;
            float rowLeft = left + labelW + 20f, rowWidth = width - labelW - 20f;

            UiKit.Text(_window, "Title", Title, th.headingSize, new Vector2(0f, top - 60f), new Vector2(width, 70f), bold: true,
                       align: TextAlignmentOptions.Left);

            float y = top - 170f;
            const float step = 95f;

            Row("Sound", ref y);
            for (int i = 0; i < clips.Length; i++)
            {
                int k = i;
                string name = i < clipNames.Length && !string.IsNullOrEmpty(clipNames[i]) ? clipNames[i] : clips[i].name;
                _soundButtons.Add(Choice(name, i, clips.Length, y + step, () => ClickSound(k)));
            }
            Row("Room", ref y);
            for (int i = 0; i < RoomChoices.All.Length; i++)
            {
                var room = RoomChoices.All[i];
                _roomButtons.Add(Choice(RoomChoices.Label(room), i, RoomChoices.All.Length, y + step, () => { _room = room; Apply(); }));
            }
            Row("Distance", ref y);
            string[] cues = { "Natural", "Flat" };
            for (int i = 0; i < cues.Length; i++)
            {
                bool natural = i == 0;
                _cueButtons.Add(Choice(cues[i], i, cues.Length, y + step, () => { _natural = natural; Apply(); }));
            }
            Row("Hearing", ref y);
            // two columns: the ear (Both, Left, Right) and the level (0, -20, -40 dB)
            float colW = (rowWidth - 32f) / 2f;
            for (int i = 0; i < HearingConditions.Ears.Length; i++)
            {
                var ear = HearingConditions.Ears[i];
                _hearingButtons.Add(UiKit.ChoiceButton(_window, HearingConditions.EarLabel(ear), i, HearingConditions.Ears.Length, rowLeft, colW, y + step,
                                                       () => { _condition = _condition.WithEar(ear); Apply(); }));
            }
            for (int i = 0; i < HearingConditions.Levels.Length; i++)
            {
                var level = HearingConditions.Levels[i];
                _levelButtons.Add(UiKit.ChoiceButton(_window, HearingConditions.LevelLabel(level), i, HearingConditions.Levels.Length, rowLeft + colW + 32f, colW,
                                                      y + step, () => { _condition = _condition.WithLevel(level); Apply(); }));
            }
            Row("Path", ref y);
            for (int i = 0; i < PathButtons.Length; i++)
            {
                var kind = PathButtons[i];
                _pathButtons.Add(Choice(PathLabel(kind), i, PathButtons.Length + 1, y + step, () => StartPath(kind)));
            }
            Choice("Stop", PathButtons.Length, PathButtons.Length + 1, y + step, StopPath);
            Row("Speed", ref y);
            for (int i = 0; i < SourcePath.SpeedSteps.Length; i++)
            {
                int k = i;
                _speedButtons.Add(Choice(SourcePath.SpeedSteps[i].ToString("0.##") + " ×", i, SourcePath.SpeedSteps.Length, y + step, () => { _speedIndex = k; Show(); }));
            }

            _status = UiKit.Text(_window, "Status", "", th.smallSize, new Vector2(0f, y - 20f), new Vector2(width, 90f), color: th.text2,
                                 align: TextAlignmentOptions.TopLeft);

            UiKit.Button(_window, "Reset Source", "Reset source", UiStyle.Gray, new Vector2(left + 160f, -top + 55f), new Vector2(320f, 0f), ResetSource);
            UiKit.Button(_window, "Back to Hall", "Back to Hall", UiStyle.Gray, new Vector2(-left - 160f, -top + 55f), new Vector2(320f, 0f), () => _exit.BackToHall());

            // a row label at the current y, then the y of the next row (the row's buttons are at y + step, see the calls above)
            void Row(string label, ref float rowY)
            {
                UiKit.Text(_window, label, label, th.bodySize, new Vector2(left + labelW / 2f, rowY), new Vector2(labelW, 50f), color: th.text2,
                           align: TextAlignmentOptions.Left);
                rowY -= step;
            }

            UiButton Choice(string label, int i, int n, float yy, System.Action click) =>
                UiKit.ChoiceButton(_window, label, i, n, rowLeft, rowWidth, yy, click);
        }

        static string PathLabel(PathKind kind) => kind == PathKind.Circle ? "Circle" : kind == PathKind.FlyBy ? "Fly-by" : "Approach";

        /// <summary>Applies the room, distance cue and hearing condition and shows every choice on its buttons.</summary>
        void Apply()
        {
            if (_output != null) _output.SetProfile(HearingConditions.Profile(_condition));
            if (reverb != null) reverb.Apply(_room);
            DistanceCue.Apply(source, _natural);
            UiKit.Select(_roomButtons, i => RoomChoices.All[i] == _room);
            UiKit.Select(_cueButtons, i => (i == 0) == _natural);
            UiKit.Select(_hearingButtons, i => HearingConditions.Ears[i] == _condition.Ear);
            UiKit.Select(_levelButtons, i => HearingConditions.Levels[i] == _condition.Level);
            Show();
        }

        void Show()
        {
            UiKit.Select(_soundButtons, i => i == _sound && _playing);
            UiKit.Select(_pathButtons, i => PathButtons[i] == _path.Kind);
            UiKit.Select(_speedButtons, i => i == _speedIndex);
            if (_status == null) return;
            string hearing = HearingConditions.Label(_condition);
            string move = _path.Kind != PathKind.None ? PathLabel(_path.Kind) + " path at " + SourcePath.SpeedSteps[_speedIndex].ToString("0.##") + " ×"
                        : grab != null && grab.Held ? "Holding the source."
                        : "Point at the source and press the grip to move it.";
            _status.text = (_sound < 0 ? "Click a sound to play it; click it again to pause." : (_playing ? "Playing " : "Paused ") + SoundName(_sound) + ".") +
                           "\n" + move + "  Hearing: " + hearing + ".";
        }

        string SoundName(int i) => i < clipNames.Length && !string.IsNullOrEmpty(clipNames[i]) ? clipNames[i] : clips[i].name;

        void ClickSound(int index)
        {
            if (index != _sound)
            {
                _sound = index;
                source.Stop();
                source.clip = clips[index];
                source.Play();
                _playing = true;
            }
            else if (_playing) { source.Pause(); _playing = false; }
            else { source.UnPause(); _playing = true; }
            Show();
            Debug.Log(Log + (_playing ? "playing " : "paused ") + SoundName(_sound));
        }

        // ---- movement ------------------------------------------------------------------------------------------------

        void StartPath(PathKind kind)
        {
            if (_rig == null || grab.Held) return;
            var head = _rig.centerEyeAnchor;
            Vector3 p = sourceRoot.position;
            // the 0 deg direction is the rig's forward (as the panels use), not where the head looks: the panel is to the left
            _path.Start(kind, head.position.x, head.position.z, _rig.transform.eulerAngles.y, p.x, Mathf.Max(p.y, 0.3f), p.z);
            Show();
            Debug.Log(Log + "path " + kind + " at " + SourcePath.SpeedSteps[_speedIndex] + " x");
        }

        void StopPath()
        {
            _path.Stop();
            Show();
        }

        /// <summary>Back to 2 m straight ahead of the head (its yaw only), 1.2 m high; ends a path.</summary>
        void ResetSource()
        {
            _path.Stop();
            Vector3 origin = _rig != null ? _rig.centerEyeAnchor.position : Vector3.zero;
            // straight ahead of the rig (0 deg az), not where the head looks: the visitor often looks at the panel on the left
            Vector3 fwd = _rig != null ? Quaternion.Euler(0f, _rig.transform.eulerAngles.y, 0f) * Vector3.forward : Vector3.forward;
            Vector3 p = origin + fwd * StartDistanceM;
            sourceRoot.position = new Vector3(p.x, StartHeightM, p.z);
            Show();
        }
    }
}
