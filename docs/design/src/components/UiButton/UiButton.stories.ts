import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import UiButton from './UiButton.vue'
import ButtonStates from './ButtonStates.vue'
import ButtonVariations from './ButtonVariations.vue'
import { iconNames } from '@/icons'

const meta = {
  title: 'Components/UiButton',
  component: UiButton,
  tags: ['autodocs'],
  parameters: { layout: 'padded' },
  argTypes: {
    buttonStyle: { control: 'select', options: ['prominent', 'gray', 'borderless', 'destructiveText', 'destructive'] },
    iconLeading: { control: 'select', options: [undefined, ...iconNames] },
    iconTrailing: { control: 'select', options: [undefined, ...iconNames] },
    state: { control: 'inline-radio', options: ['idle', 'hover', 'pressed'] },
  },
  args: { label: 'Button', buttonStyle: 'gray', selected: false, disabled: false, small: false, inGroup: false, minWidth: true, state: 'idle' },
} satisfies Meta<typeof UiButton>
export default meta
type Story = StoryObj<typeof meta>

export const Playground: Story = {
  decorators: [(story) => ({ render: () => h('div', { class: 'rounded-panel bg-surface p-48' }, [h(story())]) })],
}

export const States: Story = {
  render: () => h(ButtonStates),
  parameters: { controls: { disable: true } },
}

export const IconsAndSizes: Story = {
  render: () => h(ButtonVariations),
  parameters: { controls: { disable: true } },
}
