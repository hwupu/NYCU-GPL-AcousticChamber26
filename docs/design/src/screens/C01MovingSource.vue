<script setup lang="ts">
/**
 * C01 · Moving Source experience panel (Unity: Chambers/MovingSource MovingSourceExperience). 1100 x 1040 u, movable.
 * Rows: Sound (generated sounds only; the HW2 recordings were removed, PB-29), Room, Distance, Hearing (ear + level), Path (+ Stop), Speed; Status (text2)
 * and, under it, the RT60 warning (debug color) while the room is not calibrated on this device (PB-26, PO 2026-11-15).
 * Toolbar: Reset source (left); Measure RT60 (right) = the selected room, 4 positions x 3 repeats; the sound and the path stop while
 * it runs. Progress and result are shown in the RT60 run dialog (Screens/RT60 Run Dialog), not in the status (PB-27).
 * Calibrate moved to C04 Reverberation (PB-27); warning text PB-27.
 */
import { ref } from 'vue'
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import UiChoiceGroup from '@/components/UiChoiceGroup/UiChoiceGroup.vue'
import UiButton from '@/components/UiButton/UiButton.vue'

const props = withDefaults(defineProps<{ state?: 'idle' | 'calibrated' | 'headset' }>(), { state: 'idle' })

const sound = ref(''), room = ref('Hall 2.2 s'), cue = ref('Natural'), ear = ref('Both'), level = ref('0 dB'), path = ref(''), speed = ref('1 ×')
const opts = (l: string[]) => l.map((x) => ({ value: x, label: x }))
const status = 'Click a sound to play it; click it again to pause.\nPoint at the source and press the grip to move it.  Hearing: Normal.'
</script>

<template>
  <UiPanel movable :width="1100" :height="1040">
    <template #navigationBar>
      <UiNavigationBar title="C01 · Moving Source" />
    </template>

    <div class="flex flex-col gap-12">
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Sound</span>
        <UiChoiceGroup v-model="sound" class="flex-1" :options="opts(['White noise', 'Pink noise'])" />
      </div>
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
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Path</span>
        <UiChoiceGroup v-model="path" :options="opts(['Circle', 'Fly-by', 'Approach'])" />
        <UiButton label="Stop" :min-width="false" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Speed</span>
        <UiChoiceGroup v-model="speed" class="flex-1" :options="opts(['0.5 ×', '1 ×', '2 ×'])" />
      </div>
      <span class="whitespace-pre-line text-small text-text2">{{ status }}</span>
      <span v-if="props.state === 'idle'" class="text-small text-debug">Need to calibrate RT60 before running trials.</span>
      <span v-if="props.state === 'headset'" class="text-small text-text2">RT60 calibrated on this headset only: import it before the study.</span>
    </div>

    <template #toolbar>
      <UiButton label="Reset source" />
      <UiButton label="Measure RT60" />
    </template>
  </UiPanel>
</template>
