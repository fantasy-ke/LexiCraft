<template>
  <div class="modern-layout">
    <LayoutHeader 
      v-model:showUserMenu="showUserMenu"
      :today-stats="todayStats"
      @toggleUserMenu="toggleUserMenu"
    />

    <div class="layout-body">
      <LayoutSidebar 
        :sidebar-expanded="sidebarExpanded"
      />

      <LayoutContent 
        :sidebar-expanded="sidebarExpanded"
        :current-route-name="currentRouteName"
        @toggleSidebar="toggleSidebar"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useRuntimeStore } from '@/stores/runtime'
import LayoutHeader from './modern/LayoutHeader.vue'
import LayoutSidebar from './modern/LayoutSidebar.vue'
import LayoutContent from './modern/LayoutContent.vue'

const route = useRoute()
const runtimeStore = useRuntimeStore()

// 响应式状态
const sidebarExpanded = ref(true)
const showUserMenu = ref(false)

// 今日统计数据
const todayStats = ref({
  words: 25,
  days: 7
})

// 计算当前页面名称
const currentRouteName = computed(() => {
  const nameMap: Record<string, string> = {
    '/app/dashboard': '我的主页',
    '/app/words': '单词练习',
    '/app/articles': '文章背诵',
    '/app/setting': '设置',
    '/app/feedback': '反馈建议',
    '/app/doc': '学习资料',
    '/app/qa': '帮助中心',
    '/app/user': '个人中心'
  }
  const path = route.path
  if (runtimeStore.pageTitle) return runtimeStore.pageTitle
  return nameMap[path] || ''
})

const toggleSidebar = () => {
  sidebarExpanded.value = !sidebarExpanded.value
}

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value
}
</script>

<style scoped lang="scss">
.modern-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: var(--layout-bg);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  overflow: hidden;
  color: var(--text-primary);
}

.layout-body {
  flex: 1;
  display: flex;
  overflow: hidden;
  position: relative;
}
</style>