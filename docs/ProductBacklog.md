# Product Backlog

Product Owner: researcher. Status: Done / Built / Open / Deferred / Blocked (waiting for PO). Commits and dates: [SprintBoard](SprintBoard.md).

| ID | Ticket | Acceptance | Status |
|---|---|---|---|
| PB-01 | Project hygiene | Git repo, `.gitignore`, Git LFS, CLAUDE.md, AGENTS.md symlink | Done |
| PB-02 | Architecture | `docs/Architecture.md`, ADR-0001..0003 accepted | Done |
| PB-03 | GitHub remote | Private repo, `main` and LFS objects pushed | Done |
| PB-04 | GPL.Core / Params / Logging / Session (pure) | HW2 log + progress generalized (schema, presets, cursor, resume policy); EditMode tests pass headless | Done |
| PB-05 | GPL.Input + GPL.UI + GPL.Homing runtime | Bindings, Stop dialog, overlay canvas, pointer, `ITextEntry` (number pad first), panel placer (`OverlayPanel`); head gate, recenter listener, system recenter, handover | Done |
| PB-06 | Main Hall + catalog + routing | Hall scene, chamber list, routing to the chamber menu of unfinished sessions | Done |
| PB-07 | `_Template` chamber | Chamber menu (Study ID, Start, Config, preset line, unfinished list), presets, all resume variants proven by a kill-the-app test | Done |
| PB-08 | HumPerHW2 chamber | Same protocol and trial data as HW2 on the modules; GPL.Acoustics, SpatialSources, Responses, DebugBench extracted | Done (PO 2026-11-08; not run: Quest checklist, Android RT60, HW2 CSV comparison) |
| PB-09 | GPL.HearingLoss | ANSIS344 + test vectors; per-ear filter on the final output; per-ear test tone | Done (headset verified 2026-11-08) |
| PB-10 | HearingLoss chamber | Experience chamber (ADR-0006): own panel (sound, play / pause, ear, NIOSH presets), speaker dragged on the floor, dry, no logs | Done (headset verified 2026-11-08) |
| PB-11 | MovingSource chamber (C01) | Experience chamber: grip-grab + scripted paths with speed, room Dry / 0.7 s / 2.2 s, distance attenuation switch, hearing selector (Normal, L-20, R-20, L-40, R-40) | Built; open: PO runs `Magic > MovingSource` Step 1, 2 and the headset check (Docs/SceneSetup.md) |
| PB-12 | HiddenSource chamber (C02) | Experience chamber: random floor source 1-6 m, 360°; floor sphere on the active ray; A confirms; custom menu shows line, azimuth + distance error | Built; open: PO runs `Magic > HiddenSource` Step 1, 2 and the headset check (Docs/SceneSetup.md) |
| PB-13 | New-chamber tooling | `Magic > App > New Chamber...` copies `_Template` and renames | Done |
| PB-14 | Native keyboard on Quest | Find why `TouchScreenKeyboard` did not show in the HW2 build; `NativeKeyboardEntry` works or is documented unusable | Deferred (PO: later; built: `NativeKeyboardEntry` + test panel; likely cause GameActivity, docs/references/Keyboard.md; headset test A/B/C open) |
| PB-15 | Data manager in Config | Per chamber: list sessions (CSV + session pair), delete one or all with confirmation; only that chamber's folder | Done |
| PB-16 | Visual design system | Colors, font (Noto Sans TC), VR text sizes, panel and button styles in `docs/DesignSystem.md` | Done (v2, ADR-0005) |
| PB-17 | UI components + motion | `UiButton` styles, state brightening, settle timing on unscaled time, capsule, 60 u hit region, theme tokens, reduce-motion, live re-theming | Done |
| PB-18 | Sheets, pages, scrolling | Page stack with ‹ / ×, sheets (scale + fade, push back + dim), Stop dialog, borderless scroll list | Done |
| PB-19 | Debug bench + preset manager | Config pages: Run, Parameters from the schema, Presets (Playground, Save as Custom, Make active, Delete), chamber groups, Log files page | Done (ADR-0002) |
| PB-20 | Onboarding slides | Per-chamber slide list (text + optional image, English / 中文), next / back, usable as a protocol stage; 20a slide screen, 20b onboarding sequence | Done |
| PB-21 | Refactor before HumPerHW2 | `ProtocolChamber` base + `Onboarding` helper, `ChamberBehaviour` Debug sections + practice hooks, slide editor helpers; Template runs on them unchanged | Done |
| PB-22 | GPL.Stimuli | Tones, Gaussian white / pink noise from recipes (ramps, RMS level, seed), EditMode-verified (spectrum, distribution, ramps, level); all chambers use it instead of `SFX-C-01` (ADR-0008) | Built; open: PO re-runs Step 2 (C01-C03) / Step 4 (C04), headset check |
| PB-23 | Asset credits | `docs/AssetCredits.md` lists every third-party asset (audio, images, icons, fonts, models); open items resolved (ADR-0009) | Open: PO checks Radio.jpg, slides, SFX-C-04; replaces SFX-C-02/03 |
| PB-24 | C03 panel v2 | No pure-tone row or status text; "Sounds": 9 GPL.Stimuli sounds (white, pink, 7 library recordings leveled to -26 dBFS RMS at load, looped with a 1 s gap; no HW2 clips) in a 4-column `UiChoiceGroup` grid; Config (bottom right) opens the hearing loss test (`HearingLossTestView`), tones from the speaker | Built; open: PO runs `Magic > HearingLoss` Step 2, headset check |
| PB-25 | Main Hall covers | Cover images in `App/Covers/IMG-C-NN` (Gemini, credited; import settings by `CoverImporter`), picked per chamber in the ChamberDefinition; card = cover only, C-ID top left and title bottom left on it (white 75 %, soft shadow), gaps 24 / 28, hover = 103 % + image saturation 125 %, no unfinished pill, title "Acoustic Chambers" centered; Storybook `Screens/Main Hall` first; sky = Kenney "day" panorama (`Settings/Skyboxes/Skybox-Day.mat`, set by the PO in Lighting > Environment; materials for all 5 skies) | Built; open: headset check (hover, shadow, legibility, sky) |
