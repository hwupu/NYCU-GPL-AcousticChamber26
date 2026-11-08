<script setup lang="ts">
import { ref } from 'vue'
import UiSwitch from './UiSwitch.vue'
import DocBlock from '@/docs/DocBlock.vue'
const a = ref(false), b = ref(true)
</script>

<template>
  <DocBlock title="Switch: off, on, hover, pressed, disabled (click to toggle)">
    <div class="flex items-center gap-32">
      <UiSwitch v-model="a" /><UiSwitch v-model="b" />
      <UiSwitch :model-value="false" state="hover" /><UiSwitch :model-value="false" state="pressed" /><UiSwitch :model-value="true" state="pressed" />
      <UiSwitch :model-value="false" disabled /><UiSwitch :model-value="true" disabled />
    </div>
  </DocBlock>
</template>
