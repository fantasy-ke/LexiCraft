<template>
  <header class="floating-header">
    <button aria-label="?? LexiCraft ??" class="corner-brand" type="button" @click="goHome">
      <BrandLogo tagline="learning room"/>
    </button>

    <div ref="userMenuRef" class="desk-status">
      <div class="today-note" aria-label="今日学习">
        <span><strong>{{ todayStats.words }}</strong> 个单词</span>
        <i aria-hidden="true"></i>
        <span><strong>{{ todayStats.days }}</strong> 天连续</span>
      </div>
      <button class="avatar-button" title="打开用户菜单" type="button" @click.stop="toggleUserMenu">
        <img :src="avatarUrl" alt="用户头像" @error="handleAvatarError"/>
      </button>

      <transition name="paper-drop">
        <section v-if="showUserMenu" class="user-note" aria-label="用户菜单" @click.stop>
          <span class="note-tape" aria-hidden="true"></span>
          <div class="user-heading">
            <img :src="avatarUrl" alt="" @error="handleAvatarError"/>
            <div>
              <strong>{{ userStore.user?.username || '学习者' }}</strong>
              <small>{{ userStore.user?.email || '暂未填写邮箱' }}</small>
            </div>
          </div>

          <div class="note-links">
            <button type="button" @click="navigateTo('/app/user')"><DoodleIcon name="user" :size="19"/>个人主页</button>
            <button type="button" @click="navigateTo('/app/setting')"><DoodleIcon name="settings" :size="19"/>学习设置</button>
            <button type="button" @click="navigateTo('/app/doc')"><DoodleIcon name="note" :size="19"/>学习手记</button>
          </div>

          <div class="theme-block">
            <span class="theme-label">空间风格</span>
            <div class="style-switch" aria-label="选择学习空间风格">
              <button
                  v-for="item in themeStyles"
                  :key="item.value"
                  :class="{active: getThemeStyle() === item.value}"
                  :title="item.label"
                  type="button"
                  @click="setThemeStyle(item.value)"
              >{{ item.mark }}</button>
            </div>
          </div>

          <div class="theme-block theme-block--mode">
            <span class="theme-label">明暗</span>
            <div class="mode-switch" aria-label="选择明暗模式">
              <button :class="{active: getThemeSetting() === 'light'}" title="明亮" type="button" @click="setTheme('light')"><DoodleIcon name="sun" :size="17"/></button>
              <button :class="{active: getThemeSetting() === 'dark'}" title="暗色" type="button" @click="setTheme('dark')"><DoodleIcon name="moon" :size="17"/></button>
              <button :class="{active: getThemeSetting() === 'auto'}" title="跟随系统" type="button" @click="setTheme('auto')">AUTO</button>
            </div>
          </div>

          <button class="logout-button" type="button" @click="handleLogout"><DoodleIcon name="logout" :size="19"/>退出登录</button>
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

const props = defineProps<{
  todayStats: {words: number; days: number}
  showUserMenu: boolean
}>()

const emit = defineEmits<{(e: 'update:showUserMenu', value: boolean): void}>()
const router = useRouter()
const userStore = useUserStore()
const {setTheme, setThemeStyle, getThemeSetting, getThemeStyle} = useTheme()
const userMenuRef = ref<HTMLElement | null>(null)
const themeStyles: Array<{value: ThemeStyle; mark: string; label: string}> = [
  {value: 'editorial', mark: 'A', label: '暖调书卷'},
  {value: 'zen', mark: 'B', label: '极简专注'},
  {value: 'ink', mark: 'C', label: '趣味手绘'}
]

const avatarUrl = computed(() => {
  const user = userStore.user
  return user
      ? getUserAvatarUrl({avatar: user.avatar, email: user.email, username: user.username})
      : getDefaultAvatarUrl({username: 'User'})
})

