import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiProgressBar from './UiProgressBar.vue'

const meta = {
  title: 'Components/UiProgressBar',
  component: UiProgressBar,
  tags: ['autodocs'],
  decorators: [(story) => ({ render: () => h('div', { class: 'bg-surface p-48', style: 'width: 820px' }, [h(story())]) })],
} satisfies Meta<typeof UiProgressBar>
export default meta
type Story = StoryObj<typeof meta>

/** Nothing done yet. */
export const Start: Story = { args: { value: 0, caption: 'About 6 min left' } }

/** Part done, with the time left as caption. */
export const Partway: Story = { args: { value: 0.42, caption: 'About 3 min 30 s left' } }

/** Done. */
export const Full: Story = { args: { value: 1 } }

export const Playground: Story = { args: { value: 0.5, caption: '' }, argTypes: { value: { control: { type: 'range', min: 0, max: 1, step: 0.01 } } } }
