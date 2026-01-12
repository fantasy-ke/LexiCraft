<script setup lang="ts">
import { useSettingStore } from '@/stores/setting'

interface Props {
  visible: boolean
  listLabel: string
}

const props = defineProps<Props>()
const emit = defineEmits(['update:visible', 'exit-home', 'exit-list', 'continue'])

const settingStore = useSettingStore()

const handleClose = () => {
  emit('update:visible', false)
}

const exitHome = () => {
  emit('exit-home')
  handleClose()
}

const exitList = () => {
  emit('exit-list')
  handleClose()
}
</script>

<template>
  <div v-if="visible" class="exit-dialog-overlay" @click.self="handleClose">
    <div class="exit-dialog" :class="settingStore.theme">
      <div class="dialog-header">
        <h3 class="title">退出练习</h3>
        <button class="close-icon-btn" @click="handleClose">✕</button>
      </div>

      <div class="dialog-body">
        <!-- 提示卡片 -->
        <div class="notice-card">
          短暂的休息能让学习更有效率，期待你的回归！
        </div>

        <!-- 选项列表 -->
        <div class="options-list">
          <div class="option-item" @click="exitHome">
            <div class="option-left">
              <IconFluentHome20Regular class="icon" />
              <span class="label">返回首页</span>
            </div>
            <IconFluentArrowRight16Regular class="arrow" />
          </div>

          <div class="option-item" @click="exitList">
            <div class="option-left">
              <IconFluentList20Regular v-if="listLabel.includes('列表')" class="icon" />
              <IconFluentBook20Regular v-else class="icon" />
              <span class="label">{{ listLabel }}</span>
            </div>
            <IconFluentArrowRight16Regular class="arrow" />
          </div>
        </div>

        <!-- 底部按钮 -->
        <button class="continue-btn" @click="$emit('continue')">
          <IconFluentPlay20Regular class="play-icon" /> 继续学习
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.exit-dialog-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10000;
  padding: 1.5rem;
}

.exit-dialog {
  background: var(--bg-card-primary, #ffffff);
  border-radius: 4px; // 菱角分明
  width: 100%;
  max-width: 400px;
  box-shadow: 0 15px 40px rgba(0, 0, 0, 0.2);
  overflow: hidden;
  animation: slideUp 0.3s ease-out;

  &.dark {
    --bg-card-primary: #1e1f22;
    --color-border: #2b2d31;
    --color-text-main: #f2f3f5;
    --color-text-sub: #b5bac1;
    --color-hover: #35373c;
    --notice-bg: rgba(59, 130, 246, 0.1);
    --color-brand: #3b82f6;
  }

  --color-text-main: #111827;
  --color-text-sub: #6b7280;
  --color-border: #f3f4f6;
  --color-hover: #f9fafb;
  --notice-bg: #eff6ff;
  --color-brand: #3b82f6;
}

.dialog-header {
  padding: 1rem 1.2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid var(--color-border);

  .title {
    font-size: 1.1rem;
    font-weight: 700;
    color: var(--color-text-main);
    margin: 0;
  }

  .close-icon-btn {
    background: none;
    border: none;
    color: var(--color-text-sub);
    font-size: 1.1rem;
    cursor: pointer;
    padding: 0.3rem;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s;

    &:hover {
      color: var(--color-text-main);
    }
  }
}

.dialog-body {
  padding: 1.2rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.notice-card {
  background: var(--notice-bg);
  color: var(--color-brand);
  padding: 0.8rem 1rem;
  border-radius: 2px; // 菱角分明
  font-size: 0.85rem;
  font-weight: 500;
  line-height: 1.4;
  text-align: center;
  border-left: 3px solid var(--color-brand);
}

.options-list {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.option-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.7rem 1rem; // 高度减小
  border: 1px solid var(--color-border);
  border-radius: 2px; // 菱角分明
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: var(--color-brand);
    background: var(--color-hover);
  }

  .option-left {
    display: flex;
    align-items: center;
    gap: 0.8rem;

    .icon {
      font-size: 1.1rem;
      color: var(--color-brand);
    }

    .label {
      font-size: 0.9rem;
      font-weight: 600;
      color: var(--text-secondary);
    }
  }

  .arrow {
    color: var(--text-tertiary);
    font-size: 0.9rem;
    transition: transform 0.2s;
  }

  &:hover .arrow {
    transform: translateX(3px);
    color: var(--color-brand);
  }
}

.continue-btn {
  margin-top: 0.4rem;
  background: var(--color-brand);
  color: white;
  border: none;
  border-radius: 2px; // 菱角分明
  padding: 0.8rem; // 高度减小
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  transition: all 0.2s;

  &:hover {
    background: #2563eb;
    opacity: 0.9;
  }

  &:active {
    transform: scale(0.98);
  }

  .play-icon {
    font-size: 1.1rem;
  }
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 480px) {
  .exit-dialog {
    max-width: 100%;
  }
}
</style>
