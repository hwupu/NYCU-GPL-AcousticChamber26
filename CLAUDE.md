# CLAUDE.md — AcousticChamber26

Guide for AI coding agents. `AGENTS.md` is a git symlink to this file (Windows: Developer Mode, `core.symlinks=true`); edit only `CLAUDE.md`.

## Project
Acoustic-perception lab in VR (Meta Quest 3, Unity 6000.6.6f1, URP, OpenXR, Meta XR Core SDK 207.0.1, Audio SDK 85.0.0). A **Main Hall** lists independent **chambers** (one design each: stimuli, parameters, log schema, resume rules). Study chambers share one menu (Study ID, Start, Config, Back to Hall); a chamber in development is used through Config (Debug mode). Experience chambers (no data) have their own panel (ADR-0006). Shared features are portable modules; chamber code stays in its folder. **All our files live under `Assets/AcousticChamber/`.**

Read only the docs the task touches: [Architecture](docs/Architecture.md) (section 2 layout, 10 logging), [ADRs](docs/decisions/), [Questions](docs/Questions.md), [INSTRUCTIONS](docs/references/INSTRUCTIONS.md) (binding; section 3 Quest pitfalls), [DesignSystem](docs/DesignSystem.md). Reference implementation `../115A-535655-HumPer-HW2`: port and generalize. Skip docs/references/HLSIM_GUIDELINES.md unless needed. UI is designed first in the Storybook preview `docs/design/` ([README](docs/design/README.md)), then built in Unity.

## Process
- The user is the Product Owner (PO); you are Scrum Master and engineer. Ask before coding when an experimental parameter is unspecified (propose a default). Plan non-trivial work and get approval.
- Small steps: do the request, verify, hand over, wait. Report honestly: what was run, what was only calculated, what only a headset can show.
- Commit/push only when asked; never force-push without approval (`--force-with-lease`). Look before deleting or overwriting the PO's work.
- **UI: Storybook first.** Every new or changed UI component or screen is designed in `docs/design/` first, then built in Unity with the same name (`UiKit/<Name>.cs`); change both together so they stay in sync. Remind the PO of this whenever a UI request skips the Storybook.
- Keep docs current: ProductBacklog, SprintBoard (one short line per ticket), ADRs, each chamber's `Docs/`. Change details go in the commit message (imperative, `PB-xx`).

## Architecture rules
1. Layers, one-way: `Modules` (`GPL.*`) <- `App` <- `Chambers`. Chambers never reference each other; the App knows them only through `ChamberDefinition` assets.
2. Every module is portable: own folder, asmdefs, `README.md` (purpose, dependencies, API, how to copy). It may depend on `GPL.Core`, GPL modules named in its README (acyclic) and listed external packages; never App or Chambers.
3. Pure logic in Unity-free `*.Core` asmdefs (`noEngineReferences: true`); inject clock, writer, seed; EditMode tests; thin MonoBehaviours.
4. Namespaces `GPL.<Module>`, `AcousticChamber.App`, `AcousticChamber.Chambers.<Id>`; asmdefs `GPL.<Module>[.Core|.Editor|.Tests]`, `AcousticChamber.Chambers.<Id>[.Core|.Editor|.Tests]`, `AcousticChamber.App`.
5. Data first: a chamber's `Design.md`, `DataFormat.md`, `AnalysisPlan.md` are approved before its code.
6. Never delete data rows; restarts, pauses, dropouts are logged events; `attempt` separates repeated blocks.
7. Parameters come from presets (ADR-0002), never Inspector fields at run time.
8. Input through `GPL.Input` bindings (ADR-0003), no direct `OVRInput` in chamber code. Y (left) = Stop dialog.
9. Text entry through `ITextEntry`.
10. **Study ID** (not participant ID): number pad, up to 4 digits, shown `S0001`. Languages English and 中文 (label exactly "中文").

## Engineering rules
- Verify before handoff: `powershell -ExecutionPolicy Bypass -File Tools/Verify-Headless.ps1` (mirror `D:/UnityVerify/AcousticChamber26`, EditMode tests; `-ExecuteMethod <Class.Method>` runs a setup step). Fix every compile error and failing test; inspect saved scenes.
- New files: copy `.meta` files that exist in the mirror but not in the project back (stable GUIDs), unless the PO's editor already made them.
- Scene setup by Editor scripts only, menu **Magic** (`Magic > <Chamber> > Step N`, `Validate Setup`, `Hot Fix`): idempotent, open scene only, never touch Building Blocks the PO added; never edit scene YAML while the editor is open. GUI screens are built at run time with `GPL.UI` `UiKit` (ADR-0004, ADR-0007).
- Read SDK source (`Library/PackageCache/com.meta.xr.*`) before using an API.
- Root groups `XR`, `Audio`, `Stimuli`, `Experiment`, `Environment`, `UI` (identity transforms). Console prefix `[GPL]` / `[Chamber:<Id>]`.
- Write code with the file tools, not heredocs or `sed`.
- Choices and toggles: `UiKit.ChoiceGroup` + `UiKit.Select` / `UiButton.Selected` (DesignSystem section 4), no hand-made style swaps.

## Quest 3 pitfalls (details: INSTRUCTIONS.md section 3)
- GUI: `OVROverlayCanvas`, source canvas on layer 30, layers 30/31 off the eye cameras; re-place after recenter.
- In-scene visuals: URP Unlit/Lit material assets (`Sprites/Default` renders in one eye).
- Input System only: `OVRInputModule` fails; use the `GPL.UI` pointer + `OVRInput`. No system keyboard: on-screen number pad.
- Recenter: `OVRManager.display.RecenteredPose`, wait two frames, re-place GUI and stimuli, log it. Head pose is (0,0,0) in the first frames: wait for a valid height.
- Azimuth 0 = forward, negative = left; position = origin + yaw * (r sin az, 0, r cos az).
- Audio: tones and noise generated by `GPL.Stimuli` (ADR-0008; -26 dBFS RMS default), recordings mono at -23 LUFS imported as PCM, gains at unity, level by headset volume (log the step); reverb needs the Meta XR Audio Reflection effect on the mixer with every spatial source routed to it; RT60 from the measured calibration table. Project Setup Tool can rewrite audio settings: keep the build-time guard.

## Data and repository
One CSV per session (flush every row, console mirror) plus a `.session` file: names in `Modules/Session/README.md`, scheme in Architecture section 10. Participant data never goes into git (`/ParticipantData/` ignored). Git LFS for audio, images, models, fonts, lightmaps; commit `.meta` files; author from git config. No analysis scripts.
**Asset credits (ADR-0009):** every third-party asset (audio, image, icon, font, model) gets an entry in `docs/AssetCredits.md` before it is committed; remind the PO whenever a new asset has none. That file stays out of git until the PO says otherwise.
