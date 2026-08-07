<template>
  <header class="floating-header">
    <button class="corner-brand" type="button" @click="goHome">
      <span class="brand-mark">L</span>
      <span class="brand-copy"><strong>LexiCraft</strong><small>learning notebook</small></span>
    </button>

    <div ref="userMenuRef" class="desk-status">
      <div class="today-note" aria-label="今日学习数据">
        <span><strong>{{ todayStats.words }}</strong> 今日词数</span>
        <i aria-hidden="true"></i>
        <span><strong>{{ todayStats.days }}</strong> 连续天数</span>
      </div>
      <button class="avatar-button" title="打开个人菜单" type="button" @click.stop="toggleUserMenu">
        <img :src="avatarUrl" alt="用户头像" @error="handleAvatarError"/>
      </button>

      <transition name="paper-drop">
        <section v-if="showUserMenu" class="user-note" aria-label="个人菜单" @click.stop>
          <span class="note-tape" aria-hidden="true"></span>
          <div class="user-heading">
            <img :src="avatarUrl" alt="" @error="handleAvatarError"/>
            <div>
              <strong>{{ userStore.user?.username || '学习者' }}</strong>
              <small>{{ userStore.user?.email || '继续写下今天的一页' }}</small>
            </div>
          </div>

          <div class="note-links">
            <button type="button" @click="navigateTo('/app/user')"><DoodleIcon name="user" :size="20"/>个人手账</button>
            <button type="button" @click="navigateTo('/app/setting')"><DoodleIcon name="settings" :size="20"/>偏好设置</button>
            <button type="button" @click="navigateTo('/app/doc')"><DoodleIcon name="note" :size="20"/>帮助文档</button>
          </div>

          <div class="theme-row">
            <span>纸张颜色</span>
            <div>
              <button :class="{active: getThemeSetting() === 'light'}" title="浅色纸张" type="button" @click="setTheme('light')"><DoodleIcon name="sun" :size="18"/></button>
              <button :class="{active: getThemeSetting() === 'dark'}" title="黑板模式" type="button" @click="setTheme('dark')"><DoodleIcon name="moon" :size="18"/></button>
              <button :class="{active: getThemeSetting() === 'auto'}" title="跟随系统" type="button" @click="setTheme('auto')">A</button>
            </div>
          </div>

          <button class="logout-button" type="button" @click="handleLogout"><DoodleIcon name="logout" :size="20"/>退出登录</button>
        </section>
      </transition>
    </div>
  </header>
</template>

<script lang="ts" setup>
import {computed, onMounted, onUnmounted, ref} from 'vue'
import {useRouter} from 'vue-router'
import useTheme from '@/hooks/theme'
import {useUserStore} from '@/stores/user'
import {getDefaultAvatarUrl, getUserAvatarUrl} from '@/utils/authHelpers'
import DoodleIcon from '@/components/doodle/DoodleIcon.vue'

const props = defineProps<{
  todayStats: {words: number; days: number}
  showUserMenu: boolean
}>()

