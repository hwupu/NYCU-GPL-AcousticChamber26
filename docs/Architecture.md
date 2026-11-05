# Architecture — AcousticChamber26

Status: **accepted in principle** (2026-11-05, PO answers in [Questions.md](Questions.md)); details are refined ticket by ticket. Decisions: [decisions/](decisions/).

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
    Chambers/_Template/      copy to start a new chamber
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
Rule: a module may reference `GPL.Core` and the external packages named in its README, nothing else. HW2 code is **refactored** into these modules: generic parts only; HW2-specific logic goes to the `HumPerHW2` chamber.

| Module | Core (pure, unit-tested) | Runtime (Unity) | From HW2 |
|---|---|---|---|
| **GPL.Core** | atomic file write, seeded RNG, CSV escaping, azimuth/pose math, ID sanitizing, `IClock` | console prefix, `HeadPose` (wait for valid height) | `Maths`, parts of `ExperimentLog` |
| **GPL.Params** | `ParameterSchema` (name, type, default, min/max/step, label, group), `ParameterSet` (flat key/value, typed access, hash), `PresetStore` (default file = preset "Default", presets, active preset, Debug working copy) | `ParameterPanel` (generates Debug controls from the schema) | new (section 7) |
| **GPL.Logging** | `LogSchema` (common + chamber columns), `EventLog` (flush per row, mirror), safe append on resume | `EventLogHost` (persistentDataPath, pause/resume events) | `ExperimentLog` generalized |
| **GPL.Session** | `SessionRecord`, `SessionCursor`, `ResumePolicy`, `SessionStore` (atomic, list unfinished per chamber), `DataFiles` (pair each CSV with its session file, list, delete one / all) | `DataManagerPanel` (section 10.1) | `SessionProgress` / `ProgressStore` generalized |
| **GPL.Protocol** | stage tree (stage > block > trial), `TrialPlan`, planners (seeded shuffle, Latin square, balanced repeats, forbidden values), resume resolution | `ProtocolRunner` (coroutine state machine), stock stages (Language, Onboarding slides, Handover, Practice, Block, Rest, Thanks) | `ExperimentFlow`, `TrialPlanner` split |
| **GPL.Input** | binding table (action -> button, controller), reserved actions | `ActionReader` over `OVRInput`, `GlobalActions` (Stop dialog trigger) | Y-button and A/trigger handling |
| **GPL.UI** | localization table, slide navigation | overlay canvas, `UiPointer`, `PanelPlacer`, `Dialog`, `SlidePanel`, `LocalizedText`, HUD, **`ITextEntry`** (section 9) | `UiPointer`, `GuiPanelPlacer`, `ParticipantIdField`, `OnboardingPanel`, `LocalizedText` |
| **GPL.Homing** | gate math (tolerance, hold time) | `IHomingMethod` + methods (section 8), recenter listener, fixation cue | head gate, recenter handling |
| **GPL.Acoustics** | `Rt60Estimator`, `Rt60Calibration` (measured table, V/S scaling), room math | `ShoeboxRoom`, `ReverbTuner`, `Rt60Meter`, `ClipLevels`; Editor: room dialog, Reflection mixer, audio build-settings guard | acoustics files |
| **GPL.HearingLoss** | `ANSIS344`, presets, `HearingProfile` (left/right bands), peaking biquad | `OutputHearingLoss` on the final stereo output (section 11), optional spectrum view | new (HLSIM_GUIDELINES) |
| **GPL.SpatialSources** | placement math, constrained random placement, trajectories | `PlacedSource` (room-fixed / head-relative / re-placed after recenter), `MovingSource`, `GrabbableSource`, `PulseTrain`, `BackgroundBed` | `StimulusController`, background code split |
| **GPL.Responses** | response records (azimuth, elevation, distance, error) | rotate-with-triggers adjustment, pointing ray, place-a-marker, button choice | HW2 task |
| **GPL.DebugBench** | — | panel framework: modules register controls; hosts the `ParameterPanel`; "Run experiment trials" | `DebugPanel` generalized |

Modules stay plain folders (copy the folder plus `GPL.Core`). Turning a stable module into a Unity package later is optional.

