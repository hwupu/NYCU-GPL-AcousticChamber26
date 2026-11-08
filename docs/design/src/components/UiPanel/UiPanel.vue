<script setup lang="ts">
/**
 * UiPanel (Unity: UiPanel over OverlayPanel). The TRANSPARENT ROOT of every panel: it owns placement, scale, fade, push back
 * and dim. Inside it: a UiSurface (the rounded rectangle with the background, all the slots) and, on movable windows, a UiGrabber
 * below it. Everything animates on this one root, so the grabber hides, dims and pushes back together with the surface.
 *
 *  kind     window (fixed: Main Hall, onboarding) | dialog (confirmation: Drop out; centered, 820 u, no grabber, height by content)
 *  movable  window only: shows the UiGrabber (experience chamber menu)

 *  placement  where the panel hangs in the room (Unity; here only a data attribute): `below` (default) = centre 8 deg below the eye line, `level` = 0 deg.
 *           The placement is lifted so the grabber's lowest point stays above the floor (clearance in the Unity placer, so the handle is never hidden below it).
 *  shown    false = scaled to `sheet-scale` and faded out (appear / leave in place; settle curve, 450 ms)
 *  depth    sheets on top of this panel: 0 front, 1 = scale 94 % + dim 45 %, 2 = 88 %... (the panel stack pushes it back; the dim covers the surface only)
 * Slots (passed to the surface): navigationBar, default, statusBar, toolbar.
 */
import { computed } from 'vue'
import UiSurface from '../UiSurface/UiSurface.vue'
import UiGrabber from '../UiGrabber/UiGrabber.vue'

const props = withDefaults(defineProps<{
  kind?: 'window' | 'dialog'
  movable?: boolean
  placement?: 'below' | 'level'
  shown?: boolean
  depth?: number
  width?: number
  height?: number | 'auto'
  grabberState?: 'idle' | 'hover' | 'held'
}>(), { kind: 'window', placement: 'below', shown: true, depth: 0 })

const w = computed(() => props.width ?? (props.kind === 'dialog' ? 820 : 1000))
const h = computed(() => props.height ?? (props.kind === 'dialog' ? 'auto' : 700))
const scale = computed(() => (props.shown ? Math.pow(0.94, props.depth) : 0.94))
const showGrabber = computed(() => props.kind === 'window' && props.movable)
</script>

<template>
  <div class="ui-panel" :data-kind="kind" :data-placement="placement" :data-shown="shown ? '' : undefined" :data-pushed="depth > 0 ? '' : undefined"
       :style="{ transform: `scale(${scale})`, width: w + 'px' }">
    <UiSurface :width="w" :height="h">
      <template v-for="(_, name) in $slots" #[name]="s"><slot :name="name" v-bind="s" /></template>
    </UiSurface>
    <div class="push-dim" />
    <UiGrabber v-if="showGrabber" class="grabber" :state="grabberState" />
  </div>
</template>

<style scoped>
.ui-panel {
  position: relative;
  display: inline-block;
  vertical-align: top;
  opacity: 1;
  transform-origin: center;
  transition: transform var(--push) var(--ease-settle), opacity var(--sheet) var(--ease-settle);
}
.ui-panel:not([data-shown]) { opacity: 0; pointer-events: none; }
.push-dim { position: absolute; inset: 0; border-radius: var(--radius-panel); background: var(--color-dim); opacity: 0; pointer-events: none; transition: opacity var(--push) var(--ease-settle); }
.ui-panel[data-pushed] .push-dim { opacity: 1; }
.grabber { top: 100%; transition: opacity var(--push) var(--ease-settle); }
.ui-panel[data-pushed] .grabber { opacity: 0; pointer-events: none; }
</style>
