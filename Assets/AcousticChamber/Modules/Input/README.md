# GPL.Input

Controller actions for Meta Quest: a binding table (which button does what), a reader that answers "was *confirm*
pressed?" so study code never calls `OVRInput` directly, and the global Stop action (Y on the left controller).
Generalized from HW2's Y-button and A/trigger handling in `ExperimentFlow`.

**Copy:** this folder + `Core/` (GPL.Core). **Dependencies:** Meta XR Core SDK (`Oculus.VR`: `OVRInput`, and an
`OVRManager` in the scene, which the Camera Rig building block has).

## Parts
| Part | What |
|---|---|
| `Core/Controls.cs` | `Control` (A, B, X, Y, triggers, grips, sticks; the Meta and menu buttons belong to the system), `Hand`, `ControlKind`, labels ("Y (left)") |
| `Core/InputActions.cs` | action names: global `stop` (Y); standard `confirm` (A), `back` (B), `trigger_left/right`, `grip_left/right`, `stick_left/right`; X is free |
| `Core/BindingTable.cs` | one table per chamber: globals preset, `Declare` / `DeclareStandard`, `RemapGlobal` (explicit only), `Describe()` for menus; a control serves one action, every mistake throws at setup |
| `Runtime/IControlSource`, `OvrControlSource` | raw controller state; the OVRInput implementation (works while `Time.timeScale` is 0) |
| `Runtime/ActionReader` | `Down` / `Held` / `Up` / `Value` / `Stick` by action name; an undeclared action logs one error and reads "not pressed" |
| `Runtime/GlobalActions` | one per scene: the scene's table and reader; raises `StopRequested` on Y while `StopEnabled` (a session runs) and not `Blocked` (a dialog is open) |

## Use
```csharp
var actions = GlobalActions.Instance;
actions.Use(new BindingTable()
    .DeclareStandard(InputActions.Confirm)
    .Declare("adjust_left", Control.TriggerLeft, "Turn left"));
actions.StopEnabled = true;                    // session starts: Y may open the Stop dialog
actions.StopRequested += ShowStopDialog;       // e.g. GPL.UI StopDialog (wired by the app, see StopDialogHost)
if (actions.Reader.Down(InputActions.Confirm)) Answer();
foreach (var line in actions.Bindings.Describe()) Debug.Log(line);  // "Y (left): Stop", "A (right): Confirm", ...
```

## Notes
- The GPL.UI pointer reads the triggers itself (click = trigger). A chamber action on a trigger also fires when the
  participant clicks a button; avoid trigger actions on screens with buttons.
- `GlobalActions` does not show anything. The app connects `StopRequested` to `GPL.UI.StopDialog`, so the two modules
  stay independent.
