<script setup lang="ts">
/**
 * UiTextField (Unity: the Study ID display of NumberPadEntry; fed by ITextEntry, so a native keyboard can fill it later; SwiftUI TextField).
 * A `raised` capsule, 80 u high. Empty = `placeholder` in text3. `focused` = a 4 u `select` ring (the entry that takes the next key).
 * It shows a value; it does not own the keyboard.
 */
defineProps<{ value?: string; placeholder?: string; align?: 'center' | 'left'; focused?: boolean; disabled?: boolean }>()
</script>

<template>
  <div class="ui-text-field" :data-focused="focused ? '' : undefined" :data-align="align ?? 'center'" :data-disabled="disabled ? '' : undefined">
    <span v-if="value" class="value">{{ value }}</span>
    <span v-else class="placeholder">{{ placeholder }}</span>
    <slot />
  </div>
</template>

<style scoped>
.ui-text-field {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--gap);
  box-sizing: border-box;
  height: var(--button-height);
  padding: 0 40px;
  border-radius: var(--radius-capsule);
  background: var(--color-raised);
  font-size: var(--text-body);
  line-height: 1;
  box-shadow: 0 0 0 0 var(--color-select);
  transition: box-shadow var(--hover-out) var(--ease-settle);
}
.ui-text-field[data-align='left'] { justify-content: flex-start; }
.ui-text-field[data-focused] { box-shadow: 0 0 0 4px var(--color-select); }
.ui-text-field[data-disabled] { opacity: var(--disabled-alpha); }
.value, .placeholder { transform: translateY(var(--label-nudge)); }
.placeholder { color: var(--color-text3); }
</style>
