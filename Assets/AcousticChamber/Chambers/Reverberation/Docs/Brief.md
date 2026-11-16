# Reverberation (C04) — Brief

**Status:** built from the PO's request of 2026-11-16 (PB-27); approved in the planning answers of that day.

A **tool chamber** built as an experience chamber (ADR-0006): no study, no Study ID, no session logs. Its job is to make the
**RT60 calibration table for a given room size** on the headset, and to check the targets with that table.

- The panel sets the **room size** (shoebox W × D × H: presets of the chambers' rooms, or steppers), a **target RT60 profile** (the
  targets to check and the sweep range), and has **Calibrate** as the main action and **Measure RT60** beside it.
- Both run in the **RT60 run dialog** (progress bar, time left, Cancel). A calibration ends with "Calibration completed" only; a
  measurement shows the T_mid per target.
- The sweep report is imported in the Editor (`Magic > Acoustics > Import RT60 Calibration`); from then on every chamber of that
  size counts as calibrated on that device.

Until PB-27 the Calibrate buttons were in C01, C02 and HumPerHW2 (then C04, now C05); they are gone there (PO 2026-11-16).

Chamber ID `Reverberation`, label `C04`, folder `Chambers/Reverberation`. Docs: [Design](Design.md), [SceneSetup](SceneSetup.md).
No DataFormat / AnalysisPlan: it writes no participant data, only the RT60 reports of GPL.Acoustics (README "Report").