## 5. App shell (`Assets/AcousticChamber/App`)
- **`ChamberDefinition`** (ScriptableObject in each chamber folder): `id` (the chamber name, e.g. `HumPerHW2`: folder, data folder, file names, `chamber` column), `label` (display ID `C01`, `C02`, ...; Hall and README only, may change without touching data), title (English / 中文), description, thumbnail, scene, `version` (written into every log row), supported modes, default `ResumePolicy`, input bindings, posture, enabled flag.
- **`ChamberCatalog`**: list shown in the Hall. `Magic > App > Sync Chambers` collects every `ChamberDefinition`, updates the catalog and the build profile scene list. Removing a chamber = delete its folder + Sync. `Validate Setup` flags orphans.
- **Main Hall** (`MainHall.unity`): own rig and GUI; panel listing the chambers (title, number of unfinished sessions). No Study ID here.
- **`SessionRouter`**: on launch, if the last used chamber has unfinished sessions it opens **that chamber's menu** (not the trials). Otherwise the Hall.
- **Scenes:** one scene per chamber, each with its own Building Blocks rig, loaded singly behind a fade. A small persistent object hands over only the mode (real / Debug). The participant language is chosen inside the chamber session (Language stage, stored in the session record); system screens are English only.

## 6. Chamber menu and session flow
Every chamber starts at its **chamber menu** (generic, from `GPL.UI` + `GPL.Session`, HW2 style):
- **Study ID** (number pad, up to 4 digits, stored and shown with leading zeros and prefix: `1` -> `S0001`), **Start**, **Debug**, **Back to Hall**. All chambers are equal: same menu, presets, Debug, sessions. A chamber that is still a demo or under development is simply used through Debug.
- Status lines: **parameters in use** ("Preset: Pilot-A", section 7) and the **unfinished sessions** of this chamber ("S0012: Block 2 of 4, 3 of 8 trials done").
- **Start with a Study ID that has an unfinished session** -> Resume / Start new / Back. Several unfinished sessions can exist (one per Study ID).
- **Resume** -> the experimenter hands the headset over; the participant continues **silently** at the point given by the resume policy (no welcome screen; homing still runs if the stage requires it).
- **Start new** over an unfinished session: the old one is logged `session_superseded` and closed (never deleted).

### 6.1 Session record
One key=value text file per session, `<base>.session`, **next to its CSV** `<base>.csv` in `persistentDataPath/<ChamberId>/` (atomic write; the base name `yyyyMMdd_HHmmss_<ChamberId>_<StudyId>` is the session ID). Real sessions only; Debug runs have a CSV and no session file. Implemented in `GPL.Session` (PB-04):
`session_id, chamber, chamber_version, study_id, status (InProgress | Completed | DroppedOut | Superseded), seed, language, preset, params_hash, plan_hash, app_version, created, updated, cursor.{stage, block, trials_done, attempt}, resume_from, attempt.<stage>.<block>, param.<key> (full snapshot), custom.<key>`. The Stop dialog has no pause: a session stays `InProgress` until it completes, drops out or is superseded; quitting the app or taking the headset off leaves it resumable.

**A resumed session uses its own parameter snapshot**, not the currently active preset (the menu warns if they differ).

### 6.2 Resume policy (per stage, overridable per block)
| `ResumeFrom` | Meaning | Typical use |
|---|---|---|
| `NextTrial` | continue after the last finished trial | fixed trial lists (HW2) |
| `BlockStart` | redo the interrupted block; `attempt` + 1 | staircases, continuous blocks, adaptation to a condition |
| `StageStart` | redo the stage | practice, calibration, onboarding |
| `SessionStart` | redo everything (same CSV, attempt + 1) | very short designs |
| `NotResumable` | session closed; start a new one | designs where a break invalidates data |

Resume hooks per stage (`OnResume`): re-apply the condition, restart the background, re-run homing, an optional re-familiarization trial. The plan is rebuilt from the stored seed; a different `plan_hash` (chamber changed) blocks the resume with an explanation.

**Interrupted partial blocks in the data:** with `BlockStart`, the trials finished before the interruption stay in the CSV with `attempt = 1`, and the redone block has `attempt = 2`. The default analysis rule uses **only the highest complete attempt of each block**. Each chamber's AnalysisPlan states this.

