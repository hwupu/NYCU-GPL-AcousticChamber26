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

type State = 'idle' | 'measuring' | 'result' | 'calibrated'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C02HiddenSource, { state })])

/** The panel; the room is not calibrated on this device: warning under the status (debug color). */
export const Panel: Story = { render: wrap('idle'), parameters: { controls: { disable: true } } }

/** Measure RT60 running (selected); progress in the status; Start is off until it ends. */
export const Measuring: Story = { render: wrap('measuring'), parameters: { controls: { disable: true } } }

/** After a measurement: T_mid (mean ± SD over 4 positions x 3 repeats) and the report file. */
export const Result: Story = { render: wrap('result'), parameters: { controls: { disable: true } } }

/** An imported ISO table for this room and device: no warning. */
export const Calibrated: Story = { render: wrap('calibrated'), parameters: { controls: { disable: true } } }
