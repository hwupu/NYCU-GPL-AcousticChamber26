/**
 * Metadata of the design tokens, for the documentation pages only. The values live in theme.css (single source);
 * the pages read them live from the document. `unity` is the field of GPL.UI.UiTheme.
 */
export interface ColorToken { name: string; unity: string; use: string }

export const surfaces: ColorToken[] = [
  { name: 'surface', unity: 'surface', use: 'panels, sheets' },
  { name: 'raised', unity: 'raised', use: 'groups, fields (Study ID display), choice track' },
  { name: 'cover', unity: 'cover', use: 'cover image placeholder' },
  { name: 'select', unity: 'select', use: 'the chosen button of a choice set (UiButton.Selected)' },
  { name: 'dim', unity: 'dim', use: 'the layer behind a sheet (45 % black)' },
]

export const textColors: ColorToken[] = [
  { name: 'text', unity: 'text', use: 'primary text' },
  { name: 'text2', unity: 'text2', use: 'secondary text, values, group labels' },
  { name: 'text3', unity: 'text3', use: 'disabled text, placeholders, scroll indicator' },
  { name: 'debug', unity: 'debug', use: 'the DEBUG tag (text only)' },
  { name: 'red-text', unity: 'redText', use: 'destructive action that is not the final step' },
]

export interface ButtonStyleToken { style: string; unity: string; use: string; bold: boolean }

export const buttonStyles: ButtonStyleToken[] = [
  { style: 'prominent', unity: 'prominent', use: 'the one main action of a screen', bold: true },
  { style: 'gray', unity: 'gray', use: 'a secondary action that needs a visible target', bold: false },
  { style: 'borderless', unity: 'borderless', use: 'back, cancel, keys, list actions (on a surface)', bold: false },
  { style: 'borderless-in-group', unity: 'borderlessInGroup', use: 'borderless inside a raised group', bold: false },
  { style: 'destructive', unity: 'destructive', use: 'red fill only on the final confirmation', bold: true },
]

export interface TypeToken { name: string; unity: string; use: string; weight: 'regular' | 'bold'; size: number }

/** Sizes in u at 2 m (1 u = 1 px here); angle = size x 0.057 deg. */
export const typeScale: TypeToken[] = [
  { name: 'title', unity: 'titleSize', use: 'page and sheet titles', weight: 'bold', size: 56 },
  { name: 'heading', unity: 'headingSize', use: 'headings inside a screen', weight: 'bold', size: 44 },
  { name: 'body', unity: 'bodySize', use: 'body text, rows', weight: 'regular', size: 36 },
  { name: 'button', unity: 'buttonSize', use: 'button labels (bold on prominent and destructive)', weight: 'regular', size: 36 },
  { name: 'small', unity: 'smallSize', use: 'status lines, secondary text', weight: 'regular', size: 28 },
  { name: 'minimum', unity: 'minimumSize', use: 'Debug and labels only; never smaller', weight: 'regular', size: 24 },
]
