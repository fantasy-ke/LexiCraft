<template>
  <div class="loading-screen">
    <div class="loading-content">
      <!-- Logo 图标 -->
      <div class="loading-logo">
        <div class="logo-icon">
          <span class="logo-text">TW</span>
        </div>
      </div>
      
      <!-- 加载文本 -->
      <div class="loading-text">
        {{ loadingText }}
      </div>
      
      <!-- 进度条 -->
      <div class="progress-container">
        <div class="progress-bar">
          <div class="progress-fill" :style="{ width: progress + '%' }"></div>
        </div>
        <div class="progress-percentage">{{ Math.round(progress) }}%</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  progress: number
  loadingText?: string
}

withDefaults(defineProps<Props>(), {
  loadingText: '正在加载学习内容...'
})
</script>

<style scoped lang="scss">
.loading-screen {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: #1a1a1a;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

.loading-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
  text-align: center;
}

.loading-logo {
  .logo-icon {
    width: 80px;
    height: 80px;
    background: linear-gradient(135deg, #667eea, #764ba2);
    border-radius: 20px;
    display: flex;
    align-items: center;
    justify-content: center;
    animation: pulse 2s ease-in-out infinite;
    
    .logo-text {
      font-size: 2rem;
      font-weight: 700;
      color: white;
    }
  }
}

.loading-text {
  color: #ffffff;
  font-size: 1.125rem;
  font-weight: 500;
  opacity: 0.9;
}

.progress-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  width: 300px;
  
  .progress-bar {
    width: 100%;
    height: 4px;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 2px;
    overflow: hidden;
    
    .progress-fill {
      height: 100%;
      background: linear-gradient(90deg, #667eea, #764ba2);
      border-radius: 2px;
      transition: width 0.3s ease;
      position: relative;
      
      &::after {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: linear-gradient(90deg, transparent, rgba(255,255,255,0.3), transparent);
        animation: shimmer 1.5s infinite;
      }
    }
  }
  
  .progress-percentage {
    color: #ffffff;
    font-size: 0.875rem;
    font-weight: 600;
    opacity: 0.8;
  }
}

@keyframes pulse {
  0%, 100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.05);
    opacity: 0.8;
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
  .progress-container {
    width: 250px;
  }
  
  .loading-logo .logo-icon {
    width: 60px;
    height: 60px;
    
    .logo-text {
      font-size: 1.5rem;
    }
  }
  
  .loading-text {
    font-size: 1rem;
  }
}
</style>