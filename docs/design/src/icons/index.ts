/**
 * The icon set (Akar Icons, 24 x 24, `currentColor`): every `*.svg` in this folder, by name without the `akar-icons--` prefix
 * (`chevron-left`, `circle-check-fill`...). In Unity each becomes a sprite of the same name (one icon atlas, tinted by the label color).
 */
const files = import.meta.glob('./*.svg', { query: '?raw', import: 'default', eager: true }) as Record<string, string>

export const icons: Record<string, string> = {}
for (const [path, svg] of Object.entries(files))
  icons[path.replace(/^.*\/(akar-icons--)?/, '').replace(/\.svg$/, '')] = svg

export type IconName = string
export const iconNames = Object.keys(icons).sort()
