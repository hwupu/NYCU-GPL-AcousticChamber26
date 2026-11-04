# Architecture — AcousticChamber26

Status: **PROPOSED** (2026-11-05). Open questions are marked **Q#** and collected in [Questions.md](Questions.md). Decisions become ADRs in [decisions/](decisions/) once the PO approves them.

## 1. Goals
1. **Exploratory lab.** Many experiment designs ("chambers") in one app: white rooms, realistic rooms, outdoor scenes. They are added, removed and refined over time without breaking each other.
2. **Plug-and-play modules.** Every feature (hearing-loss simulation, homing, resume, logging, onboarding, RT60 tools, ...) lives in a self-contained folder that can be copied into another Unity project.
3. **Data integrity in the field.** Per-row flushed logs, resume after any interruption, never delete data, logging schema designed for the analysis (RM-ANOVA) before coding.
4. **Experimenter ergonomics.** Main Hall for choosing a chamber, auto-routing to an unfinished session, a Debug test bench, a cancel dialog, a validated scene setup.

Non-goals (for now): networking/multi-user, cloud upload, runtime asset downloads.

## 2. Layers

```
┌────────────────────────────────────────────────────────────────────────┐
│ Chambers   Assets/Chambers/<Id>/   one experiment design per folder     │
│            protocol, trial runner, stimuli, scene, log columns, docs    │
├────────────────────────────────────────────────────────────────────────┤
│ App shell  Assets/App/             Main Hall, ChamberCatalog,           │
│            SessionRouter (boot + auto-resume), scene transitions        │
├────────────────────────────────────────────────────────────────────────┤
│ Modules    Assets/GPL/<Module>/    portable, project-agnostic           │
│            Core (pure C#) + Runtime (Unity) + Editor + Tests + README    │
└────────────────────────────────────────────────────────────────────────┘
   dependencies point downward only; chambers never reference each other
```

## 3. Modules (`Assets/GPL`)

Each module folder:
```
Assets/GPL/<Module>/
  README.md            purpose, dependencies, public API, "copy into another project" steps, provenance
  Core/                GPL.<Module>.Core.asmdef  (noEngineReferences: true)   — optional
  Runtime/             GPL.<Module>.asmdef       (UnityEngine, Meta SDK if needed)
  Editor/              GPL.<Module>.Editor.asmdef (Magic menu steps, inspectors)
  Tests/EditMode/      GPL.<Module>.Tests.asmdef
  Assets/              materials, prefabs, images owned by the module
```
Dependency rule: a module may reference `GPL.Core` and the external packages named in its README, nothing else. Cross-module glue goes in the chamber or the App. A module copied alone (plus `GPL.Core`) must compile.

| Module | Core (pure, unit-tested) | Runtime (Unity) | Source |
|---|---|---|---|
| **GPL.Core** | atomic file write, seeded RNG, CSV escaping, azimuth/pose math, ID sanitizing, `IClock` | `GplLog` console prefix, `HeadPose` (valid-height wait) | HW2 `Maths`, `ExperimentLog` parts |
| **GPL.Logging** | `LogSchema` (common + extra columns), `EventLog` (row dictionary -> CSV, flush per row, mirror), file naming, session JSON snapshot | `EventLogHost` (persistentDataPath, app pause/resume events) | HW2 `ExperimentLog` generalized |
| **GPL.Session** | `SessionRecord`, `SessionCursor`, `ResumePolicy`, `SessionStore` (atomic JSON, list unfinished, active pointer) | — | HW2 `SessionProgress`/`ProgressStore` generalized |
| **GPL.Protocol** | stage graph (stages, blocks, trials), `TrialPlan`, planners (seeded shuffle, Latin square, balanced repeats, forbidden values), resume resolution | `ProtocolRunner` (coroutine state machine), stock stages (Language, Onboarding, Practice, Block, Rest, Thanks), cancel dialog hook | HW2 `ExperimentFlow`, `TrialPlanner` split |
| **GPL.UI** | localization table, onboarding slide navigation | `OverlayCanvas` setup, `UiPointer` (ray vs rect), `NumberPad`, `PanelPlacer`, `Dialog`, `SlidePanel`, `LocalizedText`, HUD strip | HW2 `UiPointer`, `GuiPanelPlacer`, `ParticipantIdField`, `OnboardingPanel`, `LocalizedText` |
| **GPL.Homing** | gate math (yaw/pitch tolerance, hold time) | `IHomingMethod` + methods (section 7), recenter listener, fixation cue | HW2 head gate + recenter handling |
| **GPL.Acoustics** | `Rt60Estimator` (Schroeder, T20/T30), `Rt60Calibration` (measured table, V/S scaling), room math | `ShoeboxRoom` (size shared with visual room), `ReverbTuner`, `Rt60Meter` (listener tap), `ClipLevels`; Editor: room dialog, mixer with Meta Reflection effect, audio build-settings guard | HW2 acoustics files |
| **GPL.HearingLoss** | `ANSIS344`, presets, `HearingProfile` (per-ear bands), peaking biquad, filter-bank state | `ListenerHearingLoss` (post-spatializer, per-ear filter banks on the AudioListener), optional spectrum view | new, from HLSIM_GUIDELINES |
| **GPL.SpatialSources** | placement math, random hidden placement with constraints, path/trajectory generators | `PlacedSource` (fixed in room / head-locked / re-placed after recenter), `MovingSource` (scripted trajectories), `GrabbableSource` (move with controller), `PulseTrain` | HW2 `StimulusController` split |
| **GPL.Responses** | response records (azimuth, elevation, distance, error) | pointing ray response, rotate-with-triggers adjustment (HW2 task), place-a-marker response, button choice | HW2 task + new |
| **GPL.DebugBench** | — | panel framework: modules register controls (toggles, sliders, buttons) so any chamber gets a test bench by composition | HW2 `DebugPanel` generalized |

