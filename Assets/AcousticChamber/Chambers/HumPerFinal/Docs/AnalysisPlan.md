# HumPerFinal (C06) — Analysis plan

**Status:** stub (PB-28, 2026-11-17). PO 2026-11-17: record raw data first; the analysis is planned and done outside the repo
(no scripts). The intended measures, for reference:

- **R, L:** final azimuth per participant × cell × radio position, from start +60° (`start_side = R`) and −60° (`L`), each the
  mean of the 2 repeats.
- **Bias** B = (R + L) / 2 (relative to the radio: B − `visual_az_deg`); **threshold** (R − L) / 2.
- **Robustness:** mean absolute error, `|signed_error_deg|`.
- **Test:** repeated-measures ANOVA, factors hearing (5) × anchors (2) × radio position (4) (design to be confirmed by the PO).
- **Order:** balanced 10 × 10 Latin square, `order_row` = `study_id % 10`; report repeated rows if more than 10 participants.
- **Exclusions to decide:** Debug and practice rows, dropouts, `stop_dialog == 1`, runaway trials (e.g. `|audio_final_unwrapped_deg|`
  beyond ±180°).
