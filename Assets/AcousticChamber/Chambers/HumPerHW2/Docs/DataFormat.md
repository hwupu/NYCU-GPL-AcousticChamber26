# HumPerHW2 (C01) — Data format

**Status:** approved by the PO (PB-08a, 2026-11-07). Written for an analyst (human or AI).

## Files
In `persistentDataPath/HumPerHW2/` (Quest: `/sdcard/Android/data/com.nycu.AcousticChamber26/files/HumPerHW2/`):
- `yyyyMMdd_HHmmss_HumPerHW2_S0001.csv`: one row per event, long format, UTF-8, comma-separated, header row. A resumed session
  appends to the same file.
- `yyyyMMdd_HHmmss_HumPerHW2_S0001.session`: key=value; status, seed, language, cursor, attempts, the full parameter snapshot
  (`param.<key>`) and `custom.condition_order` (e.g. `noisy,reverb,quiet`). See `Modules/Session/README.md`.
- Debug runs: `yyyyMMdd_HHmmss_HumPerHW2_DEBUG.csv`, no session file.

## Columns
Common columns: `event, timestamp_local, elapsed_s, session_id, chamber, chamber_version, study_id, mode, preset, params_hash,
stage, block, condition, trial, attempt`, then the HumPerHW2 columns, then `note`.

| Column | Type / unit | Rows | Meaning |
|---|---|---|---|
| `stage` | `handover`, `language`, `recenter`, `instructions`, `ready`, `practice`, `experiment`, `thanks` | after the menu | |
| `block` | int 1-3 | `experiment` rows | position of the condition in this session's order; `practice` rows have 1 |
| `condition` | `quiet`, `noisy`, `reverb`, `practice` | block, trial, homing rows | |
| `trial` | int, 1-based within the block | trial and gate `homing` rows | practice trials count up without limit |
| `attempt` | int ≥ 1 | block, trial, homing rows | stays 1 with `next_trial`; higher only if a preset uses another resume policy |
| `visual_az_deg` | degrees | `trial` | picture azimuth (0 ahead, negative left) |
| `audio_start_az_deg` | degrees | `trial` | sound start azimuth |
| `audio_final_az_deg` | degrees | `trial` | sound azimuth at the A press (clamped ±90) |
| `signed_error_deg` | degrees | `trial` | `audio_final − visual` (positive: the sound ended right of the picture) |
| `duration_s` | s, 3 decimals | `trial` | stimulus onset to the A press, Stop-dialog time excluded |
| `radius_m` | m | `trial` | 2.0 (practice 1.2) |
| `rt60_target_s` | s | `condition`, `trial`, `rt60_measured` | target RT60 of the condition |
| `rt60_measured_s` | s (T20) | `trial`, `rt60_measured` | measured at the block start when `rt60.measure_at_block_start` is on; else empty (reference only) |
| `background` | 0 / 1 | `condition`, `trial` | background playing |
| `stim_level_dbfs` | dBFS | `trial` | software RMS of the stimulus clip + Meta gain boost (not sound pressure) |
| `bg_level_dbfs` | dBFS | `condition`, `trial` | power sum of the three background clips (RMS measured from the WAV files by the setup step); empty without background |
| `gate_s` | s | `trial` | dot onset until the gate passed |
| `stop_dialog` | 0 / 1 | `trial` | the Stop dialog was opened during this trial |
| `volume_step` | int 0-15 | `session_start`, `session_resume`, `volume_check` | headset volume step read from Android |
| `note` | text | any | free text; `homing`, `recenter`, `background`, `condition`, `rt60_measured` and `trial` rows hold `key=value` pairs |

The `condition` column is set on every row of a block (`block_begin` to `block_end`, the gate `homing` rows, `trial`) and on the
practice rows (`practice`); it is empty on stage rows. The `trial` note is the response record (GPL.Responses
`AdjustResult.Note()`): `start_deg= final_deg= target_deg= duration_s= reversals= at_limit=` (reversals = changes of direction;
at_limit = 1 when the sound ended at ±`adjust.limit_deg`).

HW2 → HumPerHW2: the HW2 trial columns keep their names and meaning. `participant` → `study_id` (`S0001`), `mode` `EXPERIMENT` /
`DEBUG` unchanged, HW2 `condition` `practice` rows → `stage = practice`. New: `stage`, `block`, `attempt`, `radius_m`,
`rt60_target_s`, `background`, `gate_s`, `stop_dialog`, `volume_step` and the session columns.

## Events
Common (as the Template): `session_start`, `language`, `stage_begin/end`, `handover`, `homing`, `volume_check`, `recenter`, `slide`,
`block_begin/end`, `block_restart`, `trial`, `session_resume`, `participant_dropout`, `session_superseded`, `session_end`,
`app_paused/app_resumed`, `debug_begin/end`, `param_change`. HumPerHW2:
- `condition` (PB-08d-3): the acoustics were switched, before the first trial of a block (and of the practice), and again after
  a resume; columns `rt60_target_s`, `background`, `bg_level_dbfs`; note the reverb setting
  (`mode=custom rt60_target_s= reflection= rt60_expected_s=`) and the plugin status (`room_modeling=on late_reverb=on`).
- `practice_choice`: after each practice trial, note `again` or `ready`.
- `background`: note `on` / `off` and, on `on`, each source's listener-relative position (`bg1=x,y,z;...`) and `spatial=1`.
- `rt60_measured` (only when `rt60.measure_at_block_start` is on, before a block's first trial; PB-08e, Q46): columns
  `rt60_target_s`, `rt60_measured_s` (T20, empty when the measurement failed); note
  `target=0.7 valid=1 t20=0.712 t30=0.698 depth_db=48 source=block_start` (or `valid=0 reason=...`).
The Debug page's Measure RT60 and acoustics buttons write no rows (Q44): their results appear in the Debug status line and the
console only.

## Rebuilding the trial table
1. Keep `event == trial`, `mode == EXPERIMENT`, `stage == experiment`.
2. Per `session_id` and `block`, keep the highest complete attempt (6 trial rows); with `next_trial` this is attempt 1.
3. Drop sessions whose session file says `Superseded` or that have a `participant_dropout` row (AnalysisPlan).
4. Derived measures (not logged):
   - `abs_error = |signed_error_deg|`
   - `stop_short_deg = signed_error_deg × (+1 if audio_start_az_deg > 0 else −1)`: positive = stopped before reaching the picture on
     the side the sound came from (the tolerated disparity), negative = overshoot.
   - `side` = sign of `visual_az_deg`, `eccentricity = |visual_az_deg|`, `order_pos = block`.
5. Per-cell means: per `study_id` × `condition`, the mean of each measure over the 6 trials, plus the SD of `signed_error_deg`.
