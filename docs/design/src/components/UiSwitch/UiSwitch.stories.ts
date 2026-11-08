import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiSwitch from './UiSwitch.vue'
import SwitchDemo from './SwitchDemo.vue'

const meta = { title: 'Components/UiSwitch', component: UiSwitch, tags: ['autodocs'], parameters: { layout: 'padded' } } satisfies Meta<typeof UiSwitch>
export default meta
type Story = StoryObj<typeof meta>

export const States: Story = { render: () => h(SwitchDemo), parameters: { controls: { disable: true } } }
