import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import C06HumPerFinalConfig from './C06HumPerFinalConfig.vue'

const meta = {
  title: 'Screens/C06 HumPerFinal Config',
  component: C06HumPerFinalConfig,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof C06HumPerFinalConfig>
export default meta
type Story = StoryObj<typeof meta>

type State = 'idle' | 'testing' | 'uncalibrated'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C06HumPerFinalConfig, { state })])
const fixed = { controls: { disable: true } }

/** Opened from the chamber menu: room 0.7 s, normal hearing, anchors off, nothing playing, empty status. */
export const Page: Story = { render: wrap('idle'), parameters: fixed }

/** Testing a cell: left −80, anchors on, stimulus playing; the status line says what is applied. */
export const Testing: Story = { render: wrap('testing'), parameters: fixed }

/** No RT60 table for this room on this device: the warning in the status line (PB-27 text). */
export const Uncalibrated: Story = { render: wrap('uncalibrated'), parameters: fixed }
