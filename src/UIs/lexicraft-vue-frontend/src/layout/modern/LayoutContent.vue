<template>
  <main class="main-content">
    <div class="page-caption" aria-live="polite">
      <span class="caption-line" aria-hidden="true"></span>
      <span>{{ currentRouteName }}</span>
    </div>
    <div ref="contentWrapperRef" class="content-wrapper">
      <router-view></router-view>
    </div>
  </main>
</template>

<script lang="ts" setup>
import {nextTick, ref, watch} from 'vue'
import {useRoute} from 'vue-router'

defineProps<{currentRouteName: string}>()

const route = useRoute()
const contentWrapperRef = ref<HTMLElement | null>(null)

watch(() => route.path, () => {
  nextTick(() => contentWrapperRef.value?.scrollTo({top: 0, behavior: 'smooth'}))
})
</script>

<style lang="scss" scoped>
.content-area { position: relative; min-height: 100vh; padding: 88px clamp(22px, 5vw, 76px) 112px; box-sizing: border-box; }
.content-wrapper { width: 100%; height: calc(100vh - 200px); overflow: auto; scrollbar-gutter: stable; }
.page-caption { display: flex; align-items: center; gap: 12px; margin: 0 0 18px; color: var(--text-tertiary); font-family: var(--font-heading); font-size: 12px; letter-spacing: .08em; }
.page-caption::after { content: ''; width: 54px; height: 1px; background: var(--border-color); }

:global(html[data-theme-style='editorial'] .content-area) { padding-left: clamp(132px, 11vw, 170px); }
:global(html[data-theme-style='editorial'] .content-wrapper) { max-width: 1320px; margin-inline: auto; }
:global(html[data-theme-style='editorial'] .page-caption) { font-style: italic; letter-spacing: .04em; }
:global(html[data-theme-style='zen'] .content-area) { padding: 108px clamp(28px, 9vw, 150px) 96px; }
:global(html[data-theme-style='zen'] .content-wrapper) { height: calc(100vh - 204px); }
:global(html[data-theme-style='zen'] .page-caption) { font-family: var(--font-mono); font-size: 10px; letter-spacing: .14em; text-transform: uppercase; }
:global(html[data-theme-style='ink'] .page-caption) { color: var(--accent); font-family: var(--font-hand); font-size: 15px; font-weight: 800; transform: rotate(-1deg); }
:global(html[data-theme-style='ink'] .page-caption::after) { height: 2px; background: var(--border-strong); transform: rotate(-2deg); }

@media (max-width: 760px) {
  .content-area, :global(html[data-theme-style='editorial'] .content-area), :global(html[data-theme-style='zen'] .content-area) { padding: 78px 14px 92px; }
  .content-wrapper, :global(html[data-theme-style='zen'] .content-wrapper) { height: calc(100vh - 170px); }
  .page-caption { margin-bottom: 12px; }
}
</style>
