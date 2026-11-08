# Architecture — AcousticChamber26

Status: **accepted in principle** (2026-11-05, PO answers in the git history of `docs/Questions.md`); details are refined ticket by ticket. Decisions: [decisions/](decisions/).

## 1. Goals
1. **Exploratory lab.** Independent chambers (individual studies, explorations, demos) in one app, chosen from a Main Hall. Each chamber has its own protocol, stimuli, assets, parameters, input bindings, log schema and resume rules. Chambers are added, removed and refined over time.
2. **Plug-and-play modules.** Shared features are generic and live in self-contained modules that can be copied into another Unity project. Anything specific to one chamber lives in that chamber's folder.
3. **Data integrity.** Per-row flushed logs, resume after any interruption, never delete data, log schema designed for the analysis (RM-ANOVA) before coding.
4. **Experimenter ergonomics.** Hall, a chamber menu (Study ID, Start, Debug), parameter presets editable in Debug and used by real runs, a global stop dialog, validated scene setup.

Non-goals for now: networking, cloud upload, runtime asset downloads.

## 2. Folder layout
All our files sit under one root so third-party and Asset Store content (which installs into `Assets/<Vendor>`) never mixes with ours:

```
Assets/
  AcousticChamber/
    Modules/<Module>/        portable, generic (namespace GPL.<Module>)
    App/                     Main Hall, chamber catalog, routing (this project only)
    Chambers/<ChamberId>/    one chamber per folder (namespace AcousticChamber.Chambers.<ChamberId>)
    Chambers/_Template/      copy to start a new chamber (chamber ID `Template`)
    Settings/                project-level assets we own (lighting, mixer templates, fonts)
  Oculus/, XR/, Resources/, Settings/, Plugins/   Unity and Meta generated (left where the tools put them)
  <ThirdParty>/              Asset Store / vendor packages, untouched
docs/                        process, architecture, ADRs, references
                             (no analysis scripts: each chamber documents its data scheme for analysis by other tools)
```

## 3. Layers
```
Chambers  (protocol, trial runner, stimuli, parameters, log columns, bindings, docs)
   │ uses
App shell (Hall, ChamberCatalog, SessionRouter, scene transitions)
   │ uses
Modules   (generic, no knowledge of App or chambers)
```
Dependencies point downward only. Chambers never reference each other. The App knows chambers only through their `ChamberDefinition` asset.

## 4. Modules (`Assets/AcousticChamber/Modules`)
Each module:
```
<Module>/
  README.md        purpose, dependencies, public API, how to copy it into another project, provenance (HW2 file it came from)
  Core/            GPL.<Module>.Core.asmdef (noEngineReferences: true), optional
  Runtime/         GPL.<Module>.asmdef
  Editor/          GPL.<Module>.Editor.asmdef (reusable setup functions, inspectors)
  Tests/EditMode/  GPL.<Module>.Tests.asmdef
  Assets/          materials, prefabs, images owned by the module
```
Rule: a module may reference `GPL.Core`, the other GPL modules named in its README (no cycles) and the external packages named in its README; never the App or a chamber. HW2 code is **refactored** into these modules: generic parts only; HW2-specific logic goes to the `HumPerHW2` chamber.

