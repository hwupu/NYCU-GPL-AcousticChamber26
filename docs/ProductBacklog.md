# Product Backlog

Product Owner: researcher. Status: Done / Open / Blocked (waiting for PO) / Closed (won't do or accepted).

| ID | Ticket | Acceptance | Status |
|---|---|---|---|
| PB-01 | Project hygiene | Git repo, `.gitignore`, Git LFS, CLAUDE.md, AGENTS.md symlink | Done |
| PB-02 | Architecture | `docs/Architecture.md`, ADR-0001..0003 accepted | Done (rounds 1-2 answered) |
| PB-03 | GitHub remote | Private repo created, `main` and LFS objects pushed | Done (pushed 2026-11-05) |
| PB-04 | GPL.Core / Params / Logging / Session (pure) | HW2 log + progress refactored and generalized (schema, presets, cursor, resume policy); EditMode tests pass headless | Done (49 EditMode tests pass headless, 2026-11-05) |
| PB-05 | GPL.Input + GPL.UI + GPL.Homing runtime | Bindings, Stop dialog, overlay canvas, pointer, `ITextEntry` (number pad first), panel placer, slides; head gate, recenter listener, software recenter, handover | Open |
| PB-06 | Main Hall + catalog + routing | Hall scene, chamber list, routing to the chamber menu of unfinished sessions | Open |
| PB-07 | `_Template` chamber | Chamber menu (Study ID, Start, Debug, preset line, unfinished list), presets, all resume variants proven by a kill-the-app test | Open |
| PB-08 | HumPerHW2 chamber (HW2 rebuilt) | Same protocol and trial data as HW2 on the modules; GPL.Acoustics, SpatialSources, Responses, DebugBench extracted | Open |
| PB-09 | GPL.HearingLoss | ANSIS344 + test vectors; per-ear filter on the final output; per-ear test-tone check | Open |
| PB-10 | HearingLoss chamber | Brief, Design, DataFormat, AnalysisPlan approved, then built | Open |
| PB-11 | MovingSource chamber | as above | Open |
| PB-12 | HiddenSource chamber | as above | Open |
| PB-13 | New-chamber tooling | `Magic > App > New Chamber...` copies `_Template` and renames | Open |
| PB-14 | Native keyboard on Quest | Find why `TouchScreenKeyboard` did not show in the HW2 standalone build; `NativeKeyboardEntry` works or is documented as unusable | Open |
| PB-15 | Data manager in Debug | Per chamber: list sessions (CSV + session file pair), delete one or all with confirmation; only that chamber's folder | Open |
| PB-16 | Visual design system | Colors, font (Noto Sans TC for English + 中文), text sizes for VR, panel and button styles agreed before UI code; written to `docs/DesignSystem.md` | Done (accepted; applied in PB-05) |
