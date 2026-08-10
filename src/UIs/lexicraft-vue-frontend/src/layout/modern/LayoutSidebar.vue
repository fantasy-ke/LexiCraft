<template>
  <nav class="theme-dock" aria-label="学习空间导航">
    <button
        v-for="(item, index) in navItems"
        :key="item.path"
        :aria-current="isActiveRoute(item.path) ? 'page' : undefined"
        :class="{active: isActiveRoute(item.path)}"
        :title="item.label"
        class="dock-item"
        type="button"
        @click="navigateTo(item.path)"
    >
      <span class="dock-index">{{ String(index + 1).padStart(2, '0') }}</span>
      <span class="icon-wrap"><DoodleIcon :name="item.icon" :size="22"/></span>
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
  {path: '/app/dashboard', label: '今日书桌', shortLabel: '今日', icon: 'home'},
  {path: '/app/words', label: '词汇书架', shortLabel: '词汇', icon: 'words'},
  {path: '/app/articles', label: '阅读书页', shortLabel: '阅读', icon: 'book'},
  {path: '/app/doc', label: '学习手记', shortLabel: '手记', icon: 'note'},
  {path: '/app/feedback', label: '意见便笺', shortLabel: '反馈', icon: 'feedback'},
  {path: '/app/qa', label: '帮助问答', shortLabel: '问答', icon: 'help'},
  {path: '/app/setting', label: '学习设置', shortLabel: '设置', icon: 'settings'}
] as const

const navigateTo = (path: string) => router.push(path)
const isActiveRoute = (path: string) => {
  if (path === '/app/dashboard') return route.path === '/app' || route.path === '/app/dashboard'
  return route.path === path || route.path.startsWith(`${path}/`)
}
</script>

<style lang="scss" scoped>
.theme-dock { position: fixed; z-index: 80; display: flex; overflow: auto; scrollbar-width: none; }
.theme-dock::-webkit-scrollbar { display: none; }
.dock-item { position: relative; display: flex; flex: 0 0 auto; align-items: center; border: 0; color: var(--text-secondary); background: transparent; cursor: pointer; font: inherit; transition: color .18s ease, background .18s ease, transform .18s ease; }
.dock-index { display: none; font-family: var(--font-mono); }
.icon-wrap { display: grid; place-items: center; }
.dock-label { white-space: nowrap; }
.active-mark { position: absolute; background: var(--accent); }
.dock-item:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 3px; }

/* A ? Warm Editorial: a slim book index, not a conventional sidebar. */
:global(html[data-theme-style='editorial'] .theme-dock) { top: 50%; left: 18px; max-height: calc(100vh - 150px); flex-direction: column; gap: 0; border-block: 1px solid var(--border-color); background: var(--surface-overlay); box-shadow: var(--control-shadow); transform: translateY(-50%); backdrop-filter: blur(12px); }
:global(html[data-theme-style='editorial'] .dock-item) { min-width: 88px; justify-content: flex-start; gap: 8px; padding: 11px 12px; border-bottom: 1px solid var(--border-color); font-family: var(--font-heading); font-size: 12px; }
:global(html[data-theme-style='editorial'] .dock-item:last-child) { border-bottom: 0; }
:global(html[data-theme-style='editorial'] .dock-item:hover) { color: var(--text-primary); background: var(--hover-bg); }
:global(html[data-theme-style='editorial'] .dock-item.active) { color: var(--accent); background: var(--surface-card); }
:global(html[data-theme-style='editorial'] .icon-wrap) { opacity: .7; }
:global(html[data-theme-style='editorial'] .active-mark) { top: 0; bottom: 0; left: -1px; width: 3px; }

/* B ? Zen Focus: a quiet command line made of words only. */
:global(html[data-theme-style='zen'] .theme-dock) { right: 28px; bottom: 22px; left: 28px; justify-content: center; gap: clamp(12px, 3vw, 38px); background: color-mix(in srgb, var(--surface-page) 88%, transparent); backdrop-filter: blur(10px); }
:global(html[data-theme-style='zen'] .dock-item) { gap: 7px; padding: 7px 0; font-family: var(--font-mono); font-size: 11px; letter-spacing: .05em; }
:global(html[data-theme-style='zen'] .dock-index) { display: inline; color: var(--text-tertiary); font-size: 9px; }
:global(html[data-theme-style='zen'] .icon-wrap) { display: none; }
:global(html[data-theme-style='zen'] .dock-item:hover), :global(html[data-theme-style='zen'] .dock-item.active) { color: var(--text-primary); }
:global(html[data-theme-style='zen'] .active-mark) { right: 0; bottom: 0; left: 0; height: 1px; }

/* C ? Playful Ink: a doodled floating tray. */
:global(html[data-theme-style='ink'] .theme-dock) { right: 50%; bottom: 18px; max-width: min(760px, calc(100vw - 30px)); gap: 2px; padding: 8px 10px; border: 2px solid var(--border-strong); border-radius: 20px 15px 23px 17px; background: var(--surface-overlay); box-shadow: var(--card-shadow); transform: translateX(50%) rotate(-.25deg); backdrop-filter: blur(14px); }
:global(html[data-theme-style='ink'] .dock-item) { min-width: 70px; flex-direction: column; gap: 3px; padding: 7px 11px 6px; border-radius: 13px 10px 14px 11px; }
:global(html[data-theme-style='ink'] .dock-item:hover) { color: var(--text-primary); background: var(--hover-bg); transform: translateY(-3px) rotate(-1deg); }
:global(html[data-theme-style='ink'] .dock-item.active) { color: var(--accent-contrast); background: var(--accent); transform: translateY(-5px) rotate(1deg); }
:global(html[data-theme-style='ink'] .dock-label) { font-size: 11px; font-weight: 800; }
:global(html[data-theme-style='ink'] .active-mark) { top: 5px; right: 8px; width: 7px; height: 7px; border-radius: 50%; background: var(--chalk-yellow); }

@media (max-width: 760px) {
  :global(html[data-theme-style='editorial'] .theme-dock) { top: auto; right: 10px; bottom: 9px; left: 10px; max-height: none; flex-direction: row; transform: none; }
  :global(html[data-theme-style='editorial'] .dock-item) { min-width: 64px; flex: 1; justify-content: center; padding: 10px 7px; border-right: 1px solid var(--border-color); border-bottom: 0; }
  :global(html[data-theme-style='editorial'] .dock-item:last-child) { border-right: 0; }
  :global(html[data-theme-style='editorial'] .icon-wrap) { display: none; }
  :global(html[data-theme-style='editorial'] .active-mark) { top: auto; right: 0; bottom: -1px; left: 0; width: auto; height: 3px; }
  :global(html[data-theme-style='zen'] .theme-dock) { right: 12px; bottom: 8px; left: 12px; justify-content: flex-start; overflow-x: auto; }
  :global(html[data-theme-style='zen'] .dock-item) { min-width: 62px; justify-content: center; }
  :global(html[data-theme-style='ink'] .theme-dock) { right: 12px; left: 12px; bottom: 9px; max-width: none; transform: none; }
  :global(html[data-theme-style='ink'] .dock-item) { min-width: 62px; padding-inline: 8px; }
}
</style>
