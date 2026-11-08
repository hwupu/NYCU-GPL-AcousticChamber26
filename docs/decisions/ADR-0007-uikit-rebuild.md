# ADR-0007: UiKit rebuild: Storybook design, layout-based components (2026-11-09)

Status: **accepted** (PO: "big bang", PoC approved 2026-11-09). Amends ADR-0005 (components and sheets).

**Context.** The v2 components were builders with hand-computed positions (`UiKit.Button(parent, ..., pos, size)`, `Row(group, index, ...)`). Every spacing or text change meant new arithmetic in each screen, rows could not grow with their text, and the design existed only in prose. The PO asked for the components to be designed in a Storybook preview first (Vue + Tailwind, `docs/design`), then ported to Unity.

**Decision**
- **The design source is the Storybook preview** (tokens in `theme.css`, components in `src/components`); numbers (80 u buttons, 88 u rows, 116 u navigation bar, 48 u side padding, 16 u grabber gap ...) are measured from there.
- **Unity components are layouts**: uGUI `HorizontalLayoutGroup` / `VerticalLayoutGroup` / `LayoutElement`, built by code (`UiKit.Button`, `UiKit.Row`, ..., `UiPanel`, `UiSurface`, `UiPage`), so sizes come from paddings, minimum heights and flexible widths. A sliced `Image` on a layout node adds its slice size (130 u) to the preferred size: backgrounds are a child image the layout ignores.
- **Structure:** `UiPanel` (transparent root: show, push back, placement) > `UiSurface` (rounded rectangle, a stack of `UiPage`s) > page slots (navigation bar, body, status, toolbar). Dialogs and sheets are modal panels that push the panels behind back (`UiPanel.Present` / `Close` / `Stack`).
- **Naming:** Unity follows the Storybook. `Runtime/UiKit/` has one file per Storybook component, same name (list: [design README](../design/README.md)); factories are the static partial class `UiKit`. `UiKit.Select` shows the chosen button of a segmented group. `NumberPadEntry` -> `UiNumberPad`, `PanelMover` -> `UiGrabber`, `Column` -> `UiColumn` (a Config-page helper, no Storybook counterpart).
- **Icons** are sprites: Storybook SVGs exported as white 128 px PNGs by `pnpm icons`, tinted at run time (`UiIcon`, `UiIconSet`). The Vector Graphics package does not compile on Unity 6000.6.
- **Grabber** (move handle): a rounded rectangle, a real 3D object outside the canvas (the overlay clips everything else), 120 x 8 u at rest, 220 x 16 u on hover, 16 u below the surface; follows the panel's visibility. The panel turns around the head at a fixed distance; a distance move was removed (the canvas scale follows the distance, so it showed no difference).
- **Callout:** a world-space annotation (not in an overlay canvas), delayed fade-in, `Dismiss` on interaction, no periodic animation.
- **No blur** of pushed-back panels: an `OVROverlayCanvas` is composited by the Quest runtime and cannot be shader-blurred. They scale to 94 % and dim.
- **Slider** stays in the Storybook only.
- **Big bang:** the old absolute-position builders (`Window`, `Sheet`, `PageStack`, `UiLayer`, `UiShowcase`, ...) were deleted once every panel (Hall, chamber menu, Config, experience chambers, protocol screens, test panels, dialogs) was migrated.

**Consequences**
- Tests measure the laid-out rectangles (`UiKitLayoutTests`, `UiKitInputsTests`, `UiKitPagesTests`, App `LayoutTests`).
- Not verifiable without a headset: icon sharpness at 40 / 32 u, real Noto font metrics (tests use the default font), grabber feel, floor clearance, overlay rendering of the callout.
- Experience panels grew to 1100 x 980 u (MovingSource): rows are now labeled and stacked from the layout.
