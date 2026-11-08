# HumPerHW2 (C04) — Scene setup

Menu **Magic > HumPerHW2**. Every step is idempotent and edits only `Scenes/HumPerHW2.unity`; the Camera Rig is the Building
Block that came with the copy of MainHall and is never touched.

| Step | What it does |
|---|---|
| 1 - Chamber, scene and menu | ChamberDefinition (C04), the scene (copy of MainHall, Hall panel removed; asks to save the open scene first), `Config/default.params` (written once, never overwritten), the chamber menu with `HumPerHW2Chamber`, Sync Chambers. **Re-running it rebuilds the menu canvas and its HumPerHW2Chamber: run Steps 2-4 again afterwards.** |
| 2 - Homing | fixation dot (URP Unlit), recenter listener; linked to the chamber |
| 3 - Room and lighting | lighting settings `Settings/HumPerHW2.lighting` (no directional light, flat ambient 0.7, baked GI); `Environment/Room` 8.72 × 15 × 3.5 m (white walls and ceiling, light-gray floor, two baked area lights, probes) and `Environment/Shoebox Acoustics` of the same size (GPL.Acoustics `RoomBuilder`) |
| 4 - Stimuli, background and mixer | `Audio/HumPerHW2Mixer.mixer` with the "Meta XR Audio Reflection" effect; `Stimuli/Stimulus` (SFX-C-01, Decompress On Load, `PlacedSource` + `PulseTrain`), `Stimuli/Picture` (Radio.jpg, 0.25 m wide, URP Unlit, hidden), `Stimuli/Sound Marker` (Debug, hidden); `Audio/Background` (SFX-C-02..04, looping, **Streaming**, RMS measured from the WAV files and stored, Q33) and `Audio/Reverb` (`ReverbTuner` on the shoebox). All sources: AudioSource + `MetaXRAudioSource` (spatialized, acoustics on, gain boost 0), volume 1, flat rolloff, routed to the mixer |
| 5 - Instruction slides (HW2 3-5) | fitted copies of HW2's slides 3-5 (`Slides/Source/*.jpg`, 2000 × 1400) as `Slides/<lang>-<n>-<name>.png` (2000 × 936: the empty bottom strip cut off, scaled to 936 px high, centered on the image's background colour) and `Slides/Instructions.asset`; existing PNGs and the asset are kept (delete them to regenerate) |
| Bake Lighting (slow) | saves the scene and bakes the lightmaps (minutes; editor only, Q34) |
| Validate Setup | links, clips, every spatial source routed to the Reflection mixer, exactly one shoebox of the room's size, spatializer "Meta XR Audio"; warns when the lighting is not baked |

Order for a fresh scene: 1, 2, 3, 4, 5, save, Bake Lighting, Validate Setup.
Headless check on the mirror: `Tools/Verify-Headless.ps1 -ExecuteMethod AcousticChamber.Chambers.HumPerHW2.Editor.HumPerHW2Setup.BatchSteps`
(Steps 1-5, no bake, Validate).

Placeholders in the scene: the stimulus, picture and backgrounds sit at 1.2 m height; every trial places them around the measured
head position (Design.md "Stimuli"). The RT60 meter (GPL.Acoustics `Rt60Meter`) is added to the AudioListener at run time (the
listener is on the Building Block rig).

Assets: `Audio/` (HW2 clips at -23 LUFS; preparation in `Audio/README.md`), `Images/Radio.jpg` (HW2 PB-17).
