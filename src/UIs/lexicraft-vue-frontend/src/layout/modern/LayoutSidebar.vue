<template>
  <nav class="ink-dock" aria-label="主要功能">
    <button
        v-for="item in navItems"
        :key="item.path"
        :aria-current="isActiveRoute(item.path) ? 'page' : undefined"
        :class="{active: isActiveRoute(item.path)}"
        :title="item.label"
        class="dock-item"
        type="button"
        @click="navigateTo(item.path)"
    >
      <span class="icon-wrap"><DoodleIcon :name="item.icon" :size="23"/></span>
      <span class="dock-label">{{ item.shortLabel }}</span>
      <span v-if="isActiveRoute(item.path)" class="active-mark" aria-hidden="true"></span>
    </button>
  </nav>
</template>

<script lang="ts" setup>
import {useRoute, useRouter} from 'vue-router'
import DoodleIcon from '@/components/doodle/DoodleIcon.vue'

const router = useRouter()
const route = useRoute()

const navItems = [
  {path: '/app/dashboard', label: '我的学习桌', shortLabel: '主页', icon: 'home'},
  {path: '/app/words', label: '单词练习', shortLabel: '单词', icon: 'words'},
  {path: '/app/articles', label: '文章背诵', shortLabel: '文章', icon: 'book'},
  {path: '/app/doc', label: '学习资料', shortLabel: '资料', icon: 'note'},
  {path: '/app/feedback', label: '反馈建议', shortLabel: '反馈', icon: 'feedback'},
  {path: '/app/qa', label: '帮助中心', shortLabel: '帮助', icon: 'help'},
  {path: '/app/setting', label: '偏好设置', shortLabel: '设置', icon: 'settings'}
] as const

const navigateTo = (path: string) => router.push(path)

const isActiveRoute = (path: string) => {
  if (path === '/app/dashboard') return route.path === '/app' || route.path === '/app/dashboard'
  return route.path === path || route.path.startsWith(`${path}/`)
}
</script>

<style lang="scss" scoped>
.ink-dock {
  position: fixed;
  z-index: 80;
  right: 50%;
  bottom: 18px;
  display: flex;
  max-width: min(760px, calc(100vw - 30px));
  gap: 2px;
  padding: 8px 10px;
  overflow-x: auto;
  border: 2px solid var(--ink);
  border-radius: 20px 15px 23px 17px;
  background: color-mix(in srgb, var(--paper-card) 91%, transparent);
  box-shadow: 7px 8px 0 color-mix(in srgb, var(--ink) 18%, transparent);
  backdrop-filter: blur(14px);
  transform: translateX(50%) rotate(-.25deg);
  scrollbar-width: none;
}

.ink-dock::-webkit-scrollbar { display: none; }

.dock-item {
  position: relative;
  display: flex;
  min-width: 70px;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  padding: 7px 11px 6px;
  border: 0;
  border-radius: 13px 10px 14px 11px;
  color: var(--text-secondary);
  background: transparent;
  cursor: pointer;
  font: inherit;
  transition: color .18s ease, background .18s ease, transform .18s ease;
}

.dock-item:hover {
  color: var(--ink);
  background: var(--hover-bg);
  transform: translateY(-3px) rotate(-1deg);
}

.dock-item.active {
  color: var(--paper-card);
  background: var(--ink);
  transform: translateY(-5px) rotate(1deg);
}

.icon-wrap { display: grid; min-height: 25px; place-items: center; }
.dock-label { font-size: 11px; font-weight: 800; white-space: nowrap; }
.active-mark { position: absolute; right: 8px; top: 5px; width: 7px; height: 7px; border-radius: 50%; background: var(--chalk-yellow); }

.dock-item:focus-visible { outline: 3px solid var(--pencil-red); outline-offset: 3px; }

@media (max-width: 680px) {
  .ink-dock { right: 12px; left: 12px; bottom: 9px; max-width: none; justify-content: flex-start; transform: none; }
  .dock-item { min-width: 62px; padding-inline: 8px; }
}
</style>