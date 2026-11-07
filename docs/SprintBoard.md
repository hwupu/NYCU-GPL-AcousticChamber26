# Sprint Board

One short line per ticket (status, commit / date). Details: [PromptLog.md](PromptLog.md), its [archive](archive/) and the commit messages.

## Sprint 0 — Setup and architecture (2026-11-05) — closed
| Ticket | Status | Note |
|---|---|---|
| PB-01 Project hygiene | Done | `2a2cac6`; AGENTS.md is a real symlink |
| PB-02 Architecture | Done | `18494bf`; ADR-0001..0003, question rounds 1-3 |
| PB-16 Design system | Done | `18494bf`; v2 via ADR-0005 |
| PB-03 GitHub remote | Done | github.com/hwupu/NYCU-GPL-AcousticChamber26 |

## Sprint 1 — Shared foundations (2026-11-05 to 2026-11-07) — closed
| Ticket | Status | Note |
|---|---|---|
| PB-04 Pure modules (Core, Params, Logging, Session) | Done | `066d9d3` |
| PB-05 GPL.Input + GPL.UI + GPL.Homing | Done | 05a `8f5cb4a`, 05b `6ff98de`, 05c `8e33a0d` `263c37c`; slides split off as PB-20 |
| PB-17 UI components + motion | Done | `b39c55b`; ADR-0005 |
| PB-18 Sheets, pages, scrolling | Done | `993302a` |
| PB-19 Debug bench + preset manager | Done | 19a `993302a`, 19b `3259bd2`, 19c `d9b1930`; ADR-0002 amended |
| PB-06 Hall + catalog + routing | Done | 06a `a3204a0` `229ef57`, 06b `a144d4c` |
| PB-07 `_Template` chamber | Done | 07a `65c495f`, 07b `162ea44`, 07c `a144d4c` (resume test passed for every policy) |
| PB-15 Data manager | Done | core in PB-04, page in PB-19c; closed `bd81eff` |

## Sprint 2 — First real chamber (2026-11-07 ->)
| Ticket | Status | Note |
|---|---|---|
| PB-20 Onboarding slides + sequence | Done | `4093c2e`, `c32b798`; 20a slide screen, 20b onboarding sequence; headset tests passed |
| PB-21 Refactor before HumPerHW2 | Done | 21a `1b14b55`, 21c `22bba33`, 21b Debug sections + Practice trial; headset check passed |
| PB-08 HumPerHW2 chamber | Doing | 08a docs approved; 08b `GPL.Acoustics` built and committed (156 tests pass headless; first heard in 08e); next 08c SpatialSources + Responses, 08d chamber, 08e Debug + headset |
