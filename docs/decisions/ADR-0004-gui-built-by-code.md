# ADR-0004: GUI screens are built by code at run time; the scene holds only the GUI host

- Status: accepted by the engineer (PB-05a, 2026-11-05); PO may object at the headset test
- Context: HW2 built every screen in an Editor step (`HumPerSetup` Step 3) and stored it in the scene. AcousticChamber26 has
  the same chamber menu, Stop dialog, data manager and Debug parameter panel in every chamber, and some screens are
  data-driven (unfinished-session list, parameters generated from the schema, data file list).

## Decision
- **Scene (Editor step, Magic menu):** the GUI host only: `UI` group, `UiRoot` (theme), overlay canvas(es) with
  `OverlayPanel` + `OVROverlayCanvas` on hidden layer 30, `UiPointer` with its laser material asset, the eye cameras'
  culling masks. These are scene setup and follow the rule "scene setup by Editor scripts, never at run time".
- **Screens (run time):** built by code with `GPL.UI.UiKit` into the host canvas (`Background`, `Text`, `Button`,
  `NumberPadEntry`, `ModalDialog`, ...). One code path for every chamber; no per-scene copies to keep in sync.
- Visual tokens live in one `UiTheme` asset (`Assets/AcousticChamber/Settings/UiTheme.asset`), defaults = DesignSystem.md.
- Fonts: dynamic SDF font assets generated from the static Noto Sans TC files (`Magic > App > Step 1`); any 中文 glyph
  is added at run time.

## Consequences
- Screens cannot be edited in the Inspector; changes are code changes (reviewed, versioned, unit-testable in EditMode:
  `GPL.UI.Tests` builds the test panel and checks layers, number pad formatting and dialog modality without a headset).
- Every object created at run time must be on the canvas layer, otherwise the eye cameras draw it a second time
  (UiKit copies the parent's layer; a test checks it).
- First frame of a screen costs a few milliseconds to build; screens are small (< 100 objects).
