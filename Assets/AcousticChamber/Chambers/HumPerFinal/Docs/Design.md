# HumPerFinal (C05) — Design

**Status:** approved by the PO 2026-11-17 (PB-28); built in PB-28 (headset test passed 2026-11-18). Base: HumPerHW2 (the former C05, removed in PB-29; its Design.md is in git history, commit 1d3b29f); PO answers of Relabelled C06 → C05 when HumPerHW2 was removed (PB-29, PO 2026-11-17).
2026-11-17. Items marked *(default)* were proposed by the agent and approved with the rest. Differences from HW2 are marked
**(new)**.

**Change 2026-11-18 (PB-28, PO):** fewer trials per block. Each radio position is tested from **one** start side (a
*position pair*), chosen so that each hemisphere still has one start from each side; the participant's **pairing set**
(`study_id % 4`) is the same in every block and both repeats. 16 → **8 trials per block, 160 → 80 trials** (5 hearing ×
2 anchors × 4 position pairs × 2 repeats). Approved by the PO 2026-11-18 and built (headset test passed 2026-11-18).

**Change 2026-11-18 (PO): dining room and real models.** The white HW2 room is replaced by a furnished dining room (the PO's
`C05.blend`, from the Sketchfab "Dining room | Kichen baked"), and the boxes by real models: rubber duck, a standing trumpet
player, radio. The sounds keep the positions of the table below (eye height, 3 m); the duck and the player models stand on
their surfaces under their sounds (table top, floor). PO: "With a generic HRTF, people can't differentiate the 0.4 m
incongruence", so the visual and the sound may be apart vertically. Approved by the PO 2026-11-18.

## Posture and setting
Seated, posture `Seated`, the participant does not move. **Dining room (2026-11-18):** 8.75 m wide × 15 m deep × 3.5 m high
(the PO kept 8.75 m; the acoustic shoebox stays 8.72 m), the participant seated at the room centre at the near end of an oval
dining table (2.6 × 1.6 m, top 0.78 m, from 0.59 to 3.19 m ahead), facing the kitchen wall (+z); window on the left, plain wall
on the right; the chair straight ahead at the far end of the table and the vase and flower on the table removed (they would
stand between the participant and the radio positions). The room is URP Unlit with the light baked into its textures (no
lightmaps); one real-time light from the window side and flat ambient light the props (URP Lit, no shadows; soft contact
shadows under the duck, the player and the music stand). Meta XR Audio shoebox 8.72 × 15 × 3.5 m (the calibrated room), one
uniform reflection coefficient on all six surfaces, Meta XR Audio Reflection on the mixer, every spatial source routed to it.

**Acoustics (new):** one condition, `rt60_target_s` = **0.7 s** (coefficient from the RT60 calibration table, ADR-0010), no
background noise (silent room).

## Stimuli
| | Target (audio) | Radio (visual) | Anchors **(new)** |
|---|---|---|---|
| Object | Gaussian white noise, GPL.Stimuli (ADR-0008): 2 s, 20 ms cos² ramps, −26 dBFS RMS, seed 1; **looped 2 s on, 0.7 s off (new)** until A; **the first pulse of each trial (and of each try after a restart) fades in over 0.5 s, cos² (`stim.first_fade_s`; PO 2026-11-18, the first pulse after a hearing-loss block felt too loud)**, later pulses unchanged | Radio model (Sketchfab "Radio", 0.26 × 0.23 × 0.08 m), URP Lit, facing the participant, floating (no surface at 3 m and eye height; PO 2026-11-18) | Rubber duck at **0°** (Sketchfab "Rubber Duck", 0.12 m long, facing the participant) and a trumpet player at **+90°** (Sketchfab "Character Walk" posed playing the Sketchfab "Trumpet", bell toward the participant), URP Lit; the box placeholders remain only when the models are missing |
| Model position **(2026-11-18)** | — | centred on the sound | x / z under the sound; the duck's bottom on the table top (0.78 m), the player standing on the floor with the bell about 1.61 m high (`AnchorLoop.duckModelY`, `trumpetModelY`, set by Step 4 from the model export) |
| Sound | invisible | none | One at a time, looped: **duck sound, 1.5 s silence, trumpet sound, 2.5 s silence**, repeat. Each recording leveled to −26 dBFS RMS at load like the target (`StimulusClips.Leveled`). Both from the GPL.Stimuli library (PO 2026-11-17): duck = Freesound 348791 "Toy Ducks Quacking" (`Freesound/348791__nebyoolae__toy-ducks-quacking.mp3`, CC0, about 4.9 s), trumpet = OpenAIR `Natural_Baroque_Trumpet/tr-1780d-fanfare.wav` (**14.72 s**); one loop is about 24 s |
| Height | eye height | centre at eye height | centre at eye height |
| Distance | **3 m** (practice **1.5 m**), radius locked (PO 2026-11-17; HW2: 2 m / 1.2 m) | 3 m (practice 1.5 m) | 3 m |
| Azimuth | starts at **−60° or +60°**, fixed per radio position by the pairing set (practice +30°) | **−22.5, −7.5, +7.5, +22.5°** (practice 0°) | 0° and +90° |
| When | from stimulus onset to A | with the target | the whole anchor block, visible and sounding: placed around the head position when an anchor block starts (after the inter-trial interval, before the first head gate), fixed in the room (not moved by a recenter), until the next no-anchor block starts *(default)*; two anchor blocks in a row keep the same loop and positions; models hidden and the loop paused while the Stop dialog is open; hidden and silent in no-anchor blocks and the practice |

