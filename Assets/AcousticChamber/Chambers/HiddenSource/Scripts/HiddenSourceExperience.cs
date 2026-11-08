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

namespace AcousticChamber.Chambers.HiddenSource
{
    /// <summary>
    /// The HiddenSource experience chamber (Docs/Design.md, ADR-0006): no Study ID, no session, no log file. A custom menu picks the
    /// room, the distance cue and the hearing condition. Start hides the menu and plays a pulse train from a random spot on the floor
    /// (1-6 m, any direction, measured from the head at that moment); the active controller's ray moves a sphere locked to the floor;
    /// A confirms: the true spot and a line to the guess appear, the menu comes back with the azimuth and distance error.
    /// Y during a round cancels it (no result); Y in the menu opens Keep going / Back to Hall. English only.
    /// </summary>
    public class HiddenSourceExperience : MonoBehaviour
    {
        public const string Id = "HiddenSource";
        public const string Title = "C02 · Hidden Source";
        const string Log = "[Chamber:HiddenSource] ";

        public static readonly Vector2 Size = new Vector2(1100f, 900f);
        public const float EmitterHeightM = 0.3f;
        public const float PulseOnS = 2f, PulseOffS = 1f;
        public const float RevealSoundS = 3f;
        public const float GuessMinM = 0.3f, GuessMaxM = 8f;
        public const float GuessRadiusM = 0.125f;

        [Header("Set by Magic > HiddenSource steps")]
        public Transform sourceRoot;
        public AudioSource source;
        public PulseTrain pulse;
        public Transform trueSphere;
        public Transform guessSphere;
        public LineRenderer line;
        public ReverbTuner reverb;

        enum Phase { Menu, Guessing }

        Phase _phase = Phase.Menu;
        RoomChoice _room = RoomChoice.Dry;
        bool _natural = true;
        HearingCondition _condition = HearingCondition.Normal;

        readonly System.Random _rng = new System.Random();
        readonly GuessCatcher _catcher = new GuessCatcher();
        readonly List<UiButton> _roomButtons = new List<UiButton>(), _cueButtons = new List<UiButton>(), _hearingButtons = new List<UiButton>(),
                                _levelButtons = new List<UiButton>();
        OVRCameraRig _rig;
        OutputHearingLoss _output;
        GlobalActions _actions;
        ExperienceExit _exit;
        UiPanel _panel;
        TextMeshProUGUI _status;
        Spot _spot;
        double _headX, _headZ, _yawDeg;
        Vector3 _guess;
        float _soundUntil = -1f;

        void Start()
        {
            if (sourceRoot == null || source == null || pulse == null || trueSphere == null || guessSphere == null || line == null)
            {
                Debug.LogError(Log + "Source, spheres or line not linked: run Magic > HiddenSource > Step 2.");
                return;
            }
            _rig = HeadPose.FindRig();
            _output = OutputHearingLoss.Ensure();
            if (_output == null) Debug.LogError(Log + "No AudioListener in the scene: hearing loss is off.");

            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (_actions != null)
            {
                var table = new BindingTable();
                table.DeclareStandard(InputActions.Confirm); // A
                _actions.Use(table);
            }

            BuildPanel();
            var overlay = GetComponent<OverlayPanel>();
            if (overlay != null) overlay.Movable = true;
            _exit = new ExperienceExit(transform, _panel, _actions, "Hidden Source", Cleanup, InterceptStop);
            HideRound();
            Apply();
            Debug.Log(Log + "Ready.");
        }

        void OnDestroy()
        {
            _exit?.Dispose();
            RayTargets.Unregister(_catcher);
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
        }

        void Cleanup()
        {
            RayTargets.Unregister(_catcher);
            if (pulse != null) pulse.Stop();
            if (_output != null) _output.SetProfile(HearingProfile.Normal);
        }

        // ---- panel ---------------------------------------------------------------------------------------------------

