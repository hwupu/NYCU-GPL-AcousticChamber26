# HumPerFinal (C05) — Headset test (PB-28)

Standalone build on the Quest 3. Headset volume at the target step (12) unless a step says otherwise. Tick each line; note anything
odd with the time, so it can be found in the CSV.

## 1. Config page (Hall > C05 > Config)
| # | Do | Expect |
|---|---|---|
| 1 | Open Config | Acoustics **Room 0.7 s** selected, Hearing **Normal**, Anchors **Off**, Sound sphere **After confirm**; RT60 warning only if this headset has no table |
| 2 | **Play stimulus** | white-noise pulses (2 s on, **0.7 s** off) straight ahead at 3 m, gray radio box in front of them at eye height |
| 3 | Hearing **L −80**, then **R −80**, **L −40**, **R −40**, **Normal** (stimulus playing) | the pulse goes muffled and moves toward the good ear; −80 clearly stronger than −40; Normal = as before |
| 4 | Anchors **On** | yellow cube ahead, brass bar to the right at 3 m; rubber-duck quacks (about 5 s), 1.5 s, trumpet fanfare (about 15 s), 2.5 s, quacks, ... one at a time |
| 5 | Turn the head 90° | the anchors stay where they were in the room |
| 6 | Hearing **L −80** with anchors on | the anchors are filtered too (the filter is on the whole output) |
| 7 | **Measure RT60** (Anchors on) | the anchors pause during the measurement and resume after; status T_mid close to 0.7 s; Measure RT60 is selected only while it runs and never changes the Acoustics choice |
| 8 | ‹ back to the chamber menu | all sound stops, anchors hidden; reopening Config shows Normal / Off |

## 2. Debug run (Config > Run experiment trials)
| # | Do | Expect |
|---|---|---|
| 9 | Onboarding, practice | slides "Before we start" (Meta recenter), "Procedure", "Controllers", "Trials", "Ready?" with the PO's pictures (中文 too); Back on "Procedure" reads "← Back" ("← 上一頁"); the dot afterwards sits slightly above eye level (1°); practice: radio box at 0°, 1.5 m, start +30°, normal hearing, no anchors, no helper text |
| 10 | Hold one trigger for over 40 s | the sound goes all the way round (behind you and back in front); no stop at ±90° |
| 11 | Answer after a full turn | the Debug line shows the difference within ±180° |
| 12 | Experiment (fixed or Williams order) | no break screen between blocks; anchors appear / disappear between trials when the cell changes; hearing changes without notice |
| 13 | During the adjustment press **Y**, then Keep going | radio, dot, anchor models and sphere hidden while open; sounds paused; after Keep going the **same trial starts again from the dot** (same radio, same start side) |
| 14 | Press Y during the dot, Keep going | the trial continues normally (no restart) |

## 3. Real session (Study ID 12, then 13)
| # | Do | Expect |
|---|---|---|
| 15 | S0012: Start, run into block 2, quit the app while the sound is on | — |
| 16 | Start again with ID 12 → Resume | handover, recenter, dot, then **the same trial** again; the block's hearing and anchors are back |
| 17 | Finish S0012 (about 35 min) | 160 trials; thanks screen |
| 18 | Start S0013 | order row 3 (S0012 had row 2): first cell `left40_on` instead of `left40_none` |

## 4. Data (copy the folder off the headset)
- `.session`: `custom.order_row=2`, `custom.condition_order=left40_none,left40_on,normal_on,...`, `status=Completed`, no `custom.open_trial`.
- 160 `trial` rows with `stage=experiment`, 16 per `block`; per block each `visual_az_deg` × `start_side` twice (`repeat` 1, 2).
- One `trial_restart` with `reason=app_closed` (step 15) and the completed `trial` row after it with `restarts=1`; in the Debug
  file a `trial_restart` with `reason=stop_dialog` (step 13).
- `audio_final_az_deg` within (−180, 180]; `audio_final_unwrapped_deg` beyond ±180 on the trial of step 10 (Debug file).
- `condition` rows: `hl L=...;R=...` matching the cell; `anchors` rows on / off when the anchor state changes.
