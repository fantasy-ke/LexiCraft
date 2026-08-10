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
            :disabled="props.loading"
            class="btn btn-cancel"
            @click="handleCancel"
        >
          {{ props.cancelText }}
        </button>
        <button
            :disabled="props.loading"
            class="btn btn-confirm"
            @click="handleConfirm"
        >
          <span v-if="props.loading" class="loading-spinner"></span>
          {{ props.confirmText }}
        </button>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
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

<style lang="scss" scoped>
.confirm-dialog-overlay { position: fixed; inset: 0; z-index: 9999; display: flex; align-items: center; justify-content: center; padding: 1rem; box-sizing: border-box; background: color-mix(in srgb, #000 55%, transparent); backdrop-filter: blur(6px); }
.confirm-dialog { width: min(100%, 420px); overflow: hidden; border: 1px solid var(--border-strong); border-radius: var(--radius-card); color: var(--text-primary); background: var(--surface-card); box-shadow: var(--card-shadow); animation: dialog-in .24s ease-out; }
.dialog-header { padding: 1.6rem 1.6rem 0; }
.dialog-title { margin: 0; font-family: var(--font-heading); font-size: 1.35rem; font-weight: 500; }
.dialog-content { padding: 1rem 1.6rem 1.5rem; }
.dialog-message { margin: 0; color: var(--text-secondary); line-height: 1.7; }
.dialog-actions { display: flex; justify-content: flex-end; gap: .7rem; padding: 1rem 1.6rem 1.5rem; border-top: 1px solid var(--border-color); }
.btn { display: inline-flex; min-width: 78px; align-items: center; justify-content: center; gap: .5rem; padding: .7rem 1.2rem; border: 1px solid var(--border-color); border-radius: var(--radius-control); cursor: pointer; font: inherit; font-size: .85rem; transition: background .2s ease, border-color .2s ease, transform .2s ease; }
.btn:disabled { opacity: .55; cursor: not-allowed; }
.btn:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 2px; }
.btn-cancel { color: var(--text-secondary); background: var(--surface-raised); }
.btn-cancel:hover:not(:disabled) { border-color: var(--accent); color: var(--text-primary); background: var(--surface-muted); }
.btn-confirm { border-color: var(--accent); color: var(--accent-contrast); background: var(--accent); }
.btn-confirm:hover:not(:disabled) { background: var(--accent-hover); transform: translateY(-1px); }
.loading-spinner { width: 14px; height: 14px; border: 2px solid color-mix(in srgb, var(--accent-contrast) 30%, transparent); border-top-color: var(--accent-contrast); border-radius: 50%; animation: spin 1s linear infinite; }
:global(html[data-theme-style='editorial'] .confirm-dialog) { border-top-width: 5px; }
:global(html[data-theme-style='editorial'] .btn) { font-family: var(--font-sans); }
:global(html[data-theme-style='zen'] .confirm-dialog) { border-inline: 0; box-shadow: none; }
:global(html[data-theme-style='zen'] .btn) { border-radius: 0; font-family: var(--font-mono); }
:global(html[data-theme-style='ink'] .confirm-dialog) { border-width: 2px; transform: rotate(-.35deg); }
:global(html[data-theme-style='ink'] .dialog-title) { font-family: var(--font-hand); }
:global(html[data-theme-style='ink'] .btn) { border-width: 2px; font-weight: 800; }
@keyframes dialog-in { from { opacity: 0; transform: translateY(12px) scale(.98); } }
@keyframes spin { to { transform: rotate(360deg); } }
@media (max-width: 480px) { .dialog-actions { flex-direction: column-reverse; } .btn { width: 100%; } }
</style>
