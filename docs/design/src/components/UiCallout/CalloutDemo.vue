<script setup lang="ts">
import { ref } from 'vue'
import UiCallout from './UiCallout.vue'
import UiButton from '../UiButton/UiButton.vue'
import DocBlock from '@/docs/DocBlock.vue'

const shown = ref(true)
</script>

<template>
  <DocBlock bg="scene" title="Callout on a speaker model and on a sound sphere (the target is the object; the tail tip is the anchor)" note="World-space in Unity, not in an overlay canvas. Simple fade, no periodic animation. Typical use: fade in 1.5 s after the scene loaded, fade out once the user has dragged the object.">
    <div class="mb-24"><UiButton :label="shown ? 'Hide' : 'Show'" @click="shown = !shown" /></div>
    <div class="flex items-end justify-around pt-24 pb-48">
      <div class="flex flex-col items-center">
        <UiCallout label="Drag me" icon="hand" :shown="shown" :delay="1500" />
        <div class="mt-8 h-160 w-110 rounded-row bg-gray" />
        <div class="mt-8 text-minimum text-text2">speaker model</div>
      </div>
      <div class="flex flex-col items-center">
        <UiCallout label="Drag to the floor" icon="arrow-right-left" placement="top" :shown="shown" />
        <div class="mt-8 size-140 rounded-full bg-gray-pressed" />
        <div class="mt-8 text-minimum text-text2">sound sphere</div>
      </div>
      <div class="flex items-center">
        <div class="size-140 rounded-full bg-gray-pressed" />
        <UiCallout icon="pointer-hand" placement="right" :shown="shown" />
      </div>
      <div class="flex flex-col items-center">
        <div class="size-140 rounded-full bg-gray-pressed" />
        <UiCallout label="Guess here" placement="bottom" icon="circle-check" :shown="shown" />
      </div>
    </div>
  </DocBlock>
</template>
