<script setup lang="ts">
/** Documentation: icons (leading, trailing, both, icon only), sizes, and the label nudge. */
import UiButton from './UiButton.vue'
</script>

<template>
  <div class="flex flex-col gap-32">
    <section>
      <h3 class="mb-12 text-small font-bold text-text2">Icons: leading, trailing, both, icon only (the glyph never moves; only the label is nudged up)</h3>
      <div class="flex flex-wrap items-center gap-20">
        <UiButton label="Back" icon-leading="chevron-left" button-style="borderless" />
        <UiButton label="Start" icon-trailing="arrow-right" button-style="prominent" />
        <UiButton label="Hearing" icon-leading="headphone" icon-trailing="chevron-down" />
        <UiButton icon-leading="chevron-left" button-style="borderless" />
        <UiButton icon-leading="cross" button-style="borderless" />
        <UiButton icon-leading="trash-can" button-style="destructive" />
        <UiButton label="Delete" icon-leading="trash-can" button-style="destructiveText" />
        <UiButton icon-leading="sound-on" selected />
      </div>
    </section>

    <section>
      <h3 class="mb-12 text-small font-bold text-text2">Sizes: regular 80 u (min width 220 u), small 60 u, icon only = circle</h3>
      <div class="flex flex-wrap items-center gap-20">
        <UiButton label="Regular" />
        <UiButton label="Small" small />
        <UiButton label="−" small :min-width="false" in-group button-style="borderless" />
        <UiButton label="+" small :min-width="false" in-group button-style="borderless" />
        <UiButton label="1" :min-width="false" button-style="borderless" />
        <UiButton icon-leading="minus" small button-style="borderless" />
      </div>
    </section>

    <section>
      <h3 class="mb-12 text-small font-bold text-text2">Label centering: Noto Sans TC sits low, so single-line labels move up 2 u (<code>--label-nudge</code>); ascenders and descenders (Hxg) should look balanced in the capsule</h3>
      <div class="flex flex-wrap items-center gap-20">
        <UiButton label="Hxg Start 開始" button-style="prominent" />
        <UiButton label="Hxg Debug 偵錯" />
        <UiButton label="Hxg Back 返回" button-style="borderless" />
      </div>
    </section>
  </div>
</template>
