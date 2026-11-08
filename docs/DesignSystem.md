# Design System (v2, accepted 2026-11-05, PB-16 → PB-17/18)

v1 (PB-16) was accepted earlier on 2026-11-05; v2 replaces its colors, button styles and dialogs after four preview rounds with the PO. The HTML reference is now the Storybook preview in [design/](design/README.md) (rebuilt 2026-11-08 on the `uikit-rebuild` branch; the old components.html / preview.html are in git history). Decision record: [ADR-0005](decisions/ADR-0005-ui-components-and-sheets.md).

**2026-11-09: the components were rebuilt** (layout-based, designed in the Storybook preview; [ADR-0007](decisions/ADR-0007-uikit-rebuild.md)). Where this file and the Storybook (`design/`) differ in sizes, paddings or component names, the Storybook and the module README (`Assets/AcousticChamber/Modules/UI/README.md`) win; the principles, colors, motion and navigation rules below still hold. Sections 4 and 6 name the old builders (`UiKit.Row`, `Sheet`, `PageStack`): read them as `UiLay.Row`, `UiDialog` / `UiSheet`, `UiSurface` pages.

Principles: **flat, solid, quiet.** No glass, no blur at rest, no transparency except the dim layer behind a sheet. Hierarchy comes from fill and brightness, not hue. The study is about perception: the UI must not draw attention, and nothing in the UI moves while a stimulus plays.

Units: canvas units, **1 u = 1 pt**. Panels are 2 m away and the canvas scale follows the distance, so **1 u = 2 mm at 2 m ≈ 0.057°** of visual angle at any distance.

---

## 1. Panel placement and size
- 2 m in front of the head, re-placed at start and after every recenter, center **about 8° below eye level**.
- Sizes are defined at 2 m; the canvas scale follows the distance, so a closer or farther panel keeps the same visual angle.
- Menus 1000 x 700 u (≈ 53° x 39°); Debug may grow to 1200 x 800; scrolling beyond that.
- Panel corner radius **32 u**.
- HUD strip (hints during practice): 1000 x 110 u, 1.05 m above eye level (≈ 28° up), black at 60 % opacity.

## 2. Font and text
- **Noto Sans TC** for both languages (static Regular and Bold), **TextMeshPro** dynamic SDF font assets.
- Type scale (u at 2 m): Title 56 (3.2°) · Heading 44 (2.5°) · Body 36 (2.1°) · Button 36 · Small / status 28 (1.6°) · Minimum 24 (1.4°, Debug and labels only).
- Bold for titles and prominent buttons; Regular otherwise.
- Glyphs: back `‹` (U+2039), close `×` (U+00D7), backspace `←` (U+2190); Noto Sans TC has no `⌫`, `✕`, `❮`.

## 3. Colors (scheme B: mono + white)
| Token | Value | Use |
|---|---|---|
| `surface` | `#1C1C1F` | panels, sheets |
| `raised` | `#2A2A2E` | fields (Study ID display), groups |
| `cover` | `#4A4A50` | cover image placeholder |
| `text` / `text2` / `text3` | `#FFFFFF` / `#A1A1A8` / `#5E5E66` | primary / secondary / disabled text |
| `debug` | `#E0A93B` | the DEBUG tag (text only) |
| `select` | `#4A4A50` | the selected segment of a segmented control |

Button fills, **normal → hover → pressed, each brighter; pressed is the brightest**:
| Style | Normal | Hover | Pressed | Text |
|---|---|---|---|---|
| prominent | `#D6D6DB` | `#E9E9ED` | `#FFFFFF` | `#141416`, bold |
| gray | `#3A3A3F` | `#48484E` | `#5A5A61` | white |
| borderless (on surface) | none | `#2E2E33` | `#3F3F45` | white |
| borderless (inside a group) | none | `#3A3A3F` | `#4A4A50` | white |
| destructive | `#BF3D37` | `#D9453F` | `#F24D46` | white, bold |

- Red keeps hue 3° and saturation 71 %; only its brightness (HSV value) rises: 75 / 85 / 95 %.
- Red text (destructive actions that are not the final step): `#FF6B63`.
- Disabled: 40 % opacity, no hover. Borderless buttons (no fill) use `text3` for the label instead: faded white reads like a plain label.
- No chamber accent colors and no other hues; chambers are recognized by their cover image and title.

## 4. Controls
### Buttons
| Style | When |
|---|---|
| **prominent** | the one main action of a screen (Start, Keep going, Resume, Make active, Run experiment trials) |
| **gray** | a secondary action that needs a visible target (Debug, Practice trial) |
| **borderless** | everything else: back, cancel, number-pad keys, list actions, header actions (Revert, Delete all) |
| **destructive** | red fill only on the final confirmation (Drop out, Delete); elsewhere red text |

