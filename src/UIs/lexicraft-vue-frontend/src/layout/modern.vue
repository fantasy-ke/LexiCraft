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
.modern-layout {
  position: relative;
  height: 100dvh;
  overflow: hidden;
  color: var(--text-primary);
  background: var(--layout-bg);
  font-family: var(--font-family);
}

.paper-grain {
  position: fixed;
  inset: 0;
  z-index: 0;
  opacity: .38;
  pointer-events: none;
  background-image:
      radial-gradient(circle at 13% 19%, rgba(32, 39, 35, .08) 0 1px, transparent 1.4px),
      radial-gradient(circle at 78% 71%, rgba(32, 39, 35, .055) 0 1px, transparent 1.5px),
      linear-gradient(rgba(32, 39, 35, .025) 1px, transparent 1px);
  background-size: 23px 23px, 31px 31px, 100% 38px;
}

:global(.dark) .paper-grain {
  opacity: .28;
  filter: invert(1);
}
</style>