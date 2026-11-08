using System.Text;
using GPL.Input;
using GPL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AcousticChamber.App
{
    /// <summary>
    /// Headset test panel for PB-05b (no study logic): simulate a session, press Y for the Stop dialog, watch the session
    /// clock freeze while it is open, and see every standard action live. Replaced by the Hall menu (PB-06).
    /// </summary>
    [RequireComponent(typeof(StopDialogHost))]
    public class InputTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);
        const string TestStudyId = "S0001";

        StopDialogHost _host;
        GlobalActions _actions;
        UiButton _sessionButton, _modeButton, _englishButton, _chineseButton;
        TextMeshProUGUI _clock, _live, _status;
        bool _running, _debug;
        float _sessionTime;
        string _lastAction = "-";

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            _host = GetComponent<StopDialogHost>();
            if (_host.canvas == null) _host.canvas = transform;
            _host.StudyId = TestStudyId;
            _host.Chosen -= OnChosen;
            _host.Chosen += OnChosen;
            _actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (_actions != null)
            {
                var table = new BindingTable();
                foreach (var a in InputActions.Standard.Keys) table.DeclareStandard(a);
                _actions.Use(table);
            }

            var th = UiKit.Theme;
            var panel = TestPanelKit.Panel(transform, Size, "GPL.Input test panel"); // the Stop dialog pushes it back
            _status = panel.Surface.AddStatus("Start the session, then press Y.");
            var columns = TestPanelKit.Row(panel.Surface.Body, "Columns");
            UiKit.Size(columns.gameObject, flexW: 1f, flexH: 1f);
            columns.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;

            // left column: controls
            var left = TestPanelKit.Column(columns, "Controls");
            _sessionButton = TestPanelKit.Wide(left, "Session", "", UiStyle.Prominent, ToggleSession);
            _modeButton = TestPanelKit.Wide(left, "Mode", "", UiStyle.Gray, ToggleMode);
            var langs = TestPanelKit.Row(left, "Languages");
            _englishButton = TestPanelKit.Wide(langs, "English", Loc.LabelOf(UiLanguage.English), UiStyle.Gray, () => { Loc.Set(UiLanguage.English); Refresh(); });
            _chineseButton = TestPanelKit.Wide(langs, "Chinese", Loc.LabelOf(UiLanguage.Chinese), UiStyle.Gray, () => { Loc.Set(UiLanguage.Chinese); Refresh(); });
            TestPanelKit.Wide(left, "Open Stop Dialog", "Open Stop dialog", UiStyle.Borderless, () => _host.Open());

            // right column: live readout
            var right = TestPanelKit.Column(columns, "Readout");
            _clock = TestPanelKit.Text(right, "Clock", "", th.bodySize, th.text, bold: true);
            var bindings = _actions != null ? string.Join("\n", _actions.Bindings.Describe()) : "No GlobalActions in the scene.";
            TestPanelKit.Text(right, "Bindings", bindings, th.smallSize, th.text2);
            _live = TestPanelKit.Text(right, "Live", "", th.smallSize, th.text);
            Refresh();
        }

        void Update()
        {
            if (_running) _sessionTime += Time.deltaTime; // scaled time: stops while the Stop dialog freezes the session
            if (_actions != null)
            {
                foreach (var b in _actions.Bindings.All)
                    if (_actions.Reader.Down(b.Action)) _lastAction = b.Action;
                var r = _actions.Reader;
                _live.text = new StringBuilder()
                    .Append("Last action: ").Append(_lastAction).Append('\n')
                    .Append("Triggers L/R: ").Append(r.Value(InputActions.TriggerLeft).ToString("0.00"))
                    .Append(" / ").Append(r.Value(InputActions.TriggerRight).ToString("0.00")).Append('\n')
                    .Append("Grips L/R: ").Append(r.Value(InputActions.GripLeft).ToString("0.00"))
                    .Append(" / ").Append(r.Value(InputActions.GripRight).ToString("0.00")).Append('\n')
                    .Append("Sticks L/R: ").Append(Fmt(r.Stick(InputActions.StickLeft)))
                    .Append(" / ").Append(Fmt(r.Stick(InputActions.StickRight)))
                    .ToString();
            }
            _clock.text = "Session time: " + _sessionTime.ToString("0.0") + " s" + (Freeze.Active ? "  (frozen)" : "");
        }

        static string Fmt(Vector2 v) => v.x.ToString("+0.0;-0.0") + "," + v.y.ToString("+0.0;-0.0");

        void ToggleSession()
        {
            _running = !_running;
            if (_running) _sessionTime = 0f;
            SetStatus(_running ? "Session running: press Y (left controller)." : "Session stopped: Y does nothing now.");
            Refresh();
        }

        void ToggleMode()
        {
            _debug = !_debug;
            Refresh();
        }

        void OnChosen(StopChoice choice)
        {
            if (choice != StopChoice.KeepGoing) _running = false;
            SetStatus("Stop dialog: " + choice + (choice == StopChoice.DropOut ? " (" + TestStudyId + " would be logged participant_dropout)" : ""));
            Refresh();
        }

        void Refresh()
        {
            if (_actions != null) _actions.StopEnabled = _running;
            _host.DebugRun = _debug;
            TestPanelKit.LabelOf(_sessionButton).text = _running ? "Session: running (stop)" : "Session: off (start)";
            _sessionButton.Style = _running ? UiStyle.Destructive : UiStyle.Prominent;
            TestPanelKit.LabelOf(_modeButton).text = _debug ? "Mode: Config run" : "Mode: Real session";
            _modeButton.Selected = _debug;
            _englishButton.Selected = Loc.Current == UiLanguage.English;
            _chineseButton.Selected = Loc.Current == UiLanguage.Chinese;
        }

        void SetStatus(string s)
        {
            if (_status != null) _status.text = s;
            Debug.Log("[GPL] InputTestPanel: " + s);
        }
    }
}