## 7. Parameters and presets (`GPL.Params`)
Fixes the HW2 problem that Debug changes did not carry over to real trials.
- Each chamber declares a **parameter schema** (e.g. `rt60_s`, `stim_radius_m`, `hl_left`, `trials_per_block`, `gate_yaw_tol_deg`) and ships a **default file** `Chambers/<Id>/Config/default.params` (key=value text, versioned in git). It appears as the preset **Default**, which is the active preset until another one is made active.
- **Presets** are saved on the device (`persistentDataPath/<ChamberId>/presets/<name>.params`); one is **active**. Copy presets off the headset like data; useful ones can be committed back into `Config/`.
- **Debug edits a working copy** of the active preset (generated controls from the schema, plus module controls). Actions: *Save as preset*, *Overwrite preset*, *Make active*, *Revert*, and **Run experiment trials** (the real protocol with the current working copy, logged as `_DEBUG`, no resume) to experience exactly what participants will get.
- **Real runs always use the active preset** (never unsaved Debug values). The chamber menu shows it next to the Study ID: "Preset: Pilot-A". Every real session stores the full snapshot, preset name and hash; the CSV gets `preset` and `params_hash` columns.
- Debug-only switches (marker visibility, show error) are not part of presets.

## 8. Posture, handover and homing (`GPL.Homing`)
- Each chamber declares a posture: `Seated`, `Standing` or `RoomScale`. Tracking origin is always floor level.
- **Handover stage** (stock stage, used at session start and after resume): the experimenter sets up (often standing), hands over, and the participant confirms when ready; then the chamber's homing method sets the origin (and, if needed, measures eye height so stimuli are placed relative to the participant, not the experimenter).
- Homing methods implement `IHomingMethod` -> `HomingResult` (origin, yaw, eye height, method, duration, residual error), logged as `homing`:

| Method | What happens |
|---|---|
| `HeadGate` | fixation cue ahead, head within yaw/pitch tolerance for a hold time (HW2, before every trial) |
| `SystemRecenterListener` | reacts to the Meta long-press recenter; re-places GUI and head-relative stimuli (always on) |
| `SoftwareRecenter` | current head yaw/position becomes forward/origin without a system call (handover, experimenter button) |
| `AlignToTarget` | participant turns to a visible target; the room stays fixed |
| `AudioHoming` | a sound straight ahead; face it and confirm (designs without visuals) |
| `RoomAnchor` | room-scale: no re-centering of position; the virtual room is aligned once to the physical room (setup-time) |

## 9. Input bindings and text entry
- **Global actions** are the same in every chamber where possible. Reserved: **Y (left controller) = Stop dialog**. The Meta button belongs to the system. A chamber may remap a global action only by declaring it in its `ChamberDefinition` (bindings table); its menu then shows the binding.
- **Chamber actions** (confirm, adjust left/right, grab, ...) are declared per chamber in the same table and read through `GPL.Input`, never with direct `OVRInput` calls in chamber code.
- **Standard chamber actions** (defaults; a chamber declares only those it uses): `confirm` = A, `back` = B, `trigger_left/right`, `grip_left/right`, `stick_left/right`; X is free. Y works only while a session (real or Debug run) is running; in the Hall and the menus it does nothing.
- **Stop dialog (Y)**, modal, the session is frozen while open (time scale 0, audio paused):
  - **Keep going**
  - **Drop out** (participant withdraws; logged `participant_dropout`; not resumable; asks once more: "Drop out S0012?")
  - In Debug: Keep going / Back to the chamber menu.