const toggleUserMenu = () => emit('update:showUserMenu', !props.showUserMenu)
const goHome = () => router.push('/')
const navigateTo = (path: string) => {
  router.push(path)
  emit('update:showUserMenu', false)
}
const handleLogout = async () => {
  await userStore.logout()
  emit('update:showUserMenu', false)
  router.push('/login')
}
const handleAvatarError = (event: Event) => {
  const img = event.target as HTMLImageElement | null
  if (img) img.src = getDefaultAvatarUrl({email: userStore.user?.email, username: userStore.user?.username})
}
const handleClickOutside = (event: MouseEvent) => {
  if (props.showUserMenu && userMenuRef.value && !userMenuRef.value.contains(event.target as Node)) {
    emit('update:showUserMenu', false)
  }
}

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
.today-note { display: flex; align-items: center; gap: 11px; padding: 8px 13px; border: 1px solid var(--border-color); border-radius: var(--radius-control); color: var(--text-secondary); background: var(--surface-overlay); box-shadow: var(--control-shadow); backdrop-filter: blur(12px); font-size: 11px; }
.today-note strong { color: var(--text-primary); font-family: var(--font-heading); font-size: 15px; }
.today-note i { width: 1px; height: 18px; background: var(--border-color); }
.avatar-button { width: 43px; height: 43px; padding: 3px; overflow: hidden; border: 1px solid var(--border-strong); border-radius: 50%; background: var(--surface-card); box-shadow: var(--control-shadow); cursor: pointer; }
.avatar-button img { width: 100%; height: 100%; border-radius: 50%; object-fit: cover; }
.user-note { position: absolute; top: 58px; right: 0; width: min(330px, calc(100vw - 28px)); padding: 22px; border: 1px solid var(--border-strong); border-radius: var(--radius-card); color: var(--text-primary); background: var(--surface-card); box-shadow: var(--card-shadow); }
.note-tape { position: absolute; top: -8px; right: 41px; width: 54px; height: 15px; background: color-mix(in srgb, var(--accent-soft) 78%, transparent); transform: rotate(-3deg); }
.user-heading { display: flex; align-items: center; gap: 12px; padding-bottom: 16px; border-bottom: 1px solid var(--border-color); }
.user-heading img { width: 43px; height: 43px; border-radius: 50%; object-fit: cover; }
.user-heading strong, .user-heading small { display: block; }
.user-heading strong { font-family: var(--font-heading); font-size: 16px; }
.user-heading small { max-width: 220px; margin-top: 3px; overflow: hidden; color: var(--text-tertiary); font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.note-links { display: grid; gap: 3px; padding: 10px 0; border-bottom: 1px solid var(--border-color); }
.note-links button, .logout-button { display: flex; align-items: center; gap: 10px; width: 100%; padding: 9px 8px; border: 0; border-radius: var(--radius-control); color: var(--text-secondary); background: transparent; cursor: pointer; font: inherit; text-align: left; }
.note-links button:hover, .logout-button:hover { color: var(--text-primary); background: var(--hover-bg); }
.theme-block { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding-top: 13px; }
.theme-label { color: var(--text-tertiary); font-size: 11px; }
.style-switch, .mode-switch { display: flex; gap: 4px; }
.style-switch button, .mode-switch button { display: grid; min-width: 30px; height: 30px; place-items: center; padding: 0 8px; border: 1px solid var(--border-color); border-radius: var(--radius-control); color: var(--text-secondary); background: var(--surface-raised); cursor: pointer; font-family: var(--font-mono); font-size: 10px; font-weight: 800; }
.style-switch button.active, .mode-switch button.active { border-color: var(--accent); color: var(--accent-contrast); background: var(--accent); }
.logout-button { margin-top: 13px; border-top: 1px solid var(--border-color); border-radius: 0; color: var(--danger); }
.corner-brand:focus-visible, .avatar-button:focus-visible, .user-note button:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 3px; }
.paper-drop-enter-active, .paper-drop-leave-active { transition: opacity .18s ease, transform .18s ease; }
.paper-drop-enter-from, .paper-drop-leave-to { opacity: 0; transform: translateY(-8px); }

:global(html[data-theme-style='editorial'] .corner-brand) { transform: rotate(-.6deg); }
:global(html[data-theme-style='editorial'] .user-note) { border-top-width: 4px; }
:global(html[data-theme-style='zen'] .corner-brand) { transform: scale(.92); transform-origin: left center; }
:global(html[data-theme-style='zen']) .corner-brand :deep(.brand-logo__copy small), :global(html[data-theme-style='zen'] .today-note) { display: none; }
:global(html[data-theme-style='zen'] .avatar-button) { border-radius: 0; box-shadow: none; }
:global(html[data-theme-style='zen'] .user-note) { border-width: 1px 0; box-shadow: none; }
:global(html[data-theme-style='zen']) .note-links :deep(svg) { display: none; }
:global(html[data-theme-style='ink'] .corner-brand) { transform: rotate(-1deg); }
:global(html[data-theme-style='ink']) .corner-brand :deep(.brand-logo__mark) { filter: drop-shadow(3px 3px 0 var(--danger)); transform: rotate(-2deg); }
:global(html[data-theme-style='ink'] .today-note), :global(html[data-theme-style='ink'] .user-note) { border-width: 2px; transform: rotate(.35deg); }

@media (max-width: 700px) {
  .corner-brand :deep(.brand-logo__copy small), .today-note { display: none; }
  .corner-brand, .desk-status { top: 12px; }
  .corner-brand { left: 12px; }
  .desk-status { right: 12px; }
}
</style>
