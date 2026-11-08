import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiTextField from './UiTextField.vue'
import FieldsDemo from './FieldsDemo.vue'

const meta = { title: 'Components/Entry (TextField, NumberPad, Stepper, Slider)', component: UiTextField, parameters: { layout: 'padded' } } satisfies Meta<typeof UiTextField>
export default meta
type Story = StoryObj<typeof meta>

export const All: Story = { render: () => h(FieldsDemo), parameters: { controls: { disable: true } } }
