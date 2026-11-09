import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import MainHall from './MainHall.vue'

const meta = {
  title: 'Screens/Main Hall',
  component: MainHall,
  parameters: { layout: 'fullscreen' },
  render: (args) => ({ setup: () => () => h('div', { class: 'bg-black p-48' }, [h(MainHall, args)]) }),
} satisfies Meta<typeof MainHall>
export default meta
type Story = StoryObj<typeof meta>

/** Chamber covers: C-ID top left and the title bottom left on the image. */
export const Covers: Story = { args: { covers: true } }

/** Hover on the first card: it scales to 103 % and the image saturation rises to 125 %. */
export const Hover: Story = { args: { covers: true, hover: 0 } }

/** No thumbnails: solid `cover` placeholders, title in white. */
export const Placeholders: Story = { args: { covers: false } }