Origin = head position at the trial start; forward = the Meta-button recenter of the onboarding. Azimuth 0 = forward, negative =
left. Levels: all gains at unity, Meta gain boost 0 dB, flat rolloff; loudness by the headset volume (`volume.target_step` 13, PO 2026-11-18; was 12).

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

**Trials per block:** 8 = 4 position pairs × 2 repeats, shuffled without constraints *(default)* with seed
`session seed + cell index`, so the trial list of a cell does not depend on its block position. *(2026-11-18; was 16 = 4
radio positions × 2 start sides × 2 repeats.)*

**Position pairs (2026-11-18).** A position pair is one radio position with one start side (L = start −60°, the sound comes
in from the left; R = start +60°). In each hemisphere one position starts from L and the other from R, so the hemisphere
average still has an ascending and a descending series. Left hemisphere: way a ∈ {0, 1}; right hemisphere: way b ∈ {0, 1};
**pairing set** s = 2a + b = **`study_id % 4`**, fixed for the session (every block, both repeats).

| Set s | −22.5° | −7.5° | +7.5° | +22.5° | Note |
|---|---|---|---|---|---|
| 0 (a 0, b 0) | L | R | L | R | outer positions start on their own side, inner on the opposite side (mirror-symmetric) |
| 1 (a 0, b 1) | L | R | R | L | |
| 2 (a 1, b 0) | R | L | L | R | |
| 3 (a 1, b 1) | R | L | R | L | outer start on the opposite side, inner on their own side (mirror-symmetric) |

Within a participant the radio position (inner / outer) and the start side are confounded inside a hemisphere; only the
balance of sets across participants separates them. With 10 participants (Study IDs 12-21) the sets are used 3, 3, 2, 2
times (s 0, 1, 2, 3: IDs 12/16/20, 13/17/21, 14/18, 15/19), which the analysis reports; 20 participants would balance both
the Williams rows and the sets. Debug runs use the set of the Study ID entered (0 without one), as `order_row`.

## Flow
Chamber menu → (Start) → stages as HW2 (onboarding stages switchable by `onboarding.<stage>`):

| # | Stage | What happens | Resume policy |
|---|---|---|---|
| — | menu | Study ID, Start, Config; "Audio level: N (target 12)" | — |
| 1 | `handover` | stock; again after every resume | StageStart |
| 2 | `language` | English / 中文; skipped on a resume | StageStart |
| 3 | `recenter` | stock Meta-button recenter on the PO's slide 1 "Before we start" / "開始之前" (`Slides/*-1-reposition.jpg`; the buttons leading back to it read "Back" / "上一頁"); again after every resume | StageStart |
| 4 | `instructions` | the PO's illustrations (2026-11-17), titles English / 中文: "Procedure" / "實驗說明", "Controllers" / "操作說明", "Trials" / "練習回合", "Ready?" / "準備就緒？" (announces the dot); the Back / Next buttons carry the neighbouring titles, the last Next reads "Start Practice" / "開始練習" (PO 2026-11-18); they show the Y pause | StageStart |
| 5 | `ready` | stock dot + head gate (GPL.Protocol with GPL.Homing; the dot 1° above eye level, `gate.dot_elevation_deg`), `volume_check`; again after every resume | StageStart |
| 6 | `practice` | as HW2 with the radio: 0°, 1.5 m, start +30°, **normal hearing, no anchors**, no helper text; after each trial "Practice again" / "I'm ready"; at least one trial | StageStart |
| 7 | `experiment` | **10 blocks × 8 trials = 80 (2026-11-18; was 160), back to back: no block break screens, no prompted breaks (new)**; the cell changes silently in the inter-trial interval (the anchors appear or disappear) | `next_trial` |
| 8 | `thanks` | "Thank you. Please return the headset." | — |

No GUI during real trials (only the fixation dot before each trial), no feedback, no progress display.
Estimated duration (calculated, not measured): about 10.5 s per trial → about 14 min of trials, about 21 min per session with
onboarding and practice (2026-11-18; was 28 / 35 min). A block lasts about 85 s, about 3.5 anchor loops (about 24 s each).

