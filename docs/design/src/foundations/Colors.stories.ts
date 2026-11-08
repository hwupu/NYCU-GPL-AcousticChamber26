import type { Meta, StoryObj } from '@storybook/vue3-vite'
import ColorsPage from './ColorsPage.vue'

const meta = { title: 'Foundations/Colors', component: ColorsPage } satisfies Meta<typeof ColorsPage>
export default meta

export const Overview: StoryObj<typeof meta> = {}
