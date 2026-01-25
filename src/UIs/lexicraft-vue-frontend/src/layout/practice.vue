<script lang="ts" setup>
import {computed, ref} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import PracticeExitDialog from '@/components/PracticeExitDialog.vue'

const router = useRouter()
const route = useRoute()

// 练习统计数据
const currentIndex = ref(1)
const totalCount = ref(100)
const accuracy = ref<number | null>(null)
const showStats = ref(true)

// 退出确认对话框状态
const showExitConfirm = ref(false)

// 退出目标
const listTarget = computed(() => {
  if (route.path.includes('/words')) return '/app/words'
  if (route.path.includes('/articles')) return '/app/articles'
  return '/app/dashboard'
})

const listLabel = computed(() => {
  if (route.path.includes('/words')) return '返回单词列表'
  if (route.path.includes('/articles')) return '返回文章列表'
  return '返回课程列表'
})

// 练习标题
const practiceTitle = computed(() => {
  if (route.path.includes('/words')) {
    return '单词练习'
  } else if (route.path.includes('/articles')) {
    return '文章背诵'
  }
  return '学习练习'
})

const exitToHome = () => {
  router.push('/app/dashboard')
}

const exitToList = () => {
  router.push(listTarget.value)
}

// 暴露方法供子组件调用
defineExpose({
  updateStats: (current: number, total: number, acc?: number) => {
    currentIndex.value = current
    totalCount.value = total
    if (acc !== undefined) {
      accuracy.value = acc
    }
  },
  setTitle: (title: string) => {
    // 可以动态设置标题
  }
})
</script>

<template>
  <div class="practice-layout">
    <!-- 练习页面头部 -->
    <header class="practice-header">
      <div class="header-left">
        <button :title="'退出练习'" class="exit-btn icon-only" @click="showExitConfirm = true">
          <IconFluentArrowLeft24Filled class="exit-icon"/>
        </button>
      </div>

      <div class="header-center">
        <h1 class="practice-title">{{ practiceTitle }}</h1>
      </div>

      <div class="header-right">
        <div v-if="showStats" class="practice-stats">
          <div class="stat-item">
            <span class="stat-label">进度</span>
            <span class="stat-value">{{ currentIndex }}/{{ totalCount }}</span>
          </div>
          <div v-if="accuracy !== null" class="stat-item">
            <span class="stat-label">正确率</span>
            <span class="stat-value">{{ accuracy }}%</span>
          </div>
        </div>
      </div>
    </header>

    <!-- 练习内容区域 -->
    <main class="practice-content">
      <router-view></router-view>
    </main>

    <!-- 退出确认弹出层 -->
    <PracticeExitDialog
        v-model:visible="showExitConfirm"
        :list-label="listLabel"
        @continue="showExitConfirm = false"
        @exit-home="exitToHome"
        @exit-list="exitToList"
    />
  </div>
</template>

<style lang="scss" scoped>
.practice-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: #f8fafc;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

.practice-header {
  height: 60px;
  background: white;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  padding: 0 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  z-index: 100;
  flex-shrink: 0; // 防止头部被压缩

  .header-left {
    display: flex;
    align-items: center;
    min-width: 120px;

    .exit-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background: transparent;
      border: 1px solid transparent;
      border-radius: 10px;
      cursor: pointer;
      transition: all 0.2s;
      color: #475569;

      &:hover {
        background: #f1f5f9;
        color: #1e293b;
        border-color: #e2e8f0;
      }

      &:active {
        transform: scale(0.95);
      }

      .exit-icon {
        font-size: 1.25rem;
      }
    }
  }

  .header-center {
    flex: 1;
    display: flex;
    justify-content: center;

    .practice-title {
      font-size: 1.25rem;
      font-weight: 600;
      color: #1e293b;
      margin: 0;
    }
  }

  .header-right {
    display: flex;
    align-items: center;
    min-width: 120px;
    justify-content: flex-end;

    .practice-stats {
      display: flex;
      gap: 1.5rem;

      .stat-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;

        .stat-label {
          font-size: 0.75rem;
          color: #64748b;
          margin-bottom: 0.125rem;
        }

        .stat-value {
          font-size: 0.875rem;
          font-weight: 600;
          color: #3b82f6;
        }
      }
    }
  }
}

.practice-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  position: relative; // 为内部绝对定位元素提供参考
}

/* 响应式设计 */
@media (max-width: 768px) {
  .practice-header {
    padding: 0 1rem;
    height: 50px;

    .header-left {
      min-width: 80px;

      .exit-btn {
        padding: 0.375rem 0.75rem;

        .exit-text {
          display: none;
        }
      }
    }

    .header-center {
      .practice-title {
        font-size: 1.125rem;
      }
    }

    .header-right {
      min-width: 80px;

      .practice-stats {
        gap: 1rem;

        .stat-item {
          .stat-label {
            font-size: 0.625rem;
          }

          .stat-value {
            font-size: 0.75rem;
          }
        }
      }
    }
  }
}

@media (max-width: 480px) {
  .practice-header {
    .practice-stats {
      .stat-item {
        .stat-label {
          display: none;
        }
      }
    }
  }
}
</style>