- Capsule shape (radius = height / 2). Height 80 u (small 60 u), minimum width 220 u (except keys, icon and small buttons). Labels never wrap.
- Icon buttons (back `‹`, close `×`): **borderless** circles, 80 u, glyph 72 u (the fill appears on hover); the glyph is centered on its outline (TMP geometry alignment). The gap between ‹ / × and the header title is **8 u** (the title is not interactive, so the 20 u control gap does not apply).
- **Hit region:** the element's bounds, grown to at least **60 x 60 u** (invisible). Ray hit tests use the hit region.
- Pressed: instant, 97 % scale.

### Spacing and containers
- **4 u inset:** a container's padding to the shapes inside it, and the gap between those shapes (number-pad keys, segments, Hall cards side by side). Exception: groups pad their rows by **8 u** (`groupPadding`, PO 2026-11-05); rows inside stay 4 u apart.
- **Concentric corners:** inner radius = container radius − padding (4, or 8 in groups).
- 20 u gap between separate controls. Main Hall cards: 4 u apart side by side, **8 u apart vertically**.

### Number pad (Study ID)
- 3 x 4 borderless keys (`1-9`, `Clear`, `0`, `←`) inside a container (4 u inset); display field (`raised`, capsule) shows the formatted ID live: `1` → `S0001`; placeholder `S____` in `text3`.

### Switches and segmented controls
- Switch 100 x 60 u: off = gray track, `text2` knob; on = prominent track, knob in the group color (`raised`, softer than black). The whole row is the target. While the row is hovered or pressed, the off track darkens to `raised` (the hovered row's fill is the track's own gray).
- − / + steppers in a parameter row: borderless (in-group colors), fill on hover only, like every other control inside a group.
- Segmented control: `surface` track inside a group, 4 u inset, selected segment `select`.
- Toggle buttons and one-of button sets (PB-08e, e.g. Debug "Play stimulus", "0.7 s Noisy"): borderless buttons; the active one
  (playing, chosen) has the `select` fill, hover a step lighter (`gray.pressed`); `UiButton.Selected`. Prominent and destructive
  buttons have no selected state.
- **Code rule (every panel, test, debug and experience screen):** a one-of set is built with `UiLay.ChoiceGroup` (n equal segments
  on a track; its `out` list holds the buttons) and shown with `UiKit.Select(buttons, i => chosen)`; a single toggle sets `UiButton.Selected`. Never swap a button's style
  or colour by hand, never leave the chosen state unshown. Call `Select` after every change and once after building the panel.
  Several independent choices (ear, level) are separate sets side by side, each with its own `Select`.

### Groups and rows (Debug, settings)
- Group = `raised` container (radius 36, 8 u padding to the rows, rows 4 u apart), as wide as the page content (aligned with the header edges, 48 u from the window edge); rows 88 u high (radius 28): label (+ secondary line), value in `text2` (edited values in `text`), trailing control or `›`.
- Group labels: 24 u, `text2`, above the group.
- Buttons side by side in a group (Run experiment trials · Practice trial): 8 u apart; the main action prominent, the others borderless (fill on hover only).

## 5. Motion
| Token | ms | |
|---|---|---|
| hover in | 150 | fill settles in when the ray arrives |
| hover out | 350 | slower fade when the ray leaves |
| press | 0 | instant |
| release | 250 | scale and fill settle back |
| sheet / page in, out | 450 | scale 0.94 → 1, fade in (reverse to leave); no blur, see below |
| push back | 450 | the window behind scales to 94 % (88 % two levels down) and dims to 45 % black |

- Curve **settle** = cubic-bezier(.16, 1, .3, 1): fast start, long soft landing, no overshoot.
- All UI motion runs on **unscaled time** (the Stop dialog freezes `Time.timeScale`).
- **Reduce motion** (setting) makes every transition instant.
- **No UI motion while a stimulus plays.**
- The blur is a transition effect only. PB-18 ships sheets with scale and fade only (no blur: the overlay canvas would need an extra render pass per frame on Quest); a blur can be tried later if the PO misses it.
- Pages: `pageScale` 0.98, `page` 450 ms; no blur.
- Tokens: `sheetScale` 0.94 (a sheet appears from / leaves to), `pushScale` 0.94 per level behind, `dim` 45 % black.

## 6. Navigation, sheets and scrolling
### Back and close
- **‹ Back** at the top left of every pushed page (chamber menu → Hall, Debug → menu, Parameters → Debug, ...).
- **× Close** at the same spot on sheets that may be left without a choice (unfinished session, preset actions). Back and close never appear together.
- Sheets that need a decision (Stop dialog, delete confirmations, overwrite) have **Cancel**, not ×.
- Participant screens have no ‹ / × in a header. Onboarding slides (recenter, instructions) have Back / Next in their bottom bar (section 9); from the practice on the participant only moves forward (the experimenter stops with Y).

