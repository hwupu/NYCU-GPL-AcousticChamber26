<script setup lang="ts">
/**
 * UiSlider (new; Unity: a ray-driven slider, UiPointer drag like ScrollList). Groove 12 u in `gray`, fill `prominent`, knob 44 u;
 * the target is the whole 60 u tall strip (hit minimum). For continuous parameters (speed...). Steppers stay for exact values.
 */
import { computed, ref } from 'vue'

const model = defineModel<number>({ default: 0 })
const props = withDefaults(defineProps<{ min?: number; max?: number; step?: number; disabled?: boolean }>(), { min: 0, max: 1, step: 0 })
const root = ref<HTMLElement>()
const dragging = ref(false)
const pct = computed(() => ((model.value - props.min) / (props.max - props.min)) * 100)

function setFrom(e: PointerEvent) {
  const r = root.value!.getBoundingClientRect()
  const knob = 44
  let t = Math.min(1, Math.max(0, (e.clientX - r.left - knob / 2) / (r.width - knob)))
  let v = props.min + t * (props.max - props.min)
  if (props.step) v = Math.round(v / props.step) * props.step
  model.value = Math.min(props.max, Math.max(props.min, +v.toFixed(6)))
}
function down(e: PointerEvent) {
  if (props.disabled) return
  dragging.value = true
  ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
  setFrom(e)
}
const move = (e: PointerEvent) => dragging.value && setFrom(e)
const up = () => (dragging.value = false)
</script>

<template>
  <div ref="root" class="ui-slider" :data-dragging="dragging ? '' : undefined" :data-disabled="disabled ? '' : undefined" role="slider"
       :aria-valuenow="model" @pointerdown="down" @pointermove="move" @pointerup="up">
    <div class="groove"><div class="fill" :style="{ width: `calc(22px + (100% - 44px) * ${pct / 100})` }" /></div>
    <div class="knob" :style="{ left: `calc((100% - 44px) * ${pct / 100})` }" />
  </div>
</template>

<style scoped>
.ui-slider { position: relative; width: 100%; min-width: 240px; height: var(--hit-min); touch-action: none; cursor: pointer; }
.groove { position: absolute; top: 24px; left: 0; right: 0; height: 12px; border-radius: var(--radius-capsule); background: var(--color-gray); overflow: hidden; }
.fill { height: 100%; background: var(--color-prominent); }
.knob {
  position: absolute; top: 8px; width: 44px; height: 44px; border-radius: var(--radius-capsule);
  background: var(--color-prominent-pressed);
  transition: transform var(--release) var(--ease-settle);
}
.ui-slider:hover .knob { transform: scale(1.08); }
.ui-slider[data-dragging] .knob { transform: scale(var(--pressed-scale)); transition-duration: 0s; }
.ui-slider[data-disabled] { opacity: var(--disabled-alpha); cursor: default; }
</style>
