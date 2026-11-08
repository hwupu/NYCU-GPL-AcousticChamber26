<script setup lang="ts">
import UiIcon from '@/components/UiIcon/UiIcon.vue'
import { iconNames } from '@/icons'
</script>

<template>
  <div class="min-h-screen bg-surface p-48 text-body text-text">
    <h1 class="text-title font-bold">Icons</h1>
    <p class="mt-12 max-w-1000 text-small text-text2">
      {{ iconNames.length }} icons (Akar Icons, 24 x 24 grid, tinted by the text color). Names are the file names in
      <code>src/icons</code> without the <code>akar-icons--</code> prefix; in Unity each is a sprite of the same name.
      Use <code>UiIcon name="..."</code>, or <code>iconLeading</code> / <code>iconTrailing</code> on a button or row.
    </p>
    <div class="mt-32 grid grid-cols-[repeat(auto-fill,minmax(180px,1fr))] gap-8">
      <div v-for="n in iconNames" :key="n" class="flex flex-col items-center gap-8 rounded-row bg-raised p-16">
        <UiIcon :name="n" :size="48" />
        <span class="text-center text-minimum break-all text-text2">{{ n }}</span>
      </div>
    </div>
  </div>
</template>
