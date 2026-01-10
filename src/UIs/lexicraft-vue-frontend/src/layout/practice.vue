<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import ConfirmDialog from '@/components/base/ConfirmDialog.vue'

const router = useRouter()
const route = useRoute()

// 练习统计数据
const currentIndex = ref(1)
const totalCount = ref(100)
const accuracy = ref<number | null>(null)
const showStats = ref(true)

// 退出确认对话框状态
const showExitConfirm = ref(false)

// 练习标题
const practiceTitle = computed(() => {
  if (route.path.includes('practice-words')) {
    return '单词练习'
  } else if (route.path.includes('practice-articles')) {
    return '文章背诵'
  }
  return '学习练习'
})

// 退出学习确认处理
const handleExitConfirm = () => {
  showExitConfirm.value = false
  
  // 根据当前路径决定返回哪个页面
  if (route.path.includes('practice-words')) {
    router.push('/app/words')
  } else if (route.path.includes('practice-articles')) {
    router.push('/app/articles')
  } else {
    router.push('/app/dashboard')
  }
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
        <button class="exit-btn" @click="showExitConfirm = true" :title="'退出学习'">
          <i class="exit-icon">←</i>
          <span class="exit-text">退出</span>
        </button>
      </div>
      
      <div class="header-center">
        <h1 class="practice-title">{{ practiceTitle }}</h1>
      </div>
      
      <div class="header-right">
        <div class="practice-stats" v-if="showStats">
          <div class="stat-item">
            <span class="stat-label">进度</span>
            <span class="stat-value">{{ currentIndex }}/{{ totalCount }}</span>
          </div>
          <div class="stat-item" v-if="accuracy !== null">
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
    
    <!-- 退出确认对话框 -->
    <ConfirmDialog
      v-model:visible="showExitConfirm"
      title="退出学习"
      message="确定要退出当前学习吗？学习进度将会保存。"
      confirm-text="退出"
      cancel-text="继续学习"
      @confirm="handleExitConfirm"
    />
  </div>
</template>

<style scoped lang="scss">
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
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
      font-size: 0.875rem;
      color: #475569;

      &:hover {
        background: #e2e8f0;
        border-color: #cbd5e1;
      }

      .exit-icon {
        font-size: 1.125rem;
        font-weight: bold;
      }

      .exit-text {
        font-weight: 500;
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