Module order of implementation is a backlog decision (see `ProductBacklog.md`).

**Packaging (Q5).** Recommendation: start as folders under `Assets/GPL` (fast iteration, Magic menu, easy to read). Promote a module to an embedded UPM package (`Packages/com.nycu.gpl.<module>`) once its API is stable; then other projects can add it by git URL instead of copying.

## 4. App shell (`Assets/App`)
- **`ChamberDefinition`** (ScriptableObject, one per chamber, stored in the chamber folder): `id` (e.g. `C01-HearingLoss`), title (localized), description, thumbnail, scene, `version` (bumped when the design changes; written into every log row), supported modes, default `ResumePolicy`, enabled flag.
- **`ChamberCatalog`** (ScriptableObject): ordered list of definitions shown in the Hall. `Magic > App > Sync Chambers` finds every `ChamberDefinition`, updates the catalog and the build profile scene list; `Validate Setup` reports missing scenes or orphaned entries. **Removing a chamber = delete its folder + Sync.**
- **Main Hall scene** (`Assets/App/Scenes/Hall.unity`): its own rig and GUI; experimenter panel (participant ID, chamber list, mode buttons, unfinished-session list) (**Q2**).
- **`SessionRouter`** (boot logic in the Hall, runs first): reads the active-session pointer; if a real session is unfinished it loads that chamber directly and opens the resume screen there (**Q3**). Otherwise it shows the Hall.
- **Scene strategy:** one scene per chamber, loaded with `LoadScene` (single) behind a fade. **Each scene carries its own Building Blocks rig.** This keeps chambers self-contained (baked lighting, rig and acoustics per scene, copyable) and avoids `DontDestroyOnLoad` fights with the Meta rig. A tiny persistent object carries only the hand-over data (participant ID, mode, session id, language). Alternative (persistent core scene + additive chambers) rejected for now: lighting and Building Blocks complications for little gain.

## 5. Chambers (`Assets/Chambers/<Id>`)
```
Assets/Chambers/C01-HearingLoss/
  C01.ChamberDefinition.asset
  Scenes/C01.unity
  Scripts/      AcousticChamber.Chambers.C01.asmdef — protocol definition, trial runner, condition appliers
  Editor/       Magic > C01 > Step 1..N, Hot Fix
  Tests/EditMode/
  Audio/, Materials/, Models/, Images/   (chamber-owned assets)
  Docs/         Brief.md, Design.md, DataFormat.md, AnalysisPlan.md, SceneSetup.md, decisions/
Tools/C01/      analysis script (CSV -> trials.csv / per_person.csv)
```
A chamber supplies:
1. **Protocol** — the ordered stages and their resume granularity (section 6), built from stock stages plus its own.
2. **Trial runner** — `ITrialRunner.Run(TrialSpec) -> TrialResult` (coroutine): present stimuli, collect the response, return the logged values.
3. **Conditions** — `ConditionSpec` data (e.g. `hl_left=moderate, hl_right=none, rt60=0.7`) and appliers that push a spec into modules (`ListenerHearingLoss`, `ReverbTuner`, background player, ...). The same appliers drive the Debug bench, so "what the participant hears in condition X" can be auditioned exactly.
4. **Log columns** — its extra columns (`LogSchema`), documented in `DataFormat.md` before coding.
5. **Docs** — Brief (INSTRUCTIONS.md study brief table), Design, DataFormat, AnalysisPlan.

