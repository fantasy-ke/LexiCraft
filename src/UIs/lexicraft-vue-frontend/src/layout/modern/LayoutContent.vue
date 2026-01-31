<template>
  <main class="main-content">
    <div class="content-card">
      <div class="content-header">
        <button :title="sidebarExpanded ? '收起菜单' : '展开菜单'" class="sidebar-toggle-btn"
                @click="$emit('toggleSidebar')">
          <IconFluentAlignSpaceFitVertical20Regular :class="{ rotated: !sidebarExpanded }" class="toggle-icon"/>
        </button>
        <div class="page-title">
          <span class="title-text">{{ currentRouteName }}</span>
        </div>
      </div>

      <div class="content-wrapper" ref="contentWrapperRef">
        <router-view></router-view>
      </div>
    </div>
  </main>
</template>

<script lang="ts" setup>
import { ref, watch, nextTick } from 'vue'
import { useRoute } from 'vue-router'

defineProps<{
  sidebarExpanded: boolean;
  currentRouteName: string;
}>()

defineEmits<{
  (e: 'toggleSidebar'): void;
}>()

const route = useRoute()
const contentWrapperRef = ref<HTMLElement | null>(null)

// 监听路由变化，切换页面时自动滚动到顶部
watch(
  () => route.path,
  () => {
    nextTick(() => {
      if (contentWrapperRef.value) {
        contentWrapperRef.value.scrollTop = 0
      }
    })
  }
)
</script>

<style lang="scss" scoped>
/* Main Content Styles */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 1rem; /* More padding around the card */
  overflow: hidden;
  position: relative;
}

.content-card {
  flex: 1;
  background: var(--header-bg);
  border-radius: 1rem; /* Much softer radius */
  /* border: 1px solid var(--border-color); Removed */
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.03); /* Soft shadow */
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

.content-header {
  height: 60px; /* Taller header */
  display: flex;
  align-items: center;
  padding: 0 1.5rem;
  /* border-bottom: 1px solid var(--hover-bg); Removed */
  background: var(--header-bg);
  gap: 1rem;
}

.sidebar-toggle-btn {
  width: 36px;
  height: 36px;
  border: none; /* No border */
  background: transparent;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: var(--text-tertiary);
  transition: all 0.2s;

  &:hover {
    background: var(--hover-bg);
    color: var(--text-active);
  }

  .toggle-icon {
    font-size: 1.25rem;
    transition: transform 0.3s;

    &.rotated {
      transform: rotate(180deg);
    }
  }
}

.page-title {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  /* border-left: 3px solid var(--text-active); Removed */
  padding-left: 0;
  height: auto;
  display: flex;
  align-items: center;

}

.content-wrapper {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
}

@media (max-width: 768px) {
  .main-content {
    padding: 0;
  }

  .content-card {
    border-radius: 0;
    box-shadow: none;
  }

  .sidebar-toggle-btn {
    display: none;
  }

  .content-wrapper {
    padding: 1rem;
    padding-bottom: 5rem;
  }
}
</style>
