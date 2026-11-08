// Rasterizes src/icons/*.svg to white 128 px PNGs for Unity (one sprite per icon, same name).
// Usage: pnpm icons   (writes Assets/AcousticChamber/Modules/UI/Icons/<name>.png; Unity tints them with the label color)
import { readdirSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { Resvg } from '@resvg/resvg-js'

const here = dirname(fileURLToPath(import.meta.url))
const src = join(here, '../src/icons')
const out = join(here, '../../../Assets/AcousticChamber/Modules/UI/Icons')
const SIZE = 128

mkdirSync(out, { recursive: true })
let n = 0
for (const file of readdirSync(src).filter((f) => f.endsWith('.svg'))) {
  const name = file.replace(/^akar-icons--/, '').replace(/\.svg$/, '')
  const svg = readFileSync(join(src, file), 'utf8').replaceAll('currentColor', '#ffffff')
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: SIZE } }).render().asPng()
  writeFileSync(join(out, name + '.png'), png)
  n++
}
console.log(`${n} icons -> ${out}`)
