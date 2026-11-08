<script setup lang="ts">
import TypeScale from './TypeScale.vue'
</script>

<template>
  <div class="min-h-screen bg-surface p-48 text-body text-text">
    <h1 class="text-title font-bold">Typography</h1>
    <p class="mt-12 max-w-1000 text-small text-text2">
      Noto Sans TC for English and 中文, Regular and Bold only (TextMeshPro dynamic SDF font assets in Unity). Bold for titles and
      prominent / destructive buttons. Labels never wrap. Sizes are canvas units at 2 m: 1 u = 1 px here = 0.057° of visual angle;
      the panel scale follows the distance, so the angle stays the same. Nothing is smaller than <code>minimum</code> (24 u).
    </p>

    <TypeScale class="mt-24" />

    <h2 class="mt-48 text-heading font-bold">Icons are not text</h2>
    <p class="mt-12 max-w-1000 text-small text-text2">
      No glyph characters (‹ × ←) in labels: the font lacks some and they never align. Use SVG icons (Foundations / Icons, <code>UiIcon</code>).
    </p>
  </div>
</template>
