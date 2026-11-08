<script setup lang="ts">
/** Documentation matrix: every style in every state (states are forced; the real pointer works too). */
import UiButton, { type UiButtonStyle } from './UiButton.vue'

interface Row { name: string; style: UiButtonStyle; inGroup?: boolean; note: string }
const rows: Row[] = [
  { name: 'prominent (white)', style: 'prominent', note: 'the one main action; no selected state' },
  { name: 'gray', style: 'gray', note: 'secondary action with a visible target' },
  { name: 'borderless', style: 'borderless', note: 'back, cancel, keys, list actions' },
  { name: 'borderless, in group', style: 'borderless', inGroup: true, note: 'on a raised group: lighter hover fill' },
  { name: 'destructiveText', style: 'destructiveText', note: 'red label, no fill: destructive, not the final step' },
  { name: 'destructive (red)', style: 'destructive', note: 'red fill: the final confirmation only; no selected state' },
]
const columns = [
  { name: 'idle', props: {} },
  { name: 'hover', props: { state: 'hover' } },
  { name: 'pressed', props: { state: 'pressed' } },
  { name: 'selected', props: { selected: true } },
  { name: 'selected + hover', props: { selected: true, state: 'hover' } },
  { name: 'disabled', props: { disabled: true } },
] as const
</script>

<template>
  <table class="border-separate border-spacing-x-12 border-spacing-y-8 text-small">
    <thead class="text-left text-minimum text-text2">
      <tr><th>Style</th><th v-for="c in columns" :key="c.name">{{ c.name }}</th></tr>
    </thead>
    <tbody>
      <tr v-for="r in rows" :key="r.name">
        <td class="min-w-300 align-middle">
          <div class="font-bold">{{ r.name }}</div>
          <div class="text-minimum text-text2">{{ r.note }}</div>
        </td>
        <td v-for="c in columns" :key="c.name" class="rounded-group p-12" :class="r.inGroup ? 'bg-raised' : 'bg-surface'">
          <UiButton :label="c.name === 'selected + hover' ? 'selected' : c.name" :button-style="r.style" :in-group="r.inGroup" v-bind="c.props" />
        </td>
      </tr>
    </tbody>
  </table>
</template>
