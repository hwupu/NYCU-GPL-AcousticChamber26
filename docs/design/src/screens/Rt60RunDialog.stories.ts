import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import Rt60RunDialog from './Rt60RunDialog.vue'

const meta = {
  title: 'Screens/RT60 Run Dialog',
  component: Rt60RunDialog,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof Rt60RunDialog>
export default meta
type Story = StoryObj<typeof meta>

type State = 'measuring' | 'calibrating' | 'calibrated' | 'inUse' | 'result' | 'stopped'
const wrap = (state: State) => () => h('div', { class: 'bg-black p-48' }, [h(Rt60RunDialog, { state })])
const fixed = { controls: { disable: true } }

/** Measure RT60 running: step, progress bar, time left; borderless Cancel (deletes the partial report). */
export const Measuring: Story = { render: wrap('measuring'), parameters: fixed }

/** Calibrate (C04 only) running: the sweep step, progress, time left; Cancel. */
export const Calibrating: Story = { render: wrap('calibrating'), parameters: fixed }

/** Calibration done: no results, only what to import (main protocol); Close, or Use on this headset (no rebuild; imported tables win). */
export const CalibrationCompleted: Story = { render: wrap('calibrated'), parameters: fixed }

/** After Use on this headset: which size uses it now, and that it still needs the import; Close. */
export const UsedOnHeadset: Story = { render: wrap('inUse'), parameters: fixed }

/** Measurement done: T_mid per condition, deviation, flags, report name; Close. */
export const MeasureResult: Story = { render: wrap('result'), parameters: fixed }

/** Refused or failed: the reason; no report files are kept; Close. */
export const Stopped: Story = { render: wrap('stopped'), parameters: fixed }
