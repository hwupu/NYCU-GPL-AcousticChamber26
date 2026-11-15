<script setup lang="ts">
/**
 * C02 · Hidden Source experience panel (Unity: Chambers/HiddenSource HiddenSourceExperience). 1100 x 960 u, movable.
 * Rows: Room, Distance, Hearing (ear + level); Status (text2) and, under it, the RT60 warning (debug color) while the room is not
 * calibrated on this device (PB-26, PO 2026-11-15). Toolbar: Measure RT60, Calibrate (gray, left), Start (prominent, right).
 * Measure RT60 = the selected room, 4 positions x 3 repeats (about 1.5 min); Calibrate = the coefficient sweep (about 6 min);
 * both write a report (persistentDataPath/Calibration) and show progress, then the T_mid, in the status. Selected = running.
 */
import { ref } from 'vue'
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import UiChoiceGroup from '@/components/UiChoiceGroup/UiChoiceGroup.vue'
import UiButton from '@/components/UiButton/UiButton.vue'

const props = withDefaults(defineProps<{ state?: 'idle' | 'measuring' | 'result' | 'calibrated' }>(), { state: 'idle' })

const room = ref('Room 0.7 s'), cue = ref('Natural'), ear = ref('Both'), level = ref('0 dB')
const opts = (l: string[]) => l.map((x) => ({ value: x, label: x }))
const status = {
  idle: 'No round yet.',
  measuring: 'RT60: Room 0.7 s, -90 deg, repeat 2 (5/12)',
  result: 'RT60 T_mid: Room 0.7 s 0.71 ± 0.02 s. Report: RT60_HiddenSource_20261115-142301.txt',
  calibrated: 'No round yet.',
}[props.state]
</script>

<template>
  <UiPanel movable :width="1100" :height="960">
    <template #navigationBar>
      <UiNavigationBar title="C02 · Hidden Source" />
    </template>

    <div class="flex flex-col gap-12">
      <span class="text-small text-text2">A sound is hidden on the floor around you. Press Start, point to where you hear it with either controller, and press A. Then see how close you were.</span>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Room</span>
        <UiChoiceGroup v-model="room" class="flex-1" :options="opts(['Dry', 'Room 0.7 s', 'Hall 2.2 s'])" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Distance</span>
        <UiChoiceGroup v-model="cue" class="flex-1" :options="opts(['Natural', 'Flat'])" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Hearing</span>
        <UiChoiceGroup v-model="ear" :options="opts(['Both', 'Left', 'Right'])" />
        <UiChoiceGroup v-model="level" :options="opts(['0 dB', '-20 dB', '-40 dB'])" />
      </div>
      <span class="text-small text-text">{{ status }}</span>
      <span v-if="props.state !== 'calibrated'" class="text-small text-debug">RT60 not calibrated for this room on this device: values are estimates.</span>
    </div>

    <template #toolbar>
      <div class="flex gap-12">
        <UiButton label="Measure RT60" :selected="props.state === 'measuring'" />
        <UiButton label="Calibrate" />
      </div>
      <UiButton label="Start" button-style="prominent" :disabled="props.state === 'measuring'" />
    </template>
  </UiPanel>
</template>
