using System.Collections.Generic;
using AcousticChamber.App;
using GPL.Acoustics;
using GPL.Core;
using GPL.Input;
using GPL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace AcousticChamber.Chambers.Reverberation
{
    /// <summary>
    /// C04 · Reverberation (Docs/Design.md, PB-27): the RT60 calibration tool, built as an experience chamber (ADR-0006): no Study
    /// ID, no session, no log; it writes RT60 reports only. Open space; the shoebox is locked to the listener and has the size set on
    /// the panel (presets of the chambers' rooms, or W / D / H steppers). A profile sets the targets Measure RT60 checks and the
    /// sweep range of Calibrate. Both run in the RT60 run dialog (App <see cref="Rt60RunDialog"/>). A calibration is imported in the
    /// Editor (Magic > Acoustics > Import RT60 Calibration); then the room counts as calibrated in every chamber of that size.
    /// Y opens Keep going / Back to Hall. English only.
    /// </summary>
    public class ReverberationExperience : MonoBehaviour
    {
        public const string Id = "Reverberation";
        public const string Title = "C04 · Reverberation";
        const string Log = "[Chamber:Reverberation] ";

        public static readonly Vector2 Size = new Vector2(1100f, 1040f);

        [Header("Set by Magic > Reverberation steps")]
        public ReverbTuner reverb;
        [Tooltip("Mixer group with the Meta XR Audio Reflection effect: the RT60 probe is routed to it.")]
        public AudioMixerGroup output;

        RoomSize _size = RoomSizes.Presets[0].size;
        int _profile;

        GlobalActions _actions;
        ExperienceExit _exit;
        UiPanel _panel;
        UiStepper _width, _depth, _height;
        TextMeshProUGUI _profileLine;
        readonly List<UiButton> _roomButtons = new List<UiButton>(), _profileButtons = new List<UiButton>();
        Rt60RunDialog _run;

        Rt60Profile Profile => Rt60Profiles.All[_profile];
        bool Measuring => _run != null && _run.Running;

        void Start()
        {
            if (reverb == null || !reverb.HasRoom || output == null)
                Debug.LogError(Log + "Reverb, shoebox or mixer group not linked: run Magic > Reverberation > Step 2.");
            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            BuildPanel();
            var overlay = GetComponent<OverlayPanel>();
            if (overlay != null) overlay.Movable = true;
            _exit = new ExperienceExit(transform, _panel, _actions, "Reverberation", StopRun);
            SetSize(_size);
            if (reverb != null) reverb.SetDry(); // nothing sounds here; the runner sets each condition
            Debug.Log(Log + "Ready.");
        }

        void OnDestroy()
        {
            StopRun();
            _exit?.Dispose();
        }

        void StopRun()
        {
            if (_run == null) return;
            _run.Stop();
            _run = null;
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

            SmallText(body, "Intro", "Calibrate the reverberation of a room size on this headset. Stay still and quiet while it runs.", th.text2);

            var room = UiKit.LabeledRow(body, "Room", "Room", LabelWidth);
            UiKit.ChoiceGroup(room, "Room", RoomSizes.Labels(), 0, ClickRoom, out var roomButtons);
            _roomButtons.AddRange(roomButtons);

            _width = Stepper(body, "Width", _size.Width, RoomSizes.MinWidth, RoomSizes.MaxWidth, v => SetSize(new RoomSize(v, _size.Depth, _size.Height)));
            _depth = Stepper(body, "Depth", _size.Depth, RoomSizes.MinDepth, RoomSizes.MaxDepth, v => SetSize(new RoomSize(_size.Width, v, _size.Height)));
            _height = Stepper(body, "Height", _size.Height, RoomSizes.MinHeight, RoomSizes.MaxHeight, v => SetSize(new RoomSize(_size.Width, _size.Depth, v)));

            var profile = UiKit.LabeledRow(body, "Profile", "Profile", LabelWidth);
            var names = new List<string>();
            foreach (var p in Rt60Profiles.All) names.Add(p.Name);
            UiKit.ChoiceGroup(profile, "Profile", names, _profile, i => { _profile = i; ShowState(); }, out var profileButtons);
            _profileButtons.AddRange(profileButtons);

            var line = UiKit.LabeledRow(body, "Profile Line", "", LabelWidth);
            _profileLine = SmallText(line, "Text", "", th.text2);

            SmallText(body, "Import Hint", "After Calibrate: copy the report from Android/data/…/files/Calibration to the PC and import it in the Editor " +
                                           "(Magic > Acoustics > Import RT60 Calibration).", th.text2);

            var toolbar = surface.AddToolbar();
            UiKit.Button(toolbar, "Measure RT60", "Measure RT60", UiStyle.Gray, onClick: Measure);
            var spacer = UiKit.Node(toolbar, "Spacer");
            UiKit.Size(spacer.gameObject, flexW: 1f);
            UiKit.Button(toolbar, "Calibrate", "Calibrate", UiStyle.Prominent, onClick: Calibrate);
        }

        const float LabelWidth = 160f;

        static TextMeshProUGUI SmallText(Transform parent, string name, string text, Color color)
        {
            var t = UiKit.Text(parent, name, text, UiKit.Theme.smallSize, Vector2.zero, Vector2.zero, color: color, align: TextAlignmentOptions.TopLeft);
            UiKit.Size(t.gameObject, prefW: 0f, flexW: 1f);
            return t;
        }

        static UiStepper Stepper(RectTransform body, string label, double value, double min, double max, System.Action<float> changed)
        {
            var row = UiKit.LabeledRow(body, label, label, LabelWidth);
            return UiKit.Stepper(row, label, (float)value, (float)min, (float)max, (float)RoomSizes.StepM, v => RoomSizes.M(v) + " m", changed);
        }

        void ClickRoom(int index)
        {
            if (Measuring) return;
            if (index < RoomSizes.Presets.Length) SetSize(RoomSizes.Presets[index].size);
            else ShowState(); // Custom keeps the current size; change it with the steppers
        }

        /// <summary>Sets the shoebox size (open space, locked to the listener) and finds the table for it on this device.</summary>
        void SetSize(RoomSize size)
        {
            _size = RoomSizes.Clamp(size);
            if (reverb != null) reverb.SetRoomSize((float)_size.Width, (float)_size.Depth, (float)_size.Height);
            Debug.Log(Log + "room " + _size + (reverb != null ? ", " + reverb.CalibrationStatus : ""));
            ShowState();
        }

        void ShowState()
        {
            if (_width == null) return;
            _width.Value = (float)_size.Width;
            _depth.Value = (float)_size.Depth;
            _height.Value = (float)_size.Height;
            int preset = RoomSizes.PresetIndex(_size);
            UiKit.Select(_roomButtons, i => i == (preset >= 0 ? preset : RoomSizes.Presets.Length));
            UiKit.Select(_profileButtons, i => i == _profile);

            if (reverb != null)
            {
                double sweep = SweepPlan().EstimatedSeconds(), measure = MeasurePlan().EstimatedSeconds();
                _profileLine.text = "Targets " + Profile.TargetsText() + " · sweep " + SweepReflections().Length + " points, " +
                                    Rt60Eta.Duration(sweep) + " · measure " + Rt60Eta.Duration(measure);
            }
            // no table status in C04: it makes the CSV (PO 2026-11-17); the Console has reverb.CalibrationStatus
        }

        // ---- runs ----------------------------------------------------------------------------------------------------

        double[] SweepReflections() => Profile.SweepFor(reverb.ExpectedRt60For);

        Rt60Plan SweepPlan()
        {
            var plan = Rt60Plan.Sweep(Id, reverb, output, SweepReflections());
            AddMeta(plan);
            return plan;
        }

        Rt60Plan MeasurePlan()
        {
            var plan = new Rt60Plan { Chamber = Id, Reverb = reverb, Output = output };
            foreach (double t in Profile.Targets) plan.Conditions.Add(Rt60Condition.Rt60(Rt60Profile.TargetName(t), reverb, (float)t));
            AddMeta(plan);
            return plan;
        }

        void AddMeta(Rt60Plan plan)
        {
            plan.Meta.Add(new KeyValuePair<string, string>("profile", Profile.Name + ": targets " + Profile.TargetsText() +
                (Profile.FullSweep ? "; sweep = full grid" : "; sweep = grid points expected within " + Fmt.F(Rt60Profile.LowFactor, 2) +
                 " x shortest .. " + Fmt.F(Rt60Profile.HighFactor, 2) + " x longest target, plus one neighbour each side")));
            plan.Meta.Add(new KeyValuePair<string, string>("room_set_on_panel", _size.ToString() + " (open space, shoebox locked to the listener)"));
            plan.Restore = () => { if (reverb != null) reverb.SetDry(); };
        }

        bool Ready()
        {
            if (Measuring) return false;
            if (reverb == null || !reverb.HasRoom || output == null)
            {
                Debug.LogError(Log + "Reverb, shoebox or mixer group not linked: run Magic > Reverberation > Step 2.");
                return false;
            }
            return true;
        }

        void Calibrate()
        {
            if (!Ready()) return;
            Debug.Log(Log + "calibrate " + _size + ", profile " + Profile.Name + ", " + SweepReflections().Length + " points");
            _run = Rt60RunDialog.Run(transform, SweepPlan(), _ => { _run = null; if (this != null) ShowState(); });
        }

        void Measure()
        {
            if (!Ready()) return;
            var table = reverb.CalibratedTable;
            Debug.Log(Log + "measure " + _size + ", profile " + Profile.Name);
            _run = Rt60RunDialog.Run(transform, MeasurePlan(), _ => { _run = null; if (this != null) ShowState(); },
                                     condition => OutsideTable(table, condition));
        }

        /// <summary>A target the imported table cannot reach (it is clamped to the table's ends); false without a table.</summary>
        bool OutsideTable(Rt60Calibration table, string condition)
        {
            if (table == null) return false;
            foreach (double t in Profile.Targets)
                if (Rt60Profile.TargetName(t) == condition) return t < table.LowestRt60 - 1e-6 || t > table.HighestRt60 + 1e-6;
            return false;
        }
    }
}
