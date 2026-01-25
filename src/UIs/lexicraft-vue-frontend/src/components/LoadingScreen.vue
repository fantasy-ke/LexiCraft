<template>
  <div :class="settingStore.theme" class="loading-screen">
    <!-- Logo 居中显示 -->
    <div class="center-logo">
      <div class="logo-box">
        <div class="logo-icon">
          <span class="text-white font-bold text-lg">LC</span>
        </div>
        <span class="logo-text">LexionCraft</span>
      </div>
    </div>

    <div class="loading-bottom-section">
      <!-- 提示句子 -->
      <div v-if="loadingText" class="loading-tip">
        <span class="quote">“</span>
        {{ loadingText }}
        <span class="quote">”</span>
      </div>

      <!-- 进度条区域 -->
      <div class="progress-area">
        <span class="loading-label">LOADING</span>
        <div class="progress-bar-container">
          <div :style="{ width: progress + '%' }" class="progress-bar-fill"></div>
        </div>
        <span class="percentage-label">{{ Math.round(progress) }}%</span>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {useSettingStore} from '@/stores/setting.ts'

interface Props {
  progress: number
  loadingText?: string
}

const settingStore = useSettingStore()

withDefaults(defineProps<Props>(), {
  loadingText: '正在加载学习内容...'
})
</script>

<style lang="scss" scoped>
.loading-screen {
  position: fixed;
  inset: 0;
  background: var(--color-primary);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  overflow: hidden;
  transition: background 0.5s ease;

  &.dark {
    background: #000000;
  }
}

.center-logo {
  margin-bottom: 8vh; // 向上移动一些

  .logo-box {
    display: flex;
    align-items: center;
    gap: 1rem;
    animation: breathing 3s ease-in-out infinite;

    .logo-icon {
      width: 48px;
      height: 48px;
      background: linear-gradient(135deg, #3b82f6, #1d4ed8);
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 4px 15px rgba(59, 130, 246, 0.3);
    }

    .logo-text {
      font-size: 2.2rem;
      font-weight: 800;
      color: var(--text-primary, #ffffff);
      letter-spacing: -0.02em;
    }
  }
}

.loading-bottom-section {
  position: absolute;
  bottom: 12%;
  left: 50%;
  transform: translateX(-50%);
  width: 80%;
  max-width: 600px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
}

.loading-tip {
  color: var(--text-secondary, #e5e7eb);
  font-size: 0.9rem;
  font-weight: 500;
  letter-spacing: 0.02rem;
  opacity: 0.9;
  text-align: center;
  max-width: 90%;

  .quote {
    color: #3b82f6; // 蓝色引号
    font-weight: bold;
    font-size: 1.2rem;
    padding: 0 0.4rem;
  }
}

.progress-area {
  display: flex;
  align-items: center;
  gap: 1.2rem;
  width: 100%;

  .loading-label, .percentage-label {
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 0.75rem;
    font-weight: 800;
    color: var(--text-tertiary, #9ca3af);
    letter-spacing: 0.15rem;
    min-width: 4rem;
  }

  .percentage-label {
    text-align: right;
  }

  .progress-bar-container {
    flex: 1;
    height: 8px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 2px; // 硬朗风格
    overflow: hidden;
    position: relative;

    .progress-bar-fill {
      height: 100%;
      background: linear-gradient(90deg, #3b82f6, #1d4ed8); // 蓝色渐变
      transition: width 0.4s cubic-bezier(0.4, 0, 0.2, 1);
      position: relative;

      &::after {
        content: '';
        position: absolute;
        inset: 0;
        background: linear-gradient(
                90deg,
                rgba(255, 255, 255, 0) 0%,
                rgba(255, 255, 255, 0.2) 50%,
                rgba(255, 255, 255, 0) 100%
        );
        animation: shimmer 2s infinite;
      }
    }
  }
}

@keyframes breathing {
  0%, 100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.03);
    opacity: 0.9;
  }
}

@keyframes shimmer {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(100%);
  }
}

/* 响应式设计 */
@media (max-width: 768px) {
  .center-logo {
    margin-bottom: 4vh; // 移动端减少向上移动的幅度

    .logo-box {
      .logo-icon {
        width: 40px;
        height: 40px;
      }

      .logo-text {
        font-size: 1.8rem;
      }
    }
  }

  .loading-bottom-section {
    width: 90%;
    bottom: 15%;
  }

  .progress-area {
    gap: 0.5rem;

    .loading-label, .percentage-label {
      font-size: 0.65rem;
      min-width: 2.5rem;
    }
  }
}
</style>