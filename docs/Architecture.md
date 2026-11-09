# Architecture — AcousticChamber26

Status: accepted (2026-11-05); refined ticket by ticket. Decisions: [decisions/](decisions/). Progress: [SprintBoard](SprintBoard.md).

## 1. Goals
1. **Exploratory lab.** Independent chambers (studies, explorations, demos) in one app, chosen from a Main Hall. Each has its own protocol, stimuli, assets, parameters, bindings, log schema and resume rules; chambers come and go.
2. **Plug-and-play modules.** Shared features live in self-contained modules that can be copied into another Unity project; anything chamber-specific stays in the chamber folder.
3. **Data integrity.** Per-row flushed logs, resume after any interruption, never delete data, log schema designed for the analysis (RM-ANOVA) before coding.
4. **Experimenter ergonomics.** Hall, chamber menu (Study ID, Start, Config), presets editable in Config and used by real runs, a global stop dialog, validated scene setup.

Non-goals: networking, cloud upload, runtime asset downloads.

## 2. Folder layout
All our files sit under one root so third-party content (`Assets/<Vendor>`) never mixes with ours:
```
Assets/
  AcousticChamber/
    Modules/<Module>/        portable, generic (namespace GPL.<Module>)
    App/                     Main Hall, chamber catalog, routing (this project only)
    Chambers/<ChamberId>/    one chamber per folder (namespace AcousticChamber.Chambers.<ChamberId>)
    Chambers/_Template/      copy to start a new chamber (chamber ID `Template`)
    Settings/                project-level assets we own (lighting, mixer templates, fonts, UiTheme)
  Oculus/, XR/, Resources/, Settings/, Plugins/   Unity and Meta generated
  <ThirdParty>/              Asset Store / vendor packages, untouched
docs/                        process, architecture, ADRs, references (no analysis scripts)
```

## 3. Layers
```
Chambers  (protocol, trial runner, stimuli, parameters, log columns, bindings, docs)
   │ uses
App shell (Hall, ChamberCatalog, SessionRouter, scene transitions)
   │ uses
Modules   (generic, no knowledge of App or chambers)
```
Dependencies point downward only. Chambers never reference each other; the App knows them only through their `ChamberDefinition` asset.

## 4. Modules (`Assets/AcousticChamber/Modules`)
Each module:
```
<Module>/
  README.md        purpose, dependencies, public API, how to copy, provenance
  Core/            GPL.<Module>.Core.asmdef (noEngineReferences: true), optional
  Runtime/         GPL.<Module>.asmdef
  Editor/          GPL.<Module>.Editor.asmdef
  Tests/EditMode/  GPL.<Module>.Tests.asmdef
  Assets/          materials, prefabs, images owned by the module
```
A module may reference `GPL.Core`, the GPL modules named in its README (no cycles) and the packages named there; never the App or a chamber. HW2 code was refactored into modules (generic parts) and the `HumPerHW2` chamber (HW2-specific logic). Modules stay plain folders (copy the folder plus `GPL.Core`).

