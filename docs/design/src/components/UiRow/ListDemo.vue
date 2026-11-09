<script setup lang="ts">
import { ref } from 'vue'
import UiSection from '../UiSection/UiSection.vue'
import UiRow from './UiRow.vue'
import UiButton from '../UiButton/UiButton.vue'
import UiSwitch from '../UiSwitch/UiSwitch.vue'
import UiStepper from '../UiStepper/UiStepper.vue'
import UiSlider from '../UiSlider/UiSlider.vue'
import DocBlock from '@/docs/DocBlock.vue'

const sw = ref(true), gain = ref(6), speed = ref(0.4)
</script>

<template>
  <DocBlock title="Sections with rows (Debug menu style). Hover a clickable row; the info rows are not targets">
    <div class="flex max-w-904 flex-col gap-24">
      <UiSection header="RUN">
        <UiRow interactive title="Run experiment trials" icon-leading="pause" icon-trailing="chevron-right" />
        <UiRow interactive title="Playground" subtitle="Change parameters and play stimuli" value="Default" icon-trailing="chevron-right" />
        <UiRow interactive title="Presets" subtitle="Default and Custom presets&#10;The active one is marked with a check" icon-trailing="chevron-right" />
      </UiSection>

      <UiSection header="CONTROLS" footer="Controls in a row keep their own hover; the row stays still.">
        <UiRow title="Play background" subtitle="Bed of three looping recordings"><UiSwitch v-model="sw" /></UiRow>
        <UiRow title="Gain" icon-leading="sound-up"><UiStepper v-model="gain" :min="0" :max="12" :format="(v) => `+${v}`" /></UiRow>
        <UiRow title="Speed"><div class="w-360"><UiSlider v-model="speed" /></div></UiRow>
        <UiRow title="Log files" subtitle="3 files" icon-leading="folder"><UiButton label="Delete all" button-style="destructiveText" in-group small :min-width="false" /></UiRow>
        <UiRow title="S0012_2026-11-08.csv" subtitle="48 KB" icon-leading="folder"><UiButton icon-leading="trash-can" button-style="destructive" in-group small /></UiRow>
      </UiSection>

      <UiSection header="STATES">
        <UiRow interactive title="Selected row" icon-leading="circle-check-fill" selected />
        <UiRow interactive title="Hover (forced)" state="hover" />
        <UiRow interactive title="Pressed (forced)" state="pressed" />
        <UiRow interactive title="Disabled row" subtitle="Needs a finished session" disabled icon-trailing="chevron-right" />
        <UiRow title="Info row (not a button)" subtitle="Audio level: 10 (target 8)" value="OK" />
      </UiSection>
    </div>
  </DocBlock>
</template>
