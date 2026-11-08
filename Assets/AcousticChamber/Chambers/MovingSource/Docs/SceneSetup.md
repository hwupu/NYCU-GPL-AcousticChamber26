# MovingSource (C01) — Scene setup

Menu **Magic > MovingSource**. Every step is idempotent and edits only `Scenes/MovingSource.unity`; the Camera Rig is the Building
Block that came with the copy of MainHall and is never touched. Shared helpers: `App/Editor/ExperienceScene.cs`.

| Step | What it does |
|---|---|
| 1 - Chamber, scene and panel | ChamberDefinition (C01), the scene (copy of MainHall, Hall panel removed; asks to save the open scene first), `UI/Experience Panel` (overlay canvas 35° left with `MovingSourceExperience`), Global Actions, UI Pointer, Scene Fade; saves; Sync Chambers |
| 2 - Floor, source and room | `Environment/Floor` (30 × 30 m grid), `Stimuli/Source` (an Unlit orb 0.25 m with `GrabSource`; child `Emitter`: AudioSource + MetaXRAudioSource, spatialized, acoustics on, routed to `Audio/MovingSourceMixer.mixer` with the Reflection effect, natural rolloff, loop), the four sounds from `Chambers/HumPerHW2/Audio` (used in place), `Environment/Shoebox Acoustics` (24 × 24 × 8 m, locked to the listener) and `Audio/Reverb` (`ReverbTuner`); links to the panel |
| Validate Setup | panel, Global Actions, pointer, links, four clips, spatialized source, every source routed to the Reflection mixer, exactly one shoebox, spatializer "Meta XR Audio" |

Order for a fresh scene: 1, 2, save, Validate Setup. No lighting bake.
Headless check on the mirror: `Tools/Verify-Headless.ps1 -ExecuteMethod AcousticChamber.Chambers.MovingSource.Editor.MovingSourceSetup.BatchSteps`.

## Headset check (PB-11)
1. Hall > C01: the orb hovers 2 m ahead; click a sound, it plays from the orb.
2. Point a controller at the orb and press that hand's grip: the orb follows the controller; release leaves it (no throw).
3. Circle / Fly-by / Approach move it; Speed 0.25-4 ×; Stop ends the path; grabbing also ends it.
4. Room, Distance (Natural / Flat) and Hearing (L/R -20/-40 dB: the loss is on the chosen ear only) change the sound at once.
5. The handle below the panel moves it (swing the ray = direction, hand away / back = distance). Y opens Keep going / Back to Hall.
