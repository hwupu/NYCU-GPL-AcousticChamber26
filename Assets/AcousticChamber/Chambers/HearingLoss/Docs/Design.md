# HearingLoss (C03) — Design

**Status:** PO answers of 2026-11-07 (PB-10 planning), panel redone 2026-11-08 (like the Hearing Loss Test Panel); items marked *(default)* were proposed by the agent and not objected to.

## Setting
| Item | Value |
|---|---|
| Posture | standing or seated, any direction; the rig is at floor level (y = 0 is the floor) |
| Room | dry: a 30 × 30 m floor with a 1 m grid (URP Lit), no walls, no shoebox, no Reflection mixer, the source has acoustics off |
| Speaker | a box speaker 0.30 × 0.50 × 0.26 m standing on the floor, turned to face the listener; the sound comes from 0.3 m above the floor |
| Start position | 2 m straight ahead of the head (its yaw only), on the floor, once the head pose is valid |
| Dragging | point at the speaker (a 0.32 m pick sphere) and hold the trigger: it follows the point where the ray meets the floor (keeping the offset of the grab point), stays on the floor, 0.5-6 m from the head (horizontal); a ray above the horizon drags it out to the far end *(default)*. The speaker brightens under the ray and turns amber while held |
| After a recenter | the speaker stays where it is in the room; the panel is placed again *(default)* |
| Panel | overlay canvas 1100 × 900 u (set at run time), 2 m away, 35° left of straight ahead and 8° below eye level, so it does not cover the speaker *(default)* |

## Sound
| Item | Value |
|---|---|
| Pure tones | 500 Hz, 1, 2, 4, 8 kHz sines made at run time (1 s loop of whole cycles, mono, peak -20 dBFS = RMS -23 dBFS), played from the speaker like the recordings (PO 2026-11-08) |
| Recordings | `SFX-C-01` white noise, `SFX-C-02` podcast 1, `SFX-C-03` podcast 2, `SFX-C-04` street market (HW2 clips, mono, -23 LUFS; PO: "can use the audio from HW2 for now"). Used in place from `Chambers/HumPerHW2/Audio`, not copied (PO: the audio will likely change); a temporary asset link between chambers, no code link |
| Playback | one sound at a time, looped, from the speaker. Nothing plays at the start. Click a tone or recording: it plays from the beginning; click it again: pause; again: resume. Another sound starts from the beginning (PO 2026-11-08: no Play button) |
| Spatialization | Meta XR Audio (AudioSource + MetaXRAudioSource, spatialized, acoustics off), volume 1, flat rolloff (no distance attenuation, the lab rule), gain boost 0; loudness = headset volume *(default)* |

## Hearing loss
| Item | Value |
|---|---|
| Where | `GPL.HearingLoss` `OutputHearingLoss` on the AudioListener: the final left/right output, after the spatializer |
| Levels | 0 dB (None, no adjustment), -20 dB = Mild, -30 dB = Moderate, -40 dB = Severe (HLSim presets; the label is the deepest band, at 4 kHz; sources and the real cut at each tone: `docs/references/HearingLossLevels.md`) |
| Bands (dB) | 125, 250, 500, 1k, 1.5k, 2k, 3k, 4k, 6k, 8k Hz — Mild 0, 0, 0, 0, -5, -10, -10, -20, -10, 0; Moderate 0, 0, -10, -10, -15, -20, -20, -30, -20, -10; Severe 0, 0, -10, -20, -25, -30, -30, -40, -30, -20 |
| Hearing condition | Both ears / Left ear / Right ear (the loss is on these); the other ear stays at 0 dB, no adjustment (Right + -20 dB = left 0 dB, right Mild). Choosing an ear while at 0 dB sets -20 dB (PO 2026-11-08); 0 dB is normal hearing |
| Filters | 10 peaking biquads per ear, Q 24 at 125 Hz and 12 elsewhere (the original's effective values), instant changes |
| Start | Both ears selected, 0 dB (normal hearing) |

## Panel (English only)
```
C03 · Hearing Loss
Hearing condition (loss on)
[Both ears] [Left ear] [Right ear]
Hearing loss
[0 dB] [-20 dB] [-30 dB] [-40 dB]
Pure tone
[500 Hz] [1 kHz] [2 kHz] [4 kHz] [8 kHz]
Audio recording
[White noise] [Podcast 1] [Podcast 2] [Street market]
Left ear 0 dB, right ear -20 dB (at 4 kHz).  Playing 4 kHz.
Point at the speaker and hold the trigger to move it.
[Reset speaker]                                  [Back to Hall]
```
The selected condition and level are prominent (white) buttons; the sound playing is white, and turns gray again when paused. **Reset speaker** puts it 2 m ahead again.

## Leaving
**Back to Hall** (panel) or **Y** → dialog "Leave Hearing Loss?" **Back to Hall / Keep going** (time and audio frozen while open).
Leaving stops the sound; the output returns to normal hearing.

## Not in this chamber
No Study ID, presets, CSV, `.session` file, Debug bench or resume (ADR-0006). No NIOSH attribution on the panel for now (PO).
No ANSI S3.44 prediction or manual bands on the panel (the module has them).
