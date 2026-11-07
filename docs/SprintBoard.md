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
| PB-18 Sheets, pages, scrolling | Done (committed with 19a) | headset tests passed (18a sheets, 18b pages, 18c scrolling, group padding 8 u + full content width); EditMode tests pass headless (in the 91 of PB-19a) |
| PB-19 Debug bench + preset manager | Done | 19a done (headset test passed; committed with PB-18): `GPL.DebugBench` (WorkingCopy core, Debug + Parameters pages), GPL.UI Column / InfoRow / switch / header action, `Magic > App > Debug Bench Test Panel`; 92 EditMode tests pass headless. 19b Presets page done (headset test passed; Default + Custom flow, ADR-0002 amended; 94 EditMode tests pass headless). 19c Data files page done (headset test passed; `DebugBench.Files` = `DataFiles`; Delete confirms, Delete all confirms once with the count; test panel can create 3 demo files); one-line rows; 95 EditMode tests pass headless |
| PB-05c GPL.Homing | Done (`8e33a0d`, `263c37c`) | headset test passed (PO: outcome=done): head gate + fixation dot (yaw ±10°, pitch ±15°, hold 0.5 s, hint 4 s; preset params), system recenter at handover (rig stays at identity; ADR-0001 amended), recenter listener; `Magic > App > Homing Test Panel`; 103 EditMode tests pass headless. AlignToTarget / AudioHoming / RoomAnchor later |
| PB-06 Hall + catalog + routing | Done | 06a done (`a3204a0`, headset test passed, no error): `ChamberDefinition` + `ChamberCatalog` + `Magic > App > Sync Chambers` (catalog + build list), Hall card grid (`HallMenu`), scene switch with a 0.3 s fade (`AppNav`, `SceneFade`), `last_chamber` in `app.state`, Template stub chamber (`Chambers/_Template`, ID `Template`, ‹ back to the Hall), Sandbox scene for the test panels; Debug mode per chamber only (no Hall tag); 106 EditMode tests pass headless. 06b built with PB-07c: `SessionRouter` (App.Core, tested) opens the last chamber menu at launch when it has unfinished sessions (PO: passed with PB-07c) |
| PB-07 `_Template` chamber | Done | split (PO): 07a chamber menu + sessions + placeholder run with the Stop dialog; 07b GPL.Protocol + Template task (gate + press A); 07c kill-the-app test of every resume policy (`resume_from` preset parameter) + PB-06b routing. Docs approved (`Chambers/_Template/Docs`: Brief, Design, DataFormat, AnalysisPlan). 07a done (PO: headset test passed): generic `ChamberMenu` + `ChamberBehaviour` (App), `SessionLauncher` (start, supersede, snapshot; App.Core tests), volume-step sheet, Stop dialog ends the run, Template placeholder run, `.params` importer, `Magic > Template > Step 1 - Chamber menu`. Committed `65c495f`. 07b built (PO: tested): `GPL.Protocol` (ProtocolPlan, ProtocolWalker with every resume policy, ProtocolLog, stock screens), Template task (language, handover + system recenter, practice, blocks of head gate + press A, block breaks, thanks), plan hash checked on Resume, `Magic > Template > Step 2 - Homing`. Headless verify still crashes in OVRPlugin.dll (environment); pure tests (Core, Session, Logging, Protocol: 30) pass under dotnet, all runtime and editor code compiles against the editor assemblies. Committed `162ea44`. 07c built (awaiting headset test): launch routing (`SessionRouter` + Hall, PB-06b), test procedure `Chambers/_Template/Docs/ResumeTest.md` (every policy, quit during block 2 trial 3, expected CSV rows); pure tests 36 pass under dotnet. PO: resume works; unfinished rows now show the position on a second line (no date; date in the Resume dialog). 07c PO: resume works for the policies; menu and row fixes after review (status lines, titles, row centering, scroll end); PO: "perfect" (2026-11-07) |
| PB-15 Data manager UI | Done | core done in PB-04 (`DataFiles`); headset page is PB-19c (Debug bench Data files) |
| PB-05 (umbrella) | Done | 05a-c done; panel placer = `OverlayPanel`; onboarding slides split off as PB-20 |

Sprint 1 closed 2026-11-07.

## Sprint 2 — First real chamber (2026-11-07 ->)
| Ticket | Status | Note |
|---|---|---|
| PB-20 Onboarding slides | Open | port HW2 slides; needed by PB-08 |
| PB-08 HumPerHW2 chamber | Open | planning first: split, HW2 inventory, module extraction order; Design / DataFormat / AnalysisPlan before code |
