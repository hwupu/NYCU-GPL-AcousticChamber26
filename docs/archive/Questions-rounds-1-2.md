# Questions for the PO: answered rounds 1-3 (archive)

Answered questions moved out of [../Questions.md](../Questions.md) on 2026-11-07. Paths below are relative to `docs/`; the current rules live in the documents named under "Where".

## Round 1 (2026-11-05): answered
| # | Topic | PO answer (summary) | Where |
|---|---|---|---|
| Q1 | Chambers | Independent studies / explorations / demos, chosen from a Main Hall; each chamber has an HW2-style menu (Study ID, Start, Debug); some are demos without a Study ID (superseded by Q16: all chambers are equal). Rebuild HW2 as a chamber. | Architecture 5, 6, 12 |
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
| — | Extra | Own root folder (third-party assets); per-chamber key bindings, Y = Stop dialog; parameters editable in Debug, saved as presets, active preset used by real runs and shown on the chamber menu; swappable native keyboard / number pad. | ADR-0002, ADR-0003 |

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

## Round 3 (2026-11-05): answered
| # | Topic | PO answer | Where |
|---|---|---|---|
| Q18 | Visual design | Agent proposal accepted. | DesignSystem.md |

## Round 4 (2026-11-07, PB-08 HumPerHW2): answered
| # | Topic | PO answer | Where |
|---|---|---|---|
| Q19 | Condition order | **Counterbalanced by default** (not HW2's fixed Quiet, Noisy, Reverb); preset key `condition_order`, `fixed` kept as an option. | HumPerHW2 Design |
| Q20 | Practice | Repeatable as in HW2 (Practice again / I'm ready). | HumPerHW2 Design |
| Q21 | Resume | Experiment `next_trial` (RT60 and background re-applied), practice `stage_start`. | HumPerHW2 Design |
| Q29 | Counterbalancing | 3-row Latin square (QNR, NRQ, RQN by (Study ID − 1) mod 3), not all 6 orders; carry-over not balanced. | HumPerHW2 Design |
| Q30-Q34 | 08d split, sources, slides, background clips, bake | Agent defaults accepted (2026-11-07): 08d-1 skeleton, 08d-2 room + audio, 08d-3 protocol; sources created by the setup step; HW2 slides fitted to 2000 × 936 (strip cropped, centered); background levels measured in the setup step, clips Streaming; bake in the editor only. | HumPerHW2 Docs |
| Q44-Q48 | 08e Debug sections, headset test | Agent defaults accepted (2026-11-08): Debug Measure RT60 to status line + console only; Debug-only sound-sphere switch (After confirm / Always visible) with the signed-difference cue; measure at block start fills `rt60_measured_s`; leaving Debug stops playback and returns to Quiet; checklist + CSV comparison with an HW2 file from the PO. | HumPerHW2 Docs |
| Q22-Q28 | Slides, columns, gate, RT60 measurement, block break, label, split | Agent defaults accepted: HW2 slides 3-5 as instructions; HW2 columns + factor columns; gate ±5° / ±10° / 1 s; measurement off; stock block break, background continues; label `C01`; PB-08a-e. | HumPerHW2 Docs, SprintBoard |
