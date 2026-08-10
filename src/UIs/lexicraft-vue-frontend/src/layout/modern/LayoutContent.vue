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
.main-content { position: relative; display: flex; height: 100vh; min-height: 0; flex-direction: column; box-sizing: border-box; padding: 88px clamp(22px, 5vw, 76px) 24px; }
.content-wrapper { width: 100%; min-height: 0; flex: 1; overflow: auto; padding-bottom: 20px; box-sizing: border-box; scrollbar-gutter: stable; }
.page-caption { display: flex; flex: 0 0 auto; align-items: center; gap: 12px; margin: 0 0 18px; color: var(--text-tertiary); font-family: var(--font-heading); font-size: 12px; letter-spacing: .08em; }
.page-caption::after { content: ''; width: 54px; height: 1px; background: var(--border-color); }

:global(html[data-theme-style='editorial'] .main-content) { padding-left: clamp(132px, 11vw, 170px); }
:global(html[data-theme-style='editorial'] .content-wrapper) { max-width: 1320px; margin-inline: auto; }
:global(html[data-theme-style='editorial'] .page-caption) { font-style: italic; letter-spacing: .04em; }
:global(html[data-theme-style='zen'] .main-content) { padding: 88px clamp(28px, 8vw, 132px) 18px; }
:global(html[data-theme-style='zen'] .page-caption) { font-family: var(--font-mono); font-size: 10px; letter-spacing: .14em; text-transform: uppercase; }
:global(html[data-theme-style='ink'] .main-content) { padding-bottom: 108px; }
:global(html[data-theme-style='ink'] .page-caption) { color: var(--accent); font-family: var(--font-hand); font-size: 15px; font-weight: 800; transform: rotate(-1deg); }
:global(html[data-theme-style='ink'] .page-caption::after) { height: 2px; background: var(--border-strong); transform: rotate(-2deg); }

@media (max-width: 980px) {
  :global(html[data-theme-style='zen'] .main-content) { padding-top: 126px; }
}

@media (max-width: 760px) {
  .main-content,
  :global(html[data-theme-style='editorial'] .main-content),
  :global(html[data-theme-style='zen'] .main-content) { padding: 124px 12px 14px; }
  :global(html[data-theme-style='ink'] .main-content) { padding: 78px 12px 100px; }
  .content-wrapper { padding-bottom: 12px; scrollbar-gutter: auto; }
  .page-caption { margin-bottom: 12px; }
}
</style>
