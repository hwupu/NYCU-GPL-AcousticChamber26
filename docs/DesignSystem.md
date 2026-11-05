# Design System (accepted 2026-11-05, PB-16)

Accepted by the PO on 2026-11-05 (all proposals). The HW2 values these replace are in git history (commit `2a2cac6` + 1, file history of this document) and in `../115A-535655-HumPer-HW2/Assets/Editor/HumPerSetup.cs`. Colors are previewed in [design/preview.html](design/preview.html) (left: HW2, right: this design).

Units: GUI sizes are canvas units. HW2 scale: 1 unit = 0.002 m, panel 2 m away, so **1 unit ≈ 0.057°** of visual angle (44 units ≈ 2.5°).

---

## 1. Panel placement and size
### Distance and height
- 2 m in front of the head, re-placed at start and after every recenter, center **about 8° below eye level** (looking slightly down is more relaxed for long menus). Check in the headset.

### Sizes defined as angles
- sizes are defined at 2 m as in HW2, but the canvas scale follows the distance, so moving a panel closer or farther keeps the same visual angle.

### Panel size
- Menus 1000 x 700 units (2.0 x 1.4 m at 2 m ≈ 53° x 39°); Debug panel may grow to 1200 x 800 (more controls), scrolling beyond that.

### HUD strip (hints during practice)
- 1000 x 110 units, 1.05 m above eye level (≈ 28° up), black at 60 % opacity.

## 2. Font and text
### Font family
- **one font for both languages: Noto Sans TC** (its Latin letters match its Chinese), static Regular and Bold files instead of the variable font (TextMeshPro handles static weights more reliably).

### Text renderer
- **TextMeshPro** (SDF, sharp at any distance), dynamic font asset so any Chinese character works without pre-baking.

### Type scale (units at 2 m; approximate angle)
- Title 56 (3.2°) · Heading 44 (2.5°) · Body 36 (2.1°) · Button 36 · Small/status 28 (1.6°) · **Minimum 24 (1.4°), Debug only**.

### Weights
- Bold for titles and primary buttons, Regular for everything else.

### Text color
- primary text `#FFFFFF`, secondary text `#B8BECC` (status lines, descriptions), disabled `#6B7385`.

## 3. Colors
### Surfaces (dark panels against bright rooms)
- `surface` `#171A24` (panels), `surface-raised` `#292E3D` (fields), `neutral` `#384052` (neutral buttons, keys).

### Semantic colors (button meaning)
- one color per meaning:
  - `primary` (go, confirm, start) `#216B3D`
  - `neutral` (back, secondary choices) `#384052`
  - `warning` (in progress, debug actions, measurements) `#8A6A1F`
  - `danger` (drop out, delete) `#A62E2E`
  - `accent` (focus, selected, hovered outline) `#3D7BD9`

### Hover / pressed / disabled
- hover = color mixed 35 % toward white plus a 4-unit `accent` outline; pressed = 60 % toward white; disabled = 40 % opacity.

### Chamber accent
- each chamber has one accent color (in its `ChamberDefinition`), used on its Hall card and the title bar of its menu, so you always see which chamber you are in.

### Mode indicator
- a permanent **DEBUG** badge (`warning` color) in the top-right corner of every panel and of the HUD while in Debug mode; nothing in real runs.

## 4. Controls
### Buttons
- minimum height 80 (Debug 70), minimum width 220, 20 units gap; **rounded corners 12 units** (9-slice sprite).

### Number pad (Study ID)
- phone layout 3 x 4 (`1-9`, `Clear`, `0`, `⌫`), max 4 digits, display shows the formatted ID live: typing `1` shows `S0001`.

### Language buttons
- `中文` and `English`.

### Onboarding slides
- picture 2000 x 1400 with a 200 px empty strip at the bottom for the buttons (260 x 80, 50 padding); button labels name the slide they lead to (`← Procedure`, `Controllers →`). Template for every chamber's slides.

## 5. Dialogs
### Stop dialog (Y)
- `surface` box with a `danger` top bar, in the upper part of the panel; two buttons: **Keep going** (`primary`) and **Drop out** (`danger`, asks once more: "Drop out S0012?" Cancel / Drop out). Debug run: `warning` top bar, **Keep going** / **Back to the chamber menu**. The session is frozen while it is open.

### Delete in the data manager
- `danger` buttons; "Delete" confirms once; "Delete all" names the count and confirms twice.

## 6. In the scene
### Laser pointer
- white, turns `accent` while it hits a button.

### Fixation dot
- `#262626`, 26 units (≈ 1.5°); per chamber, the dot color must contrast with the room (dark on light rooms, light on dark/outdoor).

### Debug markers
- orange `#FF731A` (sphere 0.15 m, unlit) for all Debug-only markers (same family as the DEBUG badge), so anything orange in the scene is never seen by participants.

## 7. Language of the UI
- experimenter screens in English; participant screens in English / 中文.
