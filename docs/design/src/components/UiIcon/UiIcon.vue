<script setup lang="ts">
/**
 * UiIcon: one icon of the set (src/icons), tinted by the current text color. Unity: an `Image` with the icon sprite, `color` = label color.
 * Size comes from the parent (`--icon-size` in a button) or the `size` prop (u).
 */
import { computed } from 'vue'
import { icons, type IconName } from '@/icons'

const props = defineProps<{ name: IconName; size?: number }>()
const svg = computed(() => {
  const s = icons[props.name]
  if (!s) console.warn(`[UiIcon] unknown icon "${props.name}"`)
  return s ?? ''
})
</script>

<template>
  <span class="ui-icon" :style="size ? { width: `${size}px`, height: `${size}px` } : undefined" aria-hidden="true" v-html="svg" />
</template>

<style scoped>
.ui-icon {
  display: inline-block;
  flex: none;
  width: var(--icon-size);
  height: var(--icon-size);
  line-height: 0;
}
.ui-icon :deep(svg) {
  width: 100%;
  height: 100%;
}
</style>
