# HumPerFinal (C05) — Analysis plan

**Status:** stub (PB-28, 2026-11-17; position pairs 2026-11-18). PO 2026-11-17: record raw data first; the analysis is planned
and done outside the repo (no scripts). The intended measures, for reference:

- **Position pairs (2026-11-18):** each radio position is tested from one start side only (`pair_set` = `study_id % 4`,
  Design "Position pairs"); 8 trials per cell = 4 position pairs × 2 repeats.
- **R, L per hemisphere:** in each hemisphere (`visual_az_deg` < 0 / > 0) one position starts from +60° (`start_side = R`) and
  the other from −60° (`L`). Per participant × cell × hemisphere: R and L as the signed error (`signed_error_deg`, relative to
  each trial's radio) of that start side, each the mean of its 2 repeats.
- **Bias** B = (R + L) / 2 per hemisphere (relative to the radio); **threshold** (R − L) / 2.
- **Robustness:** mean absolute error, `|signed_error_deg|`.
- **Test:** repeated-measures design hearing (5) × anchors (2) × hemisphere (2), all within subject (design to be confirmed by
  the PO). Radio position (inner / outer) inside a hemisphere is confounded with start side within a participant; it can only
  be looked at across participants (`pair_set` as a between-subject factor, e.g. a mixed model), with low power at 10
  participants.
- **Participants:** 10 (Study IDs 12-21). **Order:** balanced 10 × 10 Latin square, `order_row` = `study_id % 10`; report
  repeated rows if more than 10 participants. **Pairing sets:** used 3, 3, 2, 2 times at 10 participants (sets 0-3); report.
- **Exclusions to decide:** Debug and practice rows, dropouts, `stop_dialog == 1`, runaway trials (e.g. `|audio_final_unwrapped_deg|`
  beyond ±180°).
