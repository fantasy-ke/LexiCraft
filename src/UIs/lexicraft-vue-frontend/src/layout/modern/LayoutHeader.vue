<template>
  <header class="floating-header">
    <button class="corner-brand" aria-label="返回 LexiCraft 首页" type="button" @click="goHome"><BrandLogo tagline="learning room"/></button>
    <div ref="userMenuRef" class="desk-status">
      <div class="today-note" aria-label="今日学习"><span><strong>{{ todayStats.words }}</strong> 个单词</span><i aria-hidden="true"></i><span><strong>{{ todayStats.days }}</strong> 天连续</span></div>
      <button class="avatar-button" title="打开用户菜单" type="button" @click.stop="toggleUserMenu"><img :src="avatarUrl" alt="用户头像" @error="handleAvatarError"/></button>
      <transition name="paper-drop">
        <section v-if="showUserMenu" class="user-note" aria-label="用户菜单" @click.stop>
          <div class="user-heading"><img :src="avatarUrl" alt="" @error="handleAvatarError"/><div><strong>{{ userStore.user?.username || '学习者' }}</strong><small>{{ userStore.user?.email || '暂未填写邮箱' }}</small></div></div>
          <div class="note-links">
            <button type="button" @click="navigateTo('/app/user')"><DoodleIcon name="user" :size="18"/>个人主页</button>
            <button type="button" @click="navigateTo('/app/setting')"><DoodleIcon name="settings" :size="18"/>学习设置</button>
            <button type="button" @click="navigateTo('/app/doc')"><DoodleIcon name="note" :size="18"/>学习手记</button>
          </div>
          <div class="theme-block">
            <div class="theme-block__heading"><strong>界面风格</strong><small>只改变视觉，不影响学习内容</small></div>
            <div class="style-switch" aria-label="选择学习空间风格">
              <button v-for="item in themeStyles" :key="item.value" :class="{active: getThemeStyle() === item.value}" type="button" @click="setThemeStyle(item.value)">
                <span>{{ item.label }}</span><small>{{ item.description }}</small>
              </button>
            </div>
          </div>
          <div class="theme-block theme-block--mode">
            <div class="theme-block__heading"><strong>明暗模式</strong></div>
            <div class="mode-switch" aria-label="选择明暗模式">
              <button :class="{active: getThemeSetting() === 'light'}" type="button" @click="setTheme('light')"><DoodleIcon name="sun" :size="16"/>浅色</button>
              <button :class="{active: getThemeSetting() === 'dark'}" type="button" @click="setTheme('dark')"><DoodleIcon name="moon" :size="16"/>深色</button>
              <button :class="{active: getThemeSetting() === 'auto'}" type="button" @click="setTheme('auto')">跟随系统</button>
            </div>
          </div>
          <button class="logout-button" type="button" @click="handleLogout"><DoodleIcon name="logout" :size="18"/>退出登录</button>
        </section>
      </transition>
    </div>
  </header>
</template>

<script lang="ts" setup>
import {computed, onMounted, onUnmounted, ref} from 'vue'
import {useRouter} from 'vue-router'
import useTheme, {type ThemeStyle} from '@/hooks/theme'
import {useUserStore} from '@/stores/user'
import {getDefaultAvatarUrl, getUserAvatarUrl} from '@/utils/authHelpers'
import DoodleIcon from '@/components/doodle/DoodleIcon.vue'
import BrandLogo from '@/components/BrandLogo.vue'
const props = defineProps<{todayStats: {words: number; days: number}; showUserMenu: boolean}>()
const emit = defineEmits<{(e: 'update:showUserMenu', value: boolean): void}>()
const router = useRouter()
const userStore = useUserStore()
const {setTheme, setThemeStyle, getThemeSetting, getThemeStyle} = useTheme()
const userMenuRef = ref<HTMLElement | null>(null)
const themeStyles: Array<{value: ThemeStyle; label: string; description: string}> = [
  {value: 'editorial', label: '暖调阅读', description: '柔和纸张与衬线标题'},
  {value: 'zen', label: '极简专注', description: '纯净留白与无衬线排版'},
  {value: 'ink', label: '自然笔记', description: '温暖色调与轻量手写感'}
]
const avatarUrl = computed(() => {
  const user = userStore.user
  return user ? getUserAvatarUrl({avatar: user.avatar, email: user.email, username: user.username}) : getDefaultAvatarUrl({username: 'User'})
})
const toggleUserMenu = () => emit('update:showUserMenu', !props.showUserMenu)
const goHome = () => router.push('/')
const navigateTo = (path: string) => { router.push(path); emit('update:showUserMenu', false) }
const handleLogout = async () => { await userStore.logout(); emit('update:showUserMenu', false); router.push('/login') }
const handleAvatarError = (event: Event) => { const img = event.target as HTMLImageElement | null; if (img) img.src = getDefaultAvatarUrl({email: userStore.user?.email, username: userStore.user?.username}) }
const handleClickOutside = (event: MouseEvent) => { if (props.showUserMenu && userMenuRef.value && !userMenuRef.value.contains(event.target as Node)) emit('update:showUserMenu', false) }
onMounted(() => document.addEventListener('click', handleClickOutside))
onUnmounted(() => document.removeEventListener('click', handleClickOutside))
</script>

