# HearingLoss (C03) — Brief

**Status:** built from the PO's answers (PB-10 planning, 2026-11-07); waiting for the headset check.

An **experience chamber** (ADR-0006): no study, no Study ID, no logs. A visitor hears what a typical noise-induced hearing loss
does to everyday sounds, on both ears or on one ear, and moves the sound around to hear the difference between the ears.

- A loudspeaker stands on the floor 2 m ahead. Pointing at it and holding the trigger drags it over the floor (it stays on the
  floor), anywhere from 0.5 to 6 m away.
- A panel to the left picks the sound (four clips taken from HW2 for now), plays / pauses it, picks **Both ears / Left ear /
  Right ear** and the loss: **Normal, -20 dB, -30 dB, -40 dB** (the NIOSH HLSim presets Mild, Moderate and Severe: a notch
  deepest at 4 kHz). Changes are heard at once.
- The room is dry: a floor, no walls, no reverb.
- Leave with **Back to Hall** on the panel or with **Y**.

The loss is applied to the app's final left/right output (`GPL.HearingLoss`, PB-09). It models audibility only; see the module
README for the limits (deep cuts are wide, levels are digital, no recruitment).

Chamber ID `HearingLoss`, label `C03`, folder `Chambers/HearingLoss`. Docs: [Design](Design.md), [SceneSetup](SceneSetup.md).
No DataFormat / AnalysisPlan (no data).
