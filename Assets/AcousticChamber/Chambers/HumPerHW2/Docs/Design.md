# HumPerHW2 (C01) — Design

**Status:** approved by the PO (PB-08a, 2026-11-07). Source: HW2 `Docs/ExperimentDesign.md` (protocol of 2026-10-18,
PB-26..30) and the PO answers Q19-Q28 (archive). Differences from HW2 are marked **(new)**.

## Posture and setting
Seated, posture `Seated`. Room 8.72 m wide × 15 m deep × 3.5 m high (HW2 default), white walls and ceiling, light-gray floor,
soft baked lighting without hard shadows; the participant sits near the middle. The Meta XR Audio shoebox room has the same size,
one uniform reflection coefficient on all six surfaces, rendered by the mixer effect "Meta XR Audio Reflection" (every spatial
source routed to it).

## Stimuli
| | Visual | Audio |
|---|---|---|
| Object | Radio picture (HW2 `Radio.jpg`) on a cardboard, unlit, 0.25 m wide (aspect kept, about 0.21 m high), facing the participant | White noise `SFX-C-01` (mono, -23 LUFS), pulse train 2 s on, 1 s off, until A |
| Height | Center at eye height (trial origin) | Eye height |
| Distance | 2 m; practice 1.2 m | Same; radius locked while it rotates |
| Azimuth | Trials: one of ±7.5, ±22.5, ±37.5° (never 0); practice 0° | Starts at +60 / -60° alternating (practice +30°) |

Origin = head position at the trial start; forward = the Meta-button recenter of the onboarding. Azimuth 0 = forward, negative = left.
Levels: all gains at unity, Meta gain boost 0 dB, flat rolloff (1.2 m and 2 m equally loud); loudness set by the headset volume.

## Conditions
| Condition | `rt60_target_s` | Background |
|---|---|---|
| `quiet` | 0.3 (lowest renderable target) | none |
| `noisy` | 0.7 | `SFX-C-02..04` together, looped, unity gain, at eye level 2 m: left -90°, right +90°, behind 180° |
| `reverb` | 2.2 | the same background |

- Reflection coefficient from the measured calibration table (`Rt60Calibration`, HW2 2026-10-18, Windows plugin).
- The background sources are placed once when the background starts and stay fixed in the room (not moved by a recenter); they go
  through the room like the stimulus. It keeps playing when the next condition also has a background (also over the block break);
  it stops when a `quiet` block starts. **(new: with counterbalancing a `reverb` block may start the background itself.)**
- **Order (new, Q19, Q29):** `condition_order = latin_square` (default): a 3-row Latin square, row = (Study ID − 1) mod 3:
  row 1 `quiet, noisy, reverb`, row 2 `noisy, reverb, quiet`, row 3 `reverb, quiet, noisy`. Each condition is once in each
  position; carry-over is not balanced (e.g. `reverb` always follows `noisy`). Recruit in multiples of 3.
  `fixed` = HW2's Quiet, Noisy, Reverb.
- Practice uses the `quiet` acoustics.

## Flow
Chamber menu → (Start) → stages (onboarding stages switchable by `onboarding.<stage>`, default on):

| # | Stage | What happens | Resume policy |
|---|---|---|---|
| — | menu | Study ID, Start, Debug; "Audio level: N (target 8)". | — |
| 1 | `handover` | Stock: "1. Set the IPD. 2. Hand the headset to the participant." Runs again after every resume. | StageStart |
| 2 | `language` | English / 中文; skipped on a resume. | StageStart |
| 3 | `recenter` | Stock Meta-button recenter slide. Runs again after every resume. | StageStart |
| 4 | `instructions` | HW2 onboarding slides 3-5 (Procedure, Controllers, Practice rounds), English and 中文 image sets ported from HW2 (Q22). HW2 slides 1, 2, 6 are covered by the stock stages. | StageStart; reopens its slide |
| 5 | `ready` | Stock: dot + head gate, `volume_check`. Runs again after every resume. | StageStart |
| 6 | `practice` | Practice trials with the hint HUD ("Left / right trigger: move the sound. A: confirm."); after each trial "Practice again" / "I'm ready" (中文 「再練習一次」/「我準備好了」) (Q20). At least one trial. | StageStart |
| 7 | `experiment` | 3 blocks, one per condition in the session's order, 6 trials each; stock block break between blocks ("Condition n of 3 done." + Continue) (Q26). | `next_trial` (Q21) |
| 8 | `thanks` | "Thank you. Please return the headset." | — |

