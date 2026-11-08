<script setup lang="ts">
/**
 * UiStepper (Unity: UiKit.SmallButton pair, the - / + of a parameter row; SwiftUI Stepper). Two small borderless in-group buttons
 * around the value. Fill on hover only. The value text is formatted by the caller (`format`).
 */
import { computed } from 'vue'
import UiButton from '../UiButton/UiButton.vue'

const model = defineModel<number>({ default: 0 })
const props = withDefaults(defineProps<{ min?: number; max?: number; step?: number; format?: (v: number) => string; disabled?: boolean }>(), {
  min: -Infinity, max: Infinity, step: 1,
})
const text = computed(() => (props.format ? props.format(model.value) : String(model.value)))
const bump = (d: number) => { model.value = Math.min(props.max, Math.max(props.min, +(model.value + d).toFixed(6))) }
</script>

<template>
  <div class="ui-stepper">
    <UiButton icon-leading="minus" small in-group button-style="borderless" :disabled="disabled || model <= min" @click="bump(-step)" />
    <span class="value">{{ text }}</span>
    <UiButton icon-leading="plus" small in-group button-style="borderless" :disabled="disabled || model >= max" @click="bump(step)" />
  </div>
</template>

<style scoped>
.ui-stepper { display: inline-flex; align-items: center; gap: var(--inset); }
.value { min-width: 120px; text-align: center; font-size: var(--text-body); transform: translateY(var(--label-nudge)); }
</style>
