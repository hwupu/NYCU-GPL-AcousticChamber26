# Sprint Board

One short line per ticket (status, commit / date). Acceptance: [ProductBacklog](ProductBacklog.md). Details: the commit messages.

## Sprint 0 — Setup and architecture (2026-11-05) — closed
| Ticket | Status | Note |
|---|---|---|
| PB-01 Project hygiene | Done | `2a2cac6` |
| PB-02 Architecture | Done | `18494bf`; ADR-0001..0003 |
| PB-16 Design system | Done | `18494bf`; v2 via ADR-0005 |
| PB-03 GitHub remote | Done | github.com/hwupu/NYCU-GPL-AcousticChamber26 |

## Sprint 1 — Shared foundations (2026-11-05 to 2026-11-07) — closed
| Ticket | Status | Note |
|---|---|---|
| PB-04 Pure modules | Done | `066d9d3` |
| PB-05 Input + UI + Homing | Done | 05a `8f5cb4a`, 05b `6ff98de`, 05c `8e33a0d` `263c37c`; slides split off as PB-20 |
| PB-17 UI components + motion | Done | `b39c55b` |
| PB-18 Sheets, pages, scrolling | Done | `993302a` |
| PB-19 Debug bench + preset manager | Done | 19a `993302a`, 19b `3259bd2`, 19c `d9b1930` |
| PB-06 Hall + catalog + routing | Done | 06a `a3204a0` `229ef57`, 06b `a144d4c` |
| PB-07 `_Template` chamber | Done | 07a `65c495f`, 07b `162ea44`, 07c `a144d4c` (resume test passed for every policy) |
| PB-15 Data manager | Done | `bd81eff` |

## Sprint 2 — First real chamber (2026-11-07 ->)
| Ticket | Status | Note |
|---|---|---|
| PB-20 Onboarding slides + sequence | Done | `4093c2e`, `c32b798` |
| PB-21 Refactor before HumPerHW2 | Done | 21a `1b14b55`, 21c `22bba33`, 21b |
| PB-08 HumPerHW2 chamber | Done | 08b `a373286`; 08c `32813cb`; 08d-1 `d107cbc`; 08d-2 `0345c8e`; 08d-3 `d6bce62`; 08e `fa1ce9d`. Closed by PO 2026-11-08 after an editor session (S0003, 18 trials, order reverb-quiet-noisy, data correct); not run: Quest checklist (Docs/HeadsetTest.md), Android RT60, HW2 CSV comparison |
| PB-09 GPL.HearingLoss | Done | 20 HLSim vectors exact; PO verified 2026-11-08 |
| PB-10 HearingLoss chamber | Done | PO verified 2026-11-08; panel fix: Normal hearing + auto levels |
| PB-13 New-chamber tooling | Done | PO verified 2026-11-08 |
| PB-14 Native keyboard on Quest | Deferred (PO: later) | test matrix in docs/references/Keyboard.md |
| PB-11 MovingSource chamber (C01) | Built, headset check open | run `Magic > MovingSource` 1-2 |
| PB-12 HiddenSource chamber (C02) | Built, headset check open | run `Magic > HiddenSource` 1-2 |
| Panel move handle | Built, headset check open | `UiGrabber` below the chamber menu and experience panels |
| Housekeeping 2026-11-08 | Done | `UiButton.Selected` in all test / experience panels, `UiKit.Select`, C04 relabel, C00 hidden, HearingLoss relabelled C03 |
| UiKit rebuild (branch uikit-rebuild) | Migration done, manual test pending | Storybook design `9c70bce`; PoC `265670e`; U2a `a42e005` `c4fda22`; U3 `c0269f5` (every panel migrated, old builders deleted); naming `2eafe82` (ADR-0007); 315 tests pass; headset check open (icons, fonts, grabber, panels) |
| PB-22 GPL.Stimuli | Built, scene re-run + headset check open | HW2 Steps 1-5 + Validate OK headless; library: 7 OpenAIR recordings (CC BY-SA) |
| PB-23 Asset credits | Open | AssetCredits.md committed 2026-11-10; open: Radio.jpg, slides, SFX-C-04, replace SFX-C-02/03 |
| PB-24 C03 panel v2 | Built, scene re-run + headset check open | no tone row, 9 GPL.Stimuli sounds in a 4-column grid (no HW2), Config = hearing loss test with tones from the speaker; 346 tests pass |