No GUI during real trials (only the fixation dot before each trial), no feedback. Progress shows only on the block breaks.

## Trial
1. **Inter-trial interval** `iti_s` = 0.6 s, empty view (at the start of every trial but the first of a block).
2. **Head gate:** fixation dot ahead; yaw ±5°, pitch ±10°, held 1 s; hint after 4 s (HW2 values, Q24). `homing` row.
3. **Stimuli on:** picture at `visual_az_deg`, sound at `audio_start_az_deg`, both on the circle around the trial origin; the pulse
   train starts.
4. **Adjustment:** left trigger held = the sound moves left, right trigger = right, 10°/s; both or none = still; clamped to ±90°.
5. **Confirm:** A (right controller), ignored during the first 0.3 s. `duration_s` = stimulus onset to the A press. No timeout.
6. Sound and picture off; `trial` row.

Trial lists per condition (HW2 `TrialPlanner`): audio starts +60, -60, +60, -60, +60, -60; visual side left, left, right, right,
left, right; each side's three azimuths once per condition, shuffled with seed `session seed + condition index` (`quiet` 0,
`noisy` 1, `reverb` 2), so the positions do not depend on the order.

## Parameters (`Config/default.params`, preset "Default")
| Key | Default | Range | Group |
|---|---|---|---|
| `condition_order` | `latin_square` | `latin_square`, `fixed` | Procedure |
| `trials_per_condition` | 6 | fixed by the plan (6) | Procedure |
| `iti_s` | 0.6 | 0-5, step 0.1 | Procedure |
| `radius_m`, `practice.radius_m` | 2.0, 1.2 | 0.5-5, step 0.1 | Stimuli |
| `audio_start_deg`, `practice.audio_start_deg`, `practice.visual_deg` | 60, 30, 0 | -90..90 | Stimuli |
| `visual_pool_deg` | `7.5,22.5,37.5` (mirrored left) | list | Stimuli |
| `stim.on_s`, `stim.off_s` | 2, 1 | 0.1-5 | Stimuli |
| `adjust.rate_deg_s`, `adjust.limit_deg`, `confirm_ignore_s` | 10, 90, 0.3 | | Response |
| `rt60.quiet_s`, `rt60.noisy_s`, `rt60.reverb_s` | 0.3, 0.7, 2.2 | 0.3-6 (table range) | Acoustics |
| `bg.radius_m`, `bg.azimuths_deg` | 2, `-90,90,180` | | Acoustics |
| `rt60.measure_at_block_start` | false (Q25) | switch | Acoustics |
| `room.width_m`, `room.depth_m`, `room.height_m` | 8.72, 15, 3.5 | read-only at run time (scene) | Acoustics |
| `resume_from` | `next_trial` | as Template | Resume |
| `gate.yaw_tol_deg`, `gate.pitch_tol_deg`, `gate.hold_s`, `gate.hint_after_s` | 5, 10, 1, 4 | GPL.Homing | Head gate |
| `volume.target_step` | 8 | 0-15 | Audio |
| `onboarding.*` | true | switch | Onboarding |

## Stop dialog (Y)
As the Template: Keep going / Drop out (`participant_dropout`, not resumable; replaces HW2's cancel). The trial is frozen while open
(audio paused, adjustment stopped, duration paused); the trial is flagged `stop_dialog=1`.

## Resume
Silently, as the Template: handover, recenter and the ready dot again, then the next unfinished trial (`next_trial`). Before it, the
block's condition is re-applied: reverb coefficient, background restarted (placed anew around the new origin) if the condition has
one. A practice interrupted before "I'm ready" restarts the practice stage.

## Debug (chamber sections, PB-08e)
Besides Run / Playground / Presets / Log files / Practice trial:
- **Acoustics:** 0.3 s Quiet, 0.7 s Noisy (starts the background), 2.2 s Reverb (background on), Preset (Meta Concrete, about 4 s);
  status shows the target, coefficient and calculated RT60.
- **Playback:** Play stimulus (pulse train ahead at 2 m with the picture, current condition), Play / pause background.
- **Measure RT60** (impulse at the stimulus source, Schroeder T20 / T30, about 5 s; `rt60_measured` row).
- **Sound sphere:** Always visible / After confirm; Debug trials show the signed difference (audio − visual) until A.
Leaving Debug returns to the Default acoustics.