- **Text entry is swappable** (`ITextEntry`): `NumberPadEntry` (on-canvas, reliable, HW2), `KeypadEntry` (letters + digits), `NativeKeyboardEntry` (Unity `TouchScreenKeyboard`, the Quest system keyboard). Selected per chamber or globally; the native keyboard problem from HW2 is investigated in its own ticket. (Meta's in-app Virtual Keyboard is not part of the installed Core SDK 207.)

## 10. Logging scheme
Long format, one row per event. Common columns (every chamber):
`event, timestamp_local, elapsed_s, session_id, chamber, chamber_version, study_id, mode, preset, params_hash, stage, block, condition, trial, attempt, note`
then the chamber's columns, then `note` (always the last column) (declared in its `LogSchema`; writing an undeclared column fails a unit test). RM-ANOVA factors are **explicit columns** (e.g. `hl_side`, `hl_level`, `rt60_target_s`), never parsed from notes.

Common events: `session_start` (app version, device, headset volume step entered by the experimenter), `session_resume`, `stage_begin/end`, `block_begin/end`, `block_restart`, `trial`, `homing`, `recenter`, `handover`, `app_paused/app_resumed`, `participant_dropout`, `session_superseded`, `session_end`, `debug_*`, `param_change` (Debug).

Files: `persistentDataPath/<ChamberId>/yyyyMMdd_HHmmss_<ChamberId>[_<StudyId>][_DEBUG].csv` (resume appends) and the `.session` file (the parameter snapshot lives there). Each chamber's `AnalysisPlan.md`: within factors, levels, DV, per-cell aggregation, exclusions (Debug, dropout, superseded, lower attempts). Each chamber also has `DataFormat.md` written for an analyst (human or AI): files, columns, units, events, how to rebuild the trial table and the per-cell means. No analysis scripts in the repo.

### 10.1 Data manager (in each chamber's Debug menu)
MQDH does not list the per-app folder, so files are managed in the headset.
- Shared component (`GPL.Session` `DataManagerPanel`), placed in every chamber's Debug menu; it only sees **that chamber's folder** (`persistentDataPath/<ChamberId>/`).
- One entry per session = the **pair** CSV + session file (Debug runs: CSV only). Shows date/time, Study ID, mode (real / DEBUG), status (in progress, completed, paused, dropped out, superseded), size, number of trial rows.
- **Delete** one entry (both files together) or **Delete all**; each asks for confirmation in a modal dialog ("Delete all" names the count and needs a second confirm). Deleting an unfinished session removes it from the resume list.
- Presets are not touched by the data manager.
- Deletions are not logged in any data file (the file is gone); the console mirror prints them.
- Still valid for copying files off the headset: Windows Explorer (`Quest 3 > Internal shared storage > Android > data > com.nycu.AcousticChamber26 > files`) or `adb pull` on macOS (HW2 README).

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
A chamber supplies a protocol (stages + resume policies), a trial runner (`ITrialRunner`), condition appliers (push parameter values into modules; shared by real runs and Debug), its log columns, parameter schema, bindings, posture and docs.

Planned chambers (independent; order is a backlog decision):
- **HumPerHW2** — HW2 rebuilt on the modules (proves the refactoring; same data format where possible).
- **HearingLoss** — unilateral / bilateral loss on the output.
- **MovingSource** — move a source (grab / scripted) to experience spatial audio.
- **HiddenSource** — locate an invisible source placed at random.
- Realistic room, outdoor (later).
- **_Template** — minimal working chamber to copy.

## 13. Scene setup, validation, tests
- Menu **Magic**: `Magic > App > ...`, `Magic > <ChamberId> > Step 1..N, Hot Fix`, `Magic > Validate Setup` (all catalog scenes). Module Editor code provides reusable builders (`RoomBuilder`, `MixerSetup`, `OverlayCanvasBuilder`) that chamber steps call.
- Headless entry point to run all steps + tests on the mirror project (see CLAUDE.md).
- EditMode tests per module and per chamber; HLSim test vectors must match exactly.

## 14. Build order
1. `GPL.Core`, `GPL.Params`, `GPL.Logging`, `GPL.Session` (pure) + tests, ported and generalized from HW2.
2. `GPL.Input`, `GPL.UI` (pointer, overlay, number pad behind `ITextEntry`, dialog), `GPL.Homing` runtime.
3. Hall + catalog + routing + `_Template` chamber (chamber menu, Stop dialog, presets, resume variants). **PO headset test.**
4. HumPerHW2 chamber (HW2 on the modules) incl. `GPL.Acoustics`, `GPL.SpatialSources`, `GPL.Responses`, `GPL.DebugBench`. **PO headset test**; compare a CSV with HW2.
5. `GPL.HearingLoss` + HearingLoss chamber.
6. Further chambers.
