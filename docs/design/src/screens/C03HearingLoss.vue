<script setup lang="ts">
/**
 * C03 · Hearing Loss experience panel (Unity: Chambers/HearingLoss HearingLossExperience; PO 2026-11-10).
 * 1100 x 900 u, movable. Root page: hearing condition, hearing loss, the sounds (GPL.Stimuli only) in a 4-column grid (no status text);
 * toolbar: Reset speaker (left), Config (right, settings-horizontal) -> pushes the Hearing loss test page
 * (Unity: App HearingLossTestView, the content of the Sandbox Hearing Loss Test Panel; here its tones come from the speaker).
 */
import { ref } from 'vue'
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import UiChoiceGroup from '@/components/UiChoiceGroup/UiChoiceGroup.vue'
import UiButton from '@/components/UiButton/UiButton.vue'

const props = withDefaults(defineProps<{ page?: 'root' | 'config' }>(), { page: 'root' })
const page = ref(props.page)

const ear = ref('Both ears'), level = ref('-20 dB'), sound = ref('Pink noise')
const ears = ['Both ears', 'Left ear', 'Right ear'].map((l) => ({ value: l, label: l }))
const levels = ['0 dB', '-20 dB', '-30 dB', '-40 dB'].map((l) => ({ value: l, label: l }))
const sounds = ['White noise', 'Pink noise', 'Horn', 'Horn ×2', 'Choked horn', 'Pap horn', 'Pistol', 'Opera voice', 'Trumpet'].map((l) => ({ value: l, label: l }))

const tEar = ref('Left ear'), tLevel = ref('-40 dB'), tone = ref('4 kHz')
const tLevels = ['0 dB', '-20 dB', '-30 dB', '-40 dB', '-60 dB', '-80 dB'].map((l) => ({ value: l, label: l }))
const tones = ['500 Hz', '1 kHz', '2 kHz', '4 kHz', '8 kHz'].map((l) => ({ value: l, label: l }))
</script>

<template>
  <UiPanel movable :width="1100" :height="900">
    <template #navigationBar>
      <UiNavigationBar v-if="page === 'root'" title="C03 · Hearing Loss" />
      <UiNavigationBar v-else title="Hearing loss test" @back="page = 'root'" />
    </template>

    <div v-if="page === 'root'" class="flex flex-col gap-12">
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Ear</span>
        <UiChoiceGroup v-model="ear" class="flex-1" :options="ears" />
      </div>
      <div class="flex items-center gap-20">
        <span class="w-160 flex-none text-body text-text2">Loss</span>
        <UiChoiceGroup v-model="level" class="flex-1" :options="levels" />
      </div>
      <span class="text-body text-text2">Sounds</span>
      <UiChoiceGroup v-model="sound" :columns="4" :options="sounds" />
    </div>

    <div v-else class="flex flex-col gap-12">
      <span class="text-small text-text2">Hearing condition (loss on)</span>
      <UiChoiceGroup v-model="tEar" :options="ears" />
      <span class="text-small text-text2">Hearing loss (NIOSH notch, label = 4 kHz band)</span>
      <UiChoiceGroup v-model="tLevel" :options="tLevels" />
      <span class="text-small text-text2">Pure tone (from the speaker, -20 dBFS peak, 1.5 s)</span>
      <UiChoiceGroup v-model="tone" :options="tones" />
      <pre class="m-0 whitespace-pre-wrap font-[inherit] text-small text-text">4 kHz tone (playing)
Left ear   in -23.0 dBFS   out -63.0 dBFS   change -40.0 dB   expected -40.0 dB
Right ear  in -23.0 dBFS   out -23.0 dBFS   change 0.0 dB   expected 0.0 dB</pre>
    </div>

    <template v-if="page === 'root'" #toolbar>
      <UiButton label="Reset speaker" />
      <UiButton label="Config" icon-leading="settings-horizontal" @click="page = 'config'" />
    </template>
  </UiPanel>
</template>
