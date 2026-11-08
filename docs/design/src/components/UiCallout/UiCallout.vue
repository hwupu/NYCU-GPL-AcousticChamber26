<script setup lang="ts">
/**
 * UiCallout (new; NOT part of an overlay panel). A small annotation attached to a 3D object in the scene: "this is draggable",
 * "drag me to the floor". A flat `gray-pressed` pill (readable on a dark scene) with an icon and an optional label, and a tail pointing at the target.
 * In Unity it is a world-space object (not in the OVR overlay canvas, which clips): it follows a transform, faces the head, scales
 * with distance (1 u = 0.057 deg), and fades with the settle curve. It shares the UiKit tokens, UiIcon and motion.
 *
 *  placement  where the pill sits relative to the target: top | bottom | left | right (the tail points back at the target)
 *  `delay`    ms before a fade-in starts (e.g. after the scene loaded); fade out when the user touches the object. No periodic animation: a simple settle fade only
 * The target is the thing around it; this component is only the pill and its tail (the anchor is the tail's tip).
 */
import UiIcon from '../UiIcon/UiIcon.vue'

withDefaults(defineProps<{ label?: string; icon?: string; placement?: 'top' | 'bottom' | 'left' | 'right'; delay?: number; shown?: boolean }>(), {
  placement: 'top', icon: 'hand', shown: true,
})
</script>

<template>
  <div class="ui-callout" :data-placement="placement" :data-shown="shown ? '' : undefined" :style="{ '--delay': (delay ?? 0) + 'ms' }">
    <div class="pill">
      <UiIcon :name="icon!" :size="36" />
      <span v-if="label" class="label">{{ label }}</span>
    </div>
    <div class="tail" />
  </div>
</template>


<style scoped>
.ui-callout {
  display: inline-flex;
  align-items: center;
  opacity: 0;
  transition: opacity var(--sheet) var(--ease-settle);
}
.ui-callout[data-shown] { opacity: 1; transition-delay: var(--delay, 0ms); } /* delay applies to fade-in only */
.ui-callout[data-placement='top'] { flex-direction: column; }
.ui-callout[data-placement='bottom'] { flex-direction: column-reverse; }
.ui-callout[data-placement='left'] { flex-direction: row; }
.ui-callout[data-placement='right'] { flex-direction: row-reverse; }
.pill {
  display: flex; align-items: center; gap: 12px; height: 72px; box-sizing: border-box; padding: 0 28px;
  border-radius: var(--radius-capsule); background: var(--color-gray-pressed); color: var(--color-text);
}
.label { font-size: var(--text-small); line-height: 1; white-space: nowrap; transform: translateY(var(--label-nudge)); }
.tail { width: 0; height: 0; border: 14px solid transparent; }
.ui-callout[data-placement='top'] .tail { border-top-color: var(--color-gray-pressed); border-bottom-width: 0; }
.ui-callout[data-placement='bottom'] .tail { border-bottom-color: var(--color-gray-pressed); border-top-width: 0; }
.ui-callout[data-placement='left'] .tail { border-left-color: var(--color-gray-pressed); border-right-width: 0; }
.ui-callout[data-placement='right'] .tail { border-right-color: var(--color-gray-pressed); border-left-width: 0; }
</style>
