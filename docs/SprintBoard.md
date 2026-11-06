# Sprint Board

## Sprint 0 — Setup and architecture (2026-11-05) — closed
| Ticket | Status | Note |
|---|---|---|
| PB-01 Project hygiene | Done | AGENTS.md is a real symlink (Developer Mode on) |
| PB-02 Architecture | Done | ADR-0001..0003, rounds 1-3 answered |
| PB-16 Design system | Done | `docs/DesignSystem.md` accepted |
| PB-03 GitHub remote | Done | `main` + LFS pushed to github.com/hwupu/NYCU-GPL-AcousticChamber26 |

## Sprint 1 — Shared foundations (2026-11-05 ->)
| Ticket | Status | Note |
|---|---|---|
| PB-04 Pure modules (Core, Params, Logging, Session) | Done | 49 EditMode tests pass headless (`Tools/Verify-Headless.ps1`) |
| PB-04 commit | Done | `066d9d3` |
| PB-05a GPL.UI | Done | 61 EditMode tests pass headless; test panel in MainHall (PO renamed and saved the scene, GUID kept); ADR-0004 |
| PB-05b GPL.Input + Stop dialog | Done (`6ff98de`) | headset test passed; 72 EditMode tests pass headless; `Magic > App > Input Test Panel` replaces the UI test panel in MainHall; Stop dialog without Pause (PO, ADR-0003 amended) |
| PB-17 UI components + motion | Done | headset test passed (PO: "perfect"); 78 EditMode tests pass headless (before the 72 u glyph token, which the PO's editor compiled); UiButton, theme v2 + migration, motion, hit regions, v2 number pad and dialogs; ADR-0005 |
| PB-18 Sheets, pages, scrolling | Done (uncommitted) | headset tests passed (18a sheets, 18b pages, 18c scrolling, group padding 8 u + full content width); EditMode tests pass headless (in the 91 of PB-19a) |
| PB-19 Debug bench + preset manager | In progress | 19a done (headset test passed; committed with PB-18): `GPL.DebugBench` (WorkingCopy core, Debug + Parameters pages), GPL.UI Column / InfoRow / switch / header action, `Magic > App > Debug Bench Test Panel`; 92 EditMode tests pass headless. 19b Presets page done (headset test passed; Default + Custom flow, ADR-0002 amended; 94 EditMode tests pass headless). 19c Data files page done (headset test passed; `DebugBench.Files` = `DataFiles`; Delete confirms, Delete all confirms once with the count; test panel can create 3 demo files); one-line rows; 95 EditMode tests pass headless |
| PB-05c GPL.Homing | Done (`8e33a0d`, `263c37c`) | headset test passed (PO: outcome=done): head gate + fixation dot (yaw ±10°, pitch ±15°, hold 0.5 s, hint 4 s; preset params), system recenter at handover (rig stays at identity; ADR-0001 amended), recenter listener; `Magic > App > Homing Test Panel`; 103 EditMode tests pass headless. AlignToTarget / AudioHoming / RoomAnchor later |
| PB-06 Hall + catalog + routing | In progress | 06a done (`a3204a0`, headset test passed, no error): `ChamberDefinition` + `ChamberCatalog` + `Magic > App > Sync Chambers` (catalog + build list), Hall card grid (`HallMenu`), scene switch with a 0.3 s fade (`AppNav`, `SceneFade`), `last_chamber` in `app.state`, Template stub chamber (`Chambers/_Template`, ID `Template`, ‹ back to the Hall), Sandbox scene for the test panels; Debug mode per chamber only (no Hall tag); 106 EditMode tests pass headless. 06b: launch routing to unfinished sessions (after PB-07) |
| PB-07 `_Template` chamber | Open | first headset test after PB-05..07 |
| PB-15 Data manager UI | In progress | core done in PB-04 (`DataFiles`); headset page is PB-19c (Debug bench Data files) |
