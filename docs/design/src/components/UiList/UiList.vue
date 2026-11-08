<script setup lang="ts">
/**
 * UiList (Unity: ScrollList + Column; SwiftUI List). A borderless scrolling stack of UiSections: no frame, no scrollbar. The edges
 * fade (56 u, solid `background` color) only where more content waits; a thin indicator (8 u, text3) shows while scrolling and
 * fades 0.6 s later. Sections are 24 u apart (`Column.Gap`). Set `background` to the surface behind the list (fade color).
 */
import { onMounted, ref } from 'vue'

withDefaults(defineProps<{ background?: string }>(), { background: 'var(--color-surface)' })
const el = ref<HTMLElement>()
const top = ref(false)
const bottom = ref(false)
const thumb = ref({ top: 0, height: 0 })
const scrolling = ref(false)
let timer: number | undefined

function update() {
  const e = el.value
  if (!e) return
  top.value = e.scrollTop > 1
  bottom.value = e.scrollTop + e.clientHeight < e.scrollHeight - 1
  const ratio = e.clientHeight / e.scrollHeight
  thumb.value = { top: (e.scrollTop / e.scrollHeight) * e.clientHeight, height: Math.max(48, ratio * e.clientHeight) }
}
function onScroll() {
  update()
  scrolling.value = true
  clearTimeout(timer)
  timer = window.setTimeout(() => (scrolling.value = false), 600)
}
onMounted(update)
</script>

<template>
  <div class="ui-list" :style="{ '--bg': background }">
    <div ref="el" class="scroller" @scroll="onScroll"><div class="content"><slot /></div></div>
    <div class="fade top" :data-on="top ? '' : undefined" />
    <div class="fade bottom" :data-on="bottom ? '' : undefined" />
    <div v-if="thumb.height < (el?.clientHeight ?? 1e9)" class="indicator" :data-on="scrolling ? '' : undefined" :style="{ top: thumb.top + 'px', height: thumb.height + 'px' }" />
  </div>
</template>

<style scoped>
.ui-list { position: relative; min-height: 0; flex: 1; }
.scroller { height: 100%; overflow-y: auto; scrollbar-width: none; }
.scroller::-webkit-scrollbar { display: none; }
.content { display: flex; flex-direction: column; gap: 24px; padding: 0 0 8px; }
.fade { position: absolute; left: 0; right: 0; height: 56px; pointer-events: none; opacity: 0; transition: opacity var(--hover-out) var(--ease-settle); }
.fade[data-on] { opacity: 1; }
.fade.top { top: 0; background: linear-gradient(var(--bg), transparent); }
.fade.bottom { bottom: 0; background: linear-gradient(transparent, var(--bg)); }
.indicator { position: absolute; right: -24px; width: 8px; border-radius: var(--radius-capsule); background: var(--color-text3); opacity: 0; transition: opacity 0.6s var(--ease-settle); pointer-events: none; }
.indicator[data-on] { opacity: 1; transition-duration: 0s; }
</style>
