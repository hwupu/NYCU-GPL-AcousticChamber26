# HumPerFinal (C06) — Data format

**Status:** approved by the PO 2026-11-17 (PB-28). Raw data only (PO 2026-11-17): the columns below are logged; derived
measures and formatting are decided at the analysis step. Written for an analyst (human or AI).

## Files
In `persistentDataPath/HumPerFinal/` (Quest: `/sdcard/Android/data/com.nycu.AcousticChamber26/files/HumPerFinal/`):
- `yyyyMMdd_HHmmss_HumPerFinal_S0012.csv`: one row per event, long format, UTF-8, comma-separated, header row, flushed per row.
  A resumed session appends to the same file.
- `yyyyMMdd_HHmmss_HumPerFinal_S0012.session`: key=value (status, seed, language, cursor, attempts, `param.<key>` snapshot) plus
  `custom.order_row` (0-9), `custom.condition_order` (the 10 cells, e.g. `left40_none,left40_on,...`) and, while a trial's
  target is on, `custom.open_trial` (`block:trial:restarts`, 0-based; removed with the trial row). See
  `Modules/Session/README.md`.
- Debug runs: `..._HumPerFinal_DEBUG.csv`, no session file.

## Columns
Common columns as C05: `event, timestamp_local, elapsed_s, session_id, chamber, chamber_version, study_id, mode, preset,
params_hash, stage, block, condition, trial, attempt`, then the HumPerFinal columns, then `note`.

| Column | Type / unit | Rows | Meaning |
|---|---|---|---|
| `block` | int 1-10 | `experiment` rows | position in this session's order; practice rows 1 |
| `condition` | `<hearing>_<anchors>` or `practice` | block, trial, homing, restart rows | the cell (Design) |
| `hearing` | `normal`, `left40`, `right40`, `left80`, `right80` | `condition`, `trial`, `trial_restart` | simulated loss (on other rows: the part of `condition` before `_`) |
| `anchors` | `none`, `on` | same | anchors visible and sounding (other rows: the part after `_`) |
| `order_row` | int 0-9 | same (empty in the bench's single practice trial) | `study_id % 10`; also `custom.order_row` in the session file |
| `trial` | int 1-16 within the block | trial, homing, restart rows | practice counts up without limit |
| `trial_index` | int 1-16 | `trial`, `trial_restart` | position in the cell's planned list (stable over restarts) |
| `repeat` | 1 / 2 | `trial`, `trial_restart` | repeat of this position × start side in the block |
| `visual_az_deg` | degrees | `trial`, `trial_restart` | radio azimuth (0 ahead, negative left) |
| `audio_start_az_deg` | degrees | `trial`, `trial_restart` | −60 or +60 (practice +30) |
| `start_side` | `L`, `R` | `trial`, `trial_restart` | `L` = start −60°, `R` = start +60° |
| `audio_final_az_deg` | degrees, (−180, 180] | `trial`, `trial_restart` | sound azimuth at A (or at the interruption), wrapped |
| `audio_final_unwrapped_deg` | degrees | `trial`, `trial_restart` | the same without wrapping: start + total rotation (can exceed ±180) |
| `signed_error_deg` | degrees, (−180, 180] | `trial` | shortest signed angle `audio_final − visual` (positive: right of the radio) |
| `duration_s` | s, 3 decimals | `trial`, `trial_restart` | stimulus onset to A, Stop-dialog time excluded |
| `restarts` | int ≥ 0 | `trial`, `trial_restart` | `trial`: how many times this trial was restarted before it was completed; `trial_restart`: earlier restarts of this trial (0 on the first) |
| `reversals` | int | `trial`, `trial_restart` | changes of movement direction |
| `radius_m` | m | `trial` | 3.0 (practice 1.5) |
| `rt60_target_s` | s | `condition`, `trial` | 0.7 |
| `rt60_measured_s` | s (T_mid) | `trial`, `rt60_measured` | only with `rt60.measure_at_block_start`; else empty |
| `stim_level_dbfs` | dBFS | `trial` | software RMS of the target |
| `anchor_level_dbfs` | dBFS | `condition`, `trial` | software RMS of the anchor recordings after leveling (−26; both leveled alike, the duck's value; both in the `anchors` note); empty without anchors |
| `gate_s` | s | `trial` | dot onset until the gate passed |
| `stop_dialog` | 0 / 1 | `trial` | the Stop dialog was opened during this (completed) try |
| `volume_step` | int 0-15 | `session_start`, `session_resume`, `volume_check` | headset volume step |
| `note` | text | any | `key=value` pairs (below) |

## Events
Common (as C05): `session_start`, `language`, `stage_begin/end`, `handover`, `homing`, `volume_check`, `recenter`, `slide`,
`block_begin/end`, `trial`, `session_resume`, `participant_dropout`, `session_superseded`, `session_end`, `app_paused/app_resumed`,
`debug_begin/end`, `param_change`, `practice_choice`. HumPerFinal:
- `condition`: the cell was applied (before a block's first trial, the practice, and again after a resume); note
  `hearing=left80 left_db=-80 right_db=0 curve=severe_x2 anchors=on rt60_target_s=0.7 <reverb note> calibrated=0|1 hl L=...;R=... q=... fs=48000`.
- `anchors`: note `on` / `off`; on `on` each anchor's listener-relative position (`duck=x,y,z;trumpet=x,y,z`), the clips and their
  leveling gains, the levels and the gaps (`clip_duck=348791__nebyoolae__toy-ducks-quacking gain_duck_db=.. ... gap_duck_s=1.5 gap_trumpet_s=2.5`).
- `trial_restart`: a trial interrupted by the Stop dialog (Keep going; note `reason=stop_dialog` + the adjustment note) or by the
  app closing (written at the resume; note `reason=app_closed`, answer columns empty); the interrupted try's columns. The trial
  then runs again; its completed `trial` row carries `restarts`. Rows are never deleted.
- `rt60_measured`: as C05 (off by default).

## Rebuilding the trial table
Keep `event == trial`, `mode == EXPERIMENT`, `stage == experiment`; one row per `block` × `trial_index` (the completed try).
Expect 160 rows per complete session. Drop sessions marked `Superseded` or with a `participant_dropout` row.
