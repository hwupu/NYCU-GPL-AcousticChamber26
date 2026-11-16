import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import C04Reverberation from './C04Reverberation.vue'

const meta = {
  title: 'Screens/C04 Reverberation',
  component: C04Reverberation,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof C04Reverberation>
export default meta
type Story = StoryObj<typeof meta>

type State = 'open' | 'hw2' | 'custom'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C04Reverberation, { state })])
const fixed = { controls: { disable: true } }

/** Start: Open space, profile Experience; Calibrate is the main action. No calibrated line and no warning in C04. */
export const Panel: Story = { render: wrap('open'), parameters: fixed }

/** The HW2 room preset (8.72 × 15 × 3.5 m). */
export const Hw2Room: Story = { render: wrap('hw2'), parameters: fixed }

/** A size set with the steppers: Room shows Custom. */
export const CustomSize: Story = { render: wrap('custom'), parameters: fixed }
