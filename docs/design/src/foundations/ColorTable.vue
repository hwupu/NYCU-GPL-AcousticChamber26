<script setup lang="ts">
import { onMounted, ref } from 'vue'
import type { ColorToken } from '@/theme/tokens'

const props = defineProps<{ tokens: ColorToken[] }>()
const values = ref<Record<string, string>>({})

onMounted(() => {
  const style = getComputedStyle(document.documentElement)
  for (const t of props.tokens) values.value[t.name] = style.getPropertyValue(`--color-${t.name}`).trim()
})
</script>

<template>
  <table class="w-full border-separate border-spacing-y-4 text-small">
    <thead class="text-left text-minimum text-text2">
      <tr><th class="w-120">&nbsp;</th><th>Token</th><th>Tailwind</th><th>Unity (UiTheme)</th><th>Value</th><th>Use</th></tr>
    </thead>
    <tbody>
      <tr v-for="t in tokens" :key="t.name">
        <td><div class="h-60 w-100 rounded-row border border-white/15" :style="{ background: `var(--color-${t.name})` }" /></td>
        <td class="font-bold">{{ t.name }}</td>
        <td class="text-text2">bg-{{ t.name }} / text-{{ t.name }}</td>
        <td class="text-text2">{{ t.unity }}</td>
        <td class="font-mono text-text2">{{ values[t.name] }}</td>
        <td>{{ t.use }}</td>
      </tr>
    </tbody>
  </table>
</template>
