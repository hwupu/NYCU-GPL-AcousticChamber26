# HiddenSource (C02) — Scene setup

Menu **Magic > HiddenSource**. Every step is idempotent and edits only `Scenes/HiddenSource.unity`; the Camera Rig is the Building
Block that came with the copy of MainHall and is never touched. Shared helpers: `App/Editor/ExperienceScene.cs`.

| Step | What it does |
|---|---|
| 1 - Chamber, scene and panel | ChamberDefinition (C02), the scene (copy of MainHall, Hall panel removed; asks to save the open scene first), `UI/Experience Panel` (overlay canvas, straight ahead, with `HiddenSourceExperience`), Global Actions, UI Pointer, Scene Fade; saves; Sync Chambers |
| 2 - Floor, source and room | `Environment/Floor` (30 × 30 m grid), `Stimuli/Source` (child `Emitter` at 0.3 m: AudioSource + MetaXRAudioSource + `PulseTrain`, white noise `SFX-C-01` from `Chambers/HumPerHW2/Audio`, spatialized, acoustics on, routed to `Audio/HiddenSourceMixer.mixer` with the Reflection effect, natural rolloff), `True Sphere`, `Guess Sphere` (URP Unlit, hidden), `Reveal Line` (LineRenderer), `Environment/Shoebox Acoustics` (20 × 20 × 6 m, locked to the listener) and `Audio/Reverb` (`ReverbTuner`); links to the panel |
| Validate Setup | panel, Global Actions, pointer, links, clip, spatialized source, every source routed to the Reflection mixer, exactly one shoebox, spatializer "Meta XR Audio" |

Order for a fresh scene: 1, 2, save, Validate Setup. No lighting bake.
Headless check on the mirror: `Tools/Verify-Headless.ps1 -ExecuteMethod AcousticChamber.Chambers.HiddenSource.Editor.HiddenSourceSetup.BatchSteps`.

## Headset check (PB-12)
1. Hall > C02 opens the menu; the handle below the panel turns it around the head (trigger held, swing the ray).
2. Start: the menu hides, pulses play from the floor, the laser shows, the amber sphere follows the ray on the floor (clamped 0.3-8 m).
3. A: the true white sphere and a line to the guess appear; the menu returns with "Azimuth error ... Distance error ...".
4. Room Dry / Room 0.7 s / Hall 2.2 s is audible (echo); Natural is quieter far away than Flat; Left -40 dB dulls the left ear.
5. Y during a round returns to the menu without a result; Y in the menu opens Keep going / Back to Hall.
6. Check the sign: a guess to the right of the source gives a positive azimuth error.
