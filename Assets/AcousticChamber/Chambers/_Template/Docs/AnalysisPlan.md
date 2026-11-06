# Template (C00) — Analysis plan

**Status:** approved by the PO (PB-07, 2026-11-06). The Template is a demonstration and test chamber; this plan shows the
form every chamber's plan takes.

## Design
- **Within-subject factor:** `block` (levels 1..`blocks`, default 2). No between-subject factor.
- **Dependent variable:** reaction time `rt_s` (seconds).
- **Secondary:** timeout rate (`timed_out`), gate time `gate_s`.

## Aggregation
Per participant (`study_id`) and block: the **median** `rt_s` of the trials without a timeout (the median resists single slow
presses). Timeout rate = timeouts / trials of the block.

## Exclusions
- Debug runs (`mode == DEBUG`), practice trials (`stage == practice`).
- Lower attempts of a block (keep the highest complete attempt, DataFormat step 3).
- Sessions superseded or dropped out.
- Trials with `stop_dialog == 1`.
- RT below 0.15 s (anticipation).
- A participant with fewer than half of the trials of any block left after exclusions.

## Test
One-way repeated-measures ANOVA on the per-block medians, factor `block` (Greenhouse-Geisser correction when sphericity is
violated); with 2 blocks this equals a paired t-test.
