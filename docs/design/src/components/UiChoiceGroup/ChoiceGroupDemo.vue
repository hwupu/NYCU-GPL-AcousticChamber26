<script setup lang="ts">
import { ref } from 'vue'
import UiChoiceGroup from './UiChoiceGroup.vue'
import DocBlock from '@/docs/DocBlock.vue'

const ear = ref('both'), level = ref('0'), many = ref(['a']), tab = ref('run'), speed = ref('1'), sound = ref('Pink noise')
const sounds = ['White noise', 'Pink noise', 'Horn', 'Horn ×2', 'Choked horn', 'Pap horn', 'Pistol', 'Opera voice', 'Trumpet']
  .map((label) => ({ value: label, label }))
</script>

<template>
  <DocBlock title="Segmented, single selection (two independent sets side by side: ear, level)" note="Always one chosen. Each set has its own selection; call it a column pair in the hearing row.">
    <div class="flex gap-32">
      <UiChoiceGroup v-model="ear" class="w-480" :options="[{ value: 'both', label: 'Both' }, { value: 'left', label: 'Left' }, { value: 'right', label: 'Right' }]" />
      <UiChoiceGroup v-model="level" class="w-480" :options="[{ value: '0', label: '0 dB' }, { value: '20', label: '-20 dB' }, { value: '40', label: '-40 dB' }]" />
    </div>
  </DocBlock>
  <DocBlock title="Segmented with icons, five options (speed)">
    <UiChoiceGroup v-model="speed" class="w-900" :options="[{ value: '0.5', label: '0.5x', icon: 'reduce' }, { value: '1', label: '1x' }, { value: '2', label: '2x' }, { value: '4', label: '4x' }, { value: '8', label: '8x', icon: 'enlarge' }]" />
  </DocBlock>
  <DocBlock title="Grid, 4 columns (C03 sounds)" note="columns = 4: one track, one selection across the rows; the last row fills from the left. Use it when one row would cut the labels.">
    <UiChoiceGroup v-model="sound" :columns="4" class="w-1004" :options="sounds" />
  </DocBlock>
  <DocBlock title="Segmented, multiple selection (each toggles; may be empty)">
    <UiChoiceGroup v-model="many" selection="multiple" class="w-720" :options="[{ value: 'a', label: 'Cue A', icon: 'sound-on' }, { value: 'b', label: 'Cue B' }, { value: 'c', label: 'Cue C', disabled: true }]" />
  </DocBlock>
  <DocBlock title="Tabs">
    <UiChoiceGroup v-model="tab" appearance="tabs" class="w-720" :options="[{ value: 'run', label: 'Run' }, { value: 'play', label: 'Playground' }, { value: 'files', label: 'Log files' }]" />
  </DocBlock>
  <DocBlock bg="raised" title="Small segmented inside a group">
    <UiChoiceGroup v-model="ear" small class="w-480" :options="[{ value: 'both', label: 'Both' }, { value: 'left', label: 'Left' }, { value: 'right', label: 'Right' }]" />
  </DocBlock>
</template>
