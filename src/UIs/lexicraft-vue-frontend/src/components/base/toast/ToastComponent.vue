<template>
  <Transition appear name="message-fade">
    <div v-if="visible" :class="type" :style="style" class="message" @mouseenter="handleMouseEnter"
         @mouseleave="handleMouseLeave">
      <div class="message-content">
        <IconFluentCheckmarkCircle20Filled v-if="props.type === 'success'" class="message-icon"/>
        <IconFluentErrorCircle20Filled v-if="props.type === 'warning'" class="message-icon"/>
        <IconFluentErrorCircle20Filled v-if="props.type === 'info'" class="message-icon"/>
        <IconFluentDismissCircle20Filled v-if="props.type === 'error'" class="message-icon"/>
        <span class="message-text">{{ message }}</span>
        <Close v-if="showClose" class="message-close" @click="close"/>
      </div>
    </div>
  </Transition>
</template>

<script lang="ts" setup>
import {computed, onBeforeUnmount, onMounted, ref} from 'vue'

interface Props {
  message: string
  type?: 'success' | 'warning' | 'info' | 'error'
  duration?: number
  showClose?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  type: 'info',
  duration: 3000,
  showClose: false
})

const emit = defineEmits(['close'])
const visible = ref(false)
let timer = null

const style = computed(() => ({
  // 移除offset，现在由容器管理位置
}))

const startTimer = () => {
  if (props.duration > 0) {
    timer = setTimeout(close, props.duration)
  }
}

const clearTimer = () => {
  if (timer) {
    clearTimeout(timer)
    timer = null
  }
}

const handleMouseEnter = () => {
  clearTimer()
}

const handleMouseLeave = () => {
  startTimer()
}

const close = () => {
  visible.value = false
  // 延迟发出close事件，等待动画完成
  setTimeout(() => {
    emit('close')
  }, 300) // 等待动画完成（0.3秒）
}

onMounted(() => {
  visible.value = true
  startTimer()
})

onBeforeUnmount(() => {
  clearTimer()
})

// 暴露方法给父组件
defineExpose({
  close,
  show: () => {
    visible.value = true
    startTimer()
  }
})
</script>

<style lang="scss" scoped>
.message { position: relative; min-width: 16rem; padding: .8rem 1rem; border: 1px solid var(--border-color); border-left: 4px solid currentColor; border-radius: var(--radius-control); color: var(--text-primary); background: var(--surface-overlay); box-shadow: var(--control-shadow); backdrop-filter: blur(12px); pointer-events: auto; transition: opacity .25s ease, transform .25s ease; }
.message.success { color: var(--success); background: color-mix(in srgb, var(--success) 10%, var(--surface-card)); }
.message.warning { color: var(--warning); background: color-mix(in srgb, var(--warning) 10%, var(--surface-card)); }
.message.info { color: var(--text-secondary); background: var(--surface-card); }
.message.error { color: var(--danger); background: color-mix(in srgb, var(--danger) 10%, var(--surface-card)); }
.message-content { display: flex; align-items: center; gap: 8px; }
.message-icon { flex: 0 0 auto; font-size: 1.2rem; }
.message-text { flex: 1; color: var(--text-primary); font-size: 14px; }
.message-close { color: var(--text-secondary); cursor: pointer; font-size: 1.2rem; }
:global(html[data-theme-style='zen'] .message) { border-inline: 0; border-radius: 0; box-shadow: none; font-family: var(--font-mono); }
:global(html[data-theme-style='ink'] .message) { border-width: 2px 2px 2px 5px; font-family: var(--font-hand); font-weight: 700; transform: rotate(-.4deg); }
.message-fade-enter-active, .message-fade-leave-active { transition: opacity .25s ease, transform .25s ease; }
.message-fade-enter-from, .message-fade-leave-to { opacity: 0; transform: translateY(-20px); }
</style>