## Trial
1. **Inter-trial interval** `iti_s` = 0.6 s, empty view (not before the first trial of the session or after a resume).
2. **Head gate:** fixation dot ahead, 1° above eye level (GPL.Homing default since 2026-11-18: the head drops while resting, the dot sits higher); HW2
   values otherwise (yaw ±5°, pitch ±10° around the dot, held 1 s, hint after 4 s: only the dot, the black text "Look at the dot" appears only when the gate is not passed by then, PO 2026-11-17). `homing` row.
3. **Stimuli on:** radio at `visual_az_deg`, target at `audio_start_az_deg`, both on the 3 m circle around the trial origin; the
   pulse loop starts.
4. **Adjustment:** left trigger held = the sound moves left, right trigger = right, 10°/s; both or none = still. **No limit
   (new): the sound can go round the full circle any number of times.**
5. **Confirm:** A (right controller) or X (left; left-handed participants, PO 2026-11-17), ignored during the first 0.3 s. `duration_s` = stimulus onset to A. No timeout.
6. Target and radio off; `trial` row. The anchors keep looping until the block ends.

## Pause and restart (Stop dialog, Y) (new)
The participant may pause at any time with **Y**: the stock Stop dialog (Keep going / Drop out, unchanged; the onboarding
illustration presents it as the pause). While it is open everything is frozen: target, anchors, adjustment, durations.
- **Keep going:** if the dialog was opened while the target was on (the adjustment), the trial is **restarted**: a
  `trial_restart` row (`reason=stop_dialog`) records the interrupted try (position, time), then the same trial runs again from
  the head gate with the same radio and start position (no inter-trial interval). The anchors resume. Opened during the
  inter-trial interval or the head gate, the trial simply continues (nothing was answered yet).
- **Drop out:** `participant_dropout`, not resumable (as HW2).
- **App closed or crashed:** resume as HW2: handover, recenter and the ready dot, then the **same unfinished trial** from the
  start (`next_trial`); the block's cell is re-applied (hearing filter, anchors placed anew around the new origin). If the
  target had already started, the resume writes a `trial_restart` row (`reason=app_closed`, no answer columns); the session
  file keeps the trial in progress (`custom.open_trial`) for this.

## Parameters (`Config/default.params`, preset "Default")
| Key | Default | Range | Group |
|---|---|---|---|
| `condition_order` | `williams` | `williams`, `fixed` | Procedure |
| `iti_s` | 0.6 | 0-5, step 0.1 | Procedure |
| `radius_m`, `practice.radius_m` | 3.0, 1.5 | 0.5-5, step 0.1 | Stimuli |
| `audio_start_deg`, `practice.audio_start_deg`, `practice.visual_deg` | 60, 30, 0 | −90..90 | Stimuli |
| `visual_pool_deg` | `7.5,22.5` (mirrored left; exactly two values, the inner and outer position, for the pairing sets) | list | Stimuli |
| `repeats` | 2 | 1-4 | Procedure (per position pair and block) |
| `stim.on_s`, `stim.off_s` | 2, 0.7 | 0.1-5 | Stimuli |
| `stim.first_fade_s` | 0.5 | 0-2, step 0.1 (at most `stim.on_s`) | Stimuli |
| `anchor.duck_deg`, `anchor.trumpet_deg`, `anchor.radius_m` | 0, 90, 3 | | Anchors |
| `anchor.gap_after_duck_s`, `anchor.gap_after_trumpet_s` | 1.5, 2.5 | 0-10, step 0.1 | Anchors |
| `adjust.rate_deg_s`, `confirm_ignore_s` | 10, 0.3 | | Response |
| `rt60_s` | 0.7 | 0.3-6 (table range) | Acoustics |
| `rt60.measure_at_block_start` | false | switch | Acoustics |
| `resume_from` | `next_trial` | as Template | Resume |
| `gate.*` | HW2 values; `gate.dot_elevation_deg` +1 | GPL.Homing | Head gate |
| `volume.target_step` | 13 | 0-15 | Audio |
| `onboarding.*` | true | switch | Onboarding |

Not preset keys: the 10 cells, the hearing curves, the Williams square and the pairing sets (Design, `Core`); the room size (scene).

## Config page (Debug mode)
As HW2 (Run / Playground / Presets / Log files / Practice trial, Sound sphere), with **Acoustics** (Room 0.7 s / Meta preset, one choice) and **RT60** (Measure RT60, its own group: an action, not a room choice), plus
**Hearing** (5 choices: Normal, L −40, R −40, L −80, R −80) and **Anchors** (On / Off: shows the models and plays the loop).
These new sections are designed in the Storybook first (`Screens/C05 HumPerFinal Config`), then built. Leaving Config returns
to normal hearing, no anchors.
