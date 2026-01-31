<template>
  <aside :class="{ collapsed: !sidebarExpanded }" class="sidebar">
    <nav class="sidebar-nav">
      <div class="nav-section">
        <div :class="{ active: isActiveRoute('/app/dashboard') }" class="nav-item"
             @click="navigateTo('/app/dashboard')">
          <IconFluentHome20Regular class="nav-icon"/>
          <span v-if="sidebarExpanded" class="nav-text">主页</span>
        </div>

        <div :class="{ active: isActiveRoute('/app/words') }" class="nav-item" @click="navigateTo('/app/words')">
          <IconFluentTextUnderlineDouble20Regular class="nav-icon"/>
          <span v-if="sidebarExpanded" class="nav-text">单词练习</span>
        </div>

        <div :class="{ active: isActiveRoute('/app/articles') }" class="nav-item" @click="navigateTo('/app/articles')">
          <IconFluentBookLetter20Regular class="nav-icon"/>
          <span v-if="sidebarExpanded" class="nav-text">文章背诵</span>
        </div>

        <div :class="{ active: isActiveRoute('/app/setting') }" class="nav-item" @click="navigateTo('/app/setting')">
          <IconFluentSettings20Regular class="nav-icon"/>
          <span v-if="sidebarExpanded" class="nav-text">设置</span>
        </div>
      </div>

      <div class="nav-section">
        <div v-if="sidebarExpanded" class="section-title">学习工具</div>

        <div :class="{ active: isActiveRoute('/app/feedback') }" class="nav-item" @click="navigateTo('/app/feedback')">
          <IconFluentCommentEdit20Regular class="nav-icon"/>
          <span v-if="sidebarExpanded" class="nav-text">反馈建议</span>
        </div>

        <div :class="{ active: isActiveRoute('/app/doc') }" class="nav-item" @click="navigateTo('/app/doc')">
          <IconFluentBook20Regular class="nav-icon"/>
          <span v-if="sidebarExpanded" class="nav-text">学习资料</span>
        </div>

        <div :class="{ active: isActiveRoute('/app/qa') }" class="nav-item" @click="navigateTo('/app/qa')">
          <IconFluentQuestionCircle20Regular class="nav-icon"/>
          <span v-if="sidebarExpanded" class="nav-text">帮助中心</span>
        </div>
      </div>
    </nav>

    <div class="sidebar-footer">
      <!-- 侧边栏底部暂时留空或放其他信息 -->
    </div>
  </aside>

  <!-- 移动端底部导航 -->
  <nav class="mobile-nav">
    <div :class="{ active: isActiveRoute('/app/dashboard') }" class="mobile-nav-item"
         @click="navigateTo('/app/dashboard')">
      <IconFluentHome20Regular class="nav-icon"/>
      <span>主页</span>
    </div>
    <div :class="{ active: isActiveRoute('/app/words') }" class="mobile-nav-item" @click="navigateTo('/app/words')">
      <IconFluentTextUnderlineDouble20Regular class="nav-icon"/>
      <span>单词</span>
    </div>
    <div :class="{ active: isActiveRoute('/app/articles') }" class="mobile-nav-item"
         @click="navigateTo('/app/articles')">
      <IconFluentBookLetter20Regular class="nav-icon"/>
      <span>文章</span>
    </div>
    <div :class="{ active: isActiveRoute('/app/setting') }" class="mobile-nav-item" @click="navigateTo('/app/setting')">
      <IconFluentSettings20Regular class="nav-icon"/>
      <span>设置</span>
    </div>
  </nav>
</template>

<script lang="ts" setup>
import {useRoute, useRouter} from 'vue-router'

const props = defineProps<{
  sidebarExpanded: boolean;
}>()

const router = useRouter()
const route = useRoute()

const navigateTo = (path: string) => {
  router.push(path)
}

// 判断路由是否激活
const isActiveRoute = (path: string) => {
  if (path === '/app/dashboard') {
    return route.path === '/app' || route.path === '/app/dashboard'
  }
  return route.path.includes(path.replace('/app', ''))
}
</script>

<style lang="scss" scoped>
/* Sidebar Styles */
.sidebar {
  width: 240px;
  background: var(--sidebar-bg);
  /* border-right: 1px solid var(--border-color); Removed for cleaner look */
  display: flex;
  flex-direction: column;
  z-index: 10;
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);

  &.collapsed {
    width: 0;
    border-right: none;
    overflow: hidden;
  }
}

.sidebar-nav {
  flex: 1;
  padding: 1.5rem 0.5rem; /* More horizontal padding */
  overflow-y: auto;
  overflow-x: hidden;

  .nav-section {
    margin-bottom: 2rem;

    .section-title {
      padding: 0 1rem;
      margin-bottom: 0.75rem;
      font-size: 0.7rem;
      font-weight: 700;
      color: var(--text-tertiary);
      text-transform: uppercase;
      letter-spacing: 0.08em;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.6rem 1rem;
      margin: 0.25rem 0.5rem; /* Increased margins */
      border-radius: 0.5rem; /* Softer radius */
      cursor: pointer;
      transition: all 0.2s ease-in-out;
      white-space: nowrap;
      position: relative;

      .nav-icon {
        font-size: 1.1rem;
        min-width: 1.1rem;
        text-align: center;
        color: var(--text-tertiary);
        transition: color 0.2s;
      }

      .nav-text {
        font-size: 0.875rem;
        color: var(--text-secondary);
        font-weight: 500;
        transition: color 0.2s;
      }

      &:hover {
        background: var(--hover-bg);

        .nav-text {
          color: var(--text-primary);
        }

        .nav-icon {
          color: var(--text-primary);
        }
      }

      &.active {
        background: var(--active-bg);
        color: var(--text-active);

        /* Removed side border ::before */
        
        .nav-text {
          color: var(--text-active);
          font-weight: 600;
        }

        .nav-icon {
          color: var(--text-active);
        }
      }
    }
  }
}

.sidebar-footer {
  padding: 1rem;
  border-top: 1px solid var(--border-color);
  display: flex;
  justify-content: center;
}

/* Mobile Navigation */
.mobile-nav {
  display: none;
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: var(--header-bg);
  border-top: 1px solid var(--border-color);
  padding: 0.5rem 0;
  z-index: 1000;
  justify-content: space-around;

  .mobile-nav-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.2rem;
    padding: 0.4rem;
    cursor: pointer;
    flex: 1;
    transition: all 0.2s;
    position: relative;

    .nav-icon {
      font-size: 1.25rem;
      color: var(--text-tertiary);
      transition: all 0.2s;
    }

    span {
      font-size: 0.7rem;
      color: var(--text-tertiary);
      font-weight: 500;
    }

    &.active {
      .nav-icon, span {
        color: var(--text-active);
      }

      &::before {
        content: '';
        position: absolute;
        top: 0;
        width: 20px;
        height: 3px;
        background: var(--text-active);
        border-radius: 0 0 2px 2px;
      }
    }
  }
}

@media (max-width: 768px) {
  .sidebar {
    display: none;
  }

  .mobile-nav {
    display: flex;
  }
}
</style>
