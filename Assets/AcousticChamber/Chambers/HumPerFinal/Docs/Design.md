# HumPerFinal (C06) — Design

**Status:** approved by the PO 2026-11-17 (PB-28); built in PB-28 (headset test pending). Base: C05 HumPerHW2 [Design](../../HumPerHW2/Docs/Design.md); PO answers of
2026-11-17. Items marked *(default)* were proposed by the agent and approved with the rest. Differences from C05 are marked
**(new)**.

## Posture and setting
Seated, posture `Seated`, the participant does not move. The C05 room: 8.72 m wide × 15 m deep × 3.5 m high, white walls and
ceiling, light-gray floor, soft baked lighting, the participant near the middle; Meta XR Audio shoebox of the same size, one
uniform reflection coefficient on all six surfaces, Meta XR Audio Reflection on the mixer, every spatial source routed to it.
The room geometry and materials are copied from C05 (chambers do not share assets).

**Acoustics (new):** one condition, `rt60_target_s` = **0.7 s** (coefficient from the RT60 calibration table, ADR-0010), no
background noise (silent room).

## Stimuli
| | Target (audio) | Radio (visual) | Anchors **(new)** |
|---|---|---|---|
| Object | Gaussian white noise, GPL.Stimuli (ADR-0008): 2 s, 20 ms cos² ramps, −26 dBFS RMS, seed 1; **looped 2 s on, 0.7 s off (new)** until A | Radio model; for now a **box placeholder** (`Stimuli/Radio/Model`, built by Step 4) 0.25 × 0.18 × 0.12 m, URP Lit, facing the participant *(default size)* | Rubber duck at **0°**, trumpet at **+90°**; for now **box placeholders** (`Stimuli/Anchors/Duck/Model`, `.../Trumpet/Model`: duck 0.15 m cube, yellow; trumpet 0.30 × 0.10 × 0.10 m, brass) *(default)*; a real model replaces the `Model` child |
| Sound | invisible | none | One at a time, looped: **duck sound, 1.5 s silence, trumpet sound, 2.5 s silence**, repeat. Each recording leveled to −26 dBFS RMS at load like the target (`StimulusClips.Leveled`). Placeholders until the PO adds recordings: duck = OpenAIR `Bicycle_Horn/classichorn.wav`, trumpet = OpenAIR `Natural_Baroque_Trumpet/tr-1780d-fanfare.wav` (both credited; the fanfare is **14.72 s** long, so one loop is about 19 s until the real trumpet recording replaces it) |
| Height | eye height | centre at eye height | centre at eye height |
| Distance | 2 m (practice 1.2 m), radius locked | 2 m (practice 1.2 m) | 2 m |
| Azimuth | starts at **−60° or +60°** (practice +30°) | **−22.5, −7.5, +7.5, +22.5°** (practice 0°) | 0° and +90° |
| When | from stimulus onset to A | with the target | the whole anchor block, visible and sounding: placed around the head position when an anchor block starts (after the inter-trial interval, before the first head gate), fixed in the room (not moved by a recenter), until the next no-anchor block starts *(default)*; two anchor blocks in a row keep the same loop and positions; models hidden and the loop paused while the Stop dialog is open; hidden and silent in no-anchor blocks and the practice |

Origin = head position at the trial start; forward = the Meta-button recenter of the onboarding. Azimuth 0 = forward, negative =
left. Levels: all gains at unity, Meta gain boost 0 dB, flat rolloff; loudness by the headset volume (`volume.target_step` 8).

## Hearing loss (new)
`GPL.HearingLoss` `OutputHearingLoss` on the AudioListener: the final left / right output after the spatializer, one ear filtered,
the other bit-exact (curves and sources: `docs/references/HearingLossLevels.md`). It applies to the target and the anchors.

| `hearing` | Left ear | Right ear | Curve (band gains 125 Hz-8 kHz, dB) |
|---|---|---|---|
| `normal` | 0 | 0 | none |
| `left40` | −40 | 0 | HLSim Severe: 0, 0, −10, −20, −25, −30, −30, −40, −30, −20 |
| `right40` | 0 | −40 | the same, right ear |
| `left80` | −80 | 0 | Severe × 2 (ours): 0, 0, −20, −40, −50, −60, −60, −80, −60, −40 |
| `right80` | 0 | −80 | the same, right ear |

