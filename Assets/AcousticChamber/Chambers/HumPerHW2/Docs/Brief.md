# HumPerHW2 (C01) — Brief

**Status:** approved by the PO (PB-08a, 2026-11-07).

HW2 (`../115A-535655-HumPer-HW2`, course 115A-535655) rebuilt on the GPL modules. It proves the refactoring: the same task,
stimuli, acoustics and trial data as HW2, with the AcousticChamber session, preset, resume and logging scheme. One deliberate
change: the condition order is **counterbalanced** by default (HW2 used a fixed order; PO 2026-11-07, Q19).

- **RQ1:** How do acoustic conditions affect the audio-visual disparity threshold of the ventriloquist effect in VR?
- **H1:** The threshold is larger in Reverb than in Quiet; Noisy lies at or above Quiet and below Reverb.

A seated participant rotates a pulsed white-noise source around them with the triggers until it seems to come from a small
picture of a radio (eye level, 2 m), then confirms with A. The final azimuth of the sound is logged per trial; the remaining gap
to the picture estimates the tolerated audio-visual disparity (method of adjustment).

Conditions (within subject, 6 trials each): **Quiet** (RT60 0.3 s, no background), **Noisy** (RT60 0.7 s, three background
recordings), **Reverb** (RT60 2.2 s, the same background).

Chamber ID `HumPerHW2`, label `C01`, folder `Chambers/HumPerHW2`. Docs: [Design](Design.md), [DataFormat](DataFormat.md),
[AnalysisPlan](AnalysisPlan.md).

New modules extracted from HW2 for this chamber (PB-08b, c): `GPL.Acoustics`, `GPL.SpatialSources`, `GPL.Responses`.
