<script setup lang="ts">
/**
 * UiSwitch (Unity: UiSwitch, UiKit.SwitchRow; SwiftUI Toggle). 100 x 60 u. Off: gray track, text2 knob. On: prominent track, knob in the
 * group color (`raised`). The knob settles. In a hovered or pressed UiRow the off track darkens to `raised`.
 */
const model = defineModel<boolean>({ default: false })
const props = defineProps<{ disabled?: boolean; state?: 'idle' | 'hover' | 'pressed' }>()
function toggle() { if (!props.disabled) model.value = !model.value }
</script>

<template>
  <button type="button" class="ui-switch" role="switch" :aria-checked="model" :data-on="model ? '' : undefined" :data-state="state ?? 'idle'" :disabled="disabled" @click="toggle">
    <span class="knob" />
  </button>
</template>

<style scoped>
.ui-switch {
  position: relative;
  flex: none;
  box-sizing: border-box;
  width: 100px;
  height: 60px;
  padding: 0;
  border: 0;
  border-radius: var(--radius-capsule);
  background: var(--color-gray);
  cursor: pointer;
  transition: background-color var(--hover-out) var(--ease-settle);
}
.ui-switch[data-on] { background: var(--color-prominent); }
.ui-switch:hover:not(:disabled):not([data-on]), .ui-switch[data-state='hover']:not([data-on]) { background: var(--color-gray-hover); }
.ui-switch:disabled { opacity: var(--disabled-alpha); cursor: default; }
.knob {
  position: absolute;
  top: 6px;
  left: 6px;
  width: 48px;
  height: 48px;
  border-radius: var(--radius-capsule);
  background: var(--color-text2);
  transition: transform var(--sheet) var(--ease-settle), background-color var(--hover-out) var(--ease-settle);
}
.ui-switch[data-on] .knob { transform: translateX(40px); background: var(--color-raised); }
.ui-switch:active:not(:disabled) .knob, .ui-switch[data-state='pressed'] .knob { width: 56px; }
.ui-switch[data-on]:active:not(:disabled) .knob, .ui-switch[data-on][data-state='pressed'] .knob { transform: translateX(32px); }
/* inside a hovered or pressed row the off track darkens to the row's own gray (UiRow) */
:global(.ui-row[data-interactive]:hover) .ui-switch:not([data-on]) { background: var(--color-raised); }
</style>