The label is the 4 kHz band gain, not dB HL at the eardrum. The filter switches instantly in the inter-trial interval before a
block's first trial. Participants are not told that the hearing condition changed.

## Conditions and order (new)
10 cells = 5 `hearing` × 2 `anchors` (`none`, `on`); `condition` = `<hearing>_<anchors>`, e.g. `left80_on`. One block per cell.

| Cell | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
|---|---|---|---|---|---|---|---|---|---|---|
| `condition` | `normal_none` | `normal_on` | `left40_none` | `left40_on` | `right40_none` | `right40_on` | `left80_none` | `left80_on` | `right80_none` | `right80_on` |

**Order:** a balanced Latin square (Williams design, n = 10), row = **`study_id % 10`**. Row 0 = 0, 1, 9, 2, 8, 3, 7, 4, 6, 5;
row r = row 0 + r (mod 10). Each cell appears once in each block position, and each cell directly follows every other cell
exactly once over the 10 rows. With Study IDs 12-21 every row is used once; more participants repeat rows (22 → row 2, 23 →
row 3), which the analysis reports.

| Row | Block 1-10 (cells) |
|---|---|
| 0 | 0 1 9 2 8 3 7 4 6 5 |
| 1 | 1 2 0 3 9 4 8 5 7 6 |
| 2 | 2 3 1 4 0 5 9 6 8 7 |
| 3 | 3 4 2 5 1 6 0 7 9 8 |
| 4 | 4 5 3 6 2 7 1 8 0 9 |
| 5 | 5 6 4 7 3 8 2 9 1 0 |
| 6 | 6 7 5 8 4 9 3 0 2 1 |
| 7 | 7 8 6 9 5 0 4 1 3 2 |
| 8 | 8 9 7 0 6 1 5 2 4 3 |
| 9 | 9 0 8 1 7 2 6 3 5 4 |

`condition_order = fixed` (cells 0-9 in order) is for Debug runs only.

**Trials per block:** 16 = 4 radio positions × 2 start sides × 2 repeats, shuffled without constraints *(default)* with seed
`session seed + cell index`, so the trial list of a cell does not depend on its block position.

## Flow
Chamber menu → (Start) → stages as C05 (onboarding stages switchable by `onboarding.<stage>`):

