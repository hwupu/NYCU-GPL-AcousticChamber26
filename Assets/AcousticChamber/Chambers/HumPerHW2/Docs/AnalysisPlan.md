# HumPerHW2 (C01) — Analysis plan

**Status:** approved by the PO (PB-08a, 2026-11-07). The analysis is done outside the repo (no scripts, PO decision Q14).

## Design
- **Within-subject factor:** `condition` (`quiet`, `noisy`, `reverb`), 6 trials per cell.
- **Order:** counterbalanced across participants (Design, `condition_order`); `order_pos` (`block`, 1-3) is a nuisance factor.
- **Primary DV:** `stop_short_deg` (DataFormat step 4), the tolerated audio-visual disparity.
- **Secondary:** `abs_error`, `signed_error_deg` (left / right bias), SD of `signed_error_deg` (precision), `duration_s`.

## Aggregation
Per `study_id` × `condition`: the mean over the kept trials (median as a robustness check).

## Exclusions
- Debug runs, practice trials, sessions superseded or dropped out (DataFormat steps 1-3).
- Trials with `stop_dialog == 1`.
- Trials ending at the clamp (`|audio_final_az_deg| ≥ 89.9`: forgotten or runaway trial).
- A participant with fewer than 4 of 6 trials left in any condition.
Report the number of excluded trials per reason.

## Tests
1. One-way repeated-measures ANOVA on the per-cell means of `stop_short_deg`, factor `condition` (Greenhouse-Geisser if Mauchly
   p < .05); partial η².
2. Planned contrast for H1: Reverb > Quiet, one-sided paired t-test, Cohen's d_z; Noisy vs Quiet and Reverb vs Noisy two-sided,
   Bonferroni.
3. Robustness: Friedman test; linear mixed model on trials, `stop_short_deg ~ condition + order_pos + (1 | study_id)`.
4. Order check: mixed ANOVA with the Latin-square row (3 levels) as between factor, reported descriptively when the sample per
   row is small. Carry-over is not balanced by the 3-row square (Design); state this as a limitation.
Same tests for the secondary measures, labelled exploratory.

## Notes for the write-up
Method of adjustment, not a staircase threshold; adjustment speed 10°/s for everyone; RT60 values are rendered targets from the
calibration table (measured T20 in `rt60_measured_s` when the measurement was on); levels are software levels, the headset volume
step is logged.
