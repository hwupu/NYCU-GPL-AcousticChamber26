import type { Meta, StoryObj } from '@storybook/vue3-vite'
import IconsPage from './IconsPage.vue'

const meta = { title: 'Foundations/Icons', component: IconsPage } satisfies Meta<typeof IconsPage>
export default meta

export const Gallery: StoryObj<typeof meta> = {}
