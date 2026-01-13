<template>
  <aside class="sidebar" :class="{ collapsed: !sidebarExpanded }">
    <nav class="sidebar-nav">
      <div class="nav-section">
        <div class="nav-item" @click="navigateTo('/app/dashboard')" :class="{ active: isActiveRoute('/app/dashboard') }">
          <IconFluentHome20Regular class="nav-icon" />
          <span v-if="sidebarExpanded" class="nav-text">主页</span>
        </div>

        <div class="nav-item" @click="navigateTo('/app/words')" :class="{ active: isActiveRoute('/app/words') }">
          <IconFluentTextUnderlineDouble20Regular class="nav-icon" />
          <span v-if="sidebarExpanded" class="nav-text">单词练习</span>
        </div>

        <div class="nav-item" @click="navigateTo('/app/articles')" :class="{ active: isActiveRoute('/app/articles') }">
          <IconFluentBookLetter20Regular class="nav-icon" />
          <span v-if="sidebarExpanded" class="nav-text">文章背诵</span>
        </div>

        <div class="nav-item" @click="navigateTo('/app/setting')" :class="{ active: isActiveRoute('/app/setting') }">
          <IconFluentSettings20Regular class="nav-icon" />
          <span v-if="sidebarExpanded" class="nav-text">设置</span>
        </div>
      </div>

      <div class="nav-section">
        <div class="section-title" v-if="sidebarExpanded">学习工具</div>

        <div class="nav-item" @click="navigateTo('/app/feedback')" :class="{ active: isActiveRoute('/app/feedback') }">
          <IconFluentCommentEdit20Regular class="nav-icon" />
          <span v-if="sidebarExpanded" class="nav-text">反馈建议</span>
        </div>

        <div class="nav-item" @click="navigateTo('/app/doc')" :class="{ active: isActiveRoute('/app/doc') }">
          <IconFluentBook20Regular class="nav-icon" />
          <span v-if="sidebarExpanded" class="nav-text">学习资料</span>
        </div>

        <div class="nav-item" @click="navigateTo('/app/qa')" :class="{ active: isActiveRoute('/app/qa') }">
          <IconFluentQuestionCircle20Regular class="nav-icon" />
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
    <div class="mobile-nav-item" @click="navigateTo('/app/dashboard')" :class="{ active: isActiveRoute('/app/dashboard') }">
      <IconFluentHome20Regular class="nav-icon" />
      <span>主页</span>
    </div>
    <div class="mobile-nav-item" @click="navigateTo('/app/words')" :class="{ active: isActiveRoute('/app/words') }">
      <IconFluentTextUnderlineDouble20Regular class="nav-icon" />
      <span>单词</span>
    </div>
    <div class="mobile-nav-item" @click="navigateTo('/app/articles')" :class="{ active: isActiveRoute('/app/articles') }">
      <IconFluentBookLetter20Regular class="nav-icon" />
      <span>文章</span>
    </div>
    <div class="mobile-nav-item" @click="navigateTo('/app/setting')" :class="{ active: isActiveRoute('/app/setting') }">
      <IconFluentSettings20Regular class="nav-icon" />
      <span>设置</span>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'

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

<style scoped lang="scss">
/* Sidebar Styles */
.sidebar {
  width: 240px;
  background: var(--sidebar-bg);
  border-right: 1px solid var(--border-color);
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
  padding: 1rem 0;
  overflow-y: auto;
  overflow-x: hidden;

  .nav-section {
    margin-bottom: 2rem;

    .section-title {
      padding: 0 1rem;
      margin-bottom: 0.5rem;
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--text-tertiary);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.5rem 1rem;
      margin: 0.1rem 0;
      border-radius: 2px;
      cursor: pointer;
      transition: all 0.2s;
      white-space: nowrap;
      position: relative;

      .nav-icon {
        font-size: 1.1rem;
        min-width: 1.1rem;
        text-align: center;
        color: var(--text-tertiary);
      }

      .nav-text {
        font-size: 0.85rem;
        color: var(--text-secondary);
        font-weight: 500;
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
        
        &::before {
          content: '';
          position: absolute;
          left: 0;
          top: 0;
          bottom: 0;
          width: 3px;
          background: var(--text-active);
        }

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
