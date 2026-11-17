# HumPerFinal (C05) — Scene setup

Menu **Magic > HumPerFinal**. Every step is idempotent and edits only `Scenes/HumPerFinal.unity`; the Camera Rig is the
Building Block that came with the copy of MainHall and is never touched.

| Step | What it does |
|---|---|
| 1 - Chamber, scene and menu | ChamberDefinition (C05; Hall cover `App/Covers/IMG-C-06.jpeg` until the PO picks another), the scene (copy of MainHall, Hall panel removed; asks to save the open scene first), `Config/default.params` (written once, never overwritten), the chamber menu with `HumPerFinalChamber`, Sync Chambers. **Re-running it rebuilds the menu canvas and its HumPerFinalChamber: run Steps 2-5 again afterwards.** |
| 2 - Homing | fixation dot, recenter listener; linked to the chamber |
| 3 - Room and lighting | the dining room (2026-11-18): `Environment/Room` (`ShoeboxRoom` 8.72 × 15 × 3.5 m) with child `Model` = `Models/Room.fbx` (URP Unlit with the baked textures, static, no shadows; seat on the origin, table ahead +z) and `Window Backdrop` (bright Unlit quad 0.3 m outside the window: the eye cameras' clear colour would show between the blind's slats), `Environment/Shoebox Acoustics` 8.72 × 15 × 3.5 m (`RoomBuilder.Shoebox`), `Environment/Prop Light` (real-time directional from the window side, no shadows); `Settings/HumPerFinal.lighting` without baked GI, flat ambient 0.55, no skybox, the old lightmaps unlinked. Sets the model and texture import settings and writes the materials to `Materials/Models/` from `Models/Models.json`. Stands the chamber menu panel upright on the table (PO 2026-11-18): `OverlayPanel.standOnBottom` on, `minBottomM` = the table top under the panel + 0.01 m (now 0.79 m), 2 m ahead, 0° tilt; otherwise the table, nearer than the panel, would cover it. Rebuilds Room, Shoebox Acoustics and Prop Light only |
| 4 - Stimuli, anchors and mixer | `Audio/HumPerFinalMixer.mixer` with the "Meta XR Audio Reflection" effect; `Stimuli/Stimulus` (no clip: white noise generated at run time, `PlacedSource` + `PulseTrain`), `Stimuli/Radio` (hidden; `Model` = `Models/Radio.fbx`), `Stimuli/Anchors` (`AnchorLoop`; `Duck` at 0° and `Trumpet` at +90°, each a room-fixed spatial source with a hidden child `Model` = `Models/Duck.fbx` / `Models/TrumpetPlayer.fbx`; `duckModelY` / `trumpetModelY` = the table top / floor heights from `Models.json`, so the models stand on their surfaces under the sounds; recordings from the GPL.Stimuli OpenAIR library, leveled at run time), `Stimuli/Sound Marker` (Debug), `Audio/Reverb` (`ReverbTuner` on the shoebox). All sources: AudioSource + `MetaXRAudioSource` (spatialized, acoustics on, gain boost 0), volume 1, flat rolloff, routed to the mixer |
| 5 - Onboarding slides | the PO's `Slides/<en\|zh>-<n>-<name>.jpg` (2000 × 936): slide 1 (`1-reposition`) as the chamber's recenter slide, slides 2-5 as `Slides/Instructions.asset`; titles and button labels from `SlideList` in the script. Rewrites the deck every run (the asset's GUID stays): to change a picture, replace the JPG and run Step 5 again |
| Validate Setup | links, anchors (sources, models, recordings Decompress On Load, room-fixed), every spatial source routed to the Reflection mixer, exactly one shoebox of the room's size, spatializer "Meta XR Audio"; the room model and the three models present with every material slot filled, the model heights set; the menu panel standing on the table top as Step 3 sets it; warns when a box placeholder is still shown or there is not exactly one directional light |

Order for a fresh scene: 1, 2, 3, 4, 5, save, Validate Setup. No lighting bake (2026-11-18: the dining room's light is in its textures).
Headless check on the mirror: `Tools/Verify-Headless.ps1 -ExecuteMethod AcousticChamber.Chambers.HumPerFinal.Editor.HumPerFinalSetup.BatchSteps`
(Steps 1-5, Validate). On macOS the same two runs by hand: mirror Assets, Packages, ProjectSettings, then Unity 6000.6.6f1
`-batchmode -nographics -executeMethod ...BatchSteps -quit` and `-runTests -testPlatform EditMode`.

## Models (2026-11-18)
`Models/` holds the dining room and the props exported from the PO's `C05.blend` (outside the repo, course folder
`Final/Assets/dining-room-kichen-baked/`) by its `scripts/step4_export_unity.py`: `Room.fbx` (room, furniture, kitchen, music
stand), `Duck.fbx`, `TrumpetPlayer.fbx` (the posed player as a static mesh, the trumpet, a contact shadow), `Radio.fbx`,
`Textures/`, and `Models.json` (each object's material slots, each material: Unlit = baked room texture, Lit = props,
Shadow = contact shadow; the duck's table-top and the player's floor heights). Every FBX carries the empties `Front` (toward the
participant) and `Up`; Steps 3-4 align the model with them (front = +z), so FBX axis settings do not matter, and remove them.
Origins: Room = the participant's seat on the floor; Duck = its bottom centre; TrumpetPlayer = the floor below the bell; Radio =
its centre. To change the layout: edit `C05.blend`, run the export into `Models/`, then Steps 3 and 4.
Own models: a `Model` child of `Stimuli/Radio` or an anchor that is neither the box nor the exported model is kept by Step 4
(its model height then stays NaN: centred on the sound).
Credits: `docs/AssetCredits.md` (Models).
Hearing loss: `OutputHearingLoss` is added to the AudioListener at run time (the listener is on the Building Block rig).
