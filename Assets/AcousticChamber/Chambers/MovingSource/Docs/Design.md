# MovingSource (C01) — Design

**Status:** PO answers of 2026-11-08 (Q35-Q38 defaults accepted, hearing-loss selector added). Items marked *(default)* are
proposed by the agent and wait for approval.

## Setting
| Item | Value |
|---|---|
| Posture | standing or seated; the rig is at floor level (y = 0 is the floor) |
| Room | floor and a shoebox for the reverb as in C04 (`GPL.Acoustics`); Dry = no Reflection effect |
| Source | a small orb (0.25 m, hovering); starts 2 m ahead at 1.2 m height *(default)* |
| Panel | overlay canvas to the left like C03, 2 m away, 35° left and 8° below eye level *(default)* |

## Movement
| Item | Value |
|---|---|
| Grab | point a controller's ray at the source and press that hand's **grip**: the source stays fixed to the controller (offset kept in the controller frame), free in 3D; release = it stays (no throw physics) *(default)* |
| Limits | 0.5-10 m from the head, above the floor *(default)* |
| Scripted paths | **Circle** (around the head at the source's current height and distance), **Fly-by** (left to right, 2 m ahead, from -5 m to +5 m), **Approach / recede** (straight toward the head to 0.5 m and back to 6 m). Loop until stopped; grabbing the source stops the path |
| Speed | five steps, 0.25, 0.5, 1, 2, 4 × a base speed of 30°/s (circle) and 1.5 m/s (others); default 1 × *(default; a slider is not in GPL.UI yet)* |

## Sound
| Item | Value |
|---|---|
| Clips | the four HW2 recordings (white noise, podcasts 1 and 2, street market); click to play, click again to pause, as in C03. No pure tones *(default)*. Used in place, not copied |
| Spatialization | Meta XR Audio, acoustics on |
| Distance cue | switch: **natural** (inverse-distance attenuation) or **flat** (loudness constant, the lab rule). Default natural *(Q38)* |
| Room | **Dry**, **Room 0.7 s**, **Hall 2.2 s** (HW2 RT60 table). Default Dry *(default)* |

## Hearing condition
Two button columns in one row (PO 2026-11-08), default Both + 0 dB (normal hearing): **ear** Both / Left / Right and **level**
0 dB / -20 dB / -40 dB. Level and ear are a C03 pair (the level is the 4 kHz notch; the other ear stays 0 dB); 0 dB is normal
whatever the ear.

## Panel (English only)
```
C01 · Moving Source
Sound      [White noise] [Podcast 1] [Podcast 2] [Street market] ...
Room       [Dry] [Room 0.7 s] [Hall 2.2 s]
Distance   [Natural] [Flat]
Hearing    [Both] [Left] [Right]   [0 dB] [-20 dB] [-40 dB]
Path       [Circle] [Fly-by] [Approach]   [Stop]
Speed      [0.25 ×] [0.5 ×] [1 ×] [2 ×] [4 ×]
Point at the orb and press the grip to move it.
[Reset source]                                   [Back to Hall]
```
Chosen buttons use `Selected` (built with `UiLay.ChoiceGroup` / `UiKit.Select`).

## Leaving
**Back to Hall** or **Y** → dialog "Leave Moving Source?" **Back to Hall / Keep going** (time and audio frozen). The output
returns to normal hearing.

## Not in this chamber
No Study ID, presets, CSV, `.session` file, Debug bench or resume (ADR-0006).

## Moving the panel
The handle below the panel moves it: direction and distance (`GPL.UI` `PanelMover`; PO 2026-11-08).
