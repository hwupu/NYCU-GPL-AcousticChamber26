# CLAUDE.md — AcousticChamber26

Guide for AI coding agents (Claude Code reads `CLAUDE.md`; `AGENTS.md` is a symlink to this file). Keep one source of truth here.

## What this project is
An **exploratory acoustic-perception lab in VR** (Meta Quest 3, Unity 6000.6.6f1, URP, OpenXR, Meta XR Core SDK 207.0.1, Meta XR Audio SDK 85.0.0). A **Main Hall** lists **chambers**; each chamber is one experiment design (white room, realistic room, outdoor, ...) with its own stimuli, assets, conditions, log schema and resume rules. Chambers are added, removed and refined over time.

Chambers are **independent** (individual studies, explorations, demos). Each opens with an HW2-style chamber menu (Study ID, Start, Debug, Back to Hall). All chambers are equal (same menu, presets, Debug, sessions); one still in development is simply used through Debug.

Features are built as **portable, generic modules** that can be copied into other Unity projects (plug-and-play). Anything specific to one chamber lives in that chamber's folder. HW2 is refactored, not copied: generic parts become modules, the HW2 study itself becomes the `HumPerHW2` chamber.

**All our files live under `Assets/AcousticChamber/`** (third-party and Asset Store content stays outside it).

Architecture: [docs/Architecture.md](docs/Architecture.md) and [docs/decisions/](docs/decisions/) (read before writing code). Open questions: [docs/Questions.md](docs/Questions.md). Process and engineering rules: [docs/references/INSTRUCTIONS.md](docs/references/INSTRUCTIONS.md) (binding). Hearing-loss simulator spec: [docs/references/HLSIM_GUIDELINES.md](docs/references/HLSIM_GUIDELINES.md).

Reference implementation: `../115A-535655-HumPer-HW2` (the earlier ventriloquist study). Its code is the starting point for several modules (CSV log, progress/resume, onboarding slides, number pad, UI pointer, overlay canvas, head gate, RT60 meter and calibration, room setup, mixer setup). **Port and generalize it; do not re-invent it.** Its docs (`Docs/Decisions`, `DataFormat.md`, `SpssAnalysis.md`) show the expected documentation depth.

## Roles and process (summary of INSTRUCTIONS.md section 1)
- The user is the **Product Owner (PO)**. You act as Scrum Master and engineer (AI-DLC + Scrum, solo).
- **Ask before coding** when an experimental parameter is unspecified: propose a default and ask. Plan non-trivial work and get approval.
- **Small steps:** do the current request, verify it, hand over, wait. Never build a whole chamber in one pass.
- Report honestly: what was run and verified, what was only calculated, what only a headset can show.
- **Commit only when asked; push only when asked.** Never force-push without explicit approval (`--force-with-lease`). Look before deleting or overwriting the PO's work.
- Keep `docs/` current: `ProductBacklog.md`, `SprintBoard.md`, `PromptLog.md`, `decisions/ADR-*.md`, and per chamber its own docs folder.

## Architecture rules (details in docs/Architecture.md)
1. **Three layers, one-way dependencies:** `Modules` (`GPL.*`)  <-  `App` shell  <-  `Chambers`. Modules never reference App or Chambers; chambers never reference each other; App knows chambers only through `ChamberDefinition` assets.
2. **Every module is portable:** own folder, own asmdefs, own `README.md` (purpose, dependencies, public API, how to copy). It may depend only on `GPL.Core` and on the external packages listed in its README.
3. **Pure logic in Unity-free assemblies** (`*.Core` asmdef, `noEngineReferences: true`): planners, math, log writer, progress store, HL model, filters. Inject clock, writer and random seed. Unit-test in EditMode. Keep MonoBehaviours thin.
4. **Namespaces** `GPL.<Module>` for modules, `AcousticChamber.App` for the shell, `AcousticChamber.Chambers.<ChamberId>` for chambers.
5. **Data first:** before coding a chamber, write its `Design.md`, `DataFormat.md` and `AnalysisPlan.md` (RM-ANOVA factors, levels, dependent variables, aggregation, exclusion rules) and get PO approval.
6. **Never delete data rows.** Restarts, pauses and dropouts are logged events; the `attempt` column separates repeated blocks.
7. **Parameters come from presets** (`GPL.Params`, ADR-0002), never from Inspector fields at run time. Debug edits a working copy; real runs use the active preset; a session stores its own snapshot and resumes with it.
8. **Input through `GPL.Input` bindings** (ADR-0003), no direct `OVRInput` in chamber code. Global action Y (left controller) = Stop dialog (Keep going / Drop out; Debug: Keep going / Back to the chamber menu).
9. **Text entry through `ITextEntry`** (number pad, keypad, native keyboard are swappable).
10. Terms: **Study ID** (not participant ID): number pad, up to 4 digits, shown as `S0001`. Languages: English and 中文 (label it exactly "中文", nothing longer).

