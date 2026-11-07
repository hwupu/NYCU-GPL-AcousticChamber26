# Questions for the PO

Open questions only. Answered rounds 1-5 (Q1-Q34): [archive/Questions-rounds-1-2.md](archive/Questions-rounds-1-2.md).

## Open — round 6 (2026-11-07): PB-11 MovingSource, PB-12 HiddenSource
Nothing is built for these two; the answers decide whether each one is an experience (ADR-0006, like HearingLoss) or a study
(Study ID, presets, CSV, Design/DataFormat/AnalysisPlan approved first). Proposed defaults in *italics*.

| # | Question |
|---|---|
| Q35 | **MovingSource: experience or study?** Architecture: "move a source (grab / scripted) to experience spatial audio". *Experience, no logs, like HearingLoss.* |
| Q36 | MovingSource movement: (a) the visitor drags it (on the floor like HearingLoss, or freely in 3D with the grip held), (b) scripted paths (circle around the head, fly-by left to right, approach / recede), or both? *Both: grab in 3D with the grip, plus three scripted paths with a speed slider.* |
| Q37 | MovingSource room: dry floor, or a selectable room (dry / small room / reverberant hall with the HW2 RT60 table)? *Selectable: Dry, Room 0.7 s, Hall 2.2 s (same values as HW2).* |
| Q38 | MovingSource distance cue: keep the lab's flat rolloff (loudness never changes with distance), or a switch for natural distance attenuation (inverse distance)? *A switch, default natural attenuation on, since distance is part of the experience.* |
| Q39 | **HiddenSource: experience or study?** "Locate an invisible source placed at random" sounds like a localization study (error per trial). *Study: Study ID, trials, CSV.* |
| Q40 | HiddenSource response: point with the controller ray and pull the trigger (azimuth + elevation), or walk to it, or turn the head and confirm? *Ray + trigger; log azimuth and elevation error.* |
| Q41 | HiddenSource placement: azimuth range (full 360° or the front 180°), elevations (eye level only, or −30…+30°), distance (fixed 2 m?), trials and repetitions, and the sound (pulsed white noise like HW2, 2 s on / 1 s off?). *360°, 3 elevations (−20, 0, +20°), 2 m, 12 azimuths × 3 elevations × 2 reps = 72 trials, HW2 pulse train.* |
| Q42 | HiddenSource feedback: show where the source really was after each answer (learning), never, or only in practice? *Only in practice trials.* |
| Q43 | Head movement during a HiddenSource trial: free, or the head gate first and then free? *Head gate before each trial, then free.* |

When Q35-Q43 are answered I will write the Brief / Design (and, for a study, DataFormat / AnalysisPlan) for approval, then build.