| # | Stage | What happens | Resume policy |
|---|---|---|---|
| — | menu | Study ID, Start, Config; "Audio level: N (target 8)" | — |
| 1 | `handover` | stock; again after every resume | StageStart |
| 2 | `language` | English / 中文; skipped on a resume | StageStart |
| 3 | `recenter` | stock Meta-button recenter; again after every resume | StageStart |
| 4 | `instructions` | onboarding illustrations, **supplied by the PO at the last step** (until then C05's three slides as placeholders); they show the Y pause | StageStart |
| 5 | `ready` | stock dot + head gate, `volume_check`; again after every resume | StageStart |
| 6 | `practice` | as C05 with the radio: 0°, 1.2 m, start +30°, **normal hearing, no anchors**, no helper text; after each trial "Practice again" / "I'm ready"; at least one trial | StageStart |
| 7 | `experiment` | **10 blocks × 16 trials = 160, back to back: no block break screens, no prompted breaks (new)**; the cell changes silently in the inter-trial interval (the anchors appear or disappear) | `next_trial` |
| 8 | `thanks` | "Thank you. Please return the headset." | — |

No GUI during real trials (only the fixation dot before each trial), no feedback, no progress display.
Estimated duration (calculated, not measured): about 10.5 s per trial → about 28 min of trials, about 35 min per session with
onboarding and practice.

## Trial
1. **Inter-trial interval** `iti_s` = 0.6 s, empty view (not before the first trial of the session or after a resume).
2. **Head gate:** fixation dot ahead; GPL.Homing, C05 values (yaw ±5°, pitch ±10°, held 1 s, hint after 4 s). `homing` row.
3. **Stimuli on:** radio at `visual_az_deg`, target at `audio_start_az_deg`, both on the 2 m circle around the trial origin; the
   pulse loop starts.
4. **Adjustment:** left trigger held = the sound moves left, right trigger = right, 10°/s; both or none = still. **No limit
   (new): the sound can go round the full circle any number of times.**
5. **Confirm:** A (right controller), ignored during the first 0.3 s. `duration_s` = stimulus onset to A. No timeout.
6. Target and radio off; `trial` row. The anchors keep looping until the block ends.

## Pause and restart (Stop dialog, Y) (new)
The participant may pause at any time with **Y**: the stock Stop dialog (Keep going / Drop out, unchanged; the onboarding
illustration presents it as the pause). While it is open everything is frozen: target, anchors, adjustment, durations.
- **Keep going:** if the dialog was opened while the target was on (the adjustment), the trial is **restarted**: a
  `trial_restart` row (`reason=stop_dialog`) records the interrupted try (position, time), then the same trial runs again from
  the head gate with the same radio and start position (no inter-trial interval). The anchors resume. Opened during the
  inter-trial interval or the head gate, the trial simply continues (nothing was answered yet).
- **Drop out:** `participant_dropout`, not resumable (as C05).
- **App closed or crashed:** resume as C05: handover, recenter and the ready dot, then the **same unfinished trial** from the
  start (`next_trial`); the block's cell is re-applied (hearing filter, anchors placed anew around the new origin). If the
  target had already started, the resume writes a `trial_restart` row (`reason=app_closed`, no answer columns); the session
  file keeps the trial in progress (`custom.open_trial`) for this.

## Parameters (`Config/default.params`, preset "Default")
| Key | Default | Range | Group |
|---|---|---|---|
| `condition_order` | `williams` | `williams`, `fixed` | Procedure |
| `iti_s` | 0.6 | 0-5, step 0.1 | Procedure |
| `radius_m`, `practice.radius_m` | 2.0, 1.2 | 0.5-5, step 0.1 | Stimuli |
| `audio_start_deg`, `practice.audio_start_deg`, `practice.visual_deg` | 60, 30, 0 | −90..90 | Stimuli |
| `visual_pool_deg` | `7.5,22.5` (mirrored left) | list | Stimuli |
| `repeats` | 2 | 1-4 | Procedure |
| `stim.on_s`, `stim.off_s` | 2, 0.7 | 0.1-5 | Stimuli |
| `anchor.duck_deg`, `anchor.trumpet_deg`, `anchor.radius_m` | 0, 90, 2 | | Anchors |
| `anchor.gap_after_duck_s`, `anchor.gap_after_trumpet_s` | 1.5, 2.5 | 0-10, step 0.1 | Anchors |
| `adjust.rate_deg_s`, `confirm_ignore_s` | 10, 0.3 | | Response |
| `rt60_s` | 0.7 | 0.3-6 (table range) | Acoustics |
| `rt60.measure_at_block_start` | false | switch | Acoustics |
| `resume_from` | `next_trial` | as Template | Resume |
| `gate.*` | C05 values | GPL.Homing | Head gate |
| `volume.target_step` | 8 | 0-15 | Audio |
| `onboarding.*` | true | switch | Onboarding |

Not preset keys: the 10 cells, the hearing curves and the Williams square (Design, `Core`); the room size (scene).

## Config page (Debug mode)
As C05 (Run / Playground / Presets / Log files / Practice trial, Sound sphere), with **Acoustics** (Room 0.7 s / Meta preset, one choice) and **RT60** (Measure RT60, its own group: an action, not a room choice), plus
**Hearing** (5 choices: Normal, L −40, R −40, L −80, R −80) and **Anchors** (On / Off: shows the models and plays the loop).
These new sections are designed in the Storybook first (`Screens/C06 HumPerFinal Config`), then built. Leaving Config returns
to normal hearing, no anchors.
