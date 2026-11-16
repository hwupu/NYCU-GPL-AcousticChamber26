import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import C01MovingSource from './C01MovingSource.vue'

const meta = {
  title: 'Screens/C01 Moving Source',
  component: C01MovingSource,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof C01MovingSource>
export default meta
type Story = StoryObj<typeof meta>

type State = 'idle' | 'calibrated' | 'headset'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C01MovingSource, { state })])

/** The panel: Measure RT60 (toolbar, right); no table for this room on this device -> the warning under the status. */
export const Panel: Story = { render: wrap('idle'), parameters: { controls: { disable: true } } }

/** An imported table for this room and device: no warning. Measure RT60 opens Screens/RT60 Run Dialog. */
export const Calibrated: Story = { render: wrap('calibrated'), parameters: { controls: { disable: true } } }

/** Only a table used on this headset (C04 "Use on this headset", not imported yet): the softer note in text2 instead of the warning (PO 2026-11-17). */
export const HeadsetTable: Story = { render: wrap('headset'), parameters: { controls: { disable: true } } }
