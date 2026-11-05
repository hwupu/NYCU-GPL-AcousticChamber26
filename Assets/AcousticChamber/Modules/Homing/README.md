# GPL.Homing

Establishes "forward" and checks the participant's head before a trial (Architecture section 8): the head gate with a
fixation dot, the system recenter at handover, and a listener for the recenters people do with the Meta button. Every
method returns a `HomingResult` that the chamber logs as a `homing` row (or a `recenter` row for the listener) with
`HomingResult.Note()` in the note column. Generalized from HW2's `WaitForHeadAlignment`, `RecenterAndPlaceGui` and recenter
handling in `ExperimentFlow`.

**Copy:** this folder + `Core/` and `Params/`. **Dependencies:** GPL.Core, GPL.Params (Core: the gate parameters), Meta XR Core SDK
(`Oculus.VR`: `OVRCameraRig`, `OVRManager.display`).

## Frame (PO 2026-11-06)
- Angles are relative to the rig's forward: yaw 0 = forward, negative = left, positive = right; pitch positive = up. Eye height is
  above the floor-level tracking origin.
- **The rig stays at identity.** Recentering uses the Meta system recenter, so the headset's tracking frame, the world frame and the
  logged values agree (0 deg = the participant's forward after handover). Nothing rotates the rig; `HeadPose.RigAtIdentity` checks it
  and a recenter adds `detail=rig not at identity` to its note when it is not.

## Parts
| Part | What |
|---|---|
| `Core/HeadAngles.cs` | yaw / pitch of a direction, wrap to (-180, 180], yaw error to a reference |
| `Core/GateSettings.cs` | yaw ±10°, pitch ±15°, hold 0.5 s, hint after 4 s (PO 2026-11-06; HW2 used 5° / 10° / 1 s) |
| `Core/HeadGate.cs` | pure gate: `Update(yawError, pitch, dt)`; passes after an unbroken hold (the entering frame starts it at 0); counts breaks; hint after the delay; keeps the yaw / pitch at start and at pass |
| `Core/HomingResult.cs` | method, outcome (`done`, `cancelled`, `unavailable`), duration, yaw / pitch before and after, eye height, breaks and hint time (gate), detail; `Note()` = `method=head_gate outcome=done duration_s=0.734 yaw_before_deg=-12.5 ...` |
| `Core/HomingParams.cs` | the gate as preset parameters (`gate.yaw_tol_deg`, `gate.pitch_tol_deg`, `gate.hold_s`, `gate.hint_after_s`; group "Head gate"); `ReadGate(values)` |
| `Runtime/HeadPose.cs` | yaw, pitch and eye height of the center eye relative to the rig; `RigAtIdentity`; `NotTrackedYet` (head at the floor in the first frames) |
| `Runtime/IHomingMethod.cs` | `Name`, `Running`, `Run(done)` (a coroutine; `done` is called exactly once), `Cancel()` |
| `Runtime/HeadGateMethod.cs` | shows the fixation dot, runs the gate on scaled time (the Stop dialog freezes it), raises `HintChanged(bool)`; the chamber shows the hint text ("Look at the dot", English / 中文) |
| `Runtime/SystemRecenterMethod.cs` | handover / experimenter: measures the head (yaw before = the correction), `OVRManager.display.RecenterPose()`, waits for the event (at most 60 frames) and two more frames, measures again (the residual); `unavailable` without a headset |
| `Runtime/RecenterListener.cs` | one per scene, always on: a Meta-button recenter raises `Recentered(result)` two frames later (method `system_recenter_user`; yaw before = the frame before the event); ignores the app's own recenter and the second event Quest raises for one recenter. Overlay panels re-place themselves (GPL.UI `OverlayPanel`) |
| `Runtime/FixationCue.cs` | the dot: unlit sphere, 1.5° at 2 m (0.052 m), straight ahead at eye level; needs a URP Unlit material asset (`#262626`, DesignSystem section 8) |

Not built yet (Architecture section 8; added when a chamber needs them): `AlignToTarget`, `AudioHoming`, `RoomAnchor`.

## Use
```csharp
var rig = HeadPose.FindRig();
schemaDefs.AddRange(HomingParams.GateDefs());                     // chamber schema: the gate is tunable in presets
var gate = new HeadGateMethod(rig, fixationCue, HomingParams.ReadGate(work.Values));
gate.HintChanged += on => hud.Show(on ? "gate.hint" : null);       // the chamber's localized hint
yield return gate.Run(r => log.Write(LogEvents.Homing, r.Note()));

// handover
yield return new SystemRecenterMethod(rig).Run(r => log.Write(LogEvents.Homing, r.Note()));

// always on
recenterListener.Recentered += r => { stimuli.Replace(); log.Write(LogEvents.Recenter, r.Note()); };
```

## Status
PB-05c: head gate, system recenter, recenter listener, fixation dot; test panel `Magic > App > Homing Test Panel`.

Provenance: HW2 `ExperimentFlow` (gate loop, recenter subscription with a two-frame wait, `RecenterPose` at handover).
