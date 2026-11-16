# Reverberation (C04) — Design

**Status:** PB-27, PO answers 2026-11-16 (profiles "Both", room size "Steppers + presets", cancelled runs "Delete all incomplete").
Storybook first: `Screens/C04 Reverberation`, `Screens/RT60 Run Dialog`, `Components/UiProgressBar`.

## Purpose
Make and check the RT60 calibration table (`Rt60CalibrationAsset`) of one room size on the device the participants hear (ADR-0010,
GPL.Acoustics README "Calibration"). Nothing else sounds in this chamber.

## Scene
- Open space: floor grid, the MainHall sky, no walls. Shoebox `Environment/Shoebox Acoustics` locked to the listener, clutter 0;
  `Audio/Reverb` (`ReverbTuner`, no `ShoeboxRoom`): the panel sets the shoebox size (`ReverbTuner.SetRoomSize`), which also finds the
  imported table for that size and platform.
- `Audio/ReverberationMixer.mixer` with the Meta XR Audio Reflection effect: the RT60 probe is routed to it.
- Between runs the room is Dry (reflection 0).

## Panel (1100 × 1040 u, movable, centered ahead: yaw 0°, 8° down; PO 2026-11-17)
| Row | Control | Values |
|---|---|---|
| (intro) | text2 | "Calibrate the reverberation of a room size on this headset. Stay still and quiet while it runs." |
| Room | `ChoiceGroup` | **Open space** 12 × 12 × 3.5 m (C01, C02), **HW2 room** 8.72 × 15 × 3.5 m (C05), **Custom**; start: Open space |
| Width / Depth / Height | `Stepper` | 0.1 m steps; W, D 2-30 m; H 2-10 m; rounded to 1 cm; a size equal to a preset selects it, else Custom |
| Profile | `ChoiceGroup` | Experience, HW2, Short, Long (table below); start: Experience |
| (profile line) | text2 | "Targets 0.7, 2.2 s · sweep 21 points, about 6 min · measure about 3 min" (from the plans' estimates) |
| (status) | — | none: C04 makes the CSV; it shows neither a calibrated line nor the uncalibrated warning (PO 2026-11-17) |
| (hint) | text2 | where the report is and how to import it |
| Toolbar | buttons | **Measure RT60** (gray, left), **Calibrate** (prominent, right) |

## Profiles (`GPL.Acoustics.Rt60Profiles`, PO 2026-11-16)
| Profile | Targets (T_mid) | Sweep |
|---|---|---|
| Experience | 0.7, 2.2 s | full grid (`Rt60Protocol.SweepReflections`, 21 points) |
| HW2 | 0.3, 0.7, 2.2 s | full grid |
| Short | 0.3, 0.5, 0.7, 1.0 s | grid points whose expected RT60 lies in [0.7 × 0.3, 1.3 × 1.0] s, plus one neighbour each side (at least 4) |
| Long | 1.5, 2.2, 3.0, 4.0 s | same rule: [0.7 × 1.5, 1.3 × 4.0] s |

The expected RT60 of a coefficient comes from the table in use for this size: the imported one, or the HW2 table scaled by V/S.
Open space 12 × 12 × 3.5 m with the HW2 table: Short = 0.20-0.90 (12 points), Long = 0.85-0.99 (12 points).

## Runs (App `Rt60RunDialog`)
- **Calibrate**: `Rt60Plan.Sweep` with the profile's coefficients, 0°, 3 repeats (README protocol). Report kind `sweep`, extra header
  lines `profile` and `room_set_on_panel`. Ends with "Calibration completed" and the CSV to import (main protocol); no results (PO).
  Buttons: Close, and **Use on this headset** (PO 2026-11-17): the sweep becomes this headset's table for its size at once, no
  rebuild (`Rt60HeadsetTables`, `persistentDataPath/Calibration/Tables/`); an imported table of the same size still wins; the other
  chambers then show "RT60 calibrated on this headset only: import it before the study." (text2) instead of the warning.
- **Measure RT60**: one condition per target ("Target 0.7 s"), 4 positions × 3 repeats. Ends with T_mid ± SD, deviation from the target,
  "flagged" and, with an imported table, "outside the table" for a target beyond its range (the coefficient is clamped there).
- Cancel stops the run; a cancelled, refused or failed run leaves **no report files** (its text is in the Console).
- Y opens Keep going / Back to Hall; leaving stops a run.

## Not here
No Study ID, no session, no log rows. The committed, imported table (built in the Editor) stays the main protocol; a headset table is
for trying a size before the import and is not in git.
