<script setup lang="ts">
import UiNavigationBar from './UiNavigationBar.vue'
import UiButton from '../UiButton/UiButton.vue'
import DocBlock from '@/docs/DocBlock.vue'
</script>

<template>
  <DocBlock title="Navigation bar: back, title, DEBUG tag, header actions, close" note="Bars are 120 u high: 40 u top padding + 80 u row. Back and close never appear together.">
    <div class="flex w-904 flex-col gap-24">
      <UiNavigationBar title="C04 · HumPerHW2" debug-tag />
      <UiNavigationBar title="Debug" />
      <UiNavigationBar title="Log files"><UiButton label="Delete all" button-style="destructiveText" small :min-width="false" /></UiNavigationBar>
      <UiNavigationBar title="Parameters"><UiButton label="Revert" button-style="borderless" small :min-width="false" /></UiNavigationBar>
      <UiNavigationBar title="Preset actions" leading="close" />
      <UiNavigationBar title="Main Hall" leading="none" />
    </div>
  </DocBlock>
</template>
