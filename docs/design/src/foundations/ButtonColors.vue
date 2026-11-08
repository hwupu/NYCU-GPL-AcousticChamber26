<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { buttonStyles } from '@/theme/tokens'

const states = ['', '-hover', '-pressed'] as const
const stateNames = ['normal', 'hover', 'pressed']
const values = ref<Record<string, string>>({})

onMounted(() => {
  const style = getComputedStyle(document.documentElement)
  for (const b of buttonStyles)
    for (const s of [...states, '-label']) values.value[b.style + s] = style.getPropertyValue(`--color-${b.style}${s}`).trim()
})
</script>

<template>
  <table class="w-full border-separate border-spacing-y-4 text-small">
    <thead class="text-left text-minimum text-text2">
      <tr><th>Style</th><th>Unity</th><th v-for="n in stateNames" :key="n">{{ n }}</th><th>Label</th><th>Use</th></tr>
    </thead>
    <tbody>
      <tr v-for="b in buttonStyles" :key="b.style">
        <td class="font-bold">{{ b.style }}</td>
        <td class="text-text2">{{ b.unity }}</td>
        <td v-for="(s, i) in states" :key="s">
          <div class="flex h-80 w-180 items-center justify-center rounded-capsule bg-surface">
            <div class="flex h-60 w-160 items-center justify-center rounded-capsule text-minimum"
                 :class="b.bold ? 'font-bold' : ''"
                 :style="{ background: `var(--color-${b.style}${s})`, color: `var(--color-${b.style}-label)` }">
              {{ stateNames[i] }}
            </div>
          </div>
          <div class="mt-4 font-mono text-minimum text-text2">{{ values[b.style + s] }}</div>
        </td>
        <td class="font-mono text-text2">{{ values[b.style + '-label'] }}</td>
        <td>{{ b.use }}</td>
      </tr>
    </tbody>
  </table>
</template>
