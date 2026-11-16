<script setup lang="ts">
/**
 * C05 · HumPerHW2, Config page (Debug mode; PB-08e, PB-26, PB-27; ported to the Storybook in PB-28. Unity:
 * HumPerHW2Chamber.DebugSections on the shared Config page). Playground values; nothing here writes a log row (Q44).
 *  Acoustics     0.3 s Quiet · 0.7 s Noisy · 2.2 s Reverb · Meta preset (Concrete, about 4 s): one choice, the room in use =
 *                selected. Noisy and Reverb start the background.
 *  RT60          Measure RT60: its own group since PB-28 (was in the room row); the three conditions, GPL.Acoustics ISO protocol,
 *                in the RT60 run dialog; selected while it runs.
 *  Playback      Play stimulus (pulse train ahead at 2 m with the picture) · Background: each selected while it plays (a paused
 *                background is off).
 *  Sound sphere  After confirm (default) · Always visible: Debug-only display switch (Q45).
 * Status line: the target, coefficient and calculated RT60, or the T_mid result; the RT60 warning (debug color) while no table for
 * this room and device exists. Leaving Config stops the sounds and returns the room to Quiet (Q47).
 */
import ConfigPage, { type ConfigGroup } from './ConfigPage.vue'

const props = withDefaults(defineProps<{ state?: 'idle' | 'reverb' | 'uncalibrated' }>(), { state: 'idle' })
const reverb = props.state === 'reverb'

const groups: ConfigGroup[] = [
  { label: 'Acoustics', buttons: ['0.3 s Quiet', '0.7 s Noisy', '2.2 s Reverb', 'Meta preset'], selected: [reverb ? '2.2 s Reverb' : '0.3 s Quiet'] },
  { label: 'RT60', buttons: ['Measure RT60'] },
  { label: 'Playback', buttons: ['Play stimulus', 'Background'], selected: reverb ? ['Play stimulus', 'Background'] : [] },
  { label: 'Sound sphere', buttons: ['After confirm', 'Always visible'], selected: [reverb ? 'Always visible' : 'After confirm'] },
]
const status = reverb ? 'Reverb: RT60 2.20 s (calibrated, R 0.912), background on (-21.4 dBFS)' : '' // format of HumPerHW2Chamber.DebugCondition; numbers illustrative
const warning = props.state === 'uncalibrated' ? 'Need to calibrate RT60 before running trials.' : undefined
</script>

<template>
  <ConfigPage :groups="groups" :status="status" :warning="warning" />
</template>
