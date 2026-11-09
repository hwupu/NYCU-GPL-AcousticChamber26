import type { StorybookConfig } from '@storybook/vue3-vite'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath } from 'node:url'
import { searchForWorkspaceRoot } from 'vite'

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
    // screens show the real chamber covers from the Unity project (one copy: Assets/AcousticChamber/App/Covers)
    const covers = fileURLToPath(new URL('../../../Assets/AcousticChamber/App/Covers', import.meta.url))
    cfg.server = { ...cfg.server, fs: { ...cfg.server?.fs, allow: [...(cfg.server?.fs?.allow ?? [searchForWorkspaceRoot(process.cwd())]), covers] } }
    return cfg
  },
}
export default config
