import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiChoiceGroup from './UiChoiceGroup.vue'
import ChoiceGroupDemo from './ChoiceGroupDemo.vue'

const meta = { title: 'Components/UiChoiceGroup', component: UiChoiceGroup, tags: ['autodocs'], parameters: { layout: 'padded' } } satisfies Meta<typeof UiChoiceGroup>
export default meta
type Story = StoryObj<typeof meta>

export const Variants: Story = { args: { options: [] }, render: () => h(ChoiceGroupDemo), parameters: { controls: { disable: true } } }
