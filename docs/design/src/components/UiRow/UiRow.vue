<script setup lang="ts">
/**
 * UiRow (Unity: UiKit.Row, InfoRow; SwiftUI list row). 88 u high at least (grows with a multi-line subtitle), radius 28.
 *   [iconLeading]  title (+ subtitle)  ......  [value]  [iconTrailing | trailing slot: button, switch, stepper...]
 *  - `interactive` the whole row is a target: borderless in-group fill on hover / pressed (as a button); otherwise an info row
 *  - `value`       text2 (`edited` = text); `iconTrailing="chevron-right"` is the old ›
 *  - `subtitle`    small text2; line breaks (\n) make several lines; a single-line title is nudged up like a button label
 */
import { computed } from 'vue'
import UiIcon from '../UiIcon/UiIcon.vue'
import type { IconName } from '@/icons'

const props = defineProps<{
  title: string
  subtitle?: string
  value?: string
  edited?: boolean
  iconLeading?: IconName
  iconTrailing?: IconName
  interactive?: boolean
  selected?: boolean
  disabled?: boolean
  state?: 'idle' | 'hover' | 'pressed'
}>()
defineEmits<{ click: [e: MouseEvent] }>()
const single = computed(() => !props.subtitle)
</script>

<template>
  <component
    :is="interactive ? 'button' : 'div'"
    class="ui-row"
    :type="interactive ? 'button' : undefined"
    :disabled="interactive ? disabled : undefined"
    :data-interactive="interactive ? '' : undefined"
    :data-selected="selected ? '' : undefined"
    :data-icon-leading="iconLeading ? '' : undefined"
    :data-icon-trailing="iconTrailing ? '' : undefined"
    :data-disabled="disabled ? '' : undefined"
    :data-state="state ?? 'idle'"
    @click="interactive && $emit('click', $event)"
  >
    <UiIcon v-if="iconLeading" :name="iconLeading" class="ui-row-icon" />
    <span class="ui-row-text">
      <span class="ui-row-title" :data-single="single ? '' : undefined">{{ title }}</span>
      <span v-if="subtitle" class="ui-row-subtitle">{{ subtitle }}</span>
    </span>
    <span v-if="value" class="ui-row-value" :data-edited="edited ? '' : undefined">{{ value }}</span>
    <span v-if="$slots.default" class="ui-row-trailing"><slot /></span>
    <UiIcon v-if="iconTrailing" :name="iconTrailing" class="ui-row-icon" />
  </component>
</template>

<style scoped>
.ui-row {
  display: flex;
  align-items: center;
  gap: var(--gap);
  box-sizing: border-box;
  width: 100%;
  min-height: 88px;
  padding: 12px 28px;
  border: 0;
  border-radius: var(--radius-row);
  background: transparent;
  color: var(--color-text);
  font: inherit;
  text-align: left;
  cursor: default;
  transition: background-color var(--hover-out) var(--ease-settle), transform var(--release) var(--ease-settle);
}
.ui-row[data-icon-leading] { padding-left: 21px; } /* 25 % less side padding on the icon side */
.ui-row[data-icon-trailing] { padding-right: 21px; }
.ui-row[data-interactive] { cursor: pointer; }
.ui-row[data-selected] { background: var(--color-select); }
.ui-row[data-interactive]:hover:not(:disabled), .ui-row[data-state='hover'] {
  background: var(--color-borderless-in-group-hover);
  transition-duration: var(--hover-in), var(--release);
}
.ui-row[data-interactive]:active:not(:disabled), .ui-row[data-state='pressed'] {
  background: var(--color-borderless-in-group-pressed);
  transform: scale(var(--pressed-scale));
  transition-duration: 0s;
}
.ui-row[data-disabled] { opacity: var(--disabled-alpha); }
.ui-row-icon { --icon-size: 40px; color: var(--color-text2); }
.ui-row-text { display: flex; flex: 1; flex-direction: column; min-width: 0; gap: 2px; }
.ui-row-title { font-size: var(--text-body); line-height: 1.2; }
.ui-row-title[data-single] { transform: translateY(var(--label-nudge)); }
.ui-row-subtitle { font-size: var(--text-small); line-height: 1.25; color: var(--color-text2); white-space: pre-line; }
.ui-row-value { font-size: var(--text-body); color: var(--color-text2); white-space: nowrap; transform: translateY(var(--label-nudge)); }
.ui-row-value[data-edited] { color: var(--color-text); }
.ui-row-trailing { display: flex; align-items: center; gap: var(--inset); flex: none; }
</style>
