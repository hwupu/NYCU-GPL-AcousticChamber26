# Reverberation (C04) — Scene setup

Menu **Magic > Reverberation**. Every step is idempotent and edits only `Scenes/Reverberation.unity`; the Camera Rig is the Building
Block that came with the copy of MainHall and is never touched. Shared helpers: `App/Editor/ExperienceScene.cs`.

| Step | What it does |
|---|---|
| 1 - Chamber, scene and panel | ChamberDefinition (C04, cover `App/Covers/IMG-C-03` when empty), the scene (copy of MainHall, Hall panel removed; asks to save the open scene first), `UI/Experience Panel` (overlay canvas centered ahead with `ReverberationExperience`), Global Actions, UI Pointer, Scene Fade; saves; Sync Chambers |
| 2 - Floor, mixer and room | `Environment/Floor` (30 × 30 m grid), `Audio/ReverberationMixer.mixer` (Reflection effect), `Environment/Shoebox Acoustics` (12 × 12 × 3.5 m at start, locked to the listener, clutter 0; the panel sets the size), `Audio/Reverb` (`ReverbTuner`, open space); links reverb and mixer group to the panel |
| Hot Fix - Center panel | sets the existing panel to yaw 0° (ahead), 8° down, without rebuilding it (2026-11-17) |
| Validate Setup | panel (centered), Global Actions, pointer, reverb + shoebox + mixer group linked, open space (no `ShoeboxRoom`), every source routed to the Reflection mixer, exactly one shoebox locked to the listener, spatializer "Meta XR Audio" |

Order for a fresh scene: 1, 2, save, Validate Setup. No lighting bake.
Headless check on the mirror: `Tools/Verify-Headless.ps1 -ExecuteMethod AcousticChamber.Chambers.Reverberation.Editor.ReverberationSetup.BatchSteps`.

## Headset check (PB-27)
1. Hall > C04: the panel straight ahead; Room "Open space", Profile "Experience"; no calibrated line and no warning.
2. Room presets and steppers change the profile line (sweep points and times).
3. Measure RT60 (Experience, Open space): the dialog shows the step, the bar and the time left; the result lists Target 0.7 s and
   2.2 s (within about -2 %). Close.
4. Cancel during a run: the dialog closes, no new file in `Android/data/<package>/files/Calibration/`.
5. Calibrate for a Custom size, profile Short: about the time shown; "Calibration completed" with the CSV name. Press **Use on this
   headset**: "Used on this headset". Main protocol afterwards: pull the CSV, import it in the Editor, build.
6. Y during a run: Keep going / Back to Hall; Back to Hall stops the run.
7. Headset table without a rebuild. On the Quest both chamber sizes have imported tables, which win, so check it in **Play mode**
   (no Windows tables): C04, Open space, Calibrate (any profile), Use on this headset; then C01: "RT60 calibrated on this headset
   only: import it before the study." (text2) instead of the warning. The file is `%USERPROFILE%/AppData/LocalLow/<company>/<product>/
   Calibration/Tables/RT60_Windows_12x12x3p5.csv`; delete it to go back.
