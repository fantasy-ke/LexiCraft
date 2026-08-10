<template>
  <div class="modern-layout">
    <div class="paper-grain" aria-hidden="true"></div>
    <LayoutHeader
        v-model:show-user-menu="showUserMenu"
        :today-stats="todayStats"
    />
    <LayoutContent :current-route-name="currentRouteName"/>
    <LayoutSidebar/>
  </div>
</template>

<script lang="ts" setup>
import {computed, ref} from 'vue'
import {useRoute} from 'vue-router'
import {useRuntimeStore} from '@/stores/runtime'
import LayoutHeader from './modern/LayoutHeader.vue'
import LayoutSidebar from './modern/LayoutSidebar.vue'
import LayoutContent from './modern/LayoutContent.vue'

const route = useRoute()
const runtimeStore = useRuntimeStore()
const showUserMenu = ref(false)

// 现有统计尚未接入服务端接口，保持原有展示值，避免本次视觉重构改变数据契约。
const todayStats = ref({words: 25, days: 7})

const currentRouteName = computed(() => {
  const nameMap: Record<string, string> = {
    '/app/dashboard': '我的学习桌',
    '/app/words': '单词练习',
    '/app/articles': '文章背诵',
    '/app/setting': '偏好设置',
    '/app/feedback': '写张反馈便签',
    '/app/doc': '学习资料',
    '/app/qa': '帮助中心',
    '/app/user': '个人手账'
  }
  if (runtimeStore.pageTitle) return runtimeStore.pageTitle
  return nameMap[route.path] || route.meta.title?.toString() || ''
})
</script>

<style lang="scss" scoped>
.modern-layout { position: relative; min-height: 100vh; overflow: hidden; color: var(--text-primary); background-color: var(--layout-bg); background-image: var(--texture-image); background-size: var(--texture-size); font-family: var(--font-family); transition: color .25s ease, background-color .25s ease; }
.paper-grain { position: fixed; inset: 0; z-index: 0; pointer-events: none; opacity: .3; background-image: radial-gradient(circle at 20% 16%, color-mix(in srgb, var(--text-primary) 8%, transparent) 0 .6px, transparent .8px); background-size: 25px 25px; mix-blend-mode: multiply; }
:global(html[data-theme='dark'] .paper-grain) { mix-blend-mode: screen; opacity: .14; }
:global(html[data-theme-style='zen'] .paper-grain) { display: none; }
:global(html[data-theme-style='editorial'] .paper-grain) { background-image: repeating-linear-gradient(0deg, transparent 0 5px, color-mix(in srgb, var(--text-primary) 2.5%, transparent) 6px); background-size: auto; }
</style>
