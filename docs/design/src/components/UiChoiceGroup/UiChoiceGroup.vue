<script setup lang="ts">
/**
 * UiChoiceGroup (Unity: UiKit.ChoiceButton + UiKit.Select; SwiftUI Picker). n equal buttons in a row; the chosen ones are `selected`.
 *  - `selection`   single (one of n, never empty) | multiple (each toggles)
 *  - `appearance`  segmented (a raised capsule track, 4 u inset, borderless in-group buttons) | tabs (no track, 8 u apart)
 * Code rule: choices are always shown with this component, never with hand-made style swaps.
 */
import UiButton from '../UiButton/UiButton.vue'
import type { IconName } from '@/icons'

export interface ChoiceOption { value: string; label?: string; icon?: IconName; disabled?: boolean }

const model = defineModel<string | string[]>({ default: '' })
const props = withDefaults(defineProps<{
  options: ChoiceOption[]
  selection?: 'single' | 'multiple'
  appearance?: 'segmented' | 'tabs'
  small?: boolean
}>(), { selection: 'single', appearance: 'segmented' })

const isOn = (v: string) => (Array.isArray(model.value) ? model.value.includes(v) : model.value === v)
function pick(v: string) {
  if (props.selection === 'single') { model.value = v; return }
  const cur = Array.isArray(model.value) ? model.value : []
  model.value = cur.includes(v) ? cur.filter((x) => x !== v) : [...cur, v]
}
</script>

<template>
  <div class="ui-choice-group" :data-appearance="appearance" role="group">
    <UiButton
      v-for="o in options" :key="o.value"
      :label="o.label" :icon-leading="o.icon" button-style="borderless" in-group :min-width="false" :small="small"
      :selected="isOn(o.value)" :disabled="o.disabled" class="flex-1" @click="pick(o.value)"
    />
  </div>
</template>

<style scoped>
.ui-choice-group { display: flex; box-sizing: border-box; min-width: 0; }
/* segments share the width equally and may shrink: 16 u side padding, labels never wrap */
.ui-choice-group :deep(.ui-button) { min-width: 0; padding-left: 16px; padding-right: 16px; }
.ui-choice-group[data-appearance='segmented'] {
  gap: var(--inset);
  padding: var(--inset);
  border-radius: var(--radius-capsule);
  background: var(--color-raised);
}
.ui-choice-group[data-appearance='tabs'] { gap: 8px; }
/* inside a raised group the track is the surface color (DesignSystem: segmented control) */
:global(.ui-section-group) .ui-choice-group[data-appearance='segmented'] { background: var(--color-surface); }
</style>
