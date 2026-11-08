# CLAUDE.md — AcousticChamber26

Guide for AI coding agents. `AGENTS.md` is a git symlink to this file (Windows: Developer Mode, `core.symlinks=true`); edit only `CLAUDE.md`.

## Project
Exploratory acoustic-perception lab in VR (Meta Quest 3, Unity 6000.6.6f1, URP, OpenXR, Meta XR Core SDK 207.0.1, Meta XR Audio SDK 85.0.0). A **Main Hall** lists independent **chambers** (one experiment design each: stimuli, parameters, log schema, resume rules). Every study chamber opens with the same menu (Study ID, Start, Debug, Back to Hall); a chamber in development is used through Debug. Experience chambers (no data) have their own panel (ADR-0006). Shared features are portable modules; anything chamber-specific stays in the chamber folder. **All our files live under `Assets/AcousticChamber/`.**

Read before coding: [docs/Architecture.md](docs/Architecture.md) (layout section 2, logging section 10), [docs/decisions/](docs/decisions/), open [docs/Questions.md](docs/Questions.md), [docs/references/INSTRUCTIONS.md](docs/references/INSTRUCTIONS.md) (binding; Quest pitfalls section 3), [docs/DesignSystem.md](docs/DesignSystem.md) for UI. Reference implementation `../115A-535655-HumPer-HW2`: port and generalize, do not re-invent.
Do not read docs/references/HLSIM_GUIDELINES.md, docs/design/components.html or docs/archive/ unless the task needs them.

## Process
- The user is the Product Owner (PO); you are Scrum Master and engineer. Ask before coding when an experimental parameter is unspecified (propose a default). Plan non-trivial work and get approval.
- Small steps: do the current request, verify, hand over, wait. Report honestly: what was run, what was only calculated, what only a headset can show.
- Commit only when asked; push only when asked; never force-push without approval (`--force-with-lease`). Look before deleting or overwriting the PO's work.
- Keep docs current: ProductBacklog, SprintBoard (one short line per ticket), PromptLog (rows about 200 characters; details in commit messages), ADRs, each chamber's `Docs/`.

## Architecture rules
1. Three layers, one-way: `Modules` (`GPL.*`) <- `App` <- `Chambers`. Chambers never reference each other; the App knows chambers only through `ChamberDefinition` assets.
2. Every module is portable: own folder, asmdefs, `README.md` (purpose, dependencies, public API, how to copy). It may depend on `GPL.Core`, the other GPL modules named in its README (acyclic) and the external packages listed there; never on App or Chambers.
3. Pure logic in Unity-free `*.Core` asmdefs (`noEngineReferences: true`); inject clock, writer and seed; EditMode tests; thin MonoBehaviours.
4. Namespaces `GPL.<Module>`, `AcousticChamber.App`, `AcousticChamber.Chambers.<ChamberId>`.
5. Data first: a chamber's `Design.md`, `DataFormat.md`, `AnalysisPlan.md` are approved before its code.
6. Never delete data rows; restarts, pauses, dropouts are logged events; `attempt` separates repeated blocks.
7. Parameters come from presets (ADR-0002), never from Inspector fields at run time.
8. Input through `GPL.Input` bindings (ADR-0003), no direct `OVRInput` in chamber code. Y (left) = Stop dialog.
9. Text entry through `ITextEntry`.
10. **Study ID** (not participant ID): number pad, up to 4 digits, shown `S0001`. Languages English and 中文 (label exactly "中文").

## Engineering rules
- Verify before handoff: `powershell -ExecutionPolicy Bypass -File Tools/Verify-Headless.ps1` (mirror in `D:/UnityVerify/AcousticChamber26`, EditMode tests; `-ExecuteMethod <Class.Method>` runs a setup step). Fix every compile error and failing test; inspect saved scenes.
- New files: copy `.meta` files that exist in the mirror but not in the project back into the project (stable GUIDs), unless the PO's editor already made them.
- Scene setup by Editor scripts only, menu **Magic** (`Magic > <Chamber> > Step N`, `Validate Setup`, `Hot Fix`): idempotent, open scene only, never touch Building Blocks the PO added; never edit scene YAML while the editor is open. GUI screens are built at run time with `GPL.UI.UiKit` (ADR-0004).
- Read SDK source (`Library/PackageCache/com.meta.xr.*`) before using an API.
- Root groups `XR`, `Audio`, `Stimuli`, `Experiment`, `Environment`, `UI` (identity transforms). Console prefix `[GPL]` / `[Chamber:<Id>]`.
- Asmdefs `GPL.<Module>[.Core|.Editor|.Tests]`, `AcousticChamber.Chambers.<Id>[.Core|.Editor|.Tests]`, `AcousticChamber.App`.
- Write code with the file tools, not heredocs or `sed`.
- Choices and toggles in any panel: `UiKit.ChoiceButton` + `UiKit.Select` / `UiButton.Selected` (DesignSystem section 4), so the chosen one is always visible; no hand-made style swaps.

## Quest 3 pitfalls (details: INSTRUCTIONS.md section 3)
- GUI: `OVROverlayCanvas`, source canvas on layer 30, layers 30/31 off the eye cameras; re-place after recenter.
- In-scene visuals: URP Unlit/Lit material assets; `Sprites/Default` renders in one eye.
- Input System only: `OVRInputModule` fails; use the `GPL.UI` pointer + `OVRInput`.
- No system keyboard: on-screen number pad.
- Recenter: `OVRManager.display.RecenteredPose`, wait two frames, re-place GUI and stimuli, log it.
- Head pose is (0,0,0) in the first frames: wait for a valid height.
- Azimuth 0 = forward, negative = left; position = origin + yaw * (r sin az, 0, r cos az).
- Audio: mono clips at -23 LUFS, gains at unity, level by headset volume (log the step); reverb needs the Meta XR Audio Reflection effect on the mixer with every spatial source routed to it; RT60 from the measured calibration table.
- Meta's Project Setup Tool can rewrite audio settings: keep the build-time guard.

## Data
One CSV per session (flush every row, console mirror) plus a `.session` file; names and paths: `Assets/AcousticChamber/Modules/Session/README.md`, scheme: Architecture section 10. Participant data never goes into git (`/ParticipantData/` ignored).

## Repository
Git LFS for audio, images, models, fonts, lightmaps; commit `.meta` files. Author from git config; commit messages imperative with `PB-xx`. Folder layout: Architecture section 2 (`Modules/`, `App/`, `Chambers/<Id>/`, `Chambers/_Template/`, `docs/`, `Tools/`; no analysis scripts).
