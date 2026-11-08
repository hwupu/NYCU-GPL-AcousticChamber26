<script setup lang="ts">
/** Documentation helper (not a UiKit component): a titled block on the panel color, or on `raised` for in-group checks. */
defineProps<{ title?: string; note?: string; bg?: 'surface' | 'raised' | 'scene' }>()
</script>

<template>
  <section class="mb-32">
    <h3 v-if="title" class="mb-4 text-small font-bold text-text2">{{ title }}</h3>
    <p v-if="note" class="mb-12 max-w-1000 text-minimum text-text2">{{ note }}</p>
    <div class="rounded-group p-32" :class="bg === 'raised' ? 'bg-raised' : bg === 'scene' ? 'scene' : 'bg-surface'"><slot /></div>
  </section>
</template>

<style scoped>
.scene { background: radial-gradient(ellipse at 50% 120%, #3a3a46, #0c0c10 70%); }
</style>
