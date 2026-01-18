<template>
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

      <div class="user-menu" ref="userMenuRef">
        <!-- 移动端遮罩层 -->
        <div 
          v-if="showUserMenu" 
          class="menu-overlay"
          @click="emit('update:showUserMenu', false)"
        ></div>
        
<button class="user-avatar" @click.stop="toggleUserMenu" title="个人中心">
	<img :src="avatarUrl" alt="avatar" class="avatar-img" @error="handleAvatarError" />
</button>

        <transition name="dropdown">
          <div v-if="showUserMenu" class="user-dropdown" @click.stop>
            <!-- User Info Header -->
            <div class="user-header">
              <div class="user-info">
                <div class="user-name">{{ userStore.user?.username || '未登录' }}</div>
                <div class="user-handle">{{ userStore.user?.email || '' }}</div>
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
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import useTheme from '@/hooks/theme'
import { useUserStore } from '@/stores/user'
import { getUserAvatarUrl, getDefaultAvatarUrl } from '@/utils/authHelpers'

const props = defineProps<{
  todayStats: { words: number; days: number };
  showUserMenu: boolean;
}>()

const emit = defineEmits<{
  (e: 'toggleUserMenu'): void;
  (e: 'update:showUserMenu', val: boolean): void;
}>()

const router = useRouter()
const { setTheme, getThemeSetting } = useTheme()
const userStore = useUserStore()
const userMenuRef = ref<HTMLElement | null>(null)

const avatarUrl = computed(() => {
	const user = userStore.user
	if (!user) {
		return getDefaultAvatarUrl({ username: 'User' })
	}
	return getUserAvatarUrl({
		avatar: user.avatar,
		email: user.email,
		username: user.username
	})
})

const toggleUserMenu = () => {
  emit('toggleUserMenu')
}

const goHome = () => {
  router.push('/')
}

const navigateTo = (path: string) => {
  router.push(path)
  emit('update:showUserMenu', false)
}

const handleLogout = async () => {
  await userStore.logout()
  router.push('/login')
  emit('update:showUserMenu', false)
}

const handleAvatarError = (event: Event) => {
	const img = event.target as HTMLImageElement | null
	if (!img) return
	const user = userStore.user
	img.src = getDefaultAvatarUrl({
		email: user?.email,
		username: user?.username
	})
}

// 点击外部区域关闭菜单
const handleClickOutside = (event: MouseEvent) => {
  if (userMenuRef.value && !userMenuRef.value.contains(event.target as Node)) {
    if (props.showUserMenu) {
      emit('update:showUserMenu', false)
    }
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped lang="scss">
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
        border-radius: 4px;
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
      
      .menu-overlay {
        display: none;
      }

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
        z-index: 1001;
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

// 下拉动画
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

@media (max-width: 768px) {
  .top-header {
    padding: 0 1rem;
    gap: 1rem;
    
    .header-left {
      .logo-section {
        gap: 0.5rem;
        
        .logo-icon {
          width: 32px;
          height: 32px;
        }
        
        .logo-text {
          font-size: 1.1rem;
        }
      }
    }
    
    .header-stats { 
      display: none !important;
    }
    
    .header-center { 
      display: none !important;
    }
    
    .header-right {
      gap: 0.75rem;
      margin-left: auto;
      
      .user-menu {
        // 移动端遮罩层
        .menu-overlay {
          display: block;
          position: fixed;
          inset: 0;
          background: rgba(0, 0, 0, 0.3);
          z-index: 999;
          animation: fadeIn 0.2s ease;
        }
        
        .user-avatar {
          width: 32px;
          height: 32px;
        }
        
        .user-dropdown {
          width: 280px;
          max-width: calc(100vw - 2rem);
          right: -0.5rem;
          top: calc(100% + 8px);
          z-index: 1001;
          max-height: calc(100vh - 80px);
          overflow-y: auto;
          -webkit-overflow-scrolling: touch;
          
          .user-header {
            padding: 1rem;
          }
          
          .menu-list {
            .menu-item {
              padding: 0.75rem;
              
              &:active {
                background: var(--active-bg);
              }
            }
          }
          
          .preference-section {
            padding: 0.5rem 1rem;
          }
          
          .logout-item {
            padding: 0.75rem;
            
            &:active {
              background: #fee2e2;
            }
          }
        }
      }
    }
  }
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

/* 超小屏幕优化 */
@media (max-width: 375px) {
  .top-header {
    padding: 0 0.75rem;
    gap: 0.5rem;
    
    .header-left {
      .logo-section {
        .logo-text {
          font-size: 1rem;
        }
      }
    }
    
    .header-right {
      .user-menu {
        .user-dropdown {
          width: calc(100vw - 1.5rem); /* 超小屏幕下拉菜单几乎占满屏幕 */
          right: -0.75rem; /* 调整位置 */
        }
      }
    }
  }
}
</style>