| Module | Core (pure, unit-tested) | Runtime (Unity) |
|---|---|---|
| **GPL.Core** | atomic file write, seeded RNG, CSV, ID sanitizing, `IClock`, Study ID | `HeadPose` |
| **GPL.Params** | `ParamDef`, `ParameterSchema`, `ParameterSet` (typed, hash), `PresetStore` | — |
| **GPL.Logging** | `LogSchema` (common + chamber columns), `EventLog` (flush per row, mirror, safe append) | — |
| **GPL.Session** | `SessionRecord` (cursor, attempts), `ResumeFrom`, `SessionStore`, `DataFiles` | — |
| **GPL.Protocol** | `ProtocolPlan` (stage > block > trial), `ProtocolWalker`, `ProtocolLog`, `SlideDeck`, `OnboardingStages` | `ProtocolScreens` (handover, language, recenter slide, instruction slides, dot check, messages, cue text), `Onboarding`, `SlideSet` |
| **GPL.Input** | binding table, reserved actions | `ActionReader` over `OVRInput`, `GlobalActions` (Stop trigger) |
| **GPL.UI** | localization, number-pad model, motion curves, scroll model | `OverlayPanel`, `UiPointer`, `UiKit` components (`Runtime/UiKit/`, one file per Storybook component), `ModalDialog`, `StopDialog`, `ScrollList`, `ITextEntry` |
| **GPL.Homing** | gate math | `IHomingMethod` + methods (section 8), recenter listener, fixation cue |
| **GPL.Acoustics** | `Rt60Estimator`, `Rt60Calibration` (measured table, V/S scaling), room math | `ShoeboxRoom`, `ReverbTuner`, `Rt60Meter`, `ClipLevels`; Editor: room builder, Reflection mixer, audio build-settings guard |
| **GPL.HearingLoss** | `ANSIS344`, presets, `HearingProfile`, peaking biquad | `OutputHearingLoss` on the final stereo output (section 11) |
| **GPL.Stimuli** | `StimulusRecipe` (tone, Gaussian white / pink noise, ramps, RMS level, seed; log `Note()`), `Synth`, `Fft`, `StandardRecipes` (loops, published stimuli) | `StimulusClips` (recipe → in-memory PCM `AudioClip`, recordings leveled at load), `StimulusLibrary` (credited recordings in `Library/`); [ADR-0008](decisions/ADR-0008-audio-stimuli.md) |
| **GPL.SpatialSources** | placement math, floor drag | `PlacedSource`, `PulseTrain`, `BackgroundBed`, `FloorDragSource`; planned: constrained random placement, trajectories |
| **GPL.Responses** | adjustment state and result records | `TriggerAdjust`; planned: pointing ray, place-a-marker, button choice |
| **GPL.DebugBench** | `WorkingCopy` (the Playground) | `DebugBench` pages: Config, Parameters, Presets, Log files |

Per-module detail: each module's README ([index](../Assets/AcousticChamber/Modules/README.md)).

