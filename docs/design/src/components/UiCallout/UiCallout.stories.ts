import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiCallout from './UiCallout.vue'
import CalloutDemo from './CalloutDemo.vue'
import { iconNames } from '@/icons'

const meta = {
  title: 'Components/UiCallout',
  component: UiCallout,
  tags: ['autodocs'],
  parameters: { layout: 'padded' },
  argTypes: { icon: { control: 'select', options: iconNames }, placement: { control: 'inline-radio', options: ['top', 'bottom', 'left', 'right'] } },
  args: { label: 'Drag me', icon: 'hand', placement: 'top', delay: 0,shown: true },
} satisfies Meta<typeof UiCallout>
export default meta
type Story = StoryObj<typeof meta>

export const OnObjects: Story = { render: () => h(CalloutDemo), parameters: { controls: { disable: true } } }
export const Playground: Story = {
  decorators: [(story) => ({ render: () => h('div', { class: 'rounded-group bg-black p-96' }, [h(story())]) })],
}
