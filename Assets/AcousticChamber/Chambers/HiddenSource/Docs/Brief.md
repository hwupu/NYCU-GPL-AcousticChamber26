# HiddenSource (C02) — Brief

**Status:** draft from the PO's answers of 2026-11-08 (Q39-Q43); waiting for approval.

An **experience chamber** (ADR-0006): no study, no Study ID, no logs. Hide and seek by ear: a sound is hidden on the floor
somewhere around you. Point to it, then see how close you were.

1. A **custom chamber menu** (no number pad) with the room, distance cue and hearing condition, a **Start** and **Back to Hall**.
2. **Start**: a looping pulse train plays from a random spot on the floor, 1-6 m away, any direction (360°). The speaker is not
   shown.
3. The visitor aims with the **active controller**; its ray meets the floor and a **sphere locked to the floor** follows that
   point. It moves freely with the hand.
4. **A** confirms the guess. The true source and a line from it to the guess are shown, the sound goes on.
5. The chamber menu comes back with the **result**: azimuth error (degrees) and distance error (metres), seen from the
   participant's head. One round per Start.

Room, distance cue and hearing condition are chosen as in MovingSource and can be combined.

Chamber ID `HiddenSource`, label `C02`, folder `Chambers/HiddenSource`. Docs: [Design](Design.md). No DataFormat / AnalysisPlan (no data).
