# Template (C00) — Design

**Status:** approved by the PO (PB-07, 2026-11-06).

## Posture and setting
Seated or standing in place, no room-scale movement. Default sky (no room model). The fixation dot is `#262626`, 1.5° at 2 m.

## Flow
Chamber menu → (Start) → stages (onboarding stages 1–5 each have a preset switch `onboarding.<stage>`, default on):

| # | Stage (`stage` column) | What happens | Resume policy |
|---|---|---|---|
| — | menu | Experimenter: Study ID (number pad), Start. If the ID has an unfinished session: Resume / Start new / Back. Under the buttons: "Audio level: N (target 8)" (`volume.target_step`, default 8; red when different). The read step is logged on `session_start` / `session_resume`. | — |
| 1 | `handover` | Experimenter: "1. Set the IPD. 2. Hand the headset to the participant." (numbered list) + Continue. Runs again after every resume. | StageStart |
| 2 | `language` | Participant picks English / 中文 (two buttons). Stored in the session record; skipped on a resume. | StageStart |
| 3 | `recenter` | A slide (instruction layout): title "Recenter the view", illustration (`recenterSlide`; for now the text "Look straight ahead. Hold the Meta button until the view recenters."), Back = "Language" (reopens the language choice, then this slide), Next always active; Back and Next carry arrows (`homing` row: the Meta-button recenter, or outcome `cancelled` "next without a Meta recenter"; `slide` row `recenter 1/1`). Runs again after every resume. | StageStart |
| 4 | `instructions` | Instruction slides (PB-20; `Slides/Instructions.asset`, two gray placeholders from `Magic > Template > Step 3`): title, image, Back / Next, Continue on the last. One `slide` row per slide shown. No slides in the set: the stage shows nothing. | StageStart; reopens the slide it was on |
| 5 | `ready` | Final check: "Look at the dot to begin" under the black dot, head gate (`homing` row), then the headset volume (`volume_check` row, note `target=8 match=1`). Runs again after every resume. | StageStart |
| 6 | `practice` | `practice_trials` trials, same as below; logged, excluded from analysis. | StageStart |
| 7 | `experiment` | `blocks` blocks of `trials_per_block` trials; a short "Block n of N done." with a Continue button between blocks (no A). | `resume_from` |
| 8 | `thanks` | "Thank you. Please return the headset." Session completed. | — |

## Trial
1. **Head gate:** the fixation dot appears straight ahead; the participant holds the head within the gate (`gate.*`, default yaw ±10°, pitch ±15°, 0.5 s; hint "Look at the dot" after 4 s). A `homing` row is logged.
2. **Go cue:** the dot disappears and "Press A" (中文: "請按 A") appears.
3. **Response:** A ends the trial; the reaction time is from the cue onset to the press (frame-based: the frame the cue is shown to the frame A is read, about 11 ms resolution at 90 Hz). No press within `response_timeout_s` = timeout (logged, not repeated).
4. **Inter-trial interval:** `iti_s` of empty view between trials (it runs at the start of every trial but the first of a block, so the `trial` row is the last thing a trial does and a trial interrupted by a kill is never counted).

## Parameters (`Config/default.params`, preset "Default")
| Key | Default | Range | Group |
|---|---|---|---|
| `practice_trials` | 2 | 0–10 | Procedure |
| `blocks` | 2 | 1–10 | Procedure |
| `trials_per_block` | 4 | 1–50 | Procedure |
| `response_timeout_s` | 5 | 1–30, step 1 | Procedure |
| `iti_s` | 1.0 | 0–5, step 0.1 | Procedure |
| `resume_from` | `block_start` | `next_trial`, `block_start`, `stage_start`, `session_start`, `not_resumable` | Resume |
| `gate.yaw_tol_deg`, `gate.pitch_tol_deg`, `gate.hold_s`, `gate.hint_after_s` | 10, 15, 0.5, 4 | GPL.Homing | Head gate |
| `volume.target_step` | 8 (proposed, PB-20b) | 0–15 | Audio |
| `onboarding.handover`, `.language`, `.recenter`, `.instructions`, `.ready` | true | switch | Onboarding |

`resume_from` applies to the `experiment` stage (PO 2026-11-06: one test run per policy).

## Stop dialog (Y)
Real session: Keep going / Drop out (asks "Drop out S0012?"; logs `participant_dropout`, session closed). Debug run: Keep going /
Back to the chamber menu. The trial is frozen while the dialog is open (gate timer and reaction time paused; the RT of a trial
interrupted by the dialog is flagged `stop_dialog=1`).

## Resume
On a resume the participant continues silently: no language stage; the handover, the Meta-button recenter and the final check at the dot run again (the slides reopen where they were), then the stage / block / trial given
by the policy:
- `next_trial`: after the last finished trial.
- `block_start`: the interrupted block again, `attempt` + 1 (`block_restart` row).
- `stage_start`: the experiment stage from block 1, the attempt of each block + 1.
- `session_start`: everything again (same CSV), attempts + 1.
- `not_resumable`: the menu does not offer the interrupted session (GPL.Session `CanResume` is false); Start with the same Study
  ID begins a new session (new files).

## Debug
Debug page: Run (the real protocol with the working copy, `_DEBUG` CSV, no session file, no resume), Parameters, Presets, Data.
No chamber-specific Debug controls.
