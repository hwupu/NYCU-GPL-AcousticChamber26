# ADR-0007: UiKit rebuild: Storybook design, layout-based components (2026-11-09)

Status: **accepted** (PO: strategy "big bang", PoC approved 2026-11-09). Amends ADR-0005 (components and sheets).

**Context.** The v2 components were builders with hand-computed positions (`UiKit.Button(parent, ..., pos, size)`, `Row(group, index, ...)`). Every change of spacing or text meant new arithmetic in each screen, rows could not grow with their text, and the design existed only in prose. The PO asked for a rebuilt kit: design the components in a Storybook preview first (Vue + Tailwind, `docs/design`), then port them to Unity.

**Decision**
- **The design source is the Storybook preview** (`docs/design`: tokens in `theme.css`, components in `src/components`). Numbers (80 u buttons, 88 u rows, 116 u navigation bar, 48 u side padding, 4 u grabber gap ...) are measured from there.
- **Unity components are layouts**: uGUI `HorizontalLayoutGroup` / `VerticalLayoutGroup` / `LayoutElement`, built by code (`UiKit.Button`, `UiKit.Row`, ..., `UiPanel`, `UiSurface`, `UiPage`, ...), so sizes come from paddings, minimum heights and flexible widths. A sliced `Image` on a layout node adds its slice size (130 u) to the node's preferred size: backgrounds are a child image that the layout ignores.
- **Structure:** `UiPanel` (transparent root: show, push back, placement) > `UiSurface` (rounded rectangle, a stack of `UiPage`s) > page slots (navigation bar, body, status, toolbar). Dialogs and sheets are modal panels that push the panels behind back (`UiPanel.Present` / `Close` / `Stack`).
- **Icons** are sprites: SVGs of the Storybook exported as white 128 px PNGs by `pnpm icons` and tinted at run time (`UiIcon`, `UiIconSet`). The Vector Graphics package was tried and does not compile on Unity 6000.6.
- **Grabber** (move handle): a rounded rectangle, a real 3D object outside the canvas (the overlay clips everything else), 120 x 8 u at rest and 220 x 16 u on hover, 16 u below the surface. It follows the panel's visibility (`UiPanel.GrabberVisible`). The panel is moved around the head at a fixed distance; moving it nearer or farther was removed: the canvas scale follows the distance, so it showed no difference.
- **Callout:** a world-space annotation (not in an overlay canvas), delayed fade-in, `Dismiss` on interaction, no periodic animation.
- **Blur** of pushed-back panels is not possible: an `OVROverlayCanvas` is composited by the Quest runtime and cannot be shader-blurred. Pushed-back panels scale to 94 % and dim.
- **Slider** stays in the Storybook (not in use); not built in Unity.
- **Big bang:** the old builders (`UiKit.Button`, `Group`, `Row`, `Window`, `Sheet`, `PageStack`, `UiLayer`, `UiShowcase` ...) were deleted once every panel (Hall, chamber menu, Debug bench, experience chambers, protocol screens, test panels, dialogs) was migrated. `UiKit.Select` stays to show the chosen button of a segmented group.
- **Naming (2026-11-09):** Unity follows the Storybook. `Runtime/UiKit/` has one file per Storybook component with the same name (`UiButton`, `UiRow`, `UiSection`, `UiChoiceGroup`, `UiSwitch`, `UiTextField`, `UiStepper`, `UiNumberPad`, `UiList`, `UiNavigationBar`, `UiSurface`, `UiPanel`, `UiDialog`, `UiSheet`, `UiCallout`, `UiGrabber`, `UiIcon`); the factories are the static partial class `UiKit` (`UiLay` was merged into it; the basic icon builder is `UiKit.BareIcon`). `NumberPadEntry` -> `UiNumberPad`, `PanelMover` -> `UiGrabber`, `Column` -> `UiColumn` (a Debug-page helper, no Storybook counterpart). The mapping table is in docs/design/README.md.

**Consequences**
- Panels are built from components; tests measure the laid-out rectangles (`UiKitLayoutTests`, `UiKitInputsTests`, `UiKitPagesTests`, App `LayoutTests`: Hall, chamber menu, experience panels, protocol screens).
- Not verifiable without a headset: icon sharpness at 40 / 32 u, real Noto font metrics (tests use the default font), grabber feel, floor clearance, overlay rendering of the callout.
- Experience panels grew to 1100 x 980 u (MovingSource) because the rows are now labeled and stacked from the layout.
