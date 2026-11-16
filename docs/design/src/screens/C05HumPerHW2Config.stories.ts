import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import C05HumPerHW2Config from './C05HumPerHW2Config.vue'

const meta = {
  title: 'Screens/C05 HumPerHW2 Config',
  component: C05HumPerHW2Config,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof C05HumPerHW2Config>
export default meta
type Story = StoryObj<typeof meta>

type State = 'idle' | 'reverb' | 'uncalibrated'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C05HumPerHW2Config, { state })])
const fixed = { controls: { disable: true } }

/** Opened from the chamber menu: Quiet, nothing playing, empty status. */
export const Page: Story = { render: wrap('idle'), parameters: fixed }

/** Reverb with the stimulus and the background playing, sound sphere always visible; status = the reverb setting. */
export const Reverb: Story = { render: wrap('reverb'), parameters: fixed }

/** No RT60 table for this room on this device: the warning in the status line (PB-27 text). */
export const Uncalibrated: Story = { render: wrap('uncalibrated'), parameters: fixed }
