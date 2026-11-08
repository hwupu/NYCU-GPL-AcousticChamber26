<script setup lang="ts">
/** Documentation: the three panel kinds, and a stack (a dialog on top pushes the window back). */
import { ref } from 'vue'
import UiPanel from './UiPanel.vue'
import UiDialog from '../UiDialog/UiDialog.vue'
import UiNavigationBar from '../UiNavigationBar/UiNavigationBar.vue'
import UiButton from '../UiButton/UiButton.vue'
import UiSection from '../UiSection/UiSection.vue'
import UiRow from '../UiRow/UiRow.vue'
import UiList from '../UiList/UiList.vue'
import UiChoiceGroup from '../UiChoiceGroup/UiChoiceGroup.vue'
import UiNumberPad from '../UiNumberPad/UiNumberPad.vue'

const dialog = ref(false), shown = ref(true), digits = ref(''), ear = ref('both'), level = ref('0')
</script>

<template>
  <div class="flex flex-col gap-96 bg-black p-48">
    <section>
      <h3 class="mb-16 text-small font-bold text-text2">Fixed window (Main Hall, onboarding): no grabber; here with a bottom toolbar (slide style)</h3>
      <UiPanel :height="520">
        <template #navigationBar><UiNavigationBar title="Recenter" leading="none" /></template>
        <div class="flex flex-1 items-center justify-center text-body text-text2">Slide content</div>
        <template #toolbar>
          <UiButton label="Language" icon-leading="arrow-left" button-style="borderless" />
          <UiButton label="Slide 2" icon-trailing="arrow-right" button-style="prominent" />
        </template>
      </UiPanel>
    </section>

    <section>
      <h3 class="mb-16 text-small font-bold text-text2">Chamber menu (default window, movable): grabber below the surface, status line at the bottom</h3>
      <UiPanel movable>
        <template #navigationBar><UiNavigationBar title="C04 · HumPerHW2" debug-tag /></template>
        <div class="flex flex-1 items-start gap-48">
          <UiNumberPad v-model="digits" />
          <div class="flex flex-1 flex-col gap-20">
            <UiButton label="Start" button-style="prominent" class="w-full" />
            <UiButton label="Config" icon-leading="gear" class="w-full" />
          </div>
        </div>
        <template #statusBar>Audio level: 10 (target 8) · Unfinished: S0005, S0012</template>
      </UiPanel>
      <div class="h-120" />
    </section>

    <section>
      <h3 class="mb-16 text-small font-bold text-text2">Experience chamber menu (movable, no status bar): sections and a choice row</h3>
      <UiPanel movable :height="760">
        <template #navigationBar><UiNavigationBar title="C01 · MovingSource" /></template>
        <UiList>
          <UiSection header="HEARING">
            <UiChoiceGroup v-model="ear" :options="[{ value: 'both', label: 'Both' }, { value: 'left', label: 'Left' }, { value: 'right', label: 'Right' }]" />
          </UiSection>
          <UiSection header="LEVEL">
            <UiChoiceGroup v-model="level" :options="[{ value: '0', label: '0 dB' }, { value: '20', label: '-20 dB' }, { value: '40', label: '-40 dB' }]" />
          </UiSection>
          <UiSection header="PATH">
            <UiRow interactive title="Circle" subtitle="Around the head at 2 m" icon-leading="arrow-clockwise" selected />
            <UiRow interactive title="Fly-by" subtitle="Straight past, tangent to 0° azimuth" icon-leading="arrow-right" />
            <UiRow interactive title="Approach" subtitle="From far to near" icon-leading="arrow-down" />
          </UiSection>
        </UiList>
      </UiPanel>
      <div class="h-120" />
    </section>

    <section>
      <h3 class="mb-16 text-small font-bold text-text2">Dialog (confirmation) on top of a window: the window is pushed back (94 %, dimmed) and loses its grabber. Toggle it.</h3>
      <div class="mb-16 flex gap-20">
        <UiButton :label="dialog ? 'Close dialog' : 'Open dialog'" @click="dialog = !dialog" />
        <UiButton :label="shown ? 'Hide window' : 'Show window'" @click="shown = !shown" />
      </div>
      <div class="relative h-[900px]">
        <div class="absolute left-0 top-0"><UiPanel movable :shown="shown" :depth="dialog ? 1 : 0" :height="640">
          <template #navigationBar><UiNavigationBar title="Session" leading="none" /></template>
          <div class="flex flex-1 items-center justify-center text-body text-text2">Experiment running…</div>
        </UiPanel></div>
        <div class="absolute left-90 top-140">
          <UiDialog title="Drop out S0012?" message="The session is saved as dropped out." :shown="dialog" :depth="0">
            <template #actions>
              <UiButton label="Cancel" button-style="borderless" @click="dialog = false" />
              <UiButton label="Drop out" button-style="destructive" @click="dialog = false" />
            </template>
          </UiDialog>
        </div>
      </div>
    </section>
  </div>
</template>
