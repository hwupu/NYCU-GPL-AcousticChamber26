<script setup lang="ts">
/**
 * UiSection (Unity: UiKit.Group + group label; SwiftUI Section). A `raised` rounded container (radius 36) with 8 u padding to its rows;
 * rows sit 4 u apart. The header is a 24 u `text2` label above the group, the footer a `small` `text2` note below.
 * Rows inside use radius 28 = 36 - 8 (concentric corners).
 */
defineProps<{ header?: string; footer?: string }>()
</script>

<template>
  <section class="ui-section">
    <h3 v-if="header || $slots.header" class="ui-section-header"><slot name="header">{{ header }}</slot></h3>
    <div class="ui-section-group"><slot /></div>
    <p v-if="footer || $slots.footer" class="ui-section-footer"><slot name="footer">{{ footer }}</slot></p>
  </section>
</template>

<style scoped>
.ui-section { display: flex; flex-direction: column; }
.ui-section-header { margin: 0 0 8px 24px; font-size: var(--text-minimum); font-weight: var(--font-weight-regular); line-height: 1.2; color: var(--color-text2); }
.ui-section-group {
  display: flex;
  flex-direction: column;
  gap: var(--inset);
  padding: var(--group-padding);
  border-radius: var(--radius-group);
  background: var(--color-raised);
}
.ui-section-footer { margin: 8px 24px 0; font-size: var(--text-small); line-height: 1.2; color: var(--color-text2); }
</style>
