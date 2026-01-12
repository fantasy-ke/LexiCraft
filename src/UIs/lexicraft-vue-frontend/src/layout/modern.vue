<template>
  <div class="modern-layout">
    <!-- 头部导航栏 (Full Width Header) -->
    <header class="top-header">
      <div class="header-left">
        <button class="logo-section" @click="goHome">
          <div class="logo-icon">
            <span class="text-white font-bold text-sm">LC</span>
          </div>
          <span class="logo-text">LexionCraft</span>
        </button>
      </div>

      <div class="header-center">
        <div class="search-box">
          <i class="search-icon">🔍</i>
          <input type="text" placeholder="搜索单词或文章..." class="search-input">
        </div>
      </div>

      <div class="header-right">
        <div class="header-stats">
          <div class="stat-item">
            <span class="stat-label">今日学习</span>
            <span class="stat-value">{{ todayStats.words }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">连续天数</span>
            <span class="stat-value">{{ todayStats.days }}</span>
          </div>
        </div>

        <div class="user-menu">
          <button class="user-avatar" @click="toggleUserMenu" title="个人中心">
            <img src="https://ui-avatars.com/api/?name=Sunrise&background=random" alt="avatar" class="avatar-img" />
          </button>

          <transition name="dropdown">
            <div v-if="showUserMenu" class="user-dropdown">
              <!-- User Info Header -->
              <div class="user-header">
                <div class="user-info">
                  <div class="user-name">sunrise</div>
                  <div class="user-handle">@fantasy_ke</div>
                </div>
              </div>

              <!-- Menu Items -->
              <div class="menu-list">
                <div class="menu-item" @click="navigateTo('/app/user')">
                  <IconFluentPerson20Regular class="menu-icon" />
                  <span>个人主页</span>
                </div>
                <div class="menu-item" @click="navigateTo('/app/setting')">
                  <IconFluentSettings20Regular class="menu-icon" />
                  <span>设置</span>
                </div>
                <div class="menu-item">
                  <IconFluentNote20Regular class="menu-icon" />
                  <span>更新日志</span>
                </div>
                <div class="menu-item" @click="navigateTo('/app/doc')">
                  <IconFluentBookQuestionMark20Regular class="menu-icon" />
                  <span>帮助文档</span>
                  <IconFluentArrowUpRight16Regular class="external-icon" />
                </div>
              </div>

              <div class="dropdown-divider"></div>

              <!-- Preferences -->
              <div class="preference-section">
                <div class="pref-label">偏好设置</div>
                <div class="pref-row">
                  <span class="pref-name">主题</span>
                  <div class="theme-switch">
                    <div 
                      class="theme-option" 
                      :class="{ active: getThemeSetting() === 'light' }"
                      @click="setTheme('light')"
                      title="浅色模式"
                    >
                      <IconFluentWeatherSunny16Regular />
                    </div>
                    <div 
                      class="theme-option" 
                      :class="{ active: getThemeSetting() === 'dark' }"
                      @click="setTheme('dark')"
                      title="深色模式"
                    >
                      <IconFluentWeatherMoon16Regular />
                    </div>
                    <div 
                      class="theme-option" 
                      :class="{ active: getThemeSetting() === 'auto' }"
                      @click="setTheme('auto')"
                      title="跟随系统"
                    >
                      <IconFluentLaptop16Regular />
                    </div>
                  </div>
                </div>
              </div>

              <div class="dropdown-divider"></div>

              <!-- Logout -->
              <div class="logout-item" @click="handleLogout">
                <IconFluentSignOut20Regular class="menu-icon" />
                <span>退出登录</span>
              </div>
            </div>
          </transition>
        </div>
      </div>
    </header>

    <!-- 下方主体区域 (Body: Sidebar + Content) -->
    <div class="layout-body">
      <!-- 左侧菜单 (Sidebar) -->
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

      <!-- 主内容区域 (Main Content) -->
      <main class="main-content">
        <!-- 内容卡片 (Card Wrapper) -->
        <div class="content-card">
          <!-- 侧边栏开关按钮 (Sidebar Toggle) & Page Title -->
          <div class="content-header">
            <button class="sidebar-toggle-btn" @click="toggleSidebar" :title="sidebarExpanded ? '收起菜单' : '展开菜单'">
              <IconFluentAlignSpaceFitVertical20Regular class="toggle-icon" :class="{ rotated: !sidebarExpanded }" />
            </button>
            <div class="page-title">
              <span class="title-text">{{ currentRouteName }}</span>
            </div>
          </div>

          <!-- 路由视图 (Page Content) -->
          <div class="content-wrapper">
            <router-view></router-view>
          </div>
        </div>
      </main>
    </div>

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
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import useTheme from '@/hooks/theme'
import { useRuntimeStore } from '@/stores/runtime'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const route = useRoute()
const { setTheme, getTheme, getThemeSetting } = useTheme()
const runtimeStore = useRuntimeStore()
const userStore = useUserStore()

// 响应式状态
const sidebarExpanded = ref(true)
const showUserMenu = ref(false)

// 今日统计数据
const todayStats = ref({
  words: 25,
  days: 7
})

// 方法定义
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
  // 简单匹配，实际可能需要更复杂的路由匹配逻辑
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

const goHome = () => {
  router.push('/')
}

const navigateTo = (path: string) => {
  router.push(path)
  showUserMenu.value = false
}

const handleLogout = async () => {
  await userStore.logout()
  router.push('/login')
  showUserMenu.value = false
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
.modern-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: var(--layout-bg);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  overflow: hidden;
  color: var(--text-primary);
}

/* Header Styles */
.top-header {
  height: 60px;
  background: var(--header-bg);
  border-bottom: 1px solid var(--border-color);
  display: flex;
  align-items: center;
  padding: 0 1.5rem;
  gap: 2rem;
  flex-shrink: 0;
  z-index: 200;

  .header-left {
    display: flex;
    align-items: center;

    .logo-section {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      cursor: pointer;
      background: none;
      border: none;
      padding: 0;
      transition: opacity 0.2s;

      &:hover {
        opacity: 0.8;
      }

      .logo-icon {
        width: 36px;
        height: 36px;
        background: linear-gradient(135deg, #3b82f6, #1d4ed8);
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }

      .logo-text {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--text-primary);
      }
    }
  }

  .header-center {
    flex: 1;
    max-width: 500px;
    margin: 0 auto;

    .search-box {
      position: relative;
      width: 100%;

      .search-icon {
        position: absolute;
        left: 1rem;
        top: 50%;
        transform: translateY(-50%);
        color: var(--text-tertiary);
      }

      .search-input {
        width: 100%;
        height: 38px;
        padding: 0 1rem 0 2.5rem;
        border: 1px solid var(--border-color);
        border-radius: 4px; // 统一硬朗风格
        background: var(--layout-bg);
        font-size: 0.875rem;
        transition: all 0.2s;
        color: var(--text-primary);

        &:focus {
          outline: none;
          background: var(--header-bg);
          border-color: var(--text-active);
          box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
        }

        &::placeholder {
          color: var(--text-tertiary);
        }
      }
    }
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 1.5rem;

    .header-stats {
      display: flex;
      gap: 1.5rem;

      .stat-item {
        text-align: center;

        .stat-label {
          display: block;
          font-size: 0.75rem;
          color: var(--text-tertiary);
        }

        .stat-value {
          display: block;
          font-size: 1rem;
          font-weight: 700;
          color: var(--text-active);
        }
      }
    }

    .user-menu {
      position: relative;

      .user-avatar {
        width: 36px;
        height: 36px;
        border: none;
        background: var(--layout-bg);
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.2s;
        padding: 0;
        overflow: hidden;

        .avatar-img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }

        &:hover {
          box-shadow: 0 0 0 2px var(--border-color);
        }
      }

      .user-dropdown {
        position: absolute;
        top: calc(100% + 10px);
        right: 0;
        width: 260px;
        background: var(--header-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        box-shadow: 0 10px 30px var(--shadow-color);
        z-index: 1000;
        overflow: hidden;
        padding-bottom: 0.5rem;

        .user-header {
          padding: 1.25rem 1rem;
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          
          .user-info {
            .user-name {
              font-size: 1rem;
              font-weight: 600;
              color: var(--text-primary);
              margin-bottom: 0.25rem;
            }
            .user-handle {
              font-size: 0.8rem;
              color: var(--text-tertiary);
            }
          }
          
          .user-badge {
            background: #a855f7;
            color: white;
            font-size: 0.7rem;
            padding: 2px 6px;
            border-radius: 4px;
            font-weight: 500;
          }
        }

        .dropdown-divider {
          height: 1px;
          background: var(--border-color);
          margin: 0.5rem 0;
        }

        .menu-list {
          padding: 0 0.5rem;
          
          .menu-item {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            padding: 0.6rem 0.75rem;
            border-radius: 8px;
            cursor: pointer;
            transition: all 0.2s;
            color: var(--text-secondary);

            &:hover {
              background: var(--hover-bg);
              color: var(--text-primary);
            }

            .menu-icon {
              font-size: 1.1rem;
              color: var(--text-tertiary);
            }
            
            span {
              font-size: 0.9rem;
              flex: 1;
            }
            
            .external-icon {
              font-size: 0.8rem;
              color: var(--text-tertiary);
            }
          }
        }
        
        .preference-section {
          padding: 0.5rem 1.25rem;
          
          .pref-label {
            font-size: 0.75rem;
            color: var(--text-tertiary);
            margin-bottom: 0.75rem;
          }
          
          .pref-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            
            .pref-name {
              font-size: 0.9rem;
              color: var(--text-secondary);
            }
            
            .theme-switch {
              display: flex;
              background: var(--hover-bg);
              padding: 2px;
              border-radius: 6px;
              
              .theme-option {
                width: 28px;
                height: 28px;
                display: flex;
                align-items: center;
                justify-content: center;
                border-radius: 4px;
                cursor: pointer;
                color: var(--text-tertiary);
                transition: all 0.2s;
                
                &:hover {
                  color: var(--text-primary);
                }
                
                &.active {
                  background: var(--header-bg);
                  color: var(--text-active);
                  box-shadow: 0 1px 2px var(--shadow-color);
                }
                
                font-size: 0.9rem;
              }
            }
          }
        }
        
        .logout-item {
          margin: 0 0.5rem;
          display: flex;
          align-items: center;
          gap: 0.75rem;
          padding: 0.6rem 0.75rem;
          border-radius: 8px;
          cursor: pointer;
          transition: all 0.2s;
          color: #ef4444;

          &:hover {
            background: #fef2f2;
          }
          
          .menu-icon {
            font-size: 1.1rem;
          }
          
          span {
            font-size: 0.9rem;
          }
        }
      }
    }
  }
}

