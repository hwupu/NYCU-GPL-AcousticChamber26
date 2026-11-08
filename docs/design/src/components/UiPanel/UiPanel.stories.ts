import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiPanel from './UiPanel.vue'
import PanelDemo from './PanelDemo.vue'

const meta = { title: 'Components/UiPanel (Surface, Grabber, Dialog)', component: UiPanel, tags: ['autodocs'], parameters: { layout: 'fullscreen' } } satisfies Meta<typeof UiPanel>
export default meta
type Story = StoryObj<typeof meta>

export const Kinds: Story = { render: () => h(PanelDemo), parameters: { controls: { disable: true } } }

export const Playground: Story = {
  args: { kind: 'window', movable: true, shown: true, depth: 0 },
  argTypes: { kind: { control: 'inline-radio', options: ['window', 'dialog'] }, grabberState: { control: 'inline-radio', options: ['idle', 'hover', 'held'] } },
  decorators: [(story) => ({ render: () => h('div', { class: 'bg-black p-48 pb-200' }, [h(story())]) })],
}
