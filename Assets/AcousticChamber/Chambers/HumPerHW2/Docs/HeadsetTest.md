# HumPerHW2 (C01) — Headset test (PB-08e)

Standalone build on the Quest 3 (not Link: the Android audio plugin is the one to check). Headset volume at the target step (8)
unless a step says otherwise. Tick each line; note anything odd with the time, so it can be found in the CSV.

## 1. Debug page (Hall > C01 > Debug)
| # | Do | Expect |
|---|---|---|
| 1 | Acoustics **0.3 s Quiet**, then **Play stimulus** | white-noise pulses (2 s on, 1 s off) straight ahead at 2 m, radio picture in front of them at eye height, nearly dry |
| 2 | **0.7 s Noisy** (stimulus still playing) | audible room; three background recordings start: left, right, behind |
| 3 | Turn the head 90° | the backgrounds stay where they were in the room (they turn the other way) |
| 4 | **2.2 s Reverb** | clearly longer reverberation; background keeps playing |
| 5 | **Measure RT60** in each of Quiet, Noisy, Reverb (stimulus stopped by the button) | status line: T20 / T30 close to 0.3 / 0.7 / 2.2 s (the table was measured with the Windows plugin; write the three values down) |
| 6 | **Meta preset**, then Measure RT60 | about 4 s (Concrete) |
| 7 | **Background** twice | pauses, then resumes where it stopped |
| 8 | **Sound sphere: Always visible**, Play stimulus | orange sphere on the sound |
| 9 | ‹ back to the chamber menu | all sound stops |

## 2. Debug run (Debug > Run experiment trials)
| # | Do | Expect |
|---|---|---|
| 10 | Handover, language 中文, recenter slide, the three HW2 slides, the dot | slides look right (no stretched text, dark side bands blend in), Back / Next labels in 中文 |
| 11 | Practice: hold the left, then the right trigger | the sound moves 10°/s; both or none = still; hint text below; A ends the trial |
| 12 | After the trial | sphere at the answer, "Sound ±x° from the picture (A: continue)"; then Practice again / I'm ready |
| 13 | Practice again once, then I'm ready | a second practice trial, then the first condition block |
| 14 | During a trial press **Y** | dialog; picture, dot and sphere hidden; sound paused; Keep going brings them back, the trial continues |
| 15 | Run all 18 trials | "Condition n of 3 done." after blocks 1 and 2; the background plays in Noisy and Reverb, stops in Quiet; thanks screen |

## 3. Real session (Study ID 1, then 2)
| # | Do | Expect |
|---|---|---|
| 16 | S0001: Start, run to the middle of block 2, then quit the app (or take the headset off and let it sleep) | — |
| 17 | Start again with ID 1 → Resume | handover, recenter, dot, then the next unfinished trial in the same condition (acoustics and background back on) |
| 18 | Finish S0001; start S0002 | S0002's order is noisy, reverb, quiet (S0001's was quiet, noisy, reverb) |
| 19 | Preset with `rt60.measure_at_block_start = true` (Playground > Save as Custom > Make active), short Debug run | ~5 s silence before each block's first trial; `rt60_measured` rows; `rt60_measured_s` filled on that block's trials |

## 4. Data (copy the folder off the headset, Session README)
- `HumPerHW2/<date>_HumPerHW2_S0001.csv` + `.session`: `custom.condition_order=quiet,noisy,reverb`, `status=Completed`.
- Trial rows: 18 with `stage=experiment`, 6 per condition; `signed_error_deg = audio_final_az_deg − visual_az_deg`;
  `stop_dialog=1` on the trial of step 14 (Debug file).
- Send one HumPerHW2 CSV and one HW2 CSV (`yyyyMMdd_HHmmss_<ID>.csv` from the HW2 app) for the column-by-column comparison (Q48).