<style lang="scss" scoped>
.floating-header { position: fixed; inset: 0 0 auto; z-index: 90; pointer-events: none; }
.corner-brand, .desk-status { position: absolute; top: 18px; pointer-events: auto; }
.corner-brand { left: clamp(16px, 3vw, 46px); display: flex; align-items: center; gap: 10px; padding: 4px; border: 0; color: var(--text-primary); background: transparent; cursor: pointer; text-align: left; }
.corner-brand :deep(.brand-logo__mark) { width: 42px; height: 42px; }
.corner-brand :deep(.brand-logo__copy strong) { font-size: 17px; }
.corner-brand :deep(.brand-logo__copy small) { font-size: 9px; }
.desk-status { right: clamp(16px, 3vw, 46px); display: flex; align-items: center; gap: 12px; }
.today-note { display: flex; align-items: center; gap: 11px; padding: 8px 13px; border: 1px solid var(--border-color); border-radius: var(--radius-control); color: var(--text-secondary); background: var(--surface-overlay); box-shadow: var(--control-shadow); backdrop-filter: blur(12px); font-family: var(--font-sans); font-size: 11px; }
.today-note strong { color: var(--text-primary); font-size: 14px; }
.today-note i { width: 1px; height: 18px; background: var(--border-color); }
.avatar-button { width: 42px; height: 42px; padding: 3px; overflow: hidden; border: 1px solid var(--border-color); border-radius: 50%; background: var(--surface-card); box-shadow: var(--control-shadow); cursor: pointer; }
.avatar-button img { width: 100%; height: 100%; border-radius: 50%; object-fit: cover; }
.user-note { position: absolute; top: 58px; right: 0; width: min(370px, calc(100vw - 28px)); padding: 20px; border: 1px solid var(--border-color); border-radius: var(--radius-card); color: var(--text-primary); background: var(--surface-card); box-shadow: var(--card-shadow); font-family: var(--font-sans); }
.user-heading { display: flex; align-items: center; gap: 12px; padding-bottom: 16px; border-bottom: 1px solid var(--border-color); }
.user-heading img { width: 43px; height: 43px; border-radius: 50%; object-fit: cover; }
.user-heading strong, .user-heading small { display: block; }
.user-heading strong { font-size: 15px; }
.user-heading small { max-width: 250px; margin-top: 4px; overflow: hidden; color: var(--text-tertiary); font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.note-links { display: grid; gap: 3px; padding: 10px 0; border-bottom: 1px solid var(--border-color); }
.note-links button, .logout-button { display: flex; align-items: center; gap: 10px; width: 100%; padding: 10px 9px; border: 0; border-radius: var(--radius-control); color: var(--text-secondary); background: transparent; cursor: pointer; font: inherit; text-align: left; }
.note-links button:hover, .logout-button:hover { color: var(--text-primary); background: var(--hover-bg); }
.theme-block { padding-top: 16px; }
.theme-block + .theme-block { margin-top: 4px; }
.theme-block__heading { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; margin-bottom: 9px; }
.theme-block__heading strong { font-size: 12px; }
.theme-block__heading small { color: var(--text-tertiary); font-size: 10px; }
.style-switch { display: grid; gap: 6px; }
.style-switch button { position: relative; display: grid; gap: 3px; width: 100%; padding: 10px 12px; border: 1px solid var(--border-color); border-radius: var(--radius-control); color: var(--text-primary); background: var(--surface-raised); cursor: pointer; font: inherit; text-align: left; }
.style-switch button:hover { border-color: var(--border-strong); }
.style-switch button.active { border-color: var(--accent); background: var(--accent-soft); }
.style-switch button.active::after { position: absolute; top: 50%; right: 12px; content: '✓'; color: var(--accent); font-size: 13px; transform: translateY(-50%); }
.style-switch button span { font-size: 12px; font-weight: 700; }
.style-switch button small { color: var(--text-tertiary); font-size: 10px; }
.mode-switch { display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px; }
.mode-switch button { display: flex; min-height: 34px; align-items: center; justify-content: center; gap: 5px; padding: 0 8px; border: 1px solid var(--border-color); border-radius: var(--radius-control); color: var(--text-secondary); background: var(--surface-raised); cursor: pointer; font: inherit; font-size: 10px; }
.mode-switch button.active { border-color: var(--accent); color: var(--accent); background: var(--accent-soft); }
.logout-button { margin-top: 16px; padding-top: 14px; border-top: 1px solid var(--border-color); border-radius: 0; color: var(--danger); }
.corner-brand:focus-visible, .avatar-button:focus-visible, .user-note button:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 3px; }
.paper-drop-enter-active, .paper-drop-leave-active { transition: opacity .18s ease, transform .18s ease; }
.paper-drop-enter-from, .paper-drop-leave-to { opacity: 0; transform: translateY(-8px); }
@media (max-width: 700px) { .corner-brand :deep(.brand-logo__copy small), .today-note { display: none; } .corner-brand, .desk-status { top: 12px; } .corner-brand { left: 12px; } .desk-status { right: 12px; } }
</style>