| Module | Core (pure, unit-tested) | Runtime (Unity) | From HW2 |
|---|---|---|---|
| **GPL.Core** | atomic file write, seeded RNG, CSV escaping, azimuth/pose math, ID sanitizing, `IClock` | console prefix, `HeadPose` (wait for valid height) | `Maths`, parts of `ExperimentLog` |
| **GPL.Params** | `ParamDef`, `ParameterSchema`, `ParameterSet` (flat key/value, typed access, hash), `PresetStore` (default file = preset "Default", presets, active preset) | — (the Debug controls are in `GPL.DebugBench`) | new (section 7) |
| **GPL.Logging** | `LogSchema` (common + chamber columns), `EventLog` (flush per row, mirror), safe append on resume | — (the chamber opens the `EventLog`) | `ExperimentLog` generalized |
| **GPL.Session** | `SessionRecord` (cursor, attempts), `ResumeFrom`, `SessionStore` (atomic, list unfinished per chamber), `DataFiles` (pair each CSV with its session file, list, delete one / all) | — (the Log files page is in `GPL.DebugBench`, section 10.1) | `SessionProgress` / `ProgressStore` generalized |
| **GPL.Protocol** | `ProtocolPlan` (stage > block > trial), `ProtocolWalker` (resume resolution), `ProtocolLog`, `SlideDeck`; planned: planners (seeded shuffle, Latin square, balanced repeats, forbidden values) | `ProtocolScreens` (stock screens: handover, language, recenter slide, instruction slides, dot check, messages, cue text), `SlideSet` | `ExperimentFlow`, `TrialPlanner` split |
| **GPL.Input** | binding table (action -> button, controller), reserved actions | `ActionReader` over `OVRInput`, `GlobalActions` (Stop dialog trigger) | Y-button and A/trigger handling |
| **GPL.UI** | localization table, number-pad model, motion curves, scroll model | `OverlayPanel`, `UiPointer`, `UiKit`, `UiButton`, `PageStack`, `Sheet`, `ModalDialog`, `StopDialog`, `ScrollList`, **`ITextEntry`** (section 9) | `UiPointer`, `GuiPanelPlacer`, `ParticipantIdField`, `OnboardingPanel`, `LocalizedText` |
| **GPL.Homing** | gate math (tolerance, hold time) | `IHomingMethod` + methods (section 8), recenter listener, fixation cue | head gate, recenter handling |
| **GPL.Acoustics** | `Rt60Estimator`, `Rt60Calibration` (measured table, V/S scaling), room math | `ShoeboxRoom`, `ReverbTuner`, `Rt60Meter`, `ClipLevels`; Editor: room dialog, Reflection mixer, audio build-settings guard | acoustics files |
| **GPL.HearingLoss** | `ANSIS344`, presets, `HearingProfile` (left/right bands), peaking biquad | `OutputHearingLoss` on the final stereo output (section 11), optional spectrum view | new (HLSIM_GUIDELINES) |
| **GPL.SpatialSources** | placement math, constrained random placement, trajectories | `PlacedSource` (room-fixed / head-relative / re-placed after recenter), `MovingSource`, `GrabbableSource`, `PulseTrain`, `BackgroundBed` | `StimulusController`, background code split |
| **GPL.Responses** | response records (azimuth, elevation, distance, error) | rotate-with-triggers adjustment, pointing ray, place-a-marker, button choice | HW2 task |
| **GPL.DebugBench** | `WorkingCopy` (the Playground: edits of the active preset) | `DebugBench` pages: Debug (Run experiment trials, rows Playground › / Presets › / Log files ›, chamber sections), Parameters (opened by Playground), Presets, Log files | `DebugPanel` generalized |

Modules stay plain folders (copy the folder plus `GPL.Core`). Turning a stable module into a Unity package later is optional.

