# Questions for the PO

## Round 1 (2026-11-05): answered
| # | Topic | PO answer (summary) | Where |
|---|---|---|---|
| Q1 | Chambers | Independent studies / explorations / demos, chosen from a Main Hall; each chamber has an HW2-style menu (Study ID, Start, Debug); some are demos without a Study ID. Rebuild HW2 as a chamber. | Architecture 5, 6, 12 |
| Q2 | IDs | **Study ID**, not participant ID; entered in the chamber menu. | Architecture 6 |
| Q3 | Resume | Back to the chamber menu (the experimenter is the user there): Resume (same Study ID) / Start new / Back; then hand over and resume silently. Multiple unfinished sessions. | ADR-0003 |
| Q4 | Modes | Explore = Debug; keep the name "Debug" for now. | Architecture 7 |
| Q5 | Packaging | (Question was unclear.) Decision: modules are plain folders under `Assets/AcousticChamber/Modules`. | Architecture 4 |
| Q6 | Posture | Seated, standing or room-scale, per chamber; the experimenter may stand and hand over to a seated participant. | Architecture 8 |
| Q7 | Hearing loss | On the final left/right device output, not on sources. | Architecture 11 |
| Q8 | Languages | English and 中文 (label exactly "中文"). | CLAUDE.md |
| Q9 | Reuse | Reuse HW2, refactored: shared code generic, chamber-specific code in the chamber. Ignore BaseProject. | ADR-0001 |
| Q10 | Remote | No GitHub repo yet; create it if `gh` is available. | (gh not installed) |
| Q11 | Platforms | Same as HW2. | — |
| Q12 | Restarted blocks | (Question was unclear; explained in Architecture 6.2.) Default: analyze only the highest complete attempt of each block. | Architecture 6.2 |
| — | Extra | Own root folder (third-party assets); per-chamber key bindings, Y = Stop dialog; parameters editable in Debug, saved as presets, active preset used by real runs and shown on the Study ID screen; swappable native keyboard / number pad. | ADR-0002, ADR-0003 |

## Round 2 (2026-11-05): answered
| # | Topic | PO answer | Where |
|---|---|---|---|
| — | HW2 chamber name | `HumPerHW2` | Architecture 12 |
| Q5 | Packaging | Plain folders with a README, so a folder can be copied or scanned by an AI agent. | Architecture 4 |
| Q12 | Restarted blocks | Analyze only the highest complete attempt. | Architecture 6.2 |
| Q13 | GitHub | PO creates the repo and sends the URL. | PB-03 |
| Q14 | HumPerHW2 data | New common columns + HW2 trial columns; **no analysis scripts**: a document describing the data scheme (for analysis with other tools / AI). | Architecture 10 |
| Q15 | Study ID | Number pad, up to 4 digits, leading zeros + prefix: `1` -> `S0001`. | Architecture 6 |
| Q16 | Demo chambers | No separate demo type: all chambers are equal. | Architecture 5, 6 |
| Q17 | Unsaved Debug values | Ignore: real sessions always use the active preset; the default config is the active preset until another is chosen. | ADR-0002 |
| — | Data manager | Button in each chamber's Debug menu to list and delete that chamber's data (CSV + session file as a pair), one or all. | Architecture 10.1 |

## Round 3: open
| # | Question |
|---|---|
| Q18 | Visual design: answered: agent proposal accepted (`DesignSystem.md`). |
