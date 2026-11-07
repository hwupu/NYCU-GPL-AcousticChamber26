# Product Backlog

Product Owner: researcher. Status: Done / Open / Blocked (waiting for PO) / Closed (won't do or accepted).

| ID | Ticket | Acceptance | Status |
|---|---|---|---|
| PB-01 | Project hygiene | Git repo, `.gitignore`, Git LFS, CLAUDE.md, AGENTS.md symlink | Done |
| PB-02 | Architecture | `docs/Architecture.md`, ADR-0001..0003 accepted | Done (rounds 1-2 answered; ADR-0004/0005 came with PB-05a and PB-16) |
| PB-03 | GitHub remote | Private repo created, `main` and LFS objects pushed | Done (pushed 2026-11-05) |
| PB-04 | GPL.Core / Params / Logging / Session (pure) | HW2 log + progress refactored and generalized (schema, presets, cursor, resume policy); EditMode tests pass headless | Done (49 EditMode tests pass headless, 2026-11-05) |
| PB-05 | GPL.Input + GPL.UI + GPL.Homing runtime | Bindings, Stop dialog, overlay canvas, pointer, `ITextEntry` (number pad first), panel placer, slides; head gate, recenter listener, system recenter (replaced the planned software recenter), handover | Done (2026-11-07; 05a-c; panel placer = `OverlayPanel`; slides moved to PB-20) |
| PB-06 | Main Hall + catalog + routing | Hall scene, chamber list, routing to the chamber menu of unfinished sessions | Done (2026-11-07) |
| PB-07 | `_Template` chamber | Chamber menu (Study ID, Start, Debug, preset line, unfinished list), presets, all resume variants proven by a kill-the-app test | Done (2026-11-07) |
| PB-08 | HumPerHW2 chamber (HW2 rebuilt) | Same protocol and trial data as HW2 on the modules; GPL.Acoustics, SpatialSources, Responses, DebugBench extracted | Open |
| PB-09 | GPL.HearingLoss | ANSIS344 + test vectors; per-ear filter on the final output; per-ear test-tone check | Done (2026-11-07; headless tests; headset tone check: Sandbox + Magic > App > Hearing Loss Test Panel) |
| PB-10 | HearingLoss chamber | Experience chamber (ADR-0006, PO 2026-11-07): own panel (sound, play / pause, ear, NIOSH presets), speaker dragged on the floor, dry, no logs; headset check | Built (2026-11-07; headless Steps + Validate; PO runs Magic > HearingLoss Steps 1-2, headset check open) |
| PB-11 | MovingSource chamber | as above | Open |
| PB-12 | HiddenSource chamber | as above | Open |
| PB-13 | New-chamber tooling | `Magic > App > New Chamber...` copies `_Template` and renames | Done (2026-11-07; headless: probe chamber created, compiled, its Magic steps ran) |
| PB-14 | Native keyboard on Quest | Find why `TouchScreenKeyboard` did not show in the HW2 standalone build; `NativeKeyboardEntry` works or is documented as unusable | Blocked (2026-11-07: `NativeKeyboardEntry` + Keyboard Test Panel built; likely cause GameActivity, docs/references/Keyboard.md; PO headset test A/B/C) |
| PB-15 | Data manager in Debug | Per chamber: list sessions (CSV + session file pair), delete one or all with confirmation; only that chamber's folder | Done (2026-11-07; core in PB-04, page in PB-19c) |
| PB-16 | Visual design system | Colors, font (Noto Sans TC for English + 中文), text sizes for VR, panel and button styles agreed before UI code; written to `docs/DesignSystem.md` | Done (v1 applied in PB-05; v2 accepted 2026-11-05 via `docs/design/components.html`, ADR-0005) |
| PB-17 | UI components + motion (GPL.UI) | `UiButton` component (prominent / gray / borderless / destructive, states brighten toward pressed, settle timing on unscaled time, capsule, 60 u hit region), theme tokens incl. motion, reduce-motion switch, live re-theming; existing screens on v2; EditMode tests; headset check | Done |
| PB-18 | Sheets, pages, scrolling (GPL.UI) | Page stack with ‹ back / × close, sheets (in-place scale + fade, optional blur, push back + dim, standalone during trials), Stop dialog as sheets, borderless scroll list (edge fades, indicator, thumbstick + trigger-drag); headset check | Done |
| PB-19 | Debug bench + preset manager (GPL.DebugBench) | Debug page framework (Run, Parameters from the schema, Presets: working copy, Save, Save as, Make active, Edit, Delete; chamber groups; status line); data files page from PB-15. As built: Playground, Save as Custom, Default + Custom (ADR-0002), Log files page | Done (2026-11-07; 19a-c headset tests passed) |
| PB-20 | Onboarding slides (GPL.UI or GPL.Protocol stock screen) | HW2 onboarding slides ported and generalized: per-chamber slide list (text + optional image, English / 中文), next / back, last slide hands over to the protocol; usable as a protocol stage; headset check | Done (2026-11-07; 20a slide screen, 20b onboarding sequence) |
| PB-21 | Refactor before HumPerHW2 | `ProtocolChamber` base + `Onboarding` helper (the onboarding stages of Architecture section 8 in one call), `ChamberBehaviour` Debug sections + practice hooks, slide editor helpers in `GPL.Protocol.Editor`; the Template chamber runs on them with unchanged behaviour and log rows; EditMode tests pass headless; headset check | Done |