## 5. App shell (`Assets/AcousticChamber/App`)
- **`ChamberDefinition`** (ScriptableObject in each chamber folder): `id` (the chamber name, e.g. `HumPerHW2`: folder, data folder, file names, `chamber` column), `label` (display ID `C01`, `C02`, ... (as in README.md; `C00` = the hidden Template); Hall and README only, may change without touching data), title (English / 中文), description, thumbnail, scene, `version` (written into every log row), supported modes, default `ResumePolicy`, input bindings, posture, enabled flag.
- **Chamber ID rule** (PB-06a): letters, digits and `-` only (it is a token of data file names split on `_`). The folder is the ID or `_` + ID: `Chambers/_Template/` holds the chamber `Template`. The definition is `Chambers/<Folder>/<Folder>.ChamberDefinition.asset`.
- **`ChamberCatalog`** (`App/ChamberCatalog.asset`): list shown in the Hall, ordered by label, then ID. `Magic > App > Sync Chambers` collects every `ChamberDefinition`, leaves out (with an error) those that break the ID rule, lack their scene or repeat an ID, updates the catalog and sets the build scene list to MainHall + the chamber scenes. Removing a chamber = delete its folder + Sync. `Validate Setup` flags orphans.
- **Main Hall** (`MainHall.unity`, `HallMenu`): own rig and GUI; 2-column card grid of the enabled chambers (title, number of unfinished sessions). No Study ID and **no Debug mode** here: Debug lives in each chamber (its menu's Debug button; PO 2026-11-06).
- **Sandbox** (`App/Scenes/Sandbox.unity`, not in the build): the module test panels (UI, Input, Debug bench, Homing); open it in the Editor and Play over Link (PO 2026-11-06).
- **`SessionRouter`** (PB-06b, `App/Core`; run by `HallMenu` once per app start, not on Back to the Hall): on launch, if the last used chamber (`persistentDataPath/app.state`, key `last_chamber`, written when a chamber is entered) has unfinished sessions it opens **that chamber's menu** (not the trials). Otherwise the Hall.
- **Scenes:** one scene per chamber, each with its own Building Blocks rig, loaded singly (`AppNav`) behind a fade to black: 0.3 s out, 0.3 s in (Meta `OVRScreenFade`, added to the center eye at run time by `SceneFade`; overlay panels hide their window first because the fade does not cover compositor layers). A scene switch resets `Time.timeScale` to 1. The participant language is chosen inside the chamber session (Language stage, stored in the session record); system screens are English only.

## 6. Chamber menu and session flow
Every chamber starts at its **chamber menu** (generic, from `GPL.UI` + `GPL.Session`, HW2 style):
- **Study ID** (number pad, up to 4 digits, stored and shown with leading zeros and prefix: `1` -> `S0001`), **Start**, **Debug**, **Back to Hall**. All chambers are equal: same menu, presets, Debug, sessions. A chamber that is still a demo or under development is simply used through Debug.
- Status lines under Start and Debug (PO 2026-11-06, wording 2026-11-07): **"Audio level: 10 (target 8)"** (section 8), **"Preset: Custom"** (section 7; nothing while the preset Default is active) and **"Unfinished: S0005, S0012"** (Study IDs with a resumable session; nothing when none). The sessions themselves are managed on Debug > Log files.
- **Start with a Study ID that has an unfinished session** -> Resume / Start new / Back. Several unfinished sessions can exist (one per Study ID).
- **Resume** -> the experimenter hands the headset over; the participant continues **silently** at the point given by the resume policy (no welcome screen; homing still runs if the stage requires it).
- **Start new** over an unfinished session: the old one is logged `session_superseded` and closed (never deleted).

### 6.1 Session record
One key=value text file per session, `<base>.session`, **next to its CSV** (atomic write; the base name is the session ID; names and paths in the [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#files-and-paths)). Real sessions only; Debug runs have a CSV and no session file. Implemented in `GPL.Session` (PB-04):
`session_id, chamber, chamber_version, study_id, status (InProgress | Completed | DroppedOut | Superseded), seed, language, preset, params_hash, plan_hash, app_version, created, updated, cursor.{stage, block, trials_done, attempt}, resume_from, attempt.<stage>.<block>, param.<key> (full snapshot), custom.<key>`. The Stop dialog has no pause: a session stays `InProgress` until it completes, drops out or is superseded; quitting the app or taking the headset off leaves it resumable.

**A resumed session uses its own parameter snapshot**, not the currently active preset (no warning when they differ; a resume is refused only when the chamber's protocol plan changed, `plan_hash`).

### 6.2 Resume policy (per stage, overridable per block)
What each policy continues with and how it counts attempts: [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#resume-policies-resumefrom).
| `ResumeFrom` | Typical use |
|---|---|
| `NextTrial` | fixed trial lists (HW2) |
| `BlockStart` | staircases, continuous blocks, adaptation to a condition |
| `StageStart` | practice, calibration, onboarding |
| `SessionStart` | very short designs |
| `NotResumable` | designs where a break invalidates data |

Resume hooks per stage (`OnResume`): re-apply the condition, restart the background, re-run homing, an optional re-familiarization trial. The plan is rebuilt from the stored seed; a different `plan_hash` (chamber changed) blocks the resume with an explanation.

**Interrupted partial blocks in the data:** with `BlockStart`, the trials finished before the interruption stay in the CSV with `attempt = 1`, and the redone block has `attempt = 2`. The default analysis rule uses **only the highest complete attempt of each block**. Each chamber's AnalysisPlan states this.

## 7. Parameters and presets (`GPL.Params`)
Fixes the HW2 problem that Debug changes did not carry over to real trials.
The decision (Default and Custom presets, Playground, Save as Custom, Make active, real runs on the active preset, snapshot per session) is [ADR-0002](decisions/ADR-0002-parameters-and-presets.md); the Debug pages are in the [GPL.DebugBench README](../Assets/AcousticChamber/Modules/DebugBench/README.md). Architecture-level points:
- Each chamber declares a **parameter schema** (e.g. `rt60_s`, `trials_per_block`, `gate.yaw_tol_deg`) and ships `Chambers/<Id>/Config/default.params` (key=value text, in git) = the preset **Default**.
- Presets live on the device (`persistentDataPath/<ChamberId>/presets/`); copy them off the headset like data; useful ones can be committed back into `Config/`.
- **Run experiment trials** (Debug) runs the real protocol with the Playground values, logged as `_DEBUG`, no resume, to experience exactly what participants will get.
- The CSV gets `preset` and `params_hash` columns.

## 8. Posture, handover and homing (`GPL.Homing`)
- Each chamber declares a posture: `Seated`, `Standing` or `RoomScale`. Tracking origin is always floor level.
- **Handover** (stock stage, at session start and after a resume): the experimenter sets up (often standing) and hands the headset over; "forward" is then set by the participant's Meta-button recenter and checked by the head gate (steps 2, 4 and 6 below). A chamber that needs it also measures eye height there, so stimuli are placed relative to the participant, not the experimenter.
- **Onboarding sequence** (PB-20b, PO 2026-11-07; stock stages in `GPL.Protocol`: names, switches and plan entries in `OnboardingStages`, screens and rows in `Onboarding` (PB-21); order = the chamber's stage list, each stage switchable by a preset boolean `onboarding.<stage>`):
  1. Chamber menu: Study ID, Start. A status line shows the headset volume read from Android against the chamber's target (`volume.target_step`): "Audio level: 10 (target 8)" (red when they differ). The read value is logged on `session_start` / `session_resume`; no volume pad (kept in code, `ChamberMenu.AskVolumeStep`).
  2. `handover` (experimenter): title "Protocol", a numbered list "1. Set the IPD. 2. Hand the headset to the participant." + Continue. No recenter here (the A-button recenter is dropped).
  3. `language` (participant).
  4. `recenter`: a slide in the instruction-slide layout (title, illustration or the text "Look straight ahead. Hold the Meta button until the view recenters.", Back "← Language" reopens the language choice, Next always active, no A button; `homing` row, method `system_recenter_user`, outcome `cancelled` when Next came without a recenter).
  5. `instructions`: the chamber's slides (`SlideSet`; none = nothing shown).
  6. `ready`: final check at the black dot (head gate), then a `volume_check` row with the final volume.
  7. Practice and trials (A is used only as the Template's trial response), then thanks.
  This list is the single description of the sequence; the slide layout is in [DesignSystem](DesignSystem.md) section 9, the screen API in the [GPL.Protocol README](../Assets/AcousticChamber/Modules/Protocol/README.md), a chamber's own stage table in its `Docs/Design.md`.
  The participant is not told about Y (Stop dialog): breaks and drop-outs are handled by the experimenter when the participant reports discomfort.
- Homing methods implement `IHomingMethod` -> `HomingResult` (origin, yaw, eye height, method, duration, residual error), logged as `homing`:

| Method | What happens |
|---|---|
| `HeadGate` | fixation cue ahead, head within yaw/pitch tolerance for a hold time (HW2, before every trial); defaults and preset keys in the [GPL.Homing README](../Assets/AcousticChamber/Modules/Homing/README.md) |
| `SystemRecenterListener` | reacts to the Meta long-press recenter; re-places GUI and head-relative stimuli (always on) |
| `SystemRecenter` | app-requested (not used by the onboarding since PB-20b; the participant holds the Meta button instead): the Meta system recenter makes the current head yaw/position forward/origin; head yaw measured before (the correction) and after (the residual). Replaces the planned `SoftwareRecenter` (PO 2026-11-06): the rig stays at identity, so raw tracking poses and logged values share one frame |
| `AlignToTarget` | participant turns to a visible target; the room stays fixed |
| `AudioHoming` | a sound straight ahead; face it and confirm (designs without visuals) |
| `RoomAnchor` | room-scale: no re-centering of position; the virtual room is aligned once to the physical room (setup-time) |

## 9. Input bindings and text entry
- **Global actions** are the same in every chamber where possible. Reserved: **Y (left controller) = Stop dialog**. The Meta button belongs to the system. A chamber may remap a global action only by declaring it in its `ChamberDefinition` (bindings table); its menu then shows the binding.
- **Chamber actions** (confirm, adjust left/right, grab, ...) are declared per chamber in the same table (`ChamberBehaviour.DeclareActions`, called by the chamber menu after `confirm` and `back`; PB-08d-3) and read through `GPL.Input`, never with direct `OVRInput` calls in chamber code.
- **Standard chamber actions** (defaults; a chamber declares only those it uses): `confirm` = A, `back` = B, `trigger_left/right`, `grip_left/right`, `stick_left/right`; X is free. Y works only while a session (real or Debug run) is running; in the Hall and the menus it does nothing.
- **Stop dialog (Y)**: modal, the session frozen while open (time scale 0, audio paused); scene objects that could cover it (fixation dot, the chamber's pictures and markers: `ProtocolChamber.ViewBlockers`) are hidden until it closes (PB-08d-3); Keep going / Drop out (`participant_dropout`, not resumable), in Debug Keep going / Back to the chamber menu. Behaviour: [ADR-0003](decisions/ADR-0003-sessions-resume-input.md); wording and look: [DesignSystem](DesignSystem.md) section 7.
- **Text entry is swappable** (`ITextEntry`): `UiNumberPad` (on-canvas, reliable, HW2), `KeypadEntry` (letters + digits), `NativeKeyboardEntry` (Unity `TouchScreenKeyboard`, the Quest system keyboard). Selected per chamber or globally; the native keyboard problem from HW2 is investigated in its own ticket. (Meta's in-app Virtual Keyboard is not part of the installed Core SDK 207.)

## 10. Logging scheme
Long format, one row per event: the common columns, then the chamber's columns (declared in its `LogSchema`), then `note` (always last). The column list and its rules are in the [GPL.Logging README](../Assets/AcousticChamber/Modules/Logging/README.md#columns). RM-ANOVA factors are **explicit columns** (e.g. `hl_side`, `hl_level`, `rt60_target_s`), never parsed from notes.

Common events: `session_start` (app version, device, headset volume step read from the headset), `session_resume`, `stage_begin/end`, `block_begin/end`, `block_restart`, `trial`, `homing`, `recenter`, `handover`, `slide`, `volume_check`, `app_paused/app_resumed`, `participant_dropout`, `session_superseded`, `session_end`, `debug_*`, `param_change` (Debug).

Files: one CSV per session (a resume appends) plus its `.session` file (the parameter snapshot lives there); names and paths are in the [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#files-and-paths). Each chamber's `AnalysisPlan.md`: within factors, levels, DV, per-cell aggregation, exclusions (Debug, dropout, superseded, lower attempts). Each chamber also has `DataFormat.md` written for an analyst (human or AI): files, columns, units, events, how to rebuild the trial table and the per-cell means. No analysis scripts in the repo.

### 10.1 Data manager (Debug > Log files, in each chamber)
MQDH does not list the per-app folder, so files are managed in the headset.
- Shared page (`GPL.DebugBench` Log files page over `GPL.Session` `DataFiles`), in every chamber's Debug menu; it only sees **that chamber's folder** (`persistentDataPath/<ChamberId>/`).
- One entry per session = the **pair** CSV + session file (Debug runs: CSV only). `DataFiles` knows date/time, Study ID, mode (real / DEBUG), status (in progress, completed, dropped out, superseded), size and number of trial rows; the page shows one line per session (the file name).
- **Delete** one entry (both files together) or **Delete all**; each asks for confirmation in a modal dialog ("Delete all" names the count; one confirmation, PO 2026-11-06). Deleting an unfinished session removes it from the resume list.
- Presets are not touched by the data manager.
- Deletions are not logged in any data file (the file is gone); the console mirror prints them.
- Copying files off the headset: see the [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#files-and-paths).

## 11. Hearing loss on the device output (`GPL.HearingLoss`)
- Applied to the **final left/right output channels** of the app, after the spatializer, the room and the mixer, just before the audio leaves for the device. Not on sources or files.
- Implementation: `OnAudioFilterRead` on the `AudioListener` (the last stage of Unity's output; interleaved L/R), two independent 10-band peaking banks. HW2's `Rt60Meter` taps the same point and sees the Meta spatializer and Reflection output. A test-tone check per ear (left only, right only) is part of the ticket.
- `HearingProfile { left[10], right[10] }` from None / presets / manual / ANSI S3.44 prediction; unilateral = one side None. Part of the parameters (presets) and logged.
- Limits stated in every write-up: models audibility only (band attenuation, Q = 12); no recruitment, frequency-selectivity or temporal effects; unofficial port of the NIOSH tool (HLSIM_GUIDELINES section 12 decisions are open).

## 12. Chambers (`Assets/AcousticChamber/Chambers/<Id>`)
```
<Id>/
  <Id>.ChamberDefinition.asset
  Config/default.params
  Scenes/<Id>.unity
  Core/ (pure, optional) Scripts/ Editor/ Tests/EditMode/
  Audio/ Images/ Materials/ Models/
  Docs/  Brief.md, Design.md, DataFormat.md, AnalysisPlan.md, SceneSetup.md, decisions/
```
**Chamber logic** (PB-21): a chamber's MonoBehaviour derives from `ProtocolChamber` (App; itself a `ChamberBehaviour`), which owns the run loop (walker + structure rows), the onboarding stages (`GPL.Protocol.Onboarding`, section 8), the head gate before a trial, the block break, thanks, recenter rows, the plan hash check on resume and the cleanup after Stop. The chamber overrides `Schema`, `LogSchema`, `CreatePlan`, `RunTrial` and optionally `RunStage` / `BlockEnd` / `OnAbort` / `BeforeRow` (set context columns such as `condition` before a structure row; PB-08d-3). A chamber without a staged protocol may derive from `ChamberBehaviour` directly.

A chamber supplies a protocol (stages + resume policies), a trial runner (`ITrialRunner`), condition appliers (push parameter values into modules; shared by real runs and Debug), its log columns, parameter schema, bindings, posture and docs.

Planned chambers (independent; order is a backlog decision):
- **HumPerHW2** — HW2 rebuilt on the modules (proves the refactoring; same data format where possible).
- **HearingLoss** (C02) — experience chamber (ADR-0006): unilateral / bilateral loss on the output, a speaker dragged on the floor, own panel, no logs.
- **MovingSource** — move a source (grab / scripted) to experience spatial audio.
- **HiddenSource** — locate an invisible source placed at random.
- Realistic room, outdoor (later).
- **_Template** — minimal working chamber to copy: `Magic > App > New Chamber...` (PB-13) copies it to `Chambers/<Id>` with new GUIDs and renamed code, menus and paths; docs become skeletons.

## 13. Scene setup, validation, tests
- Menu **Magic**: `Magic > App > ...`, `Magic > <ChamberId> > Step 1..N, Hot Fix`, `Magic > Validate Setup` (all catalog scenes). Module Editor code provides reusable builders (`RoomBuilder`, `MixerSetup`, `OverlayCanvasBuilder`) that chamber steps call.
- Headless entry point to run all steps + tests on the mirror project (`Tools/Verify-Headless.ps1`, see CLAUDE.md).
- EditMode tests per module and per chamber; HLSim test vectors must match exactly.

## 14. Build order
Steps 1–3 done (Sprint 1, plus `GPL.DebugBench` and `GPL.Protocol`); current state on the [SprintBoard](SprintBoard.md).
1. `GPL.Core`, `GPL.Params`, `GPL.Logging`, `GPL.Session` (pure) + tests, ported and generalized from HW2.
2. `GPL.Input`, `GPL.UI` (pointer, overlay, number pad behind `ITextEntry`, dialog), `GPL.Homing` runtime.
3. Hall + catalog + routing + `_Template` chamber (chamber menu, Stop dialog, presets, resume variants). **PO headset test.**
4. HumPerHW2 chamber (HW2 on the modules) incl. `GPL.Acoustics`, `GPL.SpatialSources`, `GPL.Responses`, `GPL.DebugBench`. **PO headset test**; compare a CSV with HW2.
5. `GPL.HearingLoss` + HearingLoss chamber.
6. Further chambers.
