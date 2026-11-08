using System.Text;
using GPL.Homing;
using GPL.Input;
using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Headset test panel for PB-05c (no study logic): run the head gate (fixation dot; the panel hides; B cancels), the
    /// system recenter (handover), and watch the head pose live and the long-press recenters the listener reports. Every
    /// result shows as the note of the <c>homing</c> / <c>recenter</c> row a chamber would log. Replaced by the Hall (PB-06).
    /// </summary>
    public class HomingTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);

        public FixationCue cue;
        public RecenterListener listener;

        OVRCameraRig _rig;
        GlobalActions _actions;
        UiPanel _panel;
        TextMeshProUGUI _live, _result, _status, _hint, _runningLine;
        HeadGateMethod _gate;
        SystemRecenterMethod _recenter;
        Coroutine _running;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            _rig = HeadPose.FindRig();
            if (cue == null) cue = FindAnyObjectByType<FixationCue>();
            if (listener == null) listener = FindAnyObjectByType<RecenterListener>();
            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (_actions != null)
            {
                var table = new BindingTable();
                table.DeclareStandard(InputActions.Back);
                _actions.Use(table);
            }
            if (listener != null)
            {
                listener.Recentered -= OnUserRecenter;
                listener.Recentered += OnUserRecenter;
            }

            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            _panel = TestPanelKit.Panel(canvas, Size, "GPL.Homing test panel");
            _status = _panel.Surface.AddStatus("Press Head gate or Recenter.");
            var columns = TestPanelKit.Row(_panel.Surface.Body, "Columns");
            UiKit.Size(columns.gameObject, flexW: 1f, flexH: 1f);
            columns.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;

            var left = TestPanelKit.Column(columns, "Controls");
            TestPanelKit.Wide(left, "Head Gate", "Head gate", UiStyle.Prominent, RunGate);
            TestPanelKit.Wide(left, "Recenter", "Recenter (system)", UiStyle.Gray, RunRecenter);
            var gs = GateSettings.Defaults;
            TestPanelKit.Text(left, "Gate Settings",
                              "Gate: yaw ±" + gs.YawToleranceDeg + "°, pitch ±" + gs.PitchToleranceDeg + "°, hold " + gs.HoldSec + " s, hint after " +
                              gs.HintAfterSec + " s.\nDuring the gate the panel hides; B cancels.\nLong-press the Meta button to test the listener.",
                              th.smallSize, th.text2);

            var right = TestPanelKit.Column(columns, "Readout");
            _live = TestPanelKit.Text(right, "Live", "", th.smallSize, th.text);
            _result = TestPanelKit.Text(right, "Result", "No homing yet.", th.minimumSize, th.text2);

            // shown while the gate runs and the panel is hidden; far below the dot (the canvas sits 8° below eye level)
            _hint = UiKit.Text(canvas, "Gate Hint", "Look at the dot", th.headingSize, new Vector2(0f, -260f), new Vector2(Size.x, 70f), bold: true);
            _runningLine = UiKit.Text(canvas, "Gate Running", "Head gate running · B cancels", th.smallSize, new Vector2(0f, -320f),
                                      new Vector2(Size.x, 40f), color: th.text3);
            _hint.gameObject.SetActive(false);
            _runningLine.gameObject.SetActive(false);

            if (_rig == null) SetStatus("No OVRCameraRig in the scene.");
            else if (cue == null) SetStatus("No FixationCue in the scene: run Magic > App > Homing Test Panel.");
        }

        void Update()
        {
            if (_rig == null) return;
            var p = HeadPose.Of(_rig);
            var sb = new StringBuilder()
                .Append("Head yaw ").Append(Deg(p.YawDeg)).Append(", pitch ").Append(Deg(p.PitchDeg)).Append('\n')
                .Append("Eye height ").Append(p.EyeHeightM.ToString("0.00")).Append(" m\n")
                .Append("Rig at identity: ").Append(HeadPose.RigAtIdentity(_rig) ? "yes" : "NO").Append('\n')
                .Append("User recenters: ").Append(listener != null ? listener.Count.ToString() : "no listener");
            if (_gate != null && _gate.Running)
                sb.Append("\nGate: held ").Append(_gate.Gate.Held.ToString("0.00")).Append(" s").Append(_gate.Gate.Aligned ? " (inside)" : " (outside)");
            _live.text = sb.ToString();

            if (_gate != null && _gate.Running && _actions != null && _actions.Reader.Down(InputActions.Back)) _gate.Cancel();
        }

        static string Deg(float v) => v.ToString("+0.0;-0.0;0.0") + "°";

        void RunGate()
        {
            if (_running != null || _rig == null) return;
            _gate = new HeadGateMethod(_rig, cue, GateSettings.Defaults);
            _gate.HintChanged += on => _hint.gameObject.SetActive(on);
            _panel.gameObject.SetActive(false);
            _runningLine.gameObject.SetActive(true);
            _running = StartCoroutine(_gate.Run(r =>
            {
                _running = null;
                _runningLine.gameObject.SetActive(false);
                _panel.gameObject.SetActive(true);
                Show("homing", r);
            }));
        }

        void RunRecenter()
        {
            if (_running != null || _rig == null) return;
            _recenter = new SystemRecenterMethod(_rig);
            SetStatus("Recentering…");
            _running = StartCoroutine(_recenter.Run(r =>
            {
                _running = null;
                Show("homing", r);
            }));
        }

        void OnUserRecenter(HomingResult r) => Show("recenter", r);

        void Show(string evt, HomingResult r)
        {
            var sb = new StringBuilder("Last row: ").Append(evt).Append('\n');
            foreach (var kv in r.Pairs()) sb.Append(kv.Key).Append(" = ").Append(kv.Value).Append('\n');
            _result.text = sb.ToString();
            SetStatus(evt + ": " + r.Method + " " + r.Outcome.ToString().ToLowerInvariant());
        }

        void SetStatus(string s)
        {
            if (_status != null) _status.text = s;
            Debug.Log("[GPL] HomingTestPanel: " + s);
        }
    }
}