`Assets/Chambers/_Template` is a minimal working chamber (one stage, one dummy trial, logging, resume) to copy when starting a new design. `Magic > App > New Chamber...` can automate the copy and renaming.

## 6. Sessions and resume

### 6.1 Session record
One JSON file per session in `persistentDataPath/sessions/` (atomic write: temp, then replace), rewritten after every **checkpoint**:
```
session_id, chamber_id, chamber_version, participant, mode, status (InProgress|Completed|Cancelled|Superseded),
seed, language, csv_file, created, updated, app_version,
cursor { stage, block, trial, attempt },
resume_policy, plan_hash, custom { key: value }   // chamber state, e.g. staircase level
```
A small `active.json` points to the current real session; the Hall reads it at launch.

### 6.2 Cursor and checkpoints
The protocol is a tree: **session > stage > block (condition / sequence) > trial**. The cursor is saved when a trial, block or stage finishes. A trial that was running when the app stopped has no result row and is never counted (as in HW2).

### 6.3 Resume policy (per stage, overridable per block)
| `ResumeFrom` | Meaning | Typical use |
|---|---|---|
| `NextTrial` | continue after the last finished trial | fixed trial lists (HW2) |
| `BlockStart` | redo the interrupted block from its first trial; `attempt` + 1 | adaptive staircases, blocks that must be continuous, adaptation to a hearing-loss condition |
| `StageStart` | redo the whole stage | practice, calibration, onboarding |
| `SessionStart` | start over in the same CSV (attempt + 1) | very short designs |
| `NotResumable` | session is marked cancelled; experimenter starts a new one | designs where a break invalidates the data |

Plus **resume hooks** per stage: `OnResumeEnter` (e.g. a "Welcome back" screen, re-run homing, re-familiarization trial, restart background audio, re-apply the condition). The plan is rebuilt deterministically from the stored seed; `plan_hash` detects a chamber version change between interruption and resume (then the router refuses and explains, instead of silently resuming into a different design).

### 6.4 Experimenter controls
- Resume screen (in the chamber): **Resume** (per policy, shows "C01, Block 2 of 4, 3 of 8 trials done, will restart block 2"), **Start new** (old session gets `participant_cancelled`, status `Superseded`), **Back to Hall**.
- Cancel dialog (Y button, real sessions): freeze, Cancel participant / Keep going (HW2 ADR-0004).
- Debug sessions are never resumed.

## 7. Homing / re-centering (`GPL.Homing`)
All methods implement `IHomingMethod`: a coroutine that ends with a `HomingResult` (origin position, yaw, method, duration, residual yaw/pitch error), which is logged (`homing` event) and becomes the trial origin.

| Method | What happens | Notes |
|---|---|---|
| `HeadGate` | fixation cue ahead; head must stay within yaw/pitch tolerance for a hold time | HW2 default, before every trial |
| `SystemRecenterListener` | reacts to the Meta long-press (`RecenteredPose`), re-places GUI and stimuli | always on, passive |
| `SoftwareRecenter` | rotates/moves the rig so the current head yaw/position becomes forward/origin, no system call | instant re-alignment by the experimenter (Debug or a button) |
| `AlignToTarget` | participant turns to face a visible target (e.g. on a wall or the chair direction); world forward fixed | keeps the room fixed, participant corrects |
| `AudioHoming` | a sound straight ahead; participant faces it and confirms | for "no visual" or hearing-loss designs |
| `PhysicalAnchor` | controller placed on a physical reference (chair arm / table mark); rig aligned to it | real-world alignment across sessions; Q |

Chambers choose a method per stage and per trial (e.g. `AlignToTarget` at block start + `HeadGate` before each trial).

## 8. Logging scheme
**Long format, one row per event, common columns first, chamber columns after.**

Common columns (every chamber):
`event, timestamp_local, elapsed_s, session_id, chamber, chamber_version, participant, mode, stage, block, condition, trial, attempt, homing_yaw_err_deg, note`

