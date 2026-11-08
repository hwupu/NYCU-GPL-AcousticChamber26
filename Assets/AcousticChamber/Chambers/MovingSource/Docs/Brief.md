# MovingSource (C01) — Brief

**Status:** draft from the PO's answers of 2026-11-08 (Q35-Q38); waiting for approval.

An **experience chamber** (ADR-0006): no study, no Study ID, no logs. A visitor moves a sound source around the room and hears
how its direction, distance and echoes change, with or without a simulated hearing loss.

- A loudspeaker floats in the room. The visitor **grabs it** (grip, in 3D) and throws or places it anywhere, or lets it
  **fly a scripted path** (circle around the head, fly-by left to right, approach / recede) at a speed set with a slider.
- A panel picks the **room** (Dry, Room 0.7 s, Hall 2.2 s: the HW2 RT60 values), the **distance cue** (natural attenuation
  on / off) and the **hearing condition**: Normal (0 dB), Left -20 dB, Right -20 dB, Left -40 dB, Right -40 dB.
- Leave with **Back to Hall** on the panel or with **Y**.

The hearing loss is `GPL.HearingLoss` on the final output, as in C03; the room is `GPL.Acoustics`, as in C04.

Chamber ID `MovingSource`, label `C01`, folder `Chambers/MovingSource`. Docs: [Design](Design.md). No DataFormat / AnalysisPlan (no data).
