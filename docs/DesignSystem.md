# Design System (v2, accepted 2026-11-05, PB-16 → PB-17/18)

The Storybook preview in [design/](design/README.md) is the design source; the components were rebuilt from it on 2026-11-09 ([ADR-0007](decisions/ADR-0007-uikit-rebuild.md), layout-based; earlier decisions: [ADR-0005](decisions/ADR-0005-ui-components-and-sheets.md)). Where this file and the Storybook or the [GPL.UI README](../Assets/AcousticChamber/Modules/UI/README.md) differ in sizes, paddings or component names, **the Storybook and the README win**; principles, colors, motion and navigation below still hold.

Principles: **flat, solid, quiet.** No glass, no blur, no transparency except the dim layer behind a sheet. Hierarchy comes from fill and brightness, not hue. The UI must not draw attention, and nothing in it moves while a stimulus plays.

Units: canvas units, **1 u = 1 pt**. Panels are 2 m away and the canvas scale follows the distance, so **1 u = 2 mm at 2 m ≈ 0.057°** of visual angle at any distance.

---

## 1. Panel placement and size
- 2 m in front of the head, re-placed at start and after every recenter, center **about 8° below eye level**.
- Menus 1000 x 700 u (≈ 53° x 39°); Config may grow to 1200 x 800; scrolling beyond that. Panel corner radius **32 u**.
- HUD strip (hints during practice): 1000 x 110 u, 1.05 m above eye level (≈ 28° up), black at 60 % opacity.

## 2. Font and text
- **Noto Sans TC** for both languages (static Regular and Bold), **TextMeshPro** dynamic SDF font assets. Bold for titles and prominent buttons; Regular otherwise.
- Type scale (u at 2 m): Title 56 (3.2°) · Heading 44 (2.5°) · Body 36 (2.1°) · Button 36 · Small / status 28 (1.6°) · Minimum 24 (1.4°, labels only).
- Glyphs: back `‹` (U+2039), close `×` (U+00D7), backspace `←` (U+2190); Noto Sans TC has no `⌫`, `✕`, `❮`.

## 3. Colors (scheme B: mono + white)
| Token | Value | Use |
|---|---|---|
| `surface` | `#1C1C1F` | panels, sheets |
| `raised` | `#2A2A2E` | fields (Study ID display), groups |
| `cover` | `#4A4A50` | cover image placeholder |
| `text` / `text2` / `text3` | `#FFFFFF` / `#A1A1A8` / `#5E5E66` | primary / secondary / disabled text |
| `debug` | `#E0A93B` | the DEBUG tag (text only) |
| `select` | `#4A4A50` | selected segment of a segmented control |

Button fills, **normal → hover → pressed, each brighter**:
| Style | Normal | Hover | Pressed | Text |
|---|---|---|---|---|
| prominent | `#D6D6DB` | `#E9E9ED` | `#FFFFFF` | `#141416`, bold |
| gray | `#3A3A3F` | `#48484E` | `#5A5A61` | white |
| borderless (on surface) | none | `#2E2E33` | `#3F3F45` | white |
| borderless (inside a group) | none | `#3A3A3F` | `#4A4A50` | white |
| destructive | `#BF3D37` | `#D9453F` | `#F24D46` | white, bold |

- Red keeps hue 3° and saturation 71 %; its brightness rises 75 / 85 / 95 %. Red text (destructive actions that are not the final step): `#FF6B63`.
- Disabled: 40 % opacity, no hover. Borderless buttons use `text3` for the label instead.
- No chamber accent colors or other hues; chambers are recognized by cover image and title.

## 4. Controls
### Buttons
| Style | When |
|---|---|
| **prominent** | the one main action of a screen (Start, Keep going, Resume, Make active, Run experiment trials) |
| **gray** | a secondary action that needs a visible target (Config, Practice trial) |
| **borderless** | everything else: back, cancel, number-pad keys, list actions, header actions (Revert, Delete all) |
| **destructive** | red fill on the final confirmation (Drop out, Delete) and on a row's trash icon button (Delete, PO 2026-11-09); elsewhere red text |

- Capsule (radius = height / 2). Height 80 u (small 60 u), minimum width 220 u (except keys, icon and small buttons). Labels never wrap.
- Icon buttons (back `‹`, close `×`): borderless circles, 80 u, glyph 72 u; gap to the header title 8 u.
- **Hit region:** the element's bounds, grown to at least **60 x 60 u** (invisible); ray hit tests use it.
- Pressed: instant, 97 % scale.

