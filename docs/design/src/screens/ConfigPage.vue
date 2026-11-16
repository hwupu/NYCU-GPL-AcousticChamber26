<script setup lang="ts">
/**
 * The study chambers' Config page (Debug mode; Unity: DebugBench.OpenDebugPage). Not a UiKit component: the shared screen the
 * chamber screens (C05, C06) fill with their sections (Unity: ChamberDefinition DebugSections, between Parameters and Data).
 * Each chamber section is UiColumn.Label + UiColumn.Buttons: a raised group of equal-width borderless buttons; UiKit.Select marks
 * the state (selected fill). Rule (PO 2026-11-17): one group = one choice or one toggle; an action that is not part of the choice
 * (Measure RT60) gets its own group.
 * In Unity the body scrolls (UiColumn); the preview shows the full height.
 */
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import UiSection from '@/components/UiSection/UiSection.vue'
import UiRow from '@/components/UiRow/UiRow.vue'
import UiButton from '@/components/UiButton/UiButton.vue'

export interface ConfigGroup { label?: string; buttons: string[]; selected?: string[] }

defineProps<{ groups: ConfigGroup[]; status?: string; warning?: string }>()
</script>

<template>
  <UiPanel :width="1000" height="auto">
    <template #navigationBar>
      <UiNavigationBar title="Config" debug-tag />
    </template>

    <div class="flex flex-col">
      <span class="label">Run</span>
      <UiSection class="group">
        <div class="flex gap-8">
          <UiButton label="Run experiment trials" button-style="prominent" class="flex-1" :min-width="false" />
          <UiButton label="Practice trial" button-style="borderless" in-group class="flex-1" :min-width="false" />
        </div>
      </UiSection>

      <span class="label">Parameters</span>
      <UiSection class="group">
        <UiRow interactive title="Playground" subtitle="Parameters of the Playground runs" value="Default" icon-trailing="chevron-right" />
        <UiRow interactive title="Presets" subtitle="real sessions use the active one" value="Active: Default" icon-trailing="chevron-right" />
      </UiSection>

      <template v-for="(g, i) in groups" :key="i">
        <span v-if="g.label" class="label">{{ g.label }}</span>
        <UiSection class="group">
          <div class="flex gap-8">
            <UiButton v-for="b in g.buttons" :key="b" :label="b" button-style="borderless" in-group :min-width="false"
                      :selected="g.selected?.includes(b)" class="flex-1" />
          </div>
        </UiSection>
      </template>

      <span class="label">Data</span>
      <UiSection class="group">
        <UiRow interactive title="Log files" subtitle="this chamber's CSV and session files" value="3 files" icon-trailing="chevron-right" />
      </UiSection>
    </div>

    <template #statusBar>
      <span v-if="warning" class="text-debug">{{ warning }}</span>
      <span v-else>{{ status }}</span>
    </template>
  </UiPanel>
</template>

<style scoped>
.label { padding: 0 16px 8px; font-size: var(--text-small); color: var(--color-text2); }
.group { margin-bottom: 24px; }
</style>
