<script setup lang="ts">
/**
 * RT60 run dialog (PB-27; Unity: App Rt60RunDialog over GPL.UI UiDialog + UiProgressBar). Opened by Measure RT60 (C01, C02, C04, C05)
 * and Calibrate (C04 only); replaces the progress and result lines in the status (PO 2026-11-16). The chamber panel behind is pushed
 * back (depth 1) and does not take clicks while the dialog is open.
 *  running     title "Measuring RT60" / "Calibrating RT60", the current step (text2), a UiProgressBar with "About … left"
 *              (from the plan's estimate, corrected by the recordings done so far); borderless Cancel.
 *  calibrated  "Calibration completed" + the report name to import in the Editor (main protocol); no results (PO); Close (borderless),
 *              Use on this headset (prominent): the other chambers use the table at once, without a rebuild, until a build has an
 *              imported table for that size (imported wins; PO 2026-11-17).
 *  inUse       after Use on this headset: "Used on this headset", for which size, and that it is not imported; Close.
 *  result      "RT60 measured": one line per condition (T_mid mean ± SD, deviation from the target, "flagged" = see the report;
 *              Dry: "no reverberation") and the report name (text2); Close.
 *  stopped     a run that did not complete (cancelled, refused, failed) leaves no report files (PO 2026-11-16): the reason; Close.
 *              A plain Cancel closes the dialog at once and does not show this state.
 */
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import UiDialog from '@/components/UiDialog/UiDialog.vue'
import UiButton from '@/components/UiButton/UiButton.vue'
import UiProgressBar from '@/components/UiProgressBar/UiProgressBar.vue'

type State = 'measuring' | 'calibrating' | 'calibrated' | 'inUse' | 'result' | 'stopped'
const props = withDefaults(defineProps<{ state?: State }>(), { state: 'measuring' })

const title = {
  measuring: 'Measuring RT60',
  calibrating: 'Calibrating RT60',
  calibrated: 'Calibration completed',
  inUse: 'Used on this headset',
  result: 'RT60 measured',
  stopped: 'RT60 not measured',
}[props.state]
const message = {
  measuring: 'Room 0.7 s, -90°, repeat 2 of 3. Stay still and quiet.',
  calibrating: 'Reflection 0.85 (14 of 21), repeat 1 of 3. Stay still and quiet.',
  calibrated: 'Import RT60_Reverberation_20261116-153012.csv in the Editor: Magic > Acoustics > Import RT60 Calibration.',
  inUse: 'The chambers with a 12 × 12 × 3.5 m room use this calibration on this headset now. Import RT60_Reverberation_20261116-153012.csv in the Editor before the study.',
  result: undefined,
  stopped: 'Another audio filter is on the listener: AudioReverbFilter. No report was written.',
}[props.state]
const running = props.state === 'measuring' || props.state === 'calibrating'
const progress = props.state === 'measuring' ? { value: 0.42, caption: 'About 55 s left' } : { value: 0.63, caption: 'About 2 min 20 s left' }
</script>

<template>
  <div class="relative h-[1100px]">
    <div class="absolute left-0 top-0">
      <UiPanel movable :depth="1" :height="960">
        <template #navigationBar><UiNavigationBar title="C02 · Hidden Source" /></template>
        <div class="flex flex-1 items-center justify-center text-body text-text2">Chamber panel</div>
      </UiPanel>
    </div>
    <div class="absolute left-90 top-160">
      <UiDialog :title="title" :message="message">
        <UiProgressBar v-if="running" :value="progress.value" :caption="progress.caption" />
        <pre v-else-if="props.state === 'result'" class="m-0 whitespace-pre-wrap font-[inherit] text-small text-text">Room 0.7 s    T_mid 0.69 ± 0.01 s   -1.4 %
Hall 2.2 s    T_mid 2.17 ± 0.03 s   -1.4 %
Short 0.3 s   T_mid 0.28 ± 0.02 s   -7.5 %   flagged
<span class="text-text2">Report: RT60_Reverberation_20261116-153012.txt</span></pre>
        <template #actions>
          <UiButton v-if="running" label="Cancel" button-style="borderless" />
          <template v-else-if="props.state === 'calibrated'">
            <UiButton label="Close" button-style="borderless" />
            <UiButton label="Use on this headset" button-style="prominent" />
          </template>
          <UiButton v-else label="Close" button-style="prominent" />
        </template>
      </UiDialog>
    </div>
  </div>
</template>
