# Template (C00) — Data format

**Status:** approved by the PO (PB-07, 2026-11-06). Written for an analyst (human or AI).

## Files
In `persistentDataPath/Template/` (Quest: `/sdcard/Android/data/com.nycu.AcousticChamber26/files/Template/`):
- `yyyyMMdd_HHmmss_Template_S0001.csv`: one row per event, long format, UTF-8, comma-separated, header in the first row. A resumed
  session appends to the same file.
- `yyyyMMdd_HHmmss_Template_S0001.session`: key=value text; status, seed, language, cursor, attempts and the full parameter
  snapshot (`param.<key>`), see `Modules/Session/README.md`.
- Debug runs: `yyyyMMdd_HHmmss_Template_DEBUG.csv`, no session file.
- `presets/`: saved presets (not data).

## Columns
Common columns (every chamber): `event, timestamp_local, elapsed_s, session_id, chamber, chamber_version, study_id, mode, preset,
params_hash, stage, block, condition, trial, attempt`, then the Template columns, then `note`.

| Column | Type / unit | Rows | Meaning |
|---|---|---|---|
| `stage` | `handover`, `language`, `recenter`, `instructions`, `ready`, `practice`, `experiment`, `thanks` | all after the menu | |
| `block` | int, 1-based | `experiment` rows | block number; `practice` rows have 1 |
| `condition` | `default` | trial rows | the Template has no conditions |
| `trial` | int, 1-based within the block | trial rows and the gate `homing` rows | |
| `attempt` | int, ≥ 1 | block, trial and homing rows of `practice` and `experiment` | attempt of the block (resume with `block_start` etc. adds 1); empty in plain stages (language, handover, thanks) |
| `rt_s` | seconds, 3 decimals | `trial` | cue onset to the A press; empty on a timeout |
| `timed_out` | 0 / 1 | `trial` | no press within `response_timeout_s` |
| `gate_s` | seconds, 3 decimals | `trial` | time from the dot onset until the gate passed |
| `gate_breaks` | int | `trial` | times the head left the gate during the hold |
| `stop_dialog` | 0 / 1 | `trial` | the Stop dialog was opened during this trial |
| `volume_step` | int 0–15, empty if not readable | `session_start`, `session_resume`, `volume_check` | headset volume step read from the headset (Android media volume); `volume_check` is the final value at the dot before the practice, note `target=N match=0|1` |
| `note` | text | any | free text; `homing` / `recenter` rows hold `key=value` pairs (`HomingResult.Note()`) |

## Events
`session_start`, `language`, `stage_begin`, `stage_end`, `handover`, `homing` (Meta-button recenter, final check at the dot, every trial's gate), `volume_check`,
`recenter` (participant used the Meta button), `slide`, `block_begin`, `block_end`, `block_restart`, `trial`, `session_resume`,
`participant_dropout`, `session_superseded`, `session_end`, `app_paused`, `app_resumed`; Debug runs also `debug_begin`,
`debug_end`, `param_change`.

A block of attempt 2 or later starts with `block_restart` instead of `block_begin`; a block continued after a `next_trial` resume
starts with `block_begin` noted `continued at trial n`. `stage_begin` rows of a handover run again after a resume are noted
`again after resume`. `language` notes the code (`en`, `zh-TW`). `slide` (stages `recenter` and `instructions`) is written each time a slide
comes on screen, note `<slide id> <n>/<N> <how>` with how = `first`, `next`, `back`, `resume` or `again` (back from the language choice; stage `recenter`) (e.g. `placeholder_2 2/2 next`);
the time a slide was on screen is the next row's timestamp minus its own (the next `slide` row or the stage's `stage_end`).

## Rebuilding the trial table
1. Keep rows with `event == trial` and `mode == EXPERIMENT`.
2. Keep `stage == experiment` (drop practice).
3. Per `session_id`, `block`: keep only the **highest attempt that is complete** (has `trials_per_block` trial rows, read from
   the session file's `param.trials_per_block`).
4. Drop sessions whose status is `Superseded` (session file) or that have a `participant_dropout` row (see AnalysisPlan).
