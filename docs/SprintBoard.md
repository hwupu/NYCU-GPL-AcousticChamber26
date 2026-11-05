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
| PB-18 Sheets, pages, scrolling | Next | |
| PB-19 Debug bench + preset manager | Open | |
| PB-05c GPL.Homing | Open | after PB-17/18 |
| PB-06 Hall + catalog + routing | Open | |
| PB-07 `_Template` chamber | Open | first headset test after PB-05..07 |
| PB-15 Data manager UI | Open | core done in PB-04 (`DataFiles`) |
