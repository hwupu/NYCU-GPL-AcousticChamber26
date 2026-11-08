import type { StorybookConfig } from '@storybook/vue3-vite'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath } from 'node:url'

const config: StorybookConfig = {
  framework: '@storybook/vue3-vite',
  stories: ['../src/**/*.stories.ts'],
  addons: ['@storybook/addon-docs', '@storybook/addon-a11y'],
  viteFinal: async (cfg) => {
    cfg.plugins = [vue(), ...(cfg.plugins ?? []), tailwindcss()]
    cfg.resolve = {
      ...cfg.resolve,
      alias: { ...(cfg.resolve?.alias ?? {}), '@': fileURLToPath(new URL('../src', import.meta.url)) },
    }
    return cfg
  },
}
export default config
