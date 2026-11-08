<script setup lang="ts">
/**
 * UiGrabber (Unity: PanelMover's handle; visionOS window grabber). A rounded-rectangle pill 220 x 16 u below the surface, outside it
 * (the center is 56 u below the edge). The ray reacts in a much larger zone (520 x 150 u): hovering anywhere in it grows the pill
 * (x1.7) and brightens it (45 % -> 95 %) with the settle curve; held = same as hover. It belongs to the UiPanel, so it hides,
 * dims and pushes back with it. Drag: swing = turn around the head, hand away / toward = farther / nearer (1:1 in distance).
 */
defineProps<{ state?: 'idle' | 'hover' | 'held' }>()
</script>

<template>
  <div class="ui-grabber" :data-state="state ?? 'idle'">
    <div class="pill" />
  </div>
</template>

<style scoped>
.ui-grabber { position: absolute; left: 50%; display: flex; align-items: center; justify-content: center; width: 520px; height: 150px; transform: translateX(-50%); cursor: grab; }
.pill {
  width: 220px; height: 16px; border-radius: var(--radius-capsule);
  background: var(--color-text); opacity: 0.45;
  transition: transform var(--hover-out) var(--ease-settle), opacity var(--hover-out) var(--ease-settle);
}
.ui-grabber:hover .pill, .ui-grabber[data-state='hover'] .pill, .ui-grabber[data-state='held'] .pill {
  transform: scale(1.7); opacity: 0.95; transition-duration: var(--hover-in);
}
.ui-grabber:active { cursor: grabbing; }
</style>
