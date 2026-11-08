<script setup lang="ts">
import ColorTable from './ColorTable.vue'
import ButtonColors from './ButtonColors.vue'
import { surfaces, textColors } from '@/theme/tokens'
</script>

<template>
  <div class="min-h-screen bg-surface p-48 text-body text-text">
    <h1 class="text-title font-bold">Colors</h1>
    <p class="mt-12 max-w-1000 text-small text-text2">
      Scheme B: mono plus white. Flat, solid, quiet: no glass, no blur at rest, no transparency except the dim layer behind a sheet.
      Hierarchy comes from fill and brightness, not hue. No chamber accent colors. Source of truth: <code>src/theme/theme.css</code>,
      named after the Unity <code>UiTheme</code> fields; the values are read live from the page.
    </p>

    <h2 class="mt-48 text-heading font-bold">Surfaces</h2>
    <ColorTable :tokens="surfaces" />

    <h2 class="mt-48 text-heading font-bold">Text</h2>
    <ColorTable :tokens="textColors" />

    <h2 class="mt-48 text-heading font-bold">Button states</h2>
    <p class="mt-12 max-w-1000 text-small text-text2">
      Normal, hover, pressed: each state is brighter, pressed is the brightest. Red keeps hue 3° and saturation 71 %; only its
      brightness rises (75 / 85 / 95 %). Disabled = 40 % opacity and no hover; a borderless button uses <code>text3</code> for the label.
      <code>select</code> (the chosen button of a choice set) is a fill like these; see Components.
    </p>
    <ButtonColors />
  </div>
</template>