### Spacing and containers
- **4 u inset:** a container's padding and the gap between shapes inside it (number-pad keys, segments, Hall cards side by side). Groups pad their rows by **8 u** (`groupPadding`); rows inside stay 4 u apart.
- **Concentric corners:** inner radius = container radius − padding.
- 20 u gap between separate controls. Hall cards: 4 u apart side by side, 8 u vertically.

### Number pad (Study ID)
3 x 4 borderless keys (`1-9`, `Clear`, `0`, `←`) in a container (4 u inset); a display field (`raised`, capsule) shows the formatted ID live (`1` → `S0001`, placeholder `S____` in `text3`). In the chamber menu the pad takes 2/5 of the width and the button column 3/5.

### Switches, steppers, segmented controls, toggles
- Switch 100 x 60 u: off = gray track, `text2` knob; on = prominent track, knob `raised`. The whole row is the target; while it is hovered or pressed the off track darkens to `raised`.
- − / + steppers in a row: borderless (in-group colors), fill on hover only.
- Segmented control: `surface` track inside a group, 4 u inset, selected segment `select`.
- Toggle buttons and one-of sets (e.g. "Play stimulus", "0.7 s Noisy"): borderless; the active one has the `select` fill, hover one step lighter (`gray.pressed`) (`UiButton.Selected`). Prominent and destructive buttons have no selected state.
- **Code rule (every panel):** a one-of set is built with `UiKit.ChoiceGroup` (n equal segments; its `out` list holds the buttons) and shown with `UiKit.Select(buttons, i => chosen)`; a single toggle sets `UiButton.Selected`. Never swap style or colour by hand, never leave the chosen state unshown. Call `Select` after every change and once after building. Independent choices (ear, level) are separate sets side by side, each with its own `Select`.

### Groups and rows (Config, settings)
- Group = `raised` container (radius 36, 8 u padding, rows 4 u apart), as wide as the page content (48 u from the window edge); rows 88 u high (radius 28): label (+ secondary line), value in `text2` (edited values in `text`), trailing control or `›`. Group labels: 24 u, `text2`, above the group.
- Buttons side by side in a group (Run experiment trials · Practice trial): 8 u apart; main action prominent, others borderless.

## 5. Motion
| Token | ms | |
|---|---|---|
| hover in / out | 150 / 350 | fill settles in when the ray arrives, fades slower when it leaves |
| press / release | 0 / 250 | instant; scale and fill settle back |
| sheet / page in, out | 450 | scale 0.94 → 1 (pages 0.98), fade |
| push back | 450 | the window behind scales to 94 % (88 % two levels down) and dims to 45 % black |

- Curve **settle** = cubic-bezier(.16, 1, .3, 1): fast start, soft landing, no overshoot.
- All UI motion runs on **unscaled time** (the Stop dialog freezes `Time.timeScale`). **Reduce motion** makes every transition instant. **No UI motion while a stimulus plays.**
- No blur: an `OVROverlayCanvas` is composited by the Quest runtime and cannot be shader-blurred (ADR-0007).
- Tokens: `sheetScale` 0.94, `pageScale` 0.98, `pushScale` 0.94 per level, `dim` 45 % black.

## 6. Navigation, sheets and scrolling
### Back and close
- **‹ Back** at the top left of every pushed page (chamber menu → Hall, Config → menu, Parameters → Config, ...).
- **× Close** at the same spot on sheets that may be left without a choice (unfinished session, preset actions). Never both.
- Sheets that need a decision (Stop dialog, delete confirmations, overwrite) have **Cancel**, not ×.
- Participant screens have no ‹ / × in a header. Onboarding slides have Back / Next in their bottom bar (section 9); from the practice on the participant only moves forward (the experimenter stops with Y).

### Pages and sheets
- A panel holds a stack of pages; pages change in place (fade + 98 % scale, no slide).
- Sheets appear in place, centered, 820 u wide, `surface`, radius 32. The window behind is pushed back and dimmed; a second sheet pushes both back. Only the top sheet takes the ray. During a trial the sheet appears alone and the scene behind it dims. Main action on the right.

### Borderless scrolling
- No frame, no scrollbar. Edges fade (solid `surface` gradient, 56 u) only where more content waits; a thin indicator (8 u, `text3`) shows while scrolling and fades 0.6 s after.
- Input: **right thumbstick** scrolls the list under the ray (up to 1200 u/s, dead zone 0.2); **trigger held and dragged** scrolls (a click becomes a drag after 12 u of ray movement); momentum after release (exponential, 0.35 s time constant, stops at the ends). Inside a list a click happens on release; elsewhere on trigger down.

