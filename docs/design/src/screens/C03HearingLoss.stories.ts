import { h } from 'vue'
import type { Meta, StoryObj } from '@storybook/vue3-vite'
import C03HearingLoss from './C03HearingLoss.vue'

const meta = {
  title: 'Screens/C03 Hearing Loss',
  component: C03HearingLoss,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof C03HearingLoss>
export default meta
type Story = StoryObj<typeof meta>

const wrap = (page: 'root' | 'config') => () => h('div', { class: 'bg-black p-48 pb-200' }, [h(C03HearingLoss, { page })])

/** The chamber panel: conditions, level, 12 sounds in a 4-column grid; Config at the bottom right. */
export const Panel: Story = { render: wrap('root'), parameters: { controls: { disable: true } } }

/** Config: the hearing loss test (the Sandbox Hearing Loss Test Panel's content), with ‹ back to the panel. */
export const Config: Story = { render: wrap('config'), parameters: { controls: { disable: true } } }
