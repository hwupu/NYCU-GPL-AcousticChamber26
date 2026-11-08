<script setup lang="ts">
/**
 * UiNavigationBar (Unity: the PageStack / Page header; SwiftUI NavigationStack bar). 80 u row below 36 u top padding (40 less 4 u: the font sits low) (116 u in all).
 *   [back]  title  ............  [DEBUG]  [actions slot: Revert, Delete all...]
 * The back button is a borderless icon button, 8 u from the title. Back and close never appear together: `leading` picks `back`
 * (chevron-left) or `close` (cross); a sheet that may be left without a choice uses close. Participant screens have no bar.
 */
import UiButton from '../UiButton/UiButton.vue'

/** `centered`: the title takes the free width and is centered across the bar (bars without a leading button: the protocol slides). */
withDefaults(defineProps<{ title: string; leading?: 'back' | 'close' | 'none'; debugTag?: boolean; centered?: boolean }>(), { leading: 'back' })
defineEmits<{ back: [] }>()
</script>

<template>
  <header class="ui-navigation-bar">
    <UiButton v-if="leading !== 'none'" :icon-leading="leading === 'back' ? 'chevron-left' : 'cross'" button-style="borderless" @click="$emit('back')" />
    <h2 class="title" :data-centered="centered ? '' : undefined">{{ title }}</h2>
    <span v-if="!centered" class="flex-1" />
    <span v-if="debugTag" class="debug-tag">DEBUG</span>
    <div class="actions"><slot /></div>
  </header>
</template>

<style scoped>
.ui-navigation-bar { display: flex; align-items: center; gap: 8px; box-sizing: border-box; height: 116px; padding-top: 36px; }
.title { margin: 0; font-size: var(--text-title); font-weight: var(--font-weight-bold); line-height: 1; transform: translateY(var(--label-nudge)); white-space: nowrap; }
.title[data-centered] { flex: 1; text-align: center; }
.debug-tag { margin-right: var(--gap); font-size: var(--text-minimum); font-weight: var(--font-weight-bold); letter-spacing: 0.12em; color: var(--color-debug); transform: translateY(var(--label-nudge)); }
.actions { display: flex; gap: 8px; }
</style>
