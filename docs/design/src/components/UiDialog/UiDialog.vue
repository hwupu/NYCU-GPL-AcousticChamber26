<script setup lang="ts">
/**
 * UiDialog (Unity: ModalDialog, StopDialog). A decision: a UiPanel of kind `dialog` (820 u, centered, no grabber, no close button: it has
 * Cancel). Title and message left-aligned; actions right-aligned, sized to their labels, the main action LAST (rightmost). The `actions`
 * slot takes UiButtons: Cancel borderless, the main action prominent, Drop out destructiveText or destructive (final confirmation).
 * Default slot (PB-27): content between the message and the actions, e.g. a UiProgressBar or result lines (Unity: UiDialog.Content).
 */
import UiPanel from '../UiPanel/UiPanel.vue'

// withDefaults: Vue casts a missing boolean prop to false, so `shown ?? true` in the template never applied.
withDefaults(defineProps<{ title: string; message?: string; shown?: boolean; depth?: number }>(), { shown: true, depth: 0 })
</script>

<template>
  <UiPanel kind="dialog" :shown="shown" :depth="depth">
    <div class="body">
      <h2 class="title">{{ title }}</h2>
      <p v-if="message" class="message">{{ message }}</p>
      <div v-if="$slots.default" class="content"><slot /></div>
      <div class="actions"><slot name="actions" /></div>
    </div>
  </UiPanel>
</template>

<style scoped>
.body { display: flex; flex-direction: column; padding: 48px 0 28px; }
.title { margin: 0; font-size: var(--text-heading); font-weight: var(--font-weight-bold); line-height: 1.2; }
.message { margin: 16px 0 0; font-size: var(--text-body); line-height: 1.3; color: var(--color-text2); }
.content { margin: 32px 0 0; }
.actions { display: flex; justify-content: flex-end; gap: 8px; margin: 40px -20px 0 0; }
</style>
