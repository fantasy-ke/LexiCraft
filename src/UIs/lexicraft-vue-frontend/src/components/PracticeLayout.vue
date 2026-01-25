<script lang="ts" setup>
import {useSettingStore} from '@/stores/setting'

// 组件属性定义
interface Props {
  maxWidth?: string
}

const props = withDefaults(defineProps<Props>(), {
  maxWidth: 'var(--toolbar-width)'
})
const settingStore = useSettingStore()
</script>

<template>
  <!-- 练习内容区域 - 使用栅格化布局 -->
  <div :class="!settingStore.showToolbar && 'footer-hide'" class="practice-content-wrapper">
    <!-- 快捷键提示区域 -->
    <div class="hints-wrap">
      <slot name="hints"></slot>
    </div>

    <!-- 布局内部容器 - 用于居中定位 -->
    <div class="practice-layout-inner">
      <!-- 练习主体区域 - 栅格布局 -->
      <div class="practice-grid">
        <!-- 导航区域 - 固定高度 -->
        <div class="navigation-area">
          <slot name="navigation"></slot>
        </div>

        <!-- 内容区域 - 自适应高度 -->
        <div class="content-area">
          <slot name="practice"></slot>
        </div>

        <!-- 底部操作区域 - 固定高度 -->
        <div class="action-area">
          <!-- 预留给底部操作按钮 -->
        </div>
      </div>

      <!-- 侧边面板 - 使用 fixed 或绝对定位相对于 inner 容器 -->
      <div
          :class="{ 'has-panel': settingStore.showPanel }"
          class="panel-wrap"
          @click.self="settingStore.showPanel = false"
      >
        <slot name="panel"></slot>
      </div>
    </div>

    <!-- 底部工具栏 -->
    <div class="footer-wrap">
      <slot name="footer"></slot>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.practice-content-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
  transition: all var(--anim-time);
  height: 100%; // 强制高度
  min-height: 0; // 解决 flex 子元素高度计算问题
}

// 布局内部容器
.practice-layout-inner {
  height: 100%; // 必须有高度，否则内部 grid 无法撑开实现内部滚动
  width: 100%;
  max-width: v-bind('props.maxWidth');
  margin: 0 auto;
  position: relative;
  display: flex;
  flex-direction: column;
}

// 栅格化布局系统
.practice-grid {
  flex: 1;
  display: grid;
  grid-template-rows: 60px 1fr 60px; // 导航区域 内容区域 操作区域
  grid-template-areas: 
    "navigation"
    "content"
    "actions";
  height: 100%;
  overflow: hidden;
  width: 100%;
}

// 导航区域 - 固定高度，确保布局稳定
.navigation-area {
  grid-area: navigation;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 60px; // 明确指定高度，与 grid-template-rows 保持一致
  min-height: 60px;
}

// 内容区域 - 自适应高度，支持滚动
.content-area {
  grid-area: content;
  display: flex;
  flex-direction: column; // 改为列布局
  justify-content: flex-start; // 从顶部开始
  align-items: center; // 水平居中
  overflow-y: auto;
  overflow-x: hidden;
  position: relative;
  padding: 1rem 0; // 添加垂直内边距

  // 自定义滚动条样式
  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-track {
    background: rgba(0, 0, 0, 0.1);
    border-radius: 3px;
  }

  &::-webkit-scrollbar-thumb {
    background: rgba(0, 0, 0, 0.3);
    border-radius: 3px;

    &:hover {
      background: rgba(0, 0, 0, 0.5);
    }
  }
}

// 操作区域 - 固定高度60px
.action-area {
  grid-area: actions;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
}

.footer-wrap {
  position: absolute;
  bottom: 1.5rem;
  left: 50%;
  transform: translateX(-50%);
  transition: all var(--anim-time);
  z-index: 99;
}

.panel-wrap {
  position: absolute;
  top: 0;
  left: calc(100% + 1rem); // 始终在容器右侧 1rem 处
  z-index: 100;
  height: 100%;
  transition: all var(--anim-time);

  &.has-panel {
    box-shadow: -5px 0 15px rgba(0, 0, 0, 0.05);
  }
}

.hints-wrap {
  position: fixed;
  left: 1.5rem;
  top: 50%;
  transform: translateY(-50%);
  z-index: 200;
  transition: all var(--anim-time);
}

// 工具栏隐藏状态
.footer-hide {
  .footer-wrap {
    bottom: -6rem;
  }
}

// 移动端适配
@media (max-width: 768px) {
  .practice-grid {
    grid-template-rows: 50px 1fr 50px; // 移动端调整高度
  }

  .navigation-area {
    min-height: 50px; // 移动端导航区域最小高度
  }

  .panel-wrap {
    top: 50px; // 移动端头部高度
    height: calc(100vh - 50px);
  }

  .footer-hide {
    .footer-wrap {
      bottom: calc(-10rem + env(safe-area-inset-bottom, 0px));
    }
  }

  .footer-wrap {
    bottom: calc(0.5rem + env(safe-area-inset-bottom, 0px));
    left: 0.5rem;
    right: 0.5rem;
    width: auto;
    transform: none;
  }

  .panel-wrap {
    position: fixed;
    top: 0;
    left: 0 !important;
    right: 0 !important;
    bottom: 0;
    height: 100vh;
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1rem;
    box-sizing: border-box;

    // 当面板未显示时，禁用指针事件
    pointer-events: none;

    // 只有当面板显示时才添加背景蒙版并启用指针事件
    &.has-panel {
      background: rgba(0, 0, 0, 0.5);
      pointer-events: auto;
    }
  }
}

// 超小屏幕适配
@media (max-width: 480px) {
  .practice-grid {
    grid-template-rows: 40px 1fr 40px; // 超小屏幕进一步压缩
  }

  .navigation-area {
    min-height: 40px; // 超小屏幕导航区域最小高度
  }

  .footer-wrap {
    bottom: calc(0.3rem + env(safe-area-inset-bottom, 0px));
    left: 0.3rem;
    right: 0.3rem;
  }

  .panel-wrap {
    padding: 0.5rem;
    left: 0 !important;
    right: 0 !important;
  }
}
</style>