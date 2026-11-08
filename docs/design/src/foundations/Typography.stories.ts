import type { Meta, StoryObj } from '@storybook/vue3-vite'
import TypographyPage from './TypographyPage.vue'

const meta = { title: 'Foundations/Typography', component: TypographyPage } satisfies Meta<typeof TypographyPage>
export default meta

export const Overview: StoryObj<typeof meta> = {}
