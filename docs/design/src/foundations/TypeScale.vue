<script setup lang="ts">
import { typeScale } from '@/theme/tokens'

const DEG_PER_UNIT = 0.057
const sample = () => 'Acoustic Chamber 聽覺實驗室'
</script>

<template>
  <table class="w-full border-separate border-spacing-y-2 text-small">
    <thead class="text-left text-minimum text-text2">
      <tr><th>Token</th><th>Tailwind</th><th>Unity (UiTheme)</th><th>Size</th><th>Angle</th><th>Weight</th><th>Use</th></tr>
    </thead>
    <tbody>
      <template v-for="t in typeScale" :key="t.name">
        <tr>
          <td class="pt-16 font-bold">{{ t.name }}</td>
          <td class="pt-16 text-text2">text-{{ t.name }}</td>
          <td class="pt-16 text-text2">{{ t.unity }}</td>
          <td class="pt-16 font-mono text-text2">{{ t.size }} u</td>
          <td class="pt-16 font-mono text-text2">{{ (t.size * DEG_PER_UNIT).toFixed(1) }}°</td>
          <td class="pt-16 text-text2">{{ t.weight }}</td>
          <td class="pt-16">{{ t.use }}</td>
        </tr>
        <tr>
          <td colspan="7" :class="[`text-${t.name}`, t.weight === 'bold' ? 'font-bold' : 'font-regular']">{{ sample() }}</td>
        </tr>
      </template>
    </tbody>
  </table>
</template>