Chamber columns are declared in its `LogSchema` (fixed order, header written once; writing an undeclared column is an error caught by unit tests). Factors for the RM-ANOVA are **explicit columns** (e.g. `hl_side`, `hl_level`, `rt60_target_s`) rather than encoded in a condition string, so the analysis script never parses notes.

Common events: `session_start` (note: app version, device, volume step entered by experimenter), `session_resume`, `stage_begin/end`, `block_begin/end`, `block_restart` (reason), `trial`, `homing`, `recenter`, `app_paused/app_resumed`, `participant_cancelled`, `session_end`, `debug_*`.

Files per session (`persistentDataPath/<ChamberId>/`):
- `yyyyMMdd_HHmmss_<ChamberId>[_<ID>][_DEBUG].csv` — events (resume appends, no second header)
- same name `_session.json` — snapshot of every parameter, the plan, seed, versions (for methods sections and audits)

Analysis: each chamber's `AnalysisPlan.md` states the within factors, levels, DV, per-cell aggregation (mean of last valid attempt), exclusion rules (cancelled, `_DEBUG`, superseded attempts) and the script in `Tools/<ChamberId>/`.

## 9. Modes
| Mode | Logged | Resumable | GUI/feedback | Purpose |
|---|---|---|---|---|
| **Experiment** | yes | yes | none during trials | data collection |
| **Debug (test bench)** | `_DEBUG` | no | everything: condition switches, markers, errors, RT60 measure | experimenter check, pilot |
| **Explore** (Q4) | optional | no | factor controls (HL per ear, RT60, background, move the source) | demos, self-experience, design exploration |

## 10. Hearing loss in a spatial scene (`GPL.HearingLoss`)
- The ear is after the head: the loss must apply **after** spatialization (HRTF) and room rendering, separately per ear. Implementation: a filter on the `AudioListener` (`OnAudioFilterRead`, interleaved stereo) with **two independent 10-band peaking banks (left, right)**. HW2's `Rt60Meter` showed that this tap sees the Meta spatializer and reflection output.
- `HearingProfile { left: int[10], right: int[10] }` from None / presets / manual / ANSI S3.44 prediction. Unilateral = one side `None`. Logged in the session snapshot and as columns per condition.
- Limitations to state in every write-up: this simulator models audibility only (attenuation by band, narrow Q = 12). It does not simulate loudness recruitment, reduced frequency selectivity or temporal smearing. The NIOSH port is unofficial (HLSIM_GUIDELINES section 12 decisions: 1500 Hz Xi, 8 kHz Q, licensing).
- It also affects GUI sounds, which is correct for "hearing like this person".

## 11. Scene setup and validation
- `Magic > App > ...` (Hall), `Magic > <ChamberId> > Step 1..N, Hot Fix`, `Magic > Validate Setup` (all scenes in the catalog: unassigned references, unrouted spatial sources, missing mixer effect, overlay layers).
- Module Editor steps are reusable building blocks called from chamber steps (e.g. `RoomBuilder.Build(size)`, `MixerSetup.EnsureReflectionMixer()`, `OverlayCanvasBuilder.Build()`).
- Headless entry point `GPL.Editor.Batch.RunAllSteps` for verification on the mirror project.

## 12. Testing
- EditMode tests per module (pure Core) and per chamber (protocol, planner, resume resolution: "interrupted at block 2 trial 3 with `BlockStart` resumes at block 2 trial 1, attempt 2").
- HLSim: test vectors from HLSIM_GUIDELINES section 9 must match exactly.
- Headset checklists in each chamber's `SceneSetup.md`.

## 13. Proposed first increments (pending PO answers)
0. Repo hygiene, CLAUDE.md, architecture (this document) — **done in this step, except the decisions marked Q**.
1. Port HW2 pure Core into `GPL.Core`, `GPL.Logging`, `GPL.Session` + tests (no scenes yet).
2. `GPL.UI` + `GPL.Homing` runtime; Hall scene with rig, panel, number pad; `ChamberCatalog`; `_Template` chamber; scene routing. **PO headset test.**
3. Resume end-to-end in `_Template` (all `ResumeFrom` variants, kill-the-app test).
4. `GPL.HearingLoss` Core (ANSIS344 + tests), then the listener filter bank, Explore controls. **PO headset test.**
5. First real chamber (Q1) from its approved Design/DataFormat/AnalysisPlan.