### Pages
- A panel holds a stack of pages; pages change in place (fade + 98 % scale, no blur, no slide).

### Sheets
- Appear in place, centered, 820 u wide, `surface`, radius 32: no slide. The window behind is pushed back and dimmed; a second sheet pushes both back. Only the top sheet takes the ray.
- During a trial no window is shown: the sheet appears alone and the scene behind it dims.
- Button order: the main action on the right.

### Borderless scrolling
- No frame and no scrollbar. The edges fade (solid `surface` gradient, 56 u) only where more content waits; a thin indicator (8 u, `text3`) shows while scrolling and fades 0.6 s after.
- Input: **right thumbstick** scrolls the list under the ray; **trigger held and dragged** scrolls (a click becomes a drag after 12 u of ray movement, so short clicks still select); a little momentum after release (exponential slow-down, 0.35 s time constant; it stops at the ends, no rubber band). Inside a list a click happens on release; elsewhere on trigger down. Thumbstick: up to 1200 u/s, dead zone 0.2.

## 7. Screens
### Main Hall
- 2-column grid of cards: a 16:9 cover (solid `cover` placeholder until a chamber has an image), the C-ID and an "N unfinished" pill on the cover, the title below; no description. The whole card is the target; the hover fill shows 4 u around the cover (card radius 40, cover 36).

### Chamber menu
- Header: ‹ (Hall), title "C04 · HumPerHW2", DEBUG tag in Debug mode. Study ID number pad; **Start** (prominent), **Debug** (gray); under them, in `text2`: "Audio level: 10 (target 8)" (red when off target), and only when needed "Preset: Custom", "Unfinished: S0005, S0012" (one line, ends in … when long).

### Stop dialog (Y)
- Real session: **Drop out** (red text) · **Keep going** (prominent); Drop out asks once more: "Drop out S0012?" Cancel · Drop out (destructive). Debug run: Back to the chamber menu (borderless) · Keep going (prominent). The session is frozen while it is open.

### Debug bench
- Pages: **Debug** (Run · Playground · Presets · chamber groups · Log files) → **Parameters** (opened by the Playground row: a copy of the active preset: − / +, Revert to the active preset, Save as Custom) → **Presets** (Default and Custom; ✓ active; tap → sheet: Make active, Delete for Custom) → **Log files** (Delete → confirm; Delete all confirms once and names the count; one-line rows: the file name, no description). Preset rules: ADR-0002.
- No DEBUG tag on the Debug pages. Playback tests (play stimulus, play background) and Debug-only display switches are **not saved** in presets (ADR-0002).

### Mode indicator
- **DEBUG** as amber text (`debug`, 24 u bold, letter-spaced) at the right of the header of the chamber menu while the chamber is in Debug mode; nothing in real runs. The Hall has no Debug mode and no tag (PO 2026-11-06).

## 8. In the scene
- Laser pointer: white URP Unlit material asset (single-pass stereo).
- Fixation dot: `#262626`, 26 u (≈ 1.5°); per chamber it must contrast with the room.
- Debug markers: orange `#FF731A` (sphere 0.15 m, unlit), never seen by participants.

## 9. Language of the UI
- **System screens are English only** (Main Hall, chamber menu, Debug, data manager, setup): they are used by us.
- **Participant screens are English / 中文** (Language screen, onboarding, instructions, HUD hints, rest and thanks). The Stop dialog (Y) always shows English and 中文 together, whatever the participant chose, also in Debug runs.
- The language is a **participant choice inside the chamber session** (Language stage after Start and the handover, before onboarding), stored in the session record (`language`); a resumed session keeps it. It applies only to the participant screens of the run (onboarding slides, cues, block breaks, thanks): the Hall, the chamber menu and Debug stay English, and the menu switches back to English when a run ends (PO 2026-11-07). The recenter slide's Back ("← Language") reopens the choice. Onboarding order: Architecture section 8.
- **Slides** (PB-20): window 1000 x 700 u = top bar 120 u (title placed like a page title: 40 u top padding + 80 u row, PO 2026-11-07) + image 1000 x 468 u (full width) + bottom bar 112 u (Back borderless at the left, Next prominent at the right, 360 u each; they name where they lead: "← Recenter", "Slide 2 →" from the neighbouring slide's short title, "← Back" / "Next →" when it has none, "← Language" on the recenter slide, "Continue" without an arrow on the last instruction slide). Message screens (language choice, block break, thanks) share one layout: text centered in a 360 u box at y 80, buttons at y −180; no screen waits for A. Illustrations: **2000 x 936 px** (the overlay renders the 1000 u canvas into 2048 px, about 2 px per u); keep text and key details **96 px** (48 u) from every edge, i.e. inside 1808 x 744 px; caption text at least **72 px** high (36 u body), never under 48 px (24 u). Without an image the slide's caption text fills the gray area.
