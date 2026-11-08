# UI design preview (Storybook)

HTML preview and component catalogue of the GPL.UI design system ([DesignSystem.md](../DesignSystem.md), ADR-0005).
**Design here first, approve, then build in Unity** (`Modules/UI`, `UiKit`). It replaces the old `components.html` / `preview.html`.

Stack: pnpm, Vite, Vue 3, Tailwind CSS 4, Storybook 10 (`@storybook/vue3-vite`), Noto Sans TC (`@fontsource`).

```
pnpm install
pnpm storybook      # http://localhost:6006
pnpm build          # static site in storybook-static/ (not committed)
pnpm typecheck
```

## Rules
1. **Units.** `1 u = 1 px` (canvas unit, 0.057° in the headset). `--spacing: 1px`, so every Tailwind number is a canvas unit:
   `h-80` = 80 u, `p-8` = 8 u, `rounded-panel` = 32 u, `text-title` = 56 u.
2. **Tokens.** `src/theme/theme.css` (`@theme static`) is the only place with values. Names follow `UiTheme` fields
   (`surface`, `prominent-hover` = `prominent.hover`, `borderless-in-group`...). The default Tailwind palette, shadows, radii and
   type sizes are removed on purpose: only design-system tokens exist. Change a value here and in `UiTheme.cs` together.
3. **Components mirror Unity.** One Vue component per `UiKit` builder or class, same name, same props as the C# parameters,
   same child structure (names of children = run-time GameObject names). Stories use only layout the Unity side can build.
4. **Stories per state.** Every component shows normal, hover, pressed, selected, disabled (forced via a `state` prop, not only by mouse).
5. Foundations pages read token values live from the page, so the docs cannot drift from the CSS.

## Layout
```
.storybook/            config (Vite + Vue + Tailwind plugins, dark Quest background)
src/theme/             theme.css (tokens), tokens.ts (names, Unity field, use: docs only)
src/foundations/       Colors, Typography, Icons pages
src/icons/             the icon set (Akar Icons SVG, `index.ts` = name -> svg); in Unity one sprite per icon, same name
src/components/        one folder per component: Name.vue + Name.stories.ts (+ documentation helpers)
src/screens/           Main Hall, chamber menu, Debug, experience panels (later)
```

## Component names and hierarchy
Names follow `UiKit` where it has one, otherwise SwiftUI / visionOS. A Vue component = one Unity class or `UiKit` builder of the
same name. Props use SwiftUI vocabulary (`iconLeading` / `iconTrailing`, `selected`, `disabled`).

| Component | Replaces (UiKit) | SwiftUI / visionOS | Notes |
|---|---|---|---|
| `UiIcon` | glyph text (‹ ×) | `Image` | sprite tinted by the label color |
| `UiButton` | `Button`, `IconButton`, `SmallButton` | `Button` | `buttonStyle` prominent / gray / borderless / destructiveText / destructive; `selected` = toggle button; no label = icon button |
| `UiSwitch` | `UiSwitch` | `Toggle` | iOS style switch |
| `UiChoiceGroup` | `ChoiceButton` + `Select` | `Picker` | `selection` single / multiple; `appearance` segmented / tabs |
| `UiSection` | `Group` | `Section` | raised rounded group with an optional header (and footer) |
| `UiRow` | `Row`, `InfoRow` | list row | leading icon, title, subtitle (one or many lines), trailing value / ›, button, icon, switch; clickable rows highlight |
| `UiList` | `ScrollList`, `Column` | `List` | scrolling stack of sections with edge fades |
| `UiNavigationBar` | `Page` header | `NavigationStack` bar | ‹ back, title, DEBUG tag, header actions |
| `UiPanel` | `OverlayPanel`, `UiLayer` | visionOS window | **transparent root**: placement (`below` = 8 deg below the eye line, default; `level` = 0 deg; lifted above the floor so the grabber is never hidden), translation, scale, fade, push back, dim; holds the surface and the grabber |
| `UiSurface` | `Window` / `Sheet` body | window background | the rounded rectangle with the background and its padding; slots: `navigationBar`, content, `statusBar` (footer line), `toolbar` (bottom bar) |
| `UiGrabber` | `PanelMover` handle | window grabber | below the surface, outside it; only on movable panels |

Panel kinds (`UiPanel` props): `window` fixed (Main Hall, onboarding), `window` + `movable` (experience chamber menu, has the grabber),
`dialog` (confirmation: Drop out; centered, 820 u, no grabber). A sheet is a `window` that pushes the one behind it back.

## Plan
1. Colors, typography, icons (done).
2. Core components (done, awaiting review): UiIcon, UiButton, UiSwitch, UiChoiceGroup, UiSection + UiRow, UiTextField, UiNumberPad, UiStepper, UiSlider (preview only: not ported to Unity until something uses it), UiNavigationBar, UiList, UiSurface, UiPanel, UiGrabber, UiDialog, UiCallout. Dim and push back are props of UiPanel (`shown`, `depth`), not a component.
3. Screens: Main Hall, default chamber menu, C01 / C02 / C03 menus, Debug menu and its pages.
