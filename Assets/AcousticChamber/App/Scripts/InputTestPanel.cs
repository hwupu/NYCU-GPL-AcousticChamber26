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
        UiButton _sessionButton, _modeButton;
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
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            var root = UiKit.Window(canvas); // the Stop dialog pushes it back
            UiKit.Text(root, "Title", "GPL.Input test panel", th.titleSize, new Vector2(-Size.x / 2f + 48f + 300f, Size.y / 2f - 80f),
                       new Vector2(600f, 80f), bold: true, align: TextAlignmentOptions.Left);
            UiKit.DebugTag(root);

            // left column: controls
            const float leftX = -250f, w = 420f;
            float y = 190f;
            _sessionButton = UiKit.Button(root, "Session", "", UiStyle.Prominent, new Vector2(leftX, y), new Vector2(w, 0f), ToggleSession);
            y -= 100f;
            _modeButton = UiKit.Button(root, "Mode", "", UiStyle.Gray, new Vector2(leftX, y), new Vector2(w, 0f), ToggleMode);
            y -= 100f;
            UiKit.Button(root, "English", Loc.LabelOf(UiLanguage.English), UiStyle.Gray, new Vector2(leftX - 110f, y), new Vector2(200f, 0f),
                         () => Loc.Set(UiLanguage.English));
            UiKit.Button(root, "Chinese", Loc.LabelOf(UiLanguage.Chinese), UiStyle.Gray, new Vector2(leftX + 110f, y), new Vector2(200f, 0f),
                         () => Loc.Set(UiLanguage.Chinese));
            foreach (var n in new[] { "English", "Chinese" }) ((RectTransform)root.Find(n)).sizeDelta = new Vector2(200f, th.buttonHeight);
            y -= 100f;
            UiKit.Button(root, "Open Stop Dialog", "Open Stop dialog", UiStyle.Borderless, new Vector2(leftX, y), new Vector2(w, 0f), () => _host.Open());

            // right column: live readout
            const float rightX = 240f;
            _clock = UiKit.Text(root, "Clock", "", th.bodySize, new Vector2(rightX, 200f), new Vector2(440f, 60f), bold: true,
                                align: TextAlignmentOptions.Left);
            var bindings = _actions != null ? string.Join("\n", _actions.Bindings.Describe()) : "No GlobalActions in the scene.";
            UiKit.Text(root, "Bindings", bindings, th.smallSize, new Vector2(rightX, 40f), new Vector2(440f, 260f),
                       color: th.text2, align: TextAlignmentOptions.TopLeft);
            _live = UiKit.Text(root, "Live", "", th.smallSize, new Vector2(rightX, -200f), new Vector2(440f, 160f), align: TextAlignmentOptions.TopLeft);

            _status = UiKit.Text(root, "Status", "Start the session, then press Y.", th.smallSize, new Vector2(0f, -Size.y / 2f + 30f),
                                 new Vector2(Size.x - 80f, 40f), color: th.text2);
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
            UiKit.LabelOf(_sessionButton).text = _running ? "Session: running (stop)" : "Session: off (start)";
            _sessionButton.Style = _running ? UiStyle.Destructive : UiStyle.Prominent;
            UiKit.LabelOf(_modeButton).text = _debug ? "Mode: Debug run" : "Mode: Real session";
        }

        void SetStatus(string s)
        {
            if (_status != null) _status.text = s;
            Debug.Log("[GPL] InputTestPanel: " + s);
        }
    }
}
