<template>
  <div v-if="props.visible" class="confirm-dialog-overlay" @click.self="handleCancel">
    <div class="confirm-dialog">
      <div class="dialog-header">
        <h3 class="dialog-title">{{ props.title }}</h3>
      </div>
      
      <div class="dialog-content">
        <p class="dialog-message">{{ props.message }}</p>
      </div>
      
      <div class="dialog-actions">
        <button 
          class="btn btn-cancel" 
          @click="handleCancel"
          :disabled="props.loading"
        >
          {{ props.cancelText }}
        </button>
        <button 
          class="btn btn-confirm" 
          @click="handleConfirm"
          :disabled="props.loading"
        >
          <span v-if="props.loading" class="loading-spinner"></span>
          {{ props.confirmText }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  visible: boolean
  title?: string
  message: string
  confirmText?: string
  cancelText?: string
  loading?: boolean
}

interface Emits {
  (e: 'update:visible', value: boolean): void
  (e: 'confirm'): void
  (e: 'cancel'): void
}

const props = withDefaults(defineProps<Props>(), {
  title: '确认操作',
  confirmText: '确定',
  cancelText: '取消',
  loading: false
})

const emit = defineEmits<Emits>()

// 处理确认
const handleConfirm = () => {
  emit('confirm')
}

// 处理取消
const handleCancel = () => {
  emit('update:visible', false)
  emit('cancel')
}
</script>

<style scoped lang="scss">
.confirm-dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
  box-sizing: border-box;
}

.confirm-dialog {
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  max-width: 400px;
  width: 100%;
  overflow: hidden;
  animation: dialogSlideIn 0.3s ease-out;
}

.dialog-header {
  padding: 1.5rem 1.5rem 0 1.5rem;
  
  .dialog-title {
    font-size: 1.25rem;
    font-weight: 600;
    color: #1f2937;
    margin: 0;
  }
}

.dialog-content {
  padding: 1rem 1.5rem;
  
  .dialog-message {
    color: #6b7280;
    line-height: 1.6;
    margin: 0;
  }
}

.dialog-actions {
  padding: 0 1.5rem 1.5rem 1.5rem;
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
}

.btn {
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  font-size: 0.875rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-cancel {
  background: #f3f4f6;
  color: #6b7280;
  
  &:hover:not(:disabled) {
    background: #e5e7eb;
  }
}

.btn-confirm {
  background: #3b82f6;
  color: white;
  
  &:hover:not(:disabled) {
    background: #2563eb;
  }
}

.loading-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top: 2px solid white;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes dialogSlideIn {
  from {
    opacity: 0;
    transform: translateY(-20px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* 响应式设计 */
@media (max-width: 480px) {
  .confirm-dialog {
    margin: 0 1rem;
    max-width: none;
  }
  
  .dialog-header {
    padding: 1rem 1rem 0 1rem;
    
    .dialog-title {
      font-size: 1.125rem;
    }
  }
  
  .dialog-content {
    padding: 0.75rem 1rem;
  }
  
  .dialog-actions {
    padding: 0 1rem 1rem 1rem;
    flex-direction: column-reverse;
    
    .btn {
      width: 100%;
      justify-content: center;
    }
  }
}

/* 暗色主题支持 */
@media (prefers-color-scheme: dark) {
  .confirm-dialog {
    background: #1f2937;
    
    .dialog-title {
      color: #f9fafb;
    }
    
    .dialog-message {
      color: #d1d5db;
    }
  }
  
  .btn-cancel {
    background: #374151;
    color: #d1d5db;
    
    &:hover:not(:disabled) {
      background: #4b5563;
    }
  }
}
</style>