## 5. App shell (`Assets/AcousticChamber/App`)
- **`ChamberDefinition`** (ScriptableObject in each chamber folder): `id` (the chamber name, e.g. `HumPerHW2`: folder, data folder, file names, `chamber` column), `label` (display ID `C01`..., `C00` = the hidden Template; Hall and README only), title (English / 中文), description, thumbnail, scene, `version` (written into every log row), supported modes, default `ResumePolicy`, input bindings, posture, enabled flag.
- **Chamber ID rule** (PB-06a): letters, digits and `-` only (it is a token of data file names split on `_`). The folder is the ID or `_` + ID; the definition is `Chambers/<Folder>/<Folder>.ChamberDefinition.asset`.
- **`ChamberCatalog`** (`App/ChamberCatalog.asset`): the Hall list, ordered by label, then ID. `Magic > App > Sync Chambers` collects every `ChamberDefinition`, leaves out (with an error) those that break the ID rule, lack their scene or repeat an ID, updates the catalog and sets the build scene list to MainHall + chamber scenes. Removing a chamber = delete its folder + Sync. `Validate Setup` flags orphans.
- **Main Hall** (`MainHall.unity`, `HallMenu`): own rig and GUI; 2-column card grid of the enabled chambers (title, number of unfinished sessions). No Study ID and no Debug mode here: Debug mode lives in each chamber (its menu's Config button).
- **Sandbox** (`App/Scenes/Sandbox.unity`, not in the build): the module test panels (UI, Input, Debug bench, Homing); open in the Editor and Play over Link.
- **`SessionRouter`** (`App/Core`; run by `HallMenu` once per app start): if the last used chamber (`persistentDataPath/app.state`, key `last_chamber`, written on entering a chamber) has unfinished sessions, the app opens **that chamber's menu**; otherwise the Hall.
- **Scenes:** one per chamber with its own Building Blocks rig, loaded singly (`AppNav`) behind a fade to black (0.3 s out, 0.3 s in; Meta `OVRScreenFade` on the center eye, added by `SceneFade`; overlay panels hide first because the fade does not cover compositor layers). A scene switch resets `Time.timeScale` to 1.

## 6. Chamber menu and session flow
Every study chamber starts at its **chamber menu** (generic, `GPL.UI` + `GPL.Session`):
- **Study ID** (number pad, up to 4 digits, shown `1` -> `S0001`), **Start**, **Config**, **Back to Hall**. All chambers are equal: same menu, presets, Config, sessions.
- Status lines under the buttons: **"Audio level: 10 (target 8)"** (section 8), **"Preset: Custom"** (nothing for Default) and **"Unfinished: S0005, S0012"** (Study IDs with a resumable session; nothing when none). Sessions are managed on Config > Log files.
- **Start with a Study ID that has an unfinished session** -> Resume / Start new / Back. One unfinished session per Study ID.
- **Resume**: the experimenter hands the headset over; the participant continues **silently** at the point given by the resume policy (no welcome screen; homing still runs if the stage requires it).
- **Start new** over an unfinished session: the old one is logged `session_superseded` and closed (never deleted).

### 6.1 Session record
One key=value file per session, `<base>.session`, next to its CSV (atomic write; names and paths: [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#files-and-paths)). Real sessions only; Debug runs have a CSV and no session file.
Keys: `session_id, chamber, chamber_version, study_id, status (InProgress | Completed | DroppedOut | Superseded), seed, language, preset, params_hash, plan_hash, app_version, created, updated, cursor.{stage, block, trials_done, attempt}, resume_from, attempt.<stage>.<block>, param.<key> (full snapshot), custom.<key>`.
There is no pause: a session stays `InProgress` until it completes, drops out or is superseded; quitting the app or taking the headset off leaves it resumable.

**A resumed session uses its own parameter snapshot**, not the active preset; it is refused only when the chamber's protocol plan changed (`plan_hash`).

### 6.2 Resume policy (per stage, overridable per block)
Definitions and attempt counting: [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#resume-policies-resumefrom).
| `ResumeFrom` | Typical use |
|---|---|
| `NextTrial` | fixed trial lists (HW2) |
| `BlockStart` | staircases, continuous blocks, adaptation to a condition |
| `StageStart` | practice, calibration, onboarding |
| `SessionStart` | very short designs |
| `NotResumable` | designs where a break invalidates data |

Per-stage `OnResume` hooks: re-apply the condition, restart the background, re-run homing, an optional re-familiarization trial. The plan is rebuilt from the stored seed; a different `plan_hash` blocks the resume with an explanation.

**Interrupted partial blocks:** with `BlockStart`, trials finished before the interruption stay in the CSV with `attempt = 1`; the redone block has `attempt = 2`. The default analysis uses **only the highest complete attempt of each block**; each chamber's AnalysisPlan states this.

## 7. Parameters and presets (`GPL.Params`)
Decision (Default and Custom presets, Playground, Save as Custom, Make active, real runs on the active preset, snapshot per session): [ADR-0002](decisions/ADR-0002-parameters-and-presets.md). Config pages: [GPL.DebugBench README](../Assets/AcousticChamber/Modules/DebugBench/README.md). Architecture-level points:
- Each chamber declares a **parameter schema** (e.g. `rt60_s`, `trials_per_block`, `gate.yaw_tol_deg`) and ships `Chambers/<Id>/Config/default.params` (key=value, in git) = the preset **Default**.
- Presets live on the device (`persistentDataPath/<ChamberId>/presets/`); copy them off like data; useful ones can be committed into `Config/`.
- **Run experiment trials** (Config) runs the real protocol with the Playground values, logged `_DEBUG`, no resume.
- The CSV gets `preset` and `params_hash` columns.

## 8. Posture, handover and homing (`GPL.Homing`)
- Each chamber declares a posture: `Seated`, `Standing` or `RoomScale`. Tracking origin is floor level.
- **Handover**: the experimenter sets up (often standing) and hands the headset over; "forward" is set by the participant's Meta-button recenter and checked by the head gate. A chamber that needs it also measures eye height there, so stimuli are placed relative to the participant.
- **Onboarding sequence** (PB-20b; stock stages in `GPL.Protocol` `OnboardingStages` / `Onboarding`; order = the chamber's stage list, each stage switchable by a preset boolean `onboarding.<stage>`):
  1. Chamber menu: Study ID, Start. A status line shows the headset volume read from Android against the target (`volume.target_step`): "Audio level: 10 (target 8)" (red when they differ). The value is logged on `session_start` / `session_resume`; no volume pad (kept in code, `ChamberMenu.AskVolumeStep`).
  2. `handover` (experimenter): title "Protocol", a numbered list "1. Set the IPD. 2. Hand the headset to the participant." + Continue. No recenter here.
  3. `language` (participant).
  4. `recenter`: a slide (title, illustration or the text "Look straight ahead. Hold the Meta button until the view recenters.", Back "← Language", Next always active, no A button); `homing` row, method `system_recenter_user`, outcome `cancelled` when Next came without a recenter.
  5. `instructions`: the chamber's slides (`SlideSet`; none = nothing shown).
  6. `ready`: final check at the black dot (head gate), then a `volume_check` row with the final volume.
  7. Practice and trials (A only as the Template's trial response), then thanks.

  This list is the single description of the sequence; slide layout: [DesignSystem](DesignSystem.md) section 9; screen API: [GPL.Protocol README](../Assets/AcousticChamber/Modules/Protocol/README.md); a chamber's stage table: its `Docs/Design.md`. The participant is not told about Y: breaks and drop-outs are handled by the experimenter.
- Homing methods implement `IHomingMethod` -> `HomingResult` (origin, yaw, eye height, method, duration, residual error), logged as `homing`:

| Method | What happens |
|---|---|
| `HeadGate` | fixation cue ahead, head within yaw/pitch tolerance for a hold time (before every trial); defaults and preset keys: [GPL.Homing README](../Assets/AcousticChamber/Modules/Homing/README.md) |
| `SystemRecenterListener` | reacts to the Meta long-press recenter; re-places GUI and head-relative stimuli (always on) |
| `SystemRecenter` | app-requested (unused by the onboarding): the Meta system recenter makes the current head yaw/position forward/origin; yaw measured before (correction) and after (residual). The rig stays at identity, so raw tracking poses and logged values share one frame |
| `AlignToTarget`, `AudioHoming`, `RoomAnchor` | not built (turn to a visible target; face a sound ahead; room-scale alignment) |

## 9. Input bindings and text entry
- **Global actions** are the same in every chamber. Reserved: **Y (left) = Stop dialog**; the Meta button belongs to the system. A chamber may remap a global action only by declaring it in its `ChamberDefinition` bindings table; its menu then shows the binding.
- **Chamber actions** (confirm, adjust, grab, ...) are declared in the same table (`ChamberBehaviour.DeclareActions`, called by the chamber menu after `confirm` and `back`) and read through `GPL.Input`.
- **Standard actions** (a chamber declares only those it uses): `confirm` = A, `back` = B, `trigger_left/right`, `grip_left/right`, `stick_left/right`; X is free. Y works only while a session (real or Debug run) is running.
- **Stop dialog (Y)**: modal, session frozen (time scale 0, audio paused); scene objects that could cover it (`ProtocolChamber.ViewBlockers`) are hidden. Real: Keep going / Drop out (`participant_dropout`, not resumable); Debug: Keep going / Back to the chamber menu. Behaviour: [ADR-0003](decisions/ADR-0003-sessions-resume-input.md); look: [DesignSystem](DesignSystem.md) section 7.
- **Text entry** is swappable (`ITextEntry`): `UiNumberPad` (on-canvas, default), `KeypadEntry` (letters + digits), `NativeKeyboardEntry` (Unity `TouchScreenKeyboard`; PB-14, deferred). Meta's in-app Virtual Keyboard is not in Core SDK 207.

## 10. Logging scheme
Long format, one row per event: common columns, the chamber's columns (`LogSchema`), then `note` (always last); list and rules: [GPL.Logging README](../Assets/AcousticChamber/Modules/Logging/README.md#columns). RM-ANOVA factors are **explicit columns** (e.g. `hl_side`, `hl_level`, `rt60_target_s`), never parsed from notes.

Common events: `session_start` (app version, device, headset volume step), `session_resume`, `stage_begin/end`, `block_begin/end`, `block_restart`, `trial`, `homing`, `recenter`, `handover`, `slide`, `volume_check`, `app_paused/app_resumed`, `participant_dropout`, `session_superseded`, `session_end`, `debug_*`, `param_change` (Config).

Files: one CSV per session (a resume appends) plus its `.session` file (holds the parameter snapshot); names and paths: [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#files-and-paths). Each chamber documents: `AnalysisPlan.md` (within factors, levels, DV, per-cell aggregation, exclusions: Debug, dropout, superseded, lower attempts) and `DataFormat.md` for an analyst (human or AI: files, columns, units, events, how to rebuild the trial table and per-cell means).

### 10.1 Data manager (Config > Log files, every chamber)
MQDH does not list the per-app folder, so files are managed in the headset.
- Page of `GPL.DebugBench` over `GPL.Session` `DataFiles`; sees only **that chamber's folder** (`persistentDataPath/<ChamberId>/`); presets are untouched.
- One entry per session = the CSV + session file pair (Debug runs: CSV only) with date/time, Study ID, mode (real / DEBUG), status, size, trial rows; shown as one line (the file name).
- **Delete** one entry (both files) or **Delete all**, each after a confirmation dialog ("Delete all" names the count). Deleting an unfinished session removes it from the resume list. Deletions are not logged in any data file; the console mirror prints them.
- Copying files off the headset: [GPL.Session README](../Assets/AcousticChamber/Modules/Session/README.md#files-and-paths).

## 11. Hearing loss on the device output (`GPL.HearingLoss`)
- Applied to the **final left/right output** of the app, after the spatializer, room and mixer; not on sources or files.
- `OnAudioFilterRead` on the `AudioListener` (interleaved L/R), two independent 10-band peaking banks. `Rt60Meter` taps the same point. A per-ear test-tone check is part of the module.
- `HearingProfile { left[10], right[10] }` from None / presets / manual / ANSI S3.44 prediction; unilateral = one side None. Part of the parameters and logged.
- Limits for every write-up: audibility only (band attenuation, Q = 12); no recruitment, frequency-selectivity or temporal effects; unofficial port of the NIOSH tool (HLSIM_GUIDELINES section 12 decisions open).

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
**Stimuli and assets:** tones and noise are generated with GPL.Stimuli from a recipe in the chamber's design (no file); recordings,
images and models live in the chamber's own `Audio/`, `Images/`, `Models/` (never another chamber's folder), and every third-party
asset has an entry in `docs/AssetCredits.md` before it is committed ([ADR-0008](decisions/ADR-0008-audio-stimuli.md),
[ADR-0009](decisions/ADR-0009-asset-credits.md)).
**Chamber logic:** a chamber's MonoBehaviour derives from `ProtocolChamber` (App; itself a `ChamberBehaviour`), which owns the run loop (walker + structure rows), the onboarding stages, the head gate before a trial, block break, thanks, recenter rows, the plan-hash check on resume and the cleanup after Stop. The chamber overrides `Schema`, `LogSchema`, `CreatePlan`, `RunTrial` and optionally `RunStage` / `BlockEnd` / `OnAbort` / `BeforeRow` (sets context columns such as `condition` before a structure row). A chamber without a staged protocol may derive from `ChamberBehaviour` directly. A chamber supplies a protocol (stages + resume policies), a trial runner, condition appliers (push parameter values into modules; shared by real runs and Debug), log columns, parameter schema, bindings, posture and docs.

Chambers (independent; order is a backlog decision):
- **C04 HumPerHW2** — HW2 rebuilt on the modules (same data format where possible).
- **C03 HearingLoss**, **C01 MovingSource**, **C02 HiddenSource** — experience chambers (ADR-0006): own panel, no logs.
- Realistic room, outdoor (later).
- **_Template** (C00, hidden) — minimal working chamber to copy: `Magic > App > New Chamber...` copies it to `Chambers/<Id>` with new GUIDs and renamed code, menus and paths; docs become skeletons.

## 13. Scene setup, validation, tests
- Menu **Magic**: `Magic > App > ...`, `Magic > <ChamberId> > Step 1..N, Hot Fix`, `Magic > Validate Setup` (all catalog scenes). Module Editor code provides reusable builders (`RoomBuilder`, `MixerSetup`, `OverlayCanvasBuilder`) that chamber steps call.
- Headless run of all steps + tests on the mirror project: `Tools/Verify-Headless.ps1` (see CLAUDE.md).
- EditMode tests per module and per chamber; HLSim test vectors must match exactly.
