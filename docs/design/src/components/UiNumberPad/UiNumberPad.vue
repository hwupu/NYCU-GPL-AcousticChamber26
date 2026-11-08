<script setup lang="ts">
/**
 * UiNumberPad (Unity: UiNumberPad, an ITextEntry; Study ID). A UiTextField showing the formatted value live (`1` -> `S0001`,
 * placeholder `S____`) above a `raised` container of 3 x 4 borderless keys (1-9, Clear, 0, backspace), 4 u inset and 4 u between keys.
 * The model holds the digits only.
 */
import { computed } from 'vue'
import UiButton from '../UiButton/UiButton.vue'
import UiTextField from '../UiTextField/UiTextField.vue'

const model = defineModel<string>({ default: '' })
/** `fill`: the pad takes the width of its cell instead of 520 u (the chamber menu gives it 2/5 of the width, the buttons 3/5). */
const props = withDefaults(defineProps<{ maxLength?: number; placeholder?: string; format?: (digits: string) => string; fill?: boolean }>(), {
  maxLength: 4,
  placeholder: 'S____',
  format: (d: string) => (d ? 'S' + d.padStart(4, '0') : ''),
})
const shown = computed(() => props.format(model.value))
const add = (d: string) => { if (model.value.length < props.maxLength) model.value += d }
const keys = ['1', '2', '3', '4', '5', '6', '7', '8', '9']
</script>

<template>
  <div class="ui-number-pad" :data-fill="fill ? '' : undefined">
    <UiTextField :value="shown" :placeholder="placeholder" />
    <div class="keys">
      <UiButton v-for="k in keys" :key="k" :label="k" button-style="borderless" in-group :min-width="false" @click="add(k)" />
      <UiButton label="Clear" button-style="borderless" in-group :min-width="false" :disabled="!model" @click="model = ''" />
      <UiButton label="0" button-style="borderless" in-group :min-width="false" @click="add('0')" />
      <UiButton icon-leading="backspace" stretch button-style="borderless" in-group :min-width="false" :disabled="!model" @click="model = model.slice(0, -1)" />
    </div>
  </div>
</template>

<style scoped>
.ui-number-pad { display: flex; flex-direction: column; gap: var(--gap); width: 520px; }
.ui-number-pad[data-fill] { width: 100%; }
.keys :deep(.ui-button) { min-width: 0; padding-inline: 0; }
.keys {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr)); /* equal keys that may shrink below their label padding (no overflow in a narrow cell) */
  gap: var(--inset);
  padding: var(--inset);
  border-radius: 44px; /* key radius 40 + 4 inset (concentric) */
  background: var(--color-raised);
}
</style>
