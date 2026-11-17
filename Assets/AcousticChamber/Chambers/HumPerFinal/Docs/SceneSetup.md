# HumPerFinal (C06) — Scene setup

Menu **Magic > HumPerFinal**. Every step is idempotent and edits only `Scenes/HumPerFinal.unity`; the Camera Rig is the
Building Block that came with the copy of MainHall and is never touched.

| Step | What it does |
|---|---|
| 1 - Chamber, scene and menu | ChamberDefinition (C06; Hall cover `App/Covers/IMG-C-06.jpeg` until the PO picks another), the scene (copy of MainHall, Hall panel removed; asks to save the open scene first), `Config/default.params` (written once, never overwritten), the chamber menu with `HumPerFinalChamber`, Sync Chambers. **Re-running it rebuilds the menu canvas and its HumPerFinalChamber: run Steps 2-5 again afterwards.** |
| 2 - Homing | fixation dot, recenter listener; linked to the chamber |
| 3 - Room and lighting | `Settings/HumPerFinal.lighting` and the C05 room (`RoomBuilder` defaults: 8.72 × 15 × 3.5 m, white, own materials in `Materials/`) with `Environment/Shoebox Acoustics` of the same size |
| 4 - Stimuli, anchors and mixer | `Audio/HumPerFinalMixer.mixer` with the "Meta XR Audio Reflection" effect; `Stimuli/Stimulus` (no clip: white noise generated at run time, `PlacedSource` + `PulseTrain`), `Stimuli/Radio` (box 0.25 × 0.18 × 0.12 m, URP Lit, hidden), `Stimuli/Anchors` (`AnchorLoop`; `Duck` at 0° and `Trumpet` at +90°, each a room-fixed spatial source with a hidden box child `Model`; recordings from the GPL.Stimuli OpenAIR library, leveled at run time), `Stimuli/Sound Marker` (Debug), `Audio/Reverb` (`ReverbTuner` on the shoebox). All sources: AudioSource + `MetaXRAudioSource` (spatialized, acoustics on, gain boost 0), volume 1, flat rolloff, routed to the mixer |
| 5 - Onboarding slides | the PO's `Slides/<en\|zh>-<n>-<name>.jpg` (2000 × 936): slide 1 (`1-reposition`) as the chamber's recenter slide, slides 2-5 as `Slides/Instructions.asset`; titles and button labels from `SlideList` in the script. Rewrites the deck every run (the asset's GUID stays): to change a picture, replace the JPG and run Step 5 again |
| Bake Lighting (slow) | saves the scene and bakes the lightmaps |
| Validate Setup | links, anchors (sources, models, recordings Decompress On Load, room-fixed), every spatial source routed to the Reflection mixer, exactly one shoebox of the room's size, spatializer "Meta XR Audio"; warns when the lighting is not baked |

Order for a fresh scene: 1, 2, 3, 4, 5, save, Bake Lighting, Validate Setup.
Headless check on the mirror: `Tools/Verify-Headless.ps1 -ExecuteMethod AcousticChamber.Chambers.HumPerFinal.Editor.HumPerFinalSetup.BatchSteps`
(Steps 1-5, no bake, Validate).

Real models: put the model under `Stimuli/Radio`, `Stimuli/Anchors/Duck` or `.../Trumpet` as the child named `Model` (replacing
the box; the root's +z faces the participant), centred on the parent; Step 4 only re-creates a missing `Model`.
Hearing loss: `OutputHearingLoss` is added to the AudioListener at run time (the listener is on the Building Block rig).
