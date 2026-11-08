import type { Preview } from '@storybook/vue3-vite'
import { themes } from 'storybook/theming'
import '../src/theme/theme.css'

// The Quest panel is always dark: the canvas is black behind the `surface` panel (DesignSystem section 3).
const preview: Preview = {
  parameters: {
    layout: 'fullscreen',
    backgrounds: { options: { quest: { name: 'Quest', value: '#000000' } }, },
    docs: { theme: themes.dark },
    options: { storySort: { order: ['Foundations', 'Components', 'Screens'] } },
  },
  initialGlobals: { backgrounds: { value: 'quest' } },
}
export default preview
