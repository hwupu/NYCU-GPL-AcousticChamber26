# UI design preview (Storybook)

HTML preview and component catalogue of the GPL.UI design system ([DesignSystem.md](../DesignSystem.md), ADR-0005, ADR-0007).
**Design here first, approve, then build in Unity** (`Modules/UI`, `UiKit`).

Stack: pnpm, Vite, Vue 3, Tailwind CSS 4, Storybook 10 (`@storybook/vue3-vite`), Noto Sans TC (`@fontsource`).

```
pnpm install
pnpm storybook      # http://localhost:6006
pnpm build          # static site in storybook-static/ (not committed)
pnpm typecheck
```

## Rules
1. **Units.** `1 u = 1 px` (canvas unit, 0.057° in the headset). `--spacing: 1px`, so every Tailwind number is a canvas unit: `h-80` = 80 u, `p-8` = 8 u, `rounded-panel` = 32 u, `text-title` = 56 u.
2. **Tokens.** `src/theme/theme.css` (`@theme static`) is the only place with values. Names follow `UiTheme` fields (`surface`, `prominent-hover` = `prominent.hover`, `borderless-in-group`...). Default Tailwind palette, shadows, radii and type sizes are removed on purpose. Change a value here and in `UiTheme.cs` together.
3. **Components mirror Unity.** One Vue component per `UiKit` builder or class, same name, same props as the C# parameters, same child structure (child names = run-time GameObject names). Stories use only layout the Unity side can build.
4. **Stories per state:** normal, hover, pressed, selected, disabled (forced via a `state` prop, not only by mouse).
5. Foundations pages read token values live from the page, so the docs cannot drift from the CSS.

## Layout
```
.storybook/            config (Vite + Vue + Tailwind plugins, dark Quest background)
src/theme/             theme.css (tokens), tokens.ts (names, Unity field, use: docs only)
src/foundations/       Colors, Typography, Icons pages
src/icons/             the icon set (Akar Icons SVG, `index.ts` = name -> svg); in Unity one sprite per icon, same name
src/components/        one folder per component: Name.vue + Name.stories.ts (+ documentation helpers)
src/screens/           Main Hall, chamber menu, Config, experience panels (C03 done)
```

## Component names and hierarchy
A Vue component = one Unity file of the same name in `Assets/AcousticChamber/Modules/UI/Runtime/UiKit/` (`UiButton.vue` -> `UiButton.cs`). Layout components are built by a static factory of `UiKit` (`UiKit.Button(...)`, `UiKit.Row(...)`); components with state are classes of the same name. Props use SwiftUI vocabulary (`iconLeading` / `iconTrailing`, `selected`, `disabled`).

| Component | Unity (`GPL.UI`) | SwiftUI / visionOS | Notes |
|---|---|---|---|
| `UiIcon` | `UiKit.Icon`, class `UiIcon` | `Image` | sprite tinted by the label color |
| `UiButton` | `UiKit.Button`, class `UiButton` | `Button` | `buttonStyle` prominent / gray / borderless / destructiveText / destructive; `selected` = toggle button; no label = icon button |
| `UiSwitch` | `UiKit.SwitchRow`, class `UiSwitch` | `Toggle` | iOS style switch |
| `UiChoiceGroup` | `UiKit.ChoiceGroup` (+ `UiKit.Select`) | `Picker` | `selection` single / multiple; `appearance` segmented / tabs; `columns` grid |
| `UiSection` | `UiKit.Section` | `Section` | raised rounded group with an optional header (and footer) |
| `UiRow` | `UiKit.Row`, `LabeledRow` | list row | leading icon, title, subtitle, trailing value / ›, button, icon, switch; clickable rows highlight |
| `UiTextField` | `UiKit.TextField`, class `UiTextField` | `TextField` | shows a value; fed by an `ITextEntry` |
| `UiNumberPad` | class `UiNumberPad` (`CreateLayout`) | - | digits, Clear, backspace |
| `UiStepper` | `UiKit.Stepper`, class `UiStepper` | `Stepper` | - / + around a value |
| `UiList` | `UiKit.List` (over `ScrollList`) | `List` | scrolling stack of sections with edge fades |
| `UiNavigationBar` | static class `UiNavigationBar` | `NavigationStack` bar | ‹ back, title, DEBUG tag, header actions |
| `UiPanel` | class `UiPanel` (over `OverlayPanel`) | visionOS window | **transparent root**: placement (`below` = 8° below the eye line, default; `level` = 0°; lifted above the floor), translation, scale, fade, push back, dim; holds the surface and the grabber |
| `UiSurface` | classes `UiSurface`, `UiPage` | window background | the rounded rectangle with its padding; slots: `navigationBar`, content, `statusBar` (footer line), `toolbar` (bottom bar) |
| `UiGrabber` | class `UiGrabber` | window grabber | pill 120 x 8 u (220 x 16 u on hover), top edge 16 u below the surface; only on movable panels; turns the panel around the head (no distance move) |
| `UiDialog` | class `UiDialog` (`ModalDialog` is the facade chambers use) | `alert` | a decision, 820 u, no grabber |
| `UiSheet` | class `UiSheet` | `sheet` | a window that pushes the one behind it back |
| `UiCallout` | class `UiCallout` | - | world-space annotation of a 3D object |
| `UiSlider` | not built | `Slider` | preview only until something uses it |

Not components (no Storybook counterpart): `UiColumn` (scrolling body of the Config-style pages), `UiPointer`, `OverlayPanel`, `ScrollList`, `UiTheme`.

Panel kinds (`UiPanel` props): `window` fixed (Main Hall, onboarding), `window` + `movable` (experience chamber menu, has the grabber), `dialog` (confirmation: Drop out; centered, 820 u, no grabber). A sheet is a `window` that pushes the one behind it back.

## Plan
Done: colors, typography, icons, core components (dim and push back are `UiPanel` props `shown`, `depth`). Screens: C03 (panel + Config page, 2026-11-10). Open: Main Hall, default chamber menu, C01 / C02 menus, Config and its pages.
