# Sprint Board

## Sprint 0 — Setup and architecture (2026-11-05) — closed
| Ticket | Status | Note |
|---|---|---|
| PB-01 Project hygiene | Done | AGENTS.md is a real symlink (Developer Mode on) |
| PB-02 Architecture | Done | ADR-0001..0003, rounds 1-3 answered |
| PB-16 Design system | Done | `docs/DesignSystem.md` accepted |
| PB-03 GitHub remote | In progress | `origin` = github.com/hwupu/NYCU-GPL-AcousticChamber26 (empty); first push waits for PO approval |

## Sprint 1 — Shared foundations (2026-11-05 ->)
| Ticket | Status | Note |
|---|---|---|
| PB-04 Pure modules (Core, Params, Logging, Session) | Done | 49 EditMode tests pass headless (`Tools/Verify-Headless.ps1`) |
| PB-04 commit | Done | `066d9d3` |
| PB-05 Input, UI, Homing runtime | Next | fonts in `Settings/Fonts` (Noto Sans TC Regular/Bold OTF, OFL); Hall scene with Camera Rig + Controller Tracking (PO); split 5a UI, 5b Input + Stop dialog, 5c Homing |
| PB-06 Hall + catalog + routing | Open | |
| PB-07 `_Template` chamber | Open | first headset test after PB-05..07 |
| PB-15 Data manager UI | Open | core done in PB-04 (`DataFiles`) |
