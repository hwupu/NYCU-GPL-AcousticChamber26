# ADR-0005: UI components, motion, sheets and design system v2 (2026-11-05)

Status: **accepted** (PO, after four preview rounds of `docs/design/components.html`).

**Context.** PB-05a built the GUI from builder functions (`UiKit.Button`) with Unity's stock color tint and an accent outline ring. The PO asked for a small UI framework: shared components whose look changes in one place, visionOS-like sheets that push the window behind back, borderless buttons with timed states, and a quieter palette suited to perception studies.

**Decision**
- **Components are code, not prefabs** (continues ADR-0004). A `UiButton` component owns its states (normal, hover, pressed, disabled), timing and style; styles are tokens in `UiTheme` (colors per state, geometry, motion). Changing a token restyles every use, also at run time. Prefabs were rejected: hard to diff and merge, and runtime content must stay on the hidden overlay layer. UI Toolkit was rejected: it cannot draw through `OVROverlayCanvas` (sharp text on Quest).
- **Design system v2** (`docs/DesignSystem.md`): scheme B (mono + white; red only for destructive), hierarchy by fill (prominent / gray / borderless / destructive), states brighten toward pressed, capsule buttons, 60 x 60 u minimum hit regions, 4 u container inset with concentric corners, settle motion (150 / 350 / 250 / 450 ms), reduce-motion switch, no UI motion while a stimulus plays.
- **Navigation:** ‹ back and × close share the top-left spot; pages change in place; **sheets** appear in place (scale and fade; no slide, no blur) and push the window behind back and dim it; decision sheets have Cancel instead of ×.
- **Borderless scrolling** for long lists (Main Hall grid, log files, presets): edge fades, transient indicator, thumbstick and trigger-drag scrolling.
- **Debug bench** gets a preset manager (Playground, Revert, Save as Custom, Presets with Make active / Delete), the UI for `GPL.Params` per ADR-0002.

**Consequences**
- PB-17 replaces the builders' internals with components; existing screens (test panels, Stop dialog, number pad) move to v2 without changing their public API where possible.
- All UI animation runs on unscaled time; the overlay canvas redraws while something animates.
- The transition blur was dropped on Quest (an extra render pass per frame); it can be tried later.
- `UiStyle` changes from Primary / Neutral / Warning / Danger to Prominent / Gray / Borderless / Destructive.

Amended 2026-11-05 (PB-18: no blur; PB-19b: Default + Custom presets per ADR-0002).
