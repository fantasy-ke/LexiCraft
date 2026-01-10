<script setup lang="ts">
import Logo from '@/components/Logo.vue'
import { useSettingStore } from '@/stores/setting.ts'
import { useRouter } from 'vue-router'
import useTheme from '@/hooks/theme.ts'
import BaseIcon from '@/components/BaseIcon.vue'
import { useRuntimeStore } from '@/stores/runtime.ts'
import { jump2Feedback } from '@/utils'
import { watch } from 'vue'
import { ShortcutKey } from '@/types/enum.ts'

const settingStore = useSettingStore()
const runtimeStore = useRuntimeStore()
const router = useRouter()
const { toggleTheme, getTheme } = useTheme()

//首页为了seo被剥离出去了，现在是一个静态页面，用nginx 重定向控制对应的跳转
function goHome() {
  window.location.href = '/'
}
watch(
  () => settingStore.sideExpand,
  n => {
    document.documentElement.style.setProperty('--aside-width', n ? '12rem' : '4.5rem')
  },{immediate: true}
)
</script>

<template>
  <div class="layout anim">
    <!--    第一个aside 占位用-->
    <div class="aside space"></div>
    <div class="aside anim fixed">
      <div class="top">
        <Logo v-if="settingStore.sideExpand" />
        <div class="row" @click="goHome">
          <IconFluentHome20Regular />
          <span v-if="settingStore.sideExpand">主页</span>
        </div>
        <div class="row" @click="router.push('/words')">
          <IconFluentTextUnderlineDouble20Regular />
          <span v-if="settingStore.sideExpand">单词</span>
        </div>
        <div id="article" class="row" @click="router.push('/articles')">
          <!--          <IconPhArticleNyTimes/>-->
          <IconFluentBookLetter20Regular />
          <span v-if="settingStore.sideExpand">文章</span>
        </div>
        <div class="row" @click="router.push('/setting')">
          <IconFluentSettings20Regular />
          <span v-if="settingStore.sideExpand">设置</span>
          <div class="red-point" :class="!settingStore.sideExpand && 'top-1 right-0'" v-if="runtimeStore.isNew"></div>
        </div>
        <div class="row" @click="router.push('/feedback')">
          <IconFluentCommentEdit20Regular />
          <span v-if="settingStore.sideExpand">反馈</span>
        </div>
        <div class="row" @click="router.push('/doc')">
          <IconFluentDocument20Regular />
          <span v-if="settingStore.sideExpand">资料</span>
        </div>
        <div class="row" @click="router.push('/qa')">
          <IconFluentQuestionCircle20Regular />
          <span v-if="settingStore.sideExpand">帮助</span>
        </div>
        <!--        <div class="row" @click="router.push('/user')">-->
        <!--          <IconFluentPerson20Regular/>-->
        <!--          <span v-if="settingStore.sideExpand">用户</span>-->
        <!--        </div>-->
      </div>
      <div class="bottom flex justify-evenly">
        <BaseIcon @click="settingStore.sideExpand = !settingStore.sideExpand">
          <IconFluentChevronLeft20Filled v-if="settingStore.sideExpand" />
          <IconFluentChevronLeft20Filled class="transform-rotate-180" v-else />
        </BaseIcon>
        <BaseIcon
          v-if="settingStore.sideExpand"
          :title="`切换主题(${settingStore.shortcutKeyMap[ShortcutKey.ToggleTheme]})`"
          @click="toggleTheme"
        >
          <IconFluentWeatherMoon16Regular v-if="getTheme() === 'light'" />
          <IconFluentWeatherSunny16Regular v-else />
        </BaseIcon>
      </div>
    </div>

    <!-- 移动端顶部菜单栏 -->
    <div class="mobile-top-nav" :class="{ collapsed: settingStore.mobileNavCollapsed }">
      <div class="nav-items">
        <div class="nav-item" @click="router.push('/')" :class="{ active: $route.path === '/' }">
          <IconFluentHome20Regular />
          <span>主页</span>
        </div>
        <div class="nav-item" @click="router.push('/words')" :class="{ active: $route.path.includes('/words') }">
          <IconFluentTextUnderlineDouble20Regular />
          <span>单词</span>
        </div>
        <div class="nav-item" @click="router.push('/articles')" :class="{ active: $route.path.includes('/articles') }">
          <IconFluentBookLetter20Regular />
          <span>文章</span>
        </div>
        <div class="nav-item" @click="router.push('/setting')" :class="{ active: $route.path === '/setting' }">
          <IconFluentSettings20Regular />
          <span>设置</span>
          <div class="red-point" v-if="runtimeStore.isNew"></div>
        </div>
      </div>
      <div class="nav-toggle" @click="settingStore.mobileNavCollapsed = !settingStore.mobileNavCollapsed">
        <IconFluentChevronDown20Filled v-if="!settingStore.mobileNavCollapsed" />
        <IconFluentChevronUp20Filled v-else />
      </div>
    </div>

    <div class="flex-1 z-1 relative main-content overflow-x-hidden">
      <router-view></router-view>
    </div>
  </div>
