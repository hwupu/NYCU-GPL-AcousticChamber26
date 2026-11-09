<script setup lang="ts">
/**
 * Main Hall (Unity: App HallMenu + HallGrid; DesignSystem section 7). Fixed window 1000 x 700 u, no ‹, title "Acoustic Chambers", centered (PO 2026-11-10).
 * Body: a scrolling 2-column grid of cards with 8 u of room around it (a hovered card grows into it), column gap 24 u,
 * row gap 28 u (PO 2026-11-10: room to scale). A card is only its cover, one target (no fill around it, nothing below it):
 *  - a 16:9 cover (radius 36; the chamber's image, or the solid `cover` color);
 *  - on it, top left the C-ID (small, bold), bottom left the title (title size 56 u, bold, letter spacing -3 %, line height 1),
 *    both 24 u in, white (`text`) at 75 %, soft shadow drop-shadow(0 0 4px rgb(0 0 0 / 0.3)) (Unity: UiKit.SoftShadow, TMP underlay);
 *  - hover: the card scales to 103 % and the image saturation rises to 125 % (text unchanged); hover in 150 ms, out 350 ms,
 *    settle curve; pressed 101.5 %. (Unity: UiButton.HoverScale / PressedScale + HallCard, image shader GPL/UI Saturation.)
 * No description, no "N unfinished" pill (PO 2026-11-10; the launch routing still opens a chamber with unfinished sessions).
 * Geometry (HallGrid, content 904 - 2 x 8 = 888 u): card = cover 432 x 243 u; 2 rows = 514 u.
 * Covers: one pool, Assets/AcousticChamber/App/Covers/IMG-C-NN.jpeg (Gemini; the ID is the credit ID, not the chamber); each
 * ChamberDefinition.thumbnail picks one: C01 = IMG-C-01, C02 = 02, C03 = 05, C04 = 04 (IMG-C-03 spare).
 *
 *  covers   false = placeholders (no thumbnails)
 *  hover    force the hover state on one card (index)
 */
import UiPanel from '@/components/UiPanel/UiPanel.vue'
import UiNavigationBar from '@/components/UiNavigationBar/UiNavigationBar.vue'
import c01 from '../../../../Assets/AcousticChamber/App/Covers/IMG-C-01.jpeg'
import c02 from '../../../../Assets/AcousticChamber/App/Covers/IMG-C-02.jpeg'
import c04 from '../../../../Assets/AcousticChamber/App/Covers/IMG-C-04.jpeg'
import c05 from '../../../../Assets/AcousticChamber/App/Covers/IMG-C-05.jpeg'

withDefaults(defineProps<{ covers?: boolean; hover?: number }>(), { covers: true, hover: -1 })

const chambers = [
  { label: 'C01', title: 'Moving Source', cover: c01 },
  { label: 'C02', title: 'Hidden Source', cover: c02 },
  { label: 'C03', title: 'Hearing Loss', cover: c05 },
  { label: 'C04', title: 'HumPerHW2', cover: c04 },
]
</script>

<template>
  <UiPanel :width="1000" :height="700">
    <template #navigationBar>
      <UiNavigationBar title="Acoustic Chambers" leading="none" centered />
    </template>
    <div class="chambers">
      <div v-for="(c, i) in chambers" :key="c.label" class="card" :data-hover="hover === i ? '' : undefined">
        <div v-if="covers" class="image" :style="{ backgroundImage: `url(${c.cover})` }" />
        <span class="c-id">{{ c.label }}</span>
        <span class="title">{{ c.title }}</span>
      </div>
    </div>
  </UiPanel>
</template>

<style scoped>
.chambers { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); /* equal columns whatever the text (Unity: minW 0) */
            column-gap: 24px; row-gap: 28px; padding: 8px; overflow-y: auto; scrollbar-width: none; min-height: 0; }
.card { position: relative; aspect-ratio: 16 / 9; border-radius: 36px; background: var(--color-cover); overflow: hidden; color: var(--color-text); cursor: pointer;
        transition: transform var(--hover-out) var(--ease-settle); }
.image { position: absolute; inset: 0; background: center / cover no-repeat; filter: saturate(1); transition: filter var(--hover-out) var(--ease-settle); }
.card:hover, .card[data-hover] { transform: scale(1.03); transition-duration: var(--hover-in); }
.card:hover .image, .card[data-hover] .image { filter: saturate(1.25); transition-duration: var(--hover-in); }
.card:active { transform: scale(1.015); transition-duration: var(--release); }
.c-id, .title { position: absolute; left: 24px; font-weight: 700; opacity: 0.75; color: var(--color-text); filter: drop-shadow(0 0 4px rgb(0 0 0 / 0.3)); }
.c-id { top: 24px; font-size: var(--text-small); line-height: 1; }
.title { right: 24px; bottom: 12px; padding-bottom: 0.15em; font-size: var(--text-title); line-height: 1; /* room for descenders inside the ellipsis clip */
         letter-spacing: -0.03em; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
</style>
