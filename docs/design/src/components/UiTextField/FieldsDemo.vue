<script setup lang="ts">
import { ref } from 'vue'
import UiTextField from './UiTextField.vue'
import UiStepper from '../UiStepper/UiStepper.vue'
import UiSlider from '../UiSlider/UiSlider.vue'
import UiNumberPad from '../UiNumberPad/UiNumberPad.vue'
import DocBlock from '@/docs/DocBlock.vue'

const digits = ref(''), n = ref(8), v = ref(0.5)
</script>

<template>
  <DocBlock title="UiTextField: empty (placeholder), filled, focused, disabled">
    <div class="flex max-w-520 flex-col gap-20">
      <UiTextField placeholder="S____" />
      <UiTextField value="S0012" />
      <UiTextField value="S0012" focused />
      <UiTextField value="S0012" disabled />
    </div>
  </DocBlock>
  <DocBlock title="UiNumberPad (type with the keys; digits only, max 4; Clear and backspace enable when there is a value)">
    <UiNumberPad v-model="digits" />
  </DocBlock>
  <DocBlock bg="raised" title="UiStepper and UiSlider (in a group)">
    <div class="flex items-center gap-48">
      <UiStepper v-model="n" :min="0" :max="12" :format="(x) => `${x} dB`" />
      <UiStepper :model-value="0" :min="0" :max="12" />
      <div class="w-420"><UiSlider v-model="v" /></div>
      <div class="w-260"><UiSlider :model-value="0.3" disabled /></div>
    </div>
  </DocBlock>
</template>
