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

type State = 'idle' | 'measuring' | 'result'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C01MovingSource, { state })])

/** The panel with the RT60 buttons (toolbar, right) and the not-calibrated warning under the status. */
export const Panel: Story = { render: wrap('idle'), parameters: { controls: { disable: true } } }

/** Measure RT60 running (selected): progress in the status; the sound and the path are stopped. */
export const Measuring: Story = { render: wrap('measuring'), parameters: { controls: { disable: true } } }

/** After a measurement: T_mid and the report file. */
export const Result: Story = { render: wrap('result'), parameters: { controls: { disable: true } } }
