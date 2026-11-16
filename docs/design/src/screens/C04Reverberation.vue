<script setup lang="ts">
/**
 * C04 · Reverberation (PB-27; Unity: Chambers/Reverberation ReverberationExperience). A calibration tool built as an experience
 * chamber (ADR-0006): no Study ID, no participant data; it writes RT60 reports only. 1100 x 1040 u, movable. Open space, the shoebox
 * locked to the listener with the size set here.
 * Rows:
 *  Room     presets of the rooms the chambers use ("Open space" 12 × 12 × 3.5 m: C01 / C02; "HW2 room" 8.72 × 15 × 3.5 m: C05) + Custom; changing a
 *           stepper selects Custom, a stepper value equal to a preset selects that preset.
 *  Width / Depth / Height   UiStepper, 0.1 m steps; W and D 2-30 m, H 2-10 m.
 *  Profile  targets (T_mid) checked by Measure and the sweep range of Calibrate (PO 2026-11-16):
 *           Experience 0.7, 2.2 s (full grid) · HW2 0.3, 0.7, 2.2 s (full grid) · Short 0.3-1.0 s · Long 1.5-4.0 s;
 *           the line under it shows the targets, the sweep points and the time.
 *  (status) none: C04 makes the CSV and shows neither a calibrated line nor the uncalibrated warning (PO 2026-11-17); the import hint.
 * Placement: centered ahead (yaw 0°, 8° below the eye line; PO 2026-11-17), not 35° left like C01 / C02.
 * Toolbar: Measure RT60 (gray, left) = the profile's targets, 4 positions x 3 repeats; Calibrate (prominent, right) = the sweep.
 * Both open the RT60 run dialog (Screens/RT60 Run Dialog).
 */
import { ref, watch } from 'vue'
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import UiChoiceGroup from '@/components/UiChoiceGroup/UiChoiceGroup.vue'
import UiButton from '@/components/UiButton/UiButton.vue'
import UiStepper from '@/components/UiStepper/UiStepper.vue'

const props = withDefaults(defineProps<{ state?: 'open' | 'hw2' | 'custom' }>(), { state: 'open' })

const presets: Record<string, [number, number, number]> = { 'Open space': [12, 12, 3.5], 'HW2 room': [8.72, 15, 3.5] }
const start: [number, number, number] = props.state === 'custom' ? [6, 8, 3] : props.state === 'hw2' ? [8.72, 15, 3.5] : [12, 12, 3.5]
const w = ref(start[0]), d = ref(start[1]), hgt = ref(start[2])
const room = ref('')
const syncRoom = () => {
  room.value = Object.keys(presets).find((k) => presets[k][0] === w.value && presets[k][1] === d.value && presets[k][2] === hgt.value) ?? 'Custom'
}
syncRoom()
watch([w, d, hgt], syncRoom)
watch(room, (k) => { const p = presets[k]; if (p) [w.value, d.value, hgt.value] = p })

const profiles: Record<string, string> = {
  Experience: 'Targets 0.7, 2.2 s · sweep 21 points, about 6 min · measure about 3 min',
  HW2: 'Targets 0.3, 0.7, 2.2 s · sweep 21 points, about 6 min · measure about 4 min',
  Short: 'Targets 0.3, 0.5, 0.7, 1.0 s · sweep 9 points, about 2 min · measure about 3 min',
  Long: 'Targets 1.5, 2.2, 3.0, 4.0 s · sweep 10 points, about 4 min · measure about 8 min',
}
const profile = ref('Experience')
const opts = (l: string[]) => l.map((x) => ({ value: x, label: x }))
const m = (v: number) => `${+v.toFixed(2)} m`
</script>

<template>
  <UiPanel movable :width="1100" :height="1040">
    <template #navigationBar>
      <UiNavigationBar title="C04 · Reverberation" />
    </template>

    <div class="flex flex-col gap-12">
      <span class="text-small text-text2">Calibrate the reverberation of a room size on this headset. Stay still and quiet while it runs.</span>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Room</span>
        <UiChoiceGroup v-model="room" class="flex-1" :options="opts(['Open space', 'HW2 room', 'Custom'])" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Width</span>
        <UiStepper v-model="w" :min="2" :max="30" :step="0.1" :format="m" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Depth</span>
        <UiStepper v-model="d" :min="2" :max="30" :step="0.1" :format="m" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Height</span>
        <UiStepper v-model="hgt" :min="2" :max="10" :step="0.1" :format="m" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Profile</span>
        <UiChoiceGroup v-model="profile" class="flex-1" :options="opts(Object.keys(profiles))" />
      </div>
      <span class="pl-180 text-small text-text2">{{ profiles[profile] }}</span>
      <span class="text-small text-text2">After Calibrate: copy the report from Android/data/…/files/Calibration to the PC and import it in the Editor (Magic &gt; Acoustics &gt; Import RT60 Calibration).</span>
    </div>

    <template #toolbar>
      <UiButton label="Measure RT60" />
      <UiButton label="Calibrate" button-style="prominent" />
    </template>
  </UiPanel>
</template>