const emit = defineEmits<{(e: 'update:showUserMenu', value: boolean): void}>()
const router = useRouter()
const userStore = useUserStore()
const {setTheme, getThemeSetting} = useTheme()
const userMenuRef = ref<HTMLElement | null>(null)

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
.corner-brand,
.desk-status { position: absolute; top: 18px; pointer-events: auto; }
.corner-brand { left: clamp(18px, 3vw, 46px); display: flex; align-items: center; gap: 10px; padding: 5px; border: 0; color: var(--ink); background: transparent; cursor: pointer; text-align: left; transform: rotate(-1deg); }
.brand-mark { display: grid; width: 41px; height: 41px; place-items: center; border: 2px solid var(--ink); border-radius: 52% 44% 48% 42%; color: var(--paper-card); background: var(--ink); box-shadow: 3px 3px 0 var(--pencil-red); font-family: var(--font-display); font-size: 25px; font-style: italic; }
.brand-copy strong { display: block; font-family: var(--font-display); font-size: 18px; }
.brand-copy small { display: block; color: var(--text-secondary); font-size: 10px; letter-spacing: .08em; }
.desk-status { right: clamp(18px, 3vw, 46px); display: flex; align-items: center; gap: 12px; }
.today-note { display: flex; align-items: center; gap: 12px; padding: 8px 13px; border: 1.6px solid var(--ink); border-radius: 12px 9px 13px 10px; background: color-mix(in srgb, var(--paper-card) 88%, transparent); box-shadow: 3px 4px 0 color-mix(in srgb, var(--ink) 13%, transparent); font-family: var(--font-hand); font-size: 11px; transform: rotate(.8deg); }
.today-note span { display: flex; align-items: baseline; gap: 4px; white-space: nowrap; }
.today-note strong { color: var(--pencil-red); font-size: 17px; }
.today-note i { width: 1px; height: 22px; background: var(--border-color); }
.avatar-button { width: 44px; height: 44px; padding: 3px; overflow: hidden; border: 2px solid var(--ink); border-radius: 47% 53% 45% 55%; background: var(--paper-card); box-shadow: 3px 4px 0 var(--chalk-yellow); cursor: pointer; transform: rotate(2deg); }
.avatar-button img { width: 100%; height: 100%; border-radius: inherit; object-fit: cover; }
.user-note { position: absolute; top: 58px; right: 0; width: min(330px, calc(100vw - 30px)); padding: 28px 24px 20px; border: 2px solid var(--ink); background: var(--paper-card); box-shadow: 9px 11px 0 color-mix(in srgb, var(--ink) 18%, transparent); transform: rotate(.4deg); }
.note-tape { position: absolute; top: -10px; left: 105px; width: 95px; height: 24px; border: 1px solid color-mix(in srgb, var(--ink) 18%, transparent); background: color-mix(in srgb, var(--chalk-yellow) 72%, transparent); transform: rotate(-3deg); }
.user-heading { display: flex; align-items: center; gap: 12px; padding-bottom: 18px; border-bottom: 1px dashed var(--ink); }
.user-heading img { width: 43px; height: 43px; border: 1.5px solid var(--ink); border-radius: 50%; object-fit: cover; }
.user-heading strong, .user-heading small { display: block; }
.user-heading strong { font-family: var(--font-display); font-size: 19px; }
.user-heading small { max-width: 210px; margin-top: 3px; overflow: hidden; color: var(--text-secondary); font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.note-links { display: grid; gap: 4px; padding-block: 12px; }
.note-links button,
.logout-button { display: flex; width: 100%; align-items: center; gap: 10px; padding: 10px; border: 0; border-radius: 9px; color: var(--ink); background: transparent; cursor: pointer; font: inherit; font-weight: 700; text-align: left; }
.note-links button:hover { background: var(--hover-bg); transform: rotate(-.5deg); }
.theme-row { display: flex; align-items: center; justify-content: space-between; padding: 12px 4px; border-block: 1px dashed var(--border-color); color: var(--text-secondary); font-size: 12px; font-weight: 700; }
.theme-row > div { display: flex; gap: 5px; }
.theme-row button { display: grid; width: 31px; height: 31px; place-items: center; border: 1.5px solid var(--ink); border-radius: 8px; color: var(--ink); background: transparent; cursor: pointer; font-weight: 800; }
.theme-row button.active { color: var(--paper-card); background: var(--ink); }
.logout-button { margin-top: 8px; color: var(--pencil-red); }
.logout-button:hover { background: color-mix(in srgb, var(--pencil-red) 12%, transparent); }
.corner-brand:focus-visible,
.avatar-button:focus-visible,
.user-note button:focus-visible { outline: 3px solid var(--pencil-red); outline-offset: 3px; }
.paper-drop-enter-active, .paper-drop-leave-active { transition: opacity .18s ease, transform .18s ease; }
.paper-drop-enter-from, .paper-drop-leave-to { opacity: 0; transform: translateY(-10px) rotate(2deg); }

@media (max-width: 700px) {
  .brand-copy small, .today-note { display: none; }
  .corner-brand, .desk-status { top: 13px; }
  .corner-brand { left: 13px; }
  .desk-status { right: 13px; }
}
</style>