</template>

<style scoped lang="scss">
.layout {
  width: 100%;
  height: 100%;
  display: flex;
  background: var(--color-primary);
}

.aside {
  background: var(--color-second);
  height: 100vh;
  padding: 1rem 1rem;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  box-shadow: rgb(0 0 0 / 3%) 0px 0px 12px 0px;
  width: var(--aside-width);
  z-index: 2;

  .row {
    @apply cursor-pointer rounded-md  text p-2 my-2 flex items-center gap-2 relative shrink-0;
    transition: all 0.5s;

    &:hover {
      background: var(--btn-primary);
      color: white;
    }

    span {
      flex-shrink: 0;
    }

    svg {
      flex-shrink: 0;
      font-size: 1.3rem !important;
    }
  }
}

// 移动端顶部菜单栏
.mobile-top-nav {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  background: var(--color-second);
  border-bottom: 1px solid var(--color-item-border);
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  z-index: 1000;
  transition: all 0.3s ease;

  .nav-items {
    display: flex;
    justify-content: space-around;
    padding: 0.5rem 0;

    .nav-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 0.5rem;
      cursor: pointer;
      transition: all 0.2s;
      min-height: 44px;
      min-width: 44px;
      justify-content: center;
      position: relative;

      svg {
        font-size: 1.2rem;
        margin-bottom: 0.2rem;
        color: var(--color-main-text);
      }

      span {
        font-size: 0.7rem;
        color: var(--color-main-text);
        text-align: center;
      }

      &.active {
        svg,
        span {
          color: var(--color-select-bg);
        }
      }

      &:active {
        transform: scale(0.95);
      }

      .red-point {
        position: absolute;
        top: 0.2rem;
        right: 0.2rem;
        width: 0.4rem;
        height: 0.4rem;
        background: #ff4444;
        border-radius: 50%;
      }
    }
  }

  .nav-toggle {
    position: absolute;
    bottom: -1.5rem;
    left: 50%;
    transform: translateX(-50%);
    background: var(--color-second);
    border: 1px solid var(--color-item-border);
    border-top: none;
    border-radius: 0 0 0.5rem 0.5rem;
    padding: 0.3rem 0.8rem;
    cursor: pointer;
    transition: all 0.3s;

    svg {
      font-size: 1rem;
      color: var(--color-main-text);
    }

    &:active {
      transform: translateX(-50%) scale(0.95);
    }
  }

  &.collapsed {
    transform: translateY(calc(-100% + 1.5rem));

    .nav-items {
      opacity: 0;
      pointer-events: none;
    }
  }
}

.main-content {
  // 移动端时为主内容区域添加顶部内边距，避免被顶部菜单遮挡
  @media (max-width: 768px) {
    padding-top: 4rem;
  }
}

// 移动端隐藏左侧菜单栏
@media (max-width: 768px) {
  .aside {
    display: none;
  }

  .aside.space {
    display: none;
  }

  .main-content {
    width: 100%;
    margin-left: 0;
  }
}

// 桌面端隐藏移动端顶部菜单栏
@media (min-width: 769px) {
  .mobile-top-nav {
    display: none;
  }
}
</style>
