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
.main-content {
  position: relative;
  z-index: 1;
  height: 100%;
  padding: 86px clamp(20px, 4vw, 62px) 112px;
  box-sizing: border-box;
}

.page-caption {
  position: absolute;
  z-index: 4;
  top: 90px;
  left: clamp(20px, 4vw, 62px);
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--text-secondary);
  font-family: var(--font-hand);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: .08em;
  pointer-events: none;
  transform: rotate(-2deg);
}

.caption-line {
  width: 28px;
  height: 8px;
  border-top: 2px solid var(--pencil-red);
  border-radius: 50%;
}

.content-wrapper {
  height: 100%;
  overflow-x: hidden;
  overflow-y: auto;
  padding: 34px 6px 16px;
  scrollbar-color: var(--ink-soft) transparent;
  scrollbar-width: thin;
}

.content-wrapper::-webkit-scrollbar { width: 7px; }
.content-wrapper::-webkit-scrollbar-thumb { border-radius: 10px; background: color-mix(in srgb, var(--ink-soft) 50%, transparent); }

@media (max-width: 768px) {
  .main-content { padding: 76px 12px 94px; }
  .page-caption { top: 77px; left: 18px; }
  .content-wrapper { padding-top: 31px; }
}

@media (prefers-reduced-motion: reduce) {
  .content-wrapper { scroll-behavior: auto; }
}
</style>