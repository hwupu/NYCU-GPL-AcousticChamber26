<script setup lang="ts">
/**
 * UiSurface (Unity: the body of Window / Sheet: the rounded rectangle with the background). Radius 32 u, `surface` fill.
 * Padding is kept: content sits 48 u from the left and right edges (groups align with the header edges); the navigation bar has its own 36 u top.
 * Slots, top to bottom:
 *   navigationBar  UiNavigationBar (optional; participant screens have none)
 *   default        the body: UiList, UiSection... (fills the rest, scrolls inside UiList)
 *   statusBar      one footer line (optional): "Audio level: 10 (target 8)", Unfinished...
 *   toolbar        bottom bar (optional): actions, e.g. Back left, Next right (slides)
 * A surface knows its size only (`width` x `height` in u); position, motion and the grabber belong to UiPanel.
 */
withDefaults(defineProps<{ width?: number; height?: number | 'auto' }>(), { width: 1000, height: 700 })
</script>

<template>
  <div class="ui-surface" :style="{ width: width + 'px', height: height === 'auto' ? undefined : height + 'px' }">
    <div v-if="$slots.navigationBar" class="slot-nav"><slot name="navigationBar" /></div>
    <div class="slot-body"><slot /></div>
    <div v-if="$slots.statusBar" class="slot-status"><slot name="statusBar" /></div>
    <div v-if="$slots.toolbar" class="slot-toolbar"><slot name="toolbar" /></div>
  </div>
</template>

<style scoped>
.ui-surface {
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  border-radius: var(--radius-panel);
  background: var(--color-surface);
  color: var(--color-text);
  overflow: hidden;
}
.slot-nav { flex: none; padding: 0 48px; }
.slot-nav + .slot-body { padding-top: 12px; }
.slot-body { display: flex; flex: 1; flex-direction: column; min-height: 0; padding: 0 48px; }
.slot-status { flex: none; padding: 12px 48px 0; font-size: var(--text-small); line-height: 1.2; color: var(--color-text2); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.slot-toolbar { display: flex; flex: none; align-items: center; justify-content: space-between; gap: var(--gap); box-sizing: border-box; height: 120px; padding: 0 48px 40px; } /* mirrors the navigation bar: 80 u row, 40 u from the edge, 48 u at the sides */
.slot-status:last-child { padding-bottom: 28px; }
.slot-body:last-child { padding-bottom: 40px; } /* no status bar, no toolbar: same 40 u from the bottom edge as the toolbar */
</style>
