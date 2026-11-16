<script setup lang="ts">
/**
 * C06 · HumPerFinal, Config page (Debug mode; PB-28. Unity: HumPerFinalChamber.DebugSections on the shared Config page).
 * Chamber sections (Playground values; nothing here writes a log row):
 *  Acoustics     Room 0.7 s (the study's only room) · Meta preset: one choice, the room in use = selected.
 *  RT60          Measure RT60: its own group (an action, not a room choice); opens the RT60 run dialog, selected while it runs.
 *  Hearing       Normal · L −40 · R −40 · L −80 · R −80: one selected; applies GPL.HearingLoss at once (−40 = HLSim Severe,
 *                −80 = Severe × 2, the label is the 4 kHz band).
 *  Anchors       Off · On: On shows the duck (0°) and trumpet (+90°) at 2 m and plays their loop (duck, 1.5 s, trumpet, 2.5 s).
 *  Playback      Play stimulus: the target loop (2 s on, 0.7 s off) with the radio ahead at 2 m; selected while it plays.
 *  Sound sphere  After confirm · Always visible (as C05, Debug only).
 * Status line: what is applied, e.g. "Hearing L −80 (Severe × 2) · Anchors on · RT60 0.7 s". The RT60 warning (debug color)
 * while the room is not calibrated on this device (PB-27 text).
 * Leaving Config (‹) stops the sounds, hides the anchors and returns to normal hearing.
 */
import ConfigPage, { type ConfigGroup } from './ConfigPage.vue'

const props = withDefaults(defineProps<{ state?: 'idle' | 'testing' | 'uncalibrated' }>(), { state: 'idle' })
const testing = props.state === 'testing'

const groups: ConfigGroup[] = [
  { label: 'Acoustics', buttons: ['Room 0.7 s', 'Meta preset'], selected: ['Room 0.7 s'] },
  { label: 'RT60', buttons: ['Measure RT60'] },
  { label: 'Hearing', buttons: ['Normal', 'L −40', 'R −40', 'L −80', 'R −80'], selected: [testing ? 'L −80' : 'Normal'] },
  { label: 'Anchors', buttons: ['Off', 'On'], selected: [testing ? 'On' : 'Off'] },
  { label: 'Playback', buttons: ['Play stimulus'], selected: testing ? ['Play stimulus'] : [] },
  { label: 'Sound sphere', buttons: ['After confirm', 'Always visible'], selected: ['After confirm'] },
]
const status = testing ? 'Hearing L −80 (Severe × 2), anchors on, RT60 0.70 s (calibrated, R 0.781)' : '' // numbers illustrative
const warning = props.state === 'uncalibrated' ? 'Need to calibrate RT60 before running trials.' : undefined
</script>

<template>
  <ConfigPage :groups="groups" :status="status" :warning="warning" />
</template>