/* Body Container */
.layout-body {
  flex: 1;
  display: flex;
  overflow: hidden;
  position: relative;
}

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
      padding: 0.5rem 1rem; // 高度减小
      margin: 0.1rem 0; // 紧凑布局
      border-radius: 2px; // 菱角分明
      cursor: pointer;
      transition: all 0.2s;
      white-space: nowrap;
      position: relative;

      .nav-icon {
        font-size: 1.1rem; // 图标也微缩一点
        min-width: 1.1rem;
        text-align: center;
        color: var(--text-tertiary);
      }

      .nav-text {
        font-size: 0.85rem; // 文字也微缩一点
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
        
        // 添加一个左侧激活条，增加菱角感
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

  .theme-btn {
    width: 36px;
    height: 36px;
    border: 1px solid var(--border-color);
    background: var(--sidebar-bg);
    border-radius: 8px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s;

    &:hover {
      background: var(--hover-bg);
      border-color: var(--text-tertiary);
    }
  }
}

/* Main Content Styles */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 1rem;
  overflow: hidden;
  position: relative;
}

.content-card {
  flex: 1;
  background: var(--header-bg);
  border-radius: 2px; // 菱角分明
  border: 1px solid var(--border-color);
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

.content-header {
  height: 40px;
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
      
      // 激活态的小横条
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

/* Responsive adjustments */
@media (max-width: 768px) {
  .sidebar {
    display: none; /* Hide desktop sidebar on mobile */
  }

  .main-content {
    padding: 0; /* Full width on mobile */
  }
  
  .content-card {
    border-radius: 0; /* Square on mobile */
    box-shadow: none;
  }
  
  .sidebar-toggle-btn {
    display: none; /* Hide toggle on mobile */
  }

  .content-wrapper {
    padding: 1rem;
    padding-top: 1rem; /* Reset padding */
    padding-bottom: 5rem; /* Space for mobile nav */
  }

  .top-header {
    padding: 0 1rem;
    .header-stats { display: none; }
    .header-center { display: none; } /* Simplify header on mobile */
  }

  .mobile-nav {
    display: flex;
  }
}
</style>