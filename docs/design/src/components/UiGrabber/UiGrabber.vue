<script setup lang="ts">
/**
 * UiGrabber (Unity: UiGrabber; visionOS window grabber). A rounded-rectangle pill below the surface, outside it: 120 x 8 u at rest,
 * 220 x 16 u on hover, its top edge 16 u below the surface (the top edge stays put, the pill grows downward). The ray reacts in a much
 * larger zone (520 x 120 u, starting at the surface edge): hovering anywhere in it grows the pill and brightens it (45 % -> 95 %)
 * with the settle curve; held = same as hover. It belongs to the UiPanel, so it hides, dims and pushes back with it.
 * Drag: swing = turn around the head (yaw, pitch). There is no distance move: the canvas scale follows the distance, so a different
 * distance would look the same.
 */
defineProps<{ state?: 'idle' | 'hover' | 'held' }>()
</script>

<template>
  <div class="ui-grabber" :data-state="state ?? 'idle'">
    <div class="pill" />
  </div>
</template>

<style scoped>
.ui-grabber { position: absolute; left: 50%; display: flex; align-items: flex-start; justify-content: center; box-sizing: border-box; width: 520px; height: 120px; padding-top: 16px; transform: translateX(-50%); cursor: grab; }
.pill {
  width: 120px; height: 8px; border-radius: var(--radius-capsule);
  background: var(--color-text); opacity: 0.45;
  transition: width var(--hover-out) var(--ease-settle), height var(--hover-out) var(--ease-settle), opacity var(--hover-out) var(--ease-settle);
}
.ui-grabber:hover .pill, .ui-grabber[data-state='hover'] .pill, .ui-grabber[data-state='held'] .pill {
  width: 220px; height: 16px; opacity: 0.95; transition-duration: var(--hover-in);
}
.ui-grabber:active { cursor: grabbing; }
</style>
