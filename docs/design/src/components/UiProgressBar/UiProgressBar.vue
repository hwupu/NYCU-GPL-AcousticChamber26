<script setup lang="ts">
/**
 * UiProgressBar (new, PB-27; Unity: UiKit.ProgressBar, class UiProgressBar; SwiftUI ProgressView). Read-only: groove 12 u in `gray`,
 * fill `prominent`, capsule ends, full width of its parent. `value` 0..1. Optional caption under it (text2, small), e.g. the time left.
 * Same groove and fill as UiSlider, without the knob.
 */
import { computed } from 'vue'

const props = withDefaults(defineProps<{ value?: number; caption?: string }>(), { value: 0 })
const pct = computed(() => Math.min(1, Math.max(0, props.value)) * 100)
</script>

<template>
  <div class="ui-progress-bar" role="progressbar" :aria-valuenow="Math.round(pct)" aria-valuemin="0" aria-valuemax="100">
    <div class="groove"><div class="fill" :style="{ width: `${pct}%` }" /></div>
    <span v-if="caption" class="caption">{{ caption }}</span>
  </div>
</template>

<style scoped>
.ui-progress-bar { display: flex; flex-direction: column; gap: 12px; width: 100%; min-width: 240px; }
.groove { height: 12px; border-radius: var(--radius-capsule); background: var(--color-gray); overflow: hidden; }
.fill { height: 100%; border-radius: var(--radius-capsule); background: var(--color-prominent); transition: width var(--release) var(--ease-settle); }
.caption { font-size: var(--text-small); line-height: 1.2; color: var(--color-text2); }
</style>
