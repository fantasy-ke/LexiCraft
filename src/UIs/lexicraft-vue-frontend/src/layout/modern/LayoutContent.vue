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

      <div class="content-wrapper">
        <router-view></router-view>
      </div>
    </div>
  </main>
</template>

<script lang="ts" setup>
defineProps<{
  sidebarExpanded: boolean;
  currentRouteName: string;
}>()

defineEmits<{
  (e: 'toggleSidebar'): void;
}>()
</script>

<style lang="scss" scoped>
/* Main Content Styles */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 0.5rem;
  overflow: hidden;
  position: relative;
}

.content-card {
  flex: 1;
  background: var(--header-bg);
  border-radius: 2px;
  border: 1px solid var(--border-color);
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

.content-header {
  height: 45px;
  display: flex;
  align-items: center;
  padding: 0 1rem;
  border-bottom: 1px solid var(--hover-bg);
  background: var(--header-bg);
  gap: 1rem;
}

.sidebar-toggle-btn {
  width: 32px;
  height: 32px;
  border: 1px solid var(--border-color);
  background: var(--header-bg);
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
    border-color: var(--text-tertiary);
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
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  border-left: 3px solid var(--text-active);
  padding-left: 0.75rem;
  height: 18px;
  line-height: 18px;
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
