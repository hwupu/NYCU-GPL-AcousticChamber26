<script setup lang="ts">
/**
 * UiButton (Unity: GPL.UI.UiButton, built by UiKit.Button / LocButton / IconButton). A capsule, 80 u high (small 60 u).
 *
 *  - `buttonStyle`  prominent (white fill: the one main action) | gray | borderless | destructiveText | destructive (red fill: final confirmation)
 *  - states         idle, hover, pressed (instant, 97 % scale, brightest), disabled (40 %), and `selected` (a toggle / the chosen one of a set)
 *  - icons          `iconLeading` / `iconTrailing` (SwiftUI naming); no label = an icon button (circle)
 *  - `state`        forces hover / pressed for documentation; at run time the pointer drives it
 * `selected` does nothing on prominent and destructive (as UiButton.Selected). `inGroup` = on a raised group (borderless hover colors).
 * Single-line labels move up by `--label-nudge` (the font sits low); icons do not.
 */
import { computed, useSlots } from 'vue'
import UiIcon from '../UiIcon/UiIcon.vue'
import type { IconName } from '@/icons'

export type UiButtonStyle = 'prominent' | 'gray' | 'borderless' | 'destructiveText' | 'destructive'

const props = withDefaults(
  defineProps<{
    label?: string
    buttonStyle?: UiButtonStyle
    iconLeading?: IconName
    iconTrailing?: IconName
    selected?: boolean
    disabled?: boolean
    small?: boolean
    inGroup?: boolean
    /** icon-only button that fills its cell instead of being a circle (number pad backspace). */
    stretch?: boolean
    /** false = no 220 u minimum width (keys, segments, steppers). */
    minWidth?: boolean
    state?: 'idle' | 'hover' | 'pressed'
  }>(),
  { buttonStyle: 'gray', minWidth: true, state: 'idle' },
)
defineEmits<{ click: [e: MouseEvent] }>()
const slots = useSlots()

const iconOnly = computed(() => !props.stretch && !props.label && !slots.default && (props.iconLeading || props.iconTrailing))
const fillStyle = computed(() => props.buttonStyle === 'prominent' || props.buttonStyle === 'destructive')
const canSelect = computed(() => props.selected && !fillStyle.value)
</script>

<template>
  <button
    type="button"
    class="ui-button"
    :data-style="buttonStyle"
    :data-in-group="inGroup && !fillStyle && buttonStyle !== 'gray' ? '' : undefined"
    :data-selected="canSelect ? '' : undefined"
    :data-small="small ? '' : undefined"
    :data-stretch="stretch ? '' : undefined"
    :data-icon-only="iconOnly ? '' : undefined"
    :data-icon-leading="iconLeading ? '' : undefined"
    :data-icon-trailing="iconTrailing ? '' : undefined"
    :data-min-width="minWidth && !iconOnly && !small ? '' : undefined"
    :data-state="state"
    :disabled="disabled"
    @click="$emit('click', $event)"
  >
    <UiIcon v-if="iconLeading" :name="iconLeading" />
    <span v-if="label || $slots.default" class="ui-button-label"><slot>{{ label }}</slot></span>
    <UiIcon v-if="iconTrailing" :name="iconTrailing" />
  </button>
</template>

<style scoped>
.ui-button {
  --f: var(--color-gray);
  --fh: var(--color-gray-hover);
  --fp: var(--color-gray-pressed);
  --l: var(--color-gray-label);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--icon-gap);
  box-sizing: border-box;
  height: var(--button-height);
  padding: 0 var(--button-padding-x);
  border: 0;
  border-radius: var(--radius-capsule);
  background: var(--f);
  color: var(--l);
  font: inherit;
  font-size: var(--text-button);
  line-height: 1;
  white-space: nowrap;
  cursor: pointer;
  user-select: none;
  transition: background-color var(--hover-out) var(--ease-settle), color var(--hover-out) var(--ease-settle),
    transform var(--release) var(--ease-settle), opacity var(--hover-out) var(--ease-settle);
}
.ui-button[data-min-width] { min-width: var(--button-min-width); }
.ui-button[data-small] { height: var(--small-button-height); padding: 0 32px; }
.ui-button[data-small] :deep(.ui-icon) { width: var(--icon-size-small); height: var(--icon-size-small); }
/* the side with an icon has 25 % less padding (an icon has its own optical margin) */
.ui-button[data-icon-leading] { padding-left: calc(var(--button-padding-x) * 0.75); }
.ui-button[data-icon-trailing] { padding-right: calc(var(--button-padding-x) * 0.75); }
.ui-button[data-small][data-icon-leading] { padding-left: 24px; }
.ui-button[data-small][data-icon-trailing] { padding-right: 24px; }
.ui-button[data-stretch] { padding: 0; }
.ui-button[data-icon-only] { width: var(--icon-button-size); padding: 0; }
.ui-button[data-icon-only][data-small] { width: var(--small-button-height); }
.ui-button-label { display: block; transform: translateY(var(--label-nudge)); }

/* styles: normal / hover / pressed / label */
.ui-button[data-style='prominent'] {
  --f: var(--color-prominent); --fh: var(--color-prominent-hover); --fp: var(--color-prominent-pressed); --l: var(--color-prominent-label);
  font-weight: var(--font-weight-bold);
}
.ui-button[data-style='borderless'],
.ui-button[data-style='destructiveText'] {
  --f: var(--color-borderless); --fh: var(--color-borderless-hover); --fp: var(--color-borderless-pressed); --l: var(--color-borderless-label);
}
.ui-button[data-style='destructiveText'] { --l: var(--color-red-text); }
.ui-button[data-in-group] {
  --f: var(--color-borderless-in-group); --fh: var(--color-borderless-in-group-hover); --fp: var(--color-borderless-in-group-pressed);
}
.ui-button[data-style='destructive'] {
  --f: var(--color-destructive); --fh: var(--color-destructive-hover); --fp: var(--color-destructive-pressed); --l: var(--color-destructive-label);
  font-weight: var(--font-weight-bold);
}
/* selected: the `select` fill, hover and pressed a step lighter (UiButton.Selected) */
.ui-button[data-selected] { --f: var(--color-select); --fh: var(--color-gray-pressed); --fp: var(--color-gray-pressed); }

/* states */
.ui-button:hover:not(:disabled),
.ui-button[data-state='hover']:not(:disabled) {
  background: var(--fh);
  transition-duration: var(--hover-in), var(--hover-in), var(--release), var(--hover-out);
}
.ui-button:active:not(:disabled),
.ui-button[data-state='pressed']:not(:disabled) {
  background: var(--fp);
  transform: scale(var(--pressed-scale));
  transition-duration: 0s;
}
.ui-button:disabled { opacity: var(--disabled-alpha); cursor: default; }
/* no fill: a faded white label reads like plain text, so the dim text color is used instead */
.ui-button:disabled[data-style='borderless'],
.ui-button:disabled[data-style='destructiveText'] { opacity: 1; color: var(--color-text3); }
</style>
