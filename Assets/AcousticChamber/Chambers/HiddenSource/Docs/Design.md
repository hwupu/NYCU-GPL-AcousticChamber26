# HiddenSource (C02) — Design

**Status:** PO answers of 2026-11-08 (Q39-Q43 replaced: an experience, not a study). Items marked *(default)* are proposed by
the agent and wait for approval.

## Round
| Step | What happens |
|---|---|
| Menu | custom chamber menu (see below); the choices stay between rounds |
| Start | the head pose must be valid; the **head position and yaw at that moment** are the reference for the whole round *(default)*; the source is drawn and the pulse train starts |
| Source | on the floor (y = 0, sound from 0.3 m above it *(default)*); azimuth uniform over 0-360° relative to the reference yaw (0 = forward, negative = left); distance uniform over 1-6 m *(default)* |
| Guess | the ray of the **active controller** (the one whose trigger or ray was last used *(default)*) meets the floor; the **guess sphere** (0.25 m, URP Unlit, amber) sits at that point, locked to the floor, clamped to 0.3-8 m from the reference. A ray above the horizon drags it to the far end |
| Confirm | **A** (right controller, through a `GPL.Input` binding) locks the guess |
| Reveal | the **true sphere** (white) appears at the source; a **line** joins it to the guess sphere; the sound goes on for 3 s, the spheres and the line stay until the next Start *(default)* |
| Result | the chamber menu comes back at once with the result line (below), while the reveal stays in the room |

## Error measures
Both are measured from the reference head position, on the floor plane, using the true source *S* and the guess *G*.
| Measure | Definition |
|---|---|
| Azimuth error | signed angle from *S* to *G* around the vertical axis, -180…+180°, positive = guess is to the right of the source |
| Distance error | |G| - |S| in metres, positive = guess farther than the source |
| Also shown | true azimuth and distance, guess azimuth and distance |

Shown on the menu, e.g. "Azimuth error +12° (source -40°, guess -28°). Distance error -0.8 m (source 3.5 m, guess 2.7 m)".
Written to the console as `[Chamber:HiddenSource]`; nothing else is stored.

## Sound
| Item | Value |
|---|---|
| Stimulus | white noise `SFX-C-01` pulse train, 2 s on / 1 s off, as in HW2 *(default)*; used in place from `Chambers/HumPerHW2/Audio` |
| Spatialization | Meta XR Audio, acoustics on; same room, distance and hearing choices as MovingSource (default Dry, natural attenuation, Normal) |
| Hearing condition | two columns, ear **Both / Left / Right** and level **0 / -20 / -40 dB** (as in MovingSource; default Both + 0 dB) |

## Chamber menu (English only)
```
C02 · Hidden Source
A sound is hidden on the floor. Point to it and press A.
Room       [Dry] [Room 0.7 s] [Hall 2.2 s]
Distance   [Natural] [Flat]
Hearing    [Both] [Left] [Right]   [0 dB] [-20 dB] [-40 dB]
Last round: Azimuth error +12° ... Distance error -0.8 m ...
[Start]                                          [Back to Hall]
```
During the round the menu is hidden; the participant sees only the floor, the guess sphere and, after A, the reveal.

## Leaving
**Back to Hall** or **Y** (dialog **Back to Hall / Keep going**, frozen while open). **Y** during a round ends it without a
result and returns to the menu *(default)*.

## Not in this chamber
No Study ID, presets, CSV, `.session` file, Debug bench, resume, trial counts or feedback rules (ADR-0006).

## Moving the menu
The handle below the panel moves it, direction and distance (`GPL.UI` `UiGrabber`, all experience panels and the study chamber menus; PO 2026-11-08). Not during a study run.