## Engineering rules (INSTRUCTIONS.md section 2-3, enforced)
- **Verify before handoff.** The PO usually has the project open in Unity, so batch mode cannot use it. Run `powershell -ExecutionPolicy Bypass -File Tools/Verify-Headless.ps1` (mirrors `Assets`, `Packages`, `ProjectSettings` to `D:/UnityVerify/AcousticChamber26`, runs EditMode tests, prints failures and compile errors; `-ExecuteMethod <Class.Method>` runs a setup step first). Fix every compile error and failing test before reporting. Inspect saved scenes afterwards.
- **New files get their `.meta` from the mirror:** after a verify run, copy `.meta` files that exist in the mirror but not in the project back into the project (stable GUIDs), unless the PO's editor already created them.
- **Scene setup by Editor scripts, never at runtime.** Menu **Magic** (`Magic > <Chamber> > Step N ...`, `Magic > Validate Setup`, `Magic > Hot Fix`). Steps are idempotent, edit only the open scene, never touch Meta Building Blocks the PO added by hand. Never edit scene YAML while the editor is open. GUI exception (ADR-0004): the scene holds only the GUI host (overlay canvas, `UiRoot`, pointer); screens are built at run time with `GPL.UI.UiKit`.
- **Read SDK source before using an API** (`Library/PackageCache/com.meta.xr.*`). Do not guess class or field names.
- Hierarchy root groups: `XR`, `Audio`, `Stimuli`, `Experiment`, `Environment`, `UI` (identity transforms).
- Console log prefix: `[GPL]` for modules, `[Chamber:<Id>]` for chambers.
- Asmdef names: `GPL.<Module>.Core`, `GPL.<Module>`, `GPL.<Module>.Editor`, `GPL.<Module>.Tests`; chambers `AcousticChamber.Chambers.<Id>[.Core|.Editor|.Tests]`; app `AcousticChamber.App`.
- Write code with the file tools, not shell heredocs or `sed` (quotes and `\n` get mangled).

## Quest 3 pitfalls (all hit in HW2; see INSTRUCTIONS.md section 3 for the full list)
- GUI: `OVROverlayCanvas`, source canvas on hidden layer 30, remove layers 30/31 from eye cameras; re-place after recenter.
- In-scene visuals: URP Unlit/Lit **material assets** (single-pass stereo). `Sprites/Default` renders in one eye.
- Input System only: Meta's `OVRInputModule` fails; use the own ray pointer (`GPL.UI`) + `OVRInput`.
- No system keyboard: on-screen number pad for IDs.
- Recenter: subscribe to `OVRManager.display.RecenteredPose`, wait two frames, re-place GUI and stimuli, log it.
- Head pose is (0,0,0) for the first frames: wait for valid height.
- Azimuth 0 = forward, negative = left, positive = right; position = origin + yaw * (r sin az, 0, r cos az).
- Audio: mono clips at -23 LUFS, all software gains at unity, level set by the headset volume (record the volume step). Room reverb needs the **Meta XR Audio Reflection** effect on the mixer and every spatial source routed to it. Exact RT60 comes from the measured calibration table, not from Eyring.
- Build-time audio settings guard (HW2 `AudioBuildSettingsGuard`): Meta's Project Setup Tool can rewrite sample rate / DSP buffer.

## Logging and data
- CSV per session, one row per event, **flush every row**, console mirror. File `yyyyMMdd_HHmmss_<ChamberId>[_<StudyId>][_DEBUG].csv` in `Application.persistentDataPath/<ChamberId>/` plus a `<same base>.session` key=value file next to it (status, seed, cursor, attempts, full parameter snapshot); see `Assets/AcousticChamber/Modules/Session/README.md`.
- Quest path `/sdcard/Android/data/com.nycu.AcousticChamber26/files/`; Windows Editor `%USERPROFILE%\AppData\LocalLow\NYCU\AcousticChamber26\`.
- Participant data never goes into git (`/ParticipantData/` is ignored).

## Repository
- Git LFS for audio, images, models, fonts, lightmaps (`.gitattributes`). Commit `.meta` files.
- Author from git config (Pu-Hsuan Wu). Commit messages: imperative, reference tickets (`PB-xx`).
- `AGENTS.md` is a git symlink (Windows needs Developer Mode and `core.symlinks=true`); edit only `CLAUDE.md`.

## Layout
```
Assets/AcousticChamber/Modules/<Module>/    portable generic modules (Core/, Runtime/, Editor/, Tests/, README.md)
Assets/AcousticChamber/App/                 Main Hall, chamber catalog, routing (this project only)
Assets/AcousticChamber/Chambers/<Id>/       one chamber per folder (Config/default.params, Scenes, Scripts, Editor, Tests, Docs)
Assets/AcousticChamber/Chambers/_Template/  copy to start a new chamber
docs/                                       process docs, architecture, ADRs (docs/decisions), questions, references
Tools/                                      developer scripts (Verify-Headless.ps1); no analysis scripts (data schemes are documented instead)
```
