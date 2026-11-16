import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import C02HiddenSource from './C02HiddenSource.vue'

const meta = {
  title: 'Screens/C02 Hidden Source',
  component: C02HiddenSource,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof C02HiddenSource>
export default meta
type Story = StoryObj<typeof meta>

type State = 'idle' | 'calibrated' | 'headset'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C02HiddenSource, { state })])

/** The panel; the room is not calibrated on this device: warning under the status (debug color). */
export const Panel: Story = { render: wrap('idle'), parameters: { controls: { disable: true } } }

/** An imported ISO table for this room and device: no warning. */
export const Calibrated: Story = { render: wrap('calibrated'), parameters: { controls: { disable: true } } }

/** Only a table used on this headset (C04 "Use on this headset", not imported yet): the softer note in text2 instead of the warning (PO 2026-11-17). */
export const HeadsetTable: Story = { render: wrap('headset'), parameters: { controls: { disable: true } } }