## 7. Screens
### Main Hall
Title **"Acoustic Chambers"**, centered, no ‹ (PO 2026-11-10). 2-column grid of cards (PO 2026-11-10; Storybook `Screens/Main Hall`), 24 u between the columns, 28 u between the rows, 8 u of room around the grid (a hovered card grows into it); two rows fit the 1000 x 700 window. A card is only its 16:9 cover, 432 x 243 u, radius 36 (the chamber's image from `App/Covers`, cropped to 16:9; solid `cover` placeholder without one). On the cover: the C-ID top left (small, bold) and the title bottom left (title size 56 u, bold, letter spacing -3 %), both 24 u in, **white at 75 %** with a soft shadow (`drop-shadow(0 0 4px rgb(0 0 0 / 0.3))`; Unity `UiKit.SoftShadow` = TMP underlay). Nothing below the cover: no description, no "N unfinished" pill (the launch routing still opens a chamber with unfinished sessions). The whole card is the target. **Hover:** the card grows to 103 % and the image saturation rises to 125 % (text unchanged), hover timing and settle curve; pressed 101.5 % (Unity: `UiButton.HoverScale` / `PressedScale`, `HallCard`, shader `GPL/UI Saturation`). The status line appears only when a chamber cannot open.

### Chamber menu
Header: ‹ (Hall), title "C04 · HumPerHW2", DEBUG tag in Debug mode. Study ID number pad; **Start** (prominent), **Config** (gray); under them in `text2`: "Audio level: 10 (target 8)" (red when off target) and, only when needed, "Preset: Custom" and "Unfinished: S0005, S0012" (one line, ends in … when long).

### Stop dialog (Y)
Real session: **Drop out** (red text) · **Keep going** (prominent); Drop out asks once more: "Drop out S0012?" Cancel · Drop out (destructive). Debug run: Back to the chamber menu (borderless) · Keep going (prominent). The session is frozen while it is open.

### Config (Debug bench)
- Pages: **Config** (Run · Playground · Presets · chamber groups · Log files) → **Parameters** (opened by the Playground row: copy of the active preset; − / +, Revert, Save as Custom) → **Presets** (Default and Custom; ✓ active; tap → sheet: Make active, Delete for Custom) → **Log files** (Delete → confirm; Delete all confirms once and names the count; one-line rows: the file name). Preset rules: ADR-0002.
- No DEBUG tag on the Config pages (the pushed pages do not set one); playback tests and Debug-only display switches are **not saved** in presets (ADR-0002).

### Mode indicator
**DEBUG** as amber text (`debug`, 24 u bold, letter-spaced) at the right of the header while the chamber is in Debug mode; nothing in real runs. The Hall has no Debug mode and no tag.

## 8. In the scene
- Laser pointer: white URP Unlit material asset (single-pass stereo).
- Fixation dot: `#262626`, 26 u (≈ 1.5°); per chamber it must contrast with the room.
- Debug markers: orange `#FF731A` (sphere 0.15 m, unlit), never seen by participants.

## 9. Language of the UI
- **System screens are English only** (Main Hall, chamber menu, Config, data manager, setup).
- **Participant screens are English / 中文** (Language screen, onboarding, instructions, HUD hints, rest and thanks). The Stop dialog (Y) always shows English and 中文 together, also in Debug runs.
- The language is a **participant choice inside the chamber session** (Language stage after Start and the handover, before onboarding), stored in the session record (`language`); a resumed session keeps it. It applies only to the participant screens of the run; the Hall, menu and Config stay English, and the menu switches back to English when a run ends. The recenter slide's Back ("← Language") reopens the choice. Onboarding order: Architecture section 8.
- **Slides:** window 1000 x 700 u = top bar 120 u (title like a page title: 40 u top padding + 80 u row) + image 1000 x 468 u (full width) + bottom bar 112 u (Back borderless at the left, Next prominent at the right, 360 u each). Buttons name where they lead: "← Recenter", "Slide 2 →" (the neighbouring slide's short title), "← Back" / "Next →" when it has none, "← Language" on the recenter slide, "Continue" without an arrow on the last instruction slide. Message screens (language choice, block break, thanks) share one layout: text centered in a 360 u box at y 80, buttons at y −180; no screen waits for A.
- **Illustrations: 2000 x 936 px** (the overlay renders the 1000 u canvas into 2048 px); keep text and key details **96 px** (48 u) from every edge, i.e. inside 1808 x 744 px; caption text at least 72 px high (36 u body), never under 48 px. Without an image the slide's caption text fills the gray area.
