# HumPerFinal (C06) — Brief

**Status:** approved by the PO 2026-11-17 (PB-28).

The final study of course 115A-535655, built on C05 HumPerHW2: the same seated adjustment task in the same white room, now with
a simulated **unilateral hearing loss** and visible, sounding **audio anchors**. The acoustics are fixed (RT60 0.7 s, silent room).

A seated participant rotates a looped white-noise source around them with the triggers, freely through 360°, until it seems to
come from a radio (eye level, 2 m), then confirms with A. Per radio position and start side, the final azimuth gives R (start at
+60°) and L (start at −60°); bias B = (R + L) / 2 and threshold (R − L) / 2 (method of adjustment, ascending and descending
series). Mean absolute error is the robustness check.

Factors (all within subject): **hearing** (normal, left −40 dB, right −40 dB, left −80 dB, right −80 dB) × **anchors** (none;
rubber duck at 0° and trumpet at +90°, both visible and sounding in turn) × **radio position** (−22.5, −7.5, +7.5, +22.5°) ×
**start side** (−60°, +60°) × 2 repeats = 160 trials. 10 participants, order by a balanced 10 × 10 Latin square
(`study_id % 10`).

- **RQ / hypotheses:** to be written by the PO.

Chamber ID `HumPerFinal`, label `C06`, folder `Chambers/HumPerFinal`. Docs: [Design](Design.md), [DataFormat](DataFormat.md),
[AnalysisPlan](AnalysisPlan.md). No new modules: GPL.Acoustics, SpatialSources, Responses, Stimuli, HearingLoss, Homing, Session.
