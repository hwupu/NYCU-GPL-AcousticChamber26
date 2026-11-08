import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiRow from './UiRow.vue'
import ListDemo from './ListDemo.vue'

const meta = { title: 'Components/UiRow and UiSection', component: UiRow, tags: ['autodocs'], parameters: { layout: 'padded' } } satisfies Meta<typeof UiRow>
export default meta
type Story = StoryObj<typeof meta>

export const Sections: Story = { args: { title: 'Row' }, render: () => h(ListDemo), parameters: { controls: { disable: true } } }

export const Playground: Story = {
  args: { title: 'Row title', subtitle: 'Secondary line', value: 'Value', interactive: true, iconTrailing: 'chevron-right' },
  decorators: [(story) => ({ render: () => h('div', { class: 'w-900 rounded-group bg-raised p-8' }, [h(story())]) })],
}
