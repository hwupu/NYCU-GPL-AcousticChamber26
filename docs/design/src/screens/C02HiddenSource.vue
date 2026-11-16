<script setup lang="ts">
/**
 * C02 · Hidden Source experience panel (Unity: Chambers/HiddenSource HiddenSourceExperience). 1100 x 960 u, movable.
 * Rows: Room, Distance, Hearing (ear + level); Status (text2) and, under it, the RT60 warning (debug color) while the room is not
 * calibrated on this device (PB-26; text PB-27). Toolbar: Measure RT60 (gray, left), Start (prominent, right).
 * Measure RT60 = the selected room, 4 positions x 3 repeats (about 1.5 min); progress and result in the RT60 run dialog
 * (Screens/RT60 Run Dialog), not in the status (PB-27). Calibrate moved to C04 Reverberation (PB-27).
 */
import { ref } from 'vue'
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import UiChoiceGroup from '@/components/UiChoiceGroup/UiChoiceGroup.vue'
import UiButton from '@/components/UiButton/UiButton.vue'

const props = withDefaults(defineProps<{ state?: 'idle' | 'calibrated' | 'headset' }>(), { state: 'idle' })

const room = ref('Room 0.7 s'), cue = ref('Natural'), ear = ref('Both'), level = ref('0 dB')
const opts = (l: string[]) => l.map((x) => ({ value: x, label: x }))
const status = 'No round yet.'
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
      <span v-if="props.state === 'idle'" class="text-small text-debug">Need to calibrate RT60 before running trials.</span>
      <span v-if="props.state === 'headset'" class="text-small text-text2">RT60 calibrated on this headset only: import it before the study.</span>
    </div>

    <template #toolbar>
      <UiButton label="Measure RT60" />
      <UiButton label="Start" button-style="prominent" />
    </template>
  </UiPanel>
</template>
