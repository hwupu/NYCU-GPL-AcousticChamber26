# GPL modules

Generic, portable building blocks for VR perception studies (Unity 6, Meta Quest). Each folder is one module with its own README, assembly definitions and tests. **To reuse a module in another project, copy its folder plus `Core/`** (every module depends on `GPL.Core` except HearingLoss; other dependencies are in the module's README).

| Module | Assembly (pure C#) | What it does | Depends on |
|---|---|---|---|
| [Core](Core/README.md) | `GPL.Core` | clock, atomic files, CSV, invariant formatting, Study ID, deterministic RNG, key=value text, hashes, data file names | — |
| [Params](Params/README.md) | `GPL.Params.Core` | parameter schema, validated parameter sets, presets with an active preset | Core |
| [Logging](Logging/README.md) | `GPL.Logging.Core` | CSV event log: common + chamber columns, flushed per row, safe append on resume | Core |
| [Session](Session/README.md) | `GPL.Session.Core` | session record, cursor, attempts, resume policies, session store, data manager (list/delete file pairs) | Core |
| [UI](UI/README.md) | `GPL.UI.Core` + `GPL.UI` | overlay canvas placement, ray pointer, `UiKit` components (buttons, rows, sections, switches, number pad, lists, pages, panels, dialogs, sheets), TextMeshPro text (English + 中文), scroll lists, localization | Core, Meta XR Core, uGUI/TMP |
| [Input](Input/README.md) | `GPL.Input.Core` + `GPL.Input` | binding table (action -> controller button), action reader over OVRInput, global Stop action (Y) | Core, Meta XR Core |
| [DebugBench](DebugBench/README.md) | `GPL.DebugBench.Core` + `GPL.DebugBench` | a chamber's Config pages: Playground (working copy of the active preset), Presets, Parameters, Log files | Core, Params, Session, UI |
| [Homing](Homing/README.md) | `GPL.Homing.Core` + `GPL.Homing` | head gate with a fixation dot, app-requested system recenter, Meta-button recenter listener; results as `homing` / `recenter` notes; gate as preset parameters | Core, Params, Meta XR Core |
| [Protocol](Protocol/README.md) | `GPL.Protocol.Core` + `GPL.Protocol` | stage tree (stage > block > trial) walked with the session cursor: resume policies, attempts, structure log rows; stock screens (handover, language, recenter slide, instruction slides, dot check, messages, cue text) | Core, Session, Logging; screens: UI, Input, Homing, Meta XR Core |
| [Acoustics](Acoustics/README.md) | `GPL.Acoustics.Core` + `GPL.Acoustics` | Meta XR Audio shoebox: RT60 target via a measured coefficient table, RT60 measurement of the rendered output (Schroeder T20/T30), clip levels; Editor: Reflection mixer, room builder, audio build-settings guard | Core, Meta XR Audio, URP |
| [Stimuli](Stimuli/README.md) | `GPL.Stimuli.Core` + `GPL.Stimuli` | generated auditory stimuli: tones, Gaussian white and pink noise (exact -3 dB/octave), cos² / linear ramps, RMS level, seeded tokens, recipes from published papers, in-memory PCM clips; a library of credited anechoic recordings (OpenAIR, CC BY-SA); rules for recordings (preparation, import, level, licences) | Core |
| [SpatialSources](SpatialSources/README.md) | `GPL.SpatialSources.Core` + `GPL.SpatialSources` | sounds on a circle around an origin (re-placed or room-fixed), a source dragged on the floor, pulse train, background bed, Debug source marker | Core, Acoustics, UI, Meta XR Audio |
| [Responses](Responses/README.md) | `GPL.Responses.Core` + `GPL.Responses` | method-of-adjustment in azimuth with the triggers, result with signed error and a log note | Core, Input |
| [HearingLoss](HearingLoss/README.md) | `GPL.HearingLoss.Core` + `GPL.HearingLoss` | hearing loss on the final L/R output: ANSI S3.44 prediction (NIOSH HLSim port), presets, manual, per-ear 10-band banks on the AudioListener, per-ear test tone | — |

Conventions
- Folder layout: `Core/` (pure, `noEngineReferences: true`), `Runtime/` (Unity), `Editor/`, `Tests/EditMode/`, `README.md`.
- Namespaces `GPL.<Module>`; assemblies `GPL.<Module>.Core`, `GPL.<Module>`, `GPL.<Module>.Editor`, `GPL.<Module>.Tests` (Core module: `GPL.Core`).
- A module references `GPL.Core`, the other GPL modules named in its README (no cycles) and the packages named there; never the App or a chamber.
- Tests: `Tools/Verify-Headless.ps1` (from the AcousticChamber26 repo) or Unity Test Runner > EditMode.