        void BuildPanel()
        {
            var th = UiKit.Theme;
            ((RectTransform)transform).sizeDelta = Size;
            _panel = UiPanel.Create(transform, "Panel", Size);
            var surface = _panel.Surface;
            surface.RootBack = () => _exit?.BackToHall();
            UiNavigationBar.Create(surface, Title, UiNavigationBar.Leading.Back);
            var body = surface.Body;
            body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 12f;

            var intro = UiKit.Text(body, "Intro", "A sound is hidden on the floor around you. Press Start, point to where you hear it with either controller, " +
                                   "and press A. Then see how close you were.", th.smallSize, Vector2.zero, Vector2.zero, color: th.text2,
                                   align: TextAlignmentOptions.TopLeft);
            UiKit.Size(intro.gameObject, prefW: 0f, flexW: 1f);

            var rooms = new List<string>();
            foreach (var r in RoomChoices.All) rooms.Add(RoomChoices.Label(r));
            var roomRow = UiKit.LabeledRow(body, "Room", "Room");
            UiKit.ChoiceGroup(roomRow, "Room", rooms, 0, i => { _room = RoomChoices.All[i]; Apply(); }, out var roomButtons);
            _roomButtons.AddRange(roomButtons);

            var cueRow = UiKit.LabeledRow(body, "Distance", "Distance");
            UiKit.ChoiceGroup(cueRow, "Distance", new[] { "Natural", "Flat" }, 0, i => { _natural = i == 0; Apply(); }, out var cueButtons);
            _cueButtons.AddRange(cueButtons);

            // two groups in one row: the ear (Both, Left, Right) and the level (0, -20, -40 dB)
            var hearing = UiKit.LabeledRow(body, "Hearing", "Hearing");
            var ears = new List<string>();
            foreach (var ear in HearingConditions.Ears) ears.Add(HearingConditions.EarLabel(ear));
            UiKit.ChoiceGroup(hearing, "Ear", ears, 0, i => { _condition = _condition.WithEar(HearingConditions.Ears[i]); Apply(); }, out var earButtons);
            _hearingButtons.AddRange(earButtons);
            var levels = new List<string>();
            foreach (var level in HearingConditions.Levels) levels.Add(HearingConditions.LevelLabel(level));
            UiKit.ChoiceGroup(hearing, "Level", levels, 0, i => { _condition = _condition.WithLevel(HearingConditions.Levels[i]); Apply(); }, out var levelButtons);
            _levelButtons.AddRange(levelButtons);

            _status = UiKit.Text(body, "Status", "No round yet.", th.smallSize, Vector2.zero, Vector2.zero, align: TextAlignmentOptions.TopLeft);
            UiKit.Size(_status.gameObject, prefW: 0f, flexW: 1f);

            var toolbar = surface.AddToolbar();
            UiKit.Button(toolbar, "Start", "Start", UiStyle.Prominent, onClick: StartRound);
        }

        /// <summary>Applies the choices to the sound and the output and shows them on the buttons.</summary>
        void Apply()
        {
            if (_output != null) _output.SetProfile(HearingConditions.Profile(_condition));
            if (reverb != null) reverb.Apply(_room);
            DistanceCue.Apply(source, _natural);
            UiKit.Select(_roomButtons, i => RoomChoices.All[i] == _room);
            UiKit.Select(_cueButtons, i => (i == 0) == _natural);
            UiKit.Select(_hearingButtons, i => HearingConditions.Ears[i] == _condition.Ear);
            UiKit.Select(_levelButtons, i => HearingConditions.Levels[i] == _condition.Level);
        }

        // ---- round ---------------------------------------------------------------------------------------------------

        void StartRound()
        {
            if (_phase != Phase.Menu || _rig == null) return;
            if (HeadPose.NotTrackedYet(_rig))
            {
                _status.text = "Waiting for head tracking. Look around and press Start again.";
                return;
            }
            var head = _rig.centerEyeAnchor;
            _headX = head.position.x;
            _headZ = head.position.z;
            _yawDeg = _rig.transform.eulerAngles.y; // 0 deg = the rig's forward, whatever the head looks at
            _spot = HiddenRound.Draw(_rng);
            HiddenRound.ToWorld(_spot, _headX, _headZ, _yawDeg, out double x, out double z);
            sourceRoot.position = new Vector3((float)x, 0f, (float)z);
            HideRound();
            Apply();
            pulse.Play(PulseOnS, PulseOffS);
            _soundUntil = -1f;

            _phase = Phase.Guessing;
            SetWindow(false);
            _catcher.Center = null;
            RayTargets.Register(_catcher); // keeps the laser on while no UI is shown
            Debug.Log(Log + "Round: source " + _spot.AzimuthDeg.ToString("0") + " deg, " + _spot.DistanceM.ToString("0.0") + " m; room=" + _room +
                      " distance=" + (_natural ? "natural" : "flat") + " hearing=" + HearingConditions.Label(_condition));
        }

