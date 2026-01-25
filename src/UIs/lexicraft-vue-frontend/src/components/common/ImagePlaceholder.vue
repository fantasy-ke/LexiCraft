<script lang="ts" setup>
interface Props {
  icon?: string
  title?: string
  width?: string
  height?: string
  bgColor?: string
  iconSize?: string
  animated?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  icon: '📚',
  title: '占位符',
  width: 'w-48',
  height: 'h-48',
  bgColor: 'bg-white/20',
  iconSize: 'text-5xl',
  animated: true
})
</script>

<template>
  <div class="flex items-center justify-center">
    <div
        :class="[
        'relative rounded-xl flex items-center justify-center backdrop-blur-sm border border-white/30 shadow-2xl transition-transform duration-500',
        width,
        height,
        bgColor,
        animated ? 'hover:scale-105' : ''
      ]"
    >
      <!-- 主要内容 -->
      <div class="text-center">
        <div :class="['mb-2', iconSize]">{{ icon }}</div>
        <div class="text-sm font-medium opacity-90">{{ title }}</div>
      </div>

      <!-- 装饰性动画元素 -->
      <template v-if="animated">
        <div class="absolute -top-2 -right-2 w-6 h-6 bg-yellow-400 rounded-full animate-pulse"></div>
        <div
            class="absolute -bottom-2 -left-2 w-4 h-4 bg-green-400 rounded-full animate-pulse"
            style="animation-delay: 0.5s;"
        ></div>
      </template>
    </div>
  </div>
</template>

<style scoped>
@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.animate-pulse {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
</style>