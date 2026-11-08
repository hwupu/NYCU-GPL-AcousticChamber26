# ADR-0004: GUI screens are built by code at run time; the scene holds only the GUI host

Status: accepted (PB-05a, 2026-11-05). Component structure since 2026-11-09: [ADR-0007](ADR-0007-uikit-rebuild.md).

**Context.** HW2 built every screen in an Editor step and stored it in the scene. Here the chamber menu, Stop dialog, data manager and Config panel are the same in every chamber, and some screens are data-driven (unfinished sessions, parameters from the schema, data files).

**Decision**
- **Scene (Editor step, Magic menu):** the GUI host only: `UI` group, `UiRoot` (theme), overlay canvas(es) with `OverlayPanel` + `OVROverlayCanvas` on hidden layer 30, `UiPointer` with its laser material asset, the eye cameras' culling masks.
- **Screens (run time):** built by code with `GPL.UI` `UiKit` into the host canvas. One code path for every chamber; no per-scene copies.
- Visual tokens live in one `UiTheme` asset (`Assets/AcousticChamber/Settings/UiTheme.asset`), defaults = DesignSystem.md.
- Fonts: dynamic SDF font assets generated from the static Noto Sans TC files (`Magic > App > Step 1`); any 中文 glyph is added at run time.

**Consequences**
- Screens cannot be edited in the Inspector; changes are code changes (reviewed, versioned, EditMode-testable without a headset).
- Every object created at run time must be on the canvas layer, or the eye cameras draw it a second time (UiKit copies the parent's layer; a test checks it).
- A screen's first frame costs a few milliseconds to build; screens are small (< 100 objects).
