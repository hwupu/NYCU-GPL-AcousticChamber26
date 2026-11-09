# HearingLoss (C03) — Design

**Status:** PO answers of 2026-11-07 (PB-10 planning), panel redone 2026-11-08 (like the Hearing Loss Test Panel), panel v2 2026-11-10 (PB-24: no tone row, sound grid, library recordings, Config); items marked *(default)* were proposed by the agent and not objected to.

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
| Pure tones | not on the panel (PO 2026-11-10); the Config page has them (hearing loss test, below) |
| Sounds (9) | all from GPL.Stimuli (PO 2026-11-10: the HW2 clips are removed, so C03 no longer links to the HumPerHW2 folder). Generated first (ADR-0008, looped): **White noise** (Gaussian, 4 s, -26 dBFS RMS, seed 1) and **Pink noise** (20 Hz-20 kHz, 5.46 s, -26 dBFS RMS, seed 1); then the 7 library recordings (OpenAIR, CC BY-SA): Horn, Horn ×2, Choked horn, Pap horn (bicycle horns), Pistol (starter pistol), Opera voice, Trumpet (baroque fanfare). The paper stimuli of `StandardRecipes` are not on the panel (PO 2026-11-10) |
| Library level | leveled at load to **-26 dBFS RMS** over the whole recording (`StimulusClips.Leveled`; the files stay unchanged; the gain is in the console). Crest factors 17-22 dB (measured from the files), so the peaks stay at or below -4.3 dBFS. Sparse sounds (horns, pistol) sound louder while sounding than the noises at the same RMS (module README "Limits") |
| Library loop | each library recording is followed by **1 s of silence** and looped (PO 2026-11-10), so it repeats once per (length + 1 s) |
| Playback | one sound at a time, looped, from the speaker. Nothing plays at the start. Click a sound: it plays from the beginning; click it again: pause; again: resume. Another sound starts from the beginning (PO 2026-11-08: no Play button) |
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
Storybook: `Screens/C03 Hearing Loss` (docs/design/src/screens/C03HearingLoss.vue).
```
‹ C03 · Hearing Loss
Ear    [Both ears] [Left ear] [Right ear]
Loss   [0 dB] [-20 dB] [-30 dB] [-40 dB]
Sounds
[White noise] [Pink noise] [Horn]        [Horn ×2]
[Choked horn] [Pap horn]   [Pistol]      [Opera voice]
[Trumpet]
[Reset speaker]                              [⚙ Config]
```
The sounds are one `UiChoiceGroup` with 4 columns (PO 2026-11-10: wide enough for the labels; a scrolling list only when there are more than 16). No status text (PO 2026-11-10). The selected condition and level are highlighted; the sound playing is highlighted, and turns normal again when paused. **Reset speaker** puts it 2 m ahead again. ‹ goes back to the Hall.

**Config** (bottom right, `settings-horizontal` icon) pushes the page **Hearing loss test**: the view of the Sandbox Hearing Loss Test Panel (`App/HearingLossTestView`: ear, 0 to -80 dB, test tones 500 Hz-8 kHz at -20 dBFS peak for 1.5 s, in / out / expected dB per ear). Here the tones come **from the speaker** (PO 2026-11-10; the Sandbox panel keeps them in the head): a second spatialized source `Emitter/Test Tone` (same settings, not looped, so the paused sound keeps its place), tones made by GPL.Stimuli with 10 ms cos² ramps. Head direction changes each ear's "in" level; the per-ear change and expected values still match (the filters act on what reaches each ear). Opening it pauses the sound; the test sets its own loss while open; ‹ stops the tone, returns to the panel, and the chamber's ear and level apply again.

## Leaving
**Back to Hall** (panel) or **Y** → dialog "Leave Hearing Loss?" **Back to Hall / Keep going** (time and audio frozen while open).
Leaving stops the sound; the output returns to normal hearing.

## Not in this chamber
No Study ID, presets, CSV, `.session` file, Debug bench or resume (ADR-0006). No NIOSH attribution on the panel for now (PO).
No ANSI S3.44 prediction or manual bands on the panel (the module has them).
