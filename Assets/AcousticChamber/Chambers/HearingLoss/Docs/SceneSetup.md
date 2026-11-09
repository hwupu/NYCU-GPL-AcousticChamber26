# HearingLoss (C03) — Scene setup

Menu **Magic > HearingLoss**. Every step is idempotent and edits only `Scenes/HearingLoss.unity`; the Camera Rig is the Building
Block that came with the copy of MainHall and is never touched.

| Step | What it does |
|---|---|
| 1 - Chamber, scene and panel | ChamberDefinition (C03), the scene (copy of MainHall, Hall panel removed; asks to save the open scene first), `UI/Experience Panel` (overlay canvas 35° left with `HearingLossExperience`; re-running keeps its links), Global Actions, UI Pointer, Scene Fade; saves; Sync Chambers |
| 2 - Floor, speaker and sounds | `Environment/Floor` (30 × 30 m quad, `Materials/Floor.mat` URP Lit with the generated `FloorGrid.png`), `Stimuli/Speaker` (Body, Woofer, Tweeter in URP Lit; `FloorDragSource`; child `Emitter` at 0.3 m with AudioSource + MetaXRAudioSource: spatialized, acoustics off, no mixer, volume 1, flat rolloff, loop, no clip), the three recordings from `Chambers/HumPerHW2/Audio` (used in place, not copied; their import settings belong to HumPerHW2), links to the panel; tones, white and pink noise are generated at run time (GPL.Stimuli) |
| Validate Setup | panel, Global Actions, pointer, links, four clips, the source spatialized and dry (no mixer, acoustics off, no shoebox in the scene), spatializer "Meta XR Audio" |

Order for a fresh scene: 1, 2, save, Validate Setup. No lighting bake (the MainHall directional light lights the floor and speaker).
Headless check on the mirror: `Tools/Verify-Headless.ps1 -ExecuteMethod AcousticChamber.Chambers.HearingLoss.Editor.HearingLossSetup.BatchSteps`.

At run time `OutputHearingLoss` is added to the AudioListener (on the Camera Rig) by code, so the Building Block is not edited.