        void Update()
        {
            if (_soundUntil >= 0f && Time.time >= _soundUntil)
            {
                _soundUntil = -1f;
                pulse.Stop();
            }
            if (_phase != Phase.Guessing) return;

            if (UiPointer.HasActiveRay)
            {
                var r = UiPointer.ActiveRay;
                // the ray meets the plane at the sphere's centre height, so the laser points at the centre of the sphere
                FloorDrag.FloorPoint(r.origin.x, r.origin.y, r.origin.z, r.direction.x, r.direction.y, r.direction.z, GuessRadiusM, GuessMaxM + 12.0,
                                     out double gx, out double gz);
                FloorDrag.ClampToRing(ref gx, ref gz, _headX, _headZ, GuessMinM, GuessMaxM);
                _guess = new Vector3((float)gx, GuessRadiusM, (float)gz);
                guessSphere.position = _guess;
                _catcher.Center = _guess;
                guessSphere.gameObject.SetActive(true);
            }
            if (_actions != null && guessSphere.gameObject.activeSelf && _actions.Reader.Down(InputActions.Confirm)) Confirm();
        }

        void Confirm()
        {
            var guessSpot = HiddenRound.FromWorld(_guess.x, _guess.z, _headX, _headZ, _yawDeg);
            var result = new RoundResult(_spot, guessSpot);

            trueSphere.position = new Vector3(sourceRoot.position.x, GuessRadiusM, sourceRoot.position.z);
            trueSphere.gameObject.SetActive(true);
            line.positionCount = 2;
            // centre to centre (PO 2026-11-08)
            line.SetPosition(0, trueSphere.position);
            line.SetPosition(1, _guess);
            line.enabled = true;
            _soundUntil = Time.time + RevealSoundS;

            RayTargets.Unregister(_catcher);
            _phase = Phase.Menu;
            _status.text = result.Describe();
            SetWindow(true);
            Debug.Log(Log + "Result: " + result.Describe());
        }

        /// <summary>Y during a round: cancel it and return to the menu (true = the press was used).</summary>
        bool InterceptStop()
        {
            if (_phase != Phase.Guessing) return false;
            RayTargets.Unregister(_catcher);
            pulse.Stop();
            HideRound();
            _phase = Phase.Menu;
            _status.text = "Round cancelled.";
            SetWindow(true);
            Debug.Log(Log + "Round cancelled.");
            return true;
        }

        void HideRound()
        {
            if (trueSphere != null) trueSphere.gameObject.SetActive(false);
            if (guessSphere != null) guessSphere.gameObject.SetActive(false);
            if (line != null) line.enabled = false;
        }

        void SetWindow(bool shown)
        {
            if (_panel != null) _panel.Shown = shown;
        }

        /// <summary>
        /// Being registered keeps the UI pointer's laser on while the menu is hidden; its hit distance ends the laser at the centre of the
        /// guess sphere. It ignores the trigger.
        /// </summary>
        sealed class GuessCatcher : IRayTarget
        {
            public Vector3? Center;

            public bool RayHit(Ray ray, float maxDistance, out float distance)
            {
                distance = Center.HasValue ? Vector3.Dot(Center.Value - ray.origin, ray.direction) : 0f;
                return Center.HasValue && distance > 0f && distance <= maxDistance;
            }
            public void RayHover(bool on) { }
            public void RayPress(Ray ray) { }
            public Vector3 RayDrag(Ray ray) => ray.origin + ray.direction;
            public void RayRelease() { }
        }
    }
}
