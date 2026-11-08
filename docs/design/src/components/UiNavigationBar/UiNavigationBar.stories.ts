import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiNavigationBar from './UiNavigationBar.vue'
import NavDemo from './NavDemo.vue'

const meta = { title: 'Components/UiNavigationBar', component: UiNavigationBar, tags: ['autodocs'], parameters: { layout: 'padded' } } satisfies Meta<typeof UiNavigationBar>
export default meta
type Story = StoryObj<typeof meta>

export const Variants: Story = { args: { title: 'Title' }, render: () => h(NavDemo), parameters: { controls: { disable: true } } }
