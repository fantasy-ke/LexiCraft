<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  provider: 'github' | 'gitee'
  name: string
  icon?: string
  color?: string
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
})

const emit = defineEmits<{
  click: []
}>()

const buttonClass = computed(() => {
  return `oauth-button oauth-button--${props.provider}`
})

const handleClick = () => {
  if (!props.loading) {
    emit('click')
  }
}
</script>

<template>
  <button 
    :class="buttonClass"
    @click="handleClick"
    :disabled="loading"
    class="w-full flex items-center justify-center gap-3 px-6 py-3 rounded-xl font-medium transition-all duration-200 border-2"
  >
    <IconMdiGithub v-if="provider === 'github'" class="text-2xl" />
    <IconSimpleIconsGitee v-if="provider === 'gitee'" class="text-2xl" />
    
    <span>使用 {{ name }} 登录</span>
    
    <IconEosIconsLoading v-if="loading" class="text-xl animate-spin" />
  </button>
</template>

<style scoped>
.oauth-button {
  position: relative;
  overflow: hidden;
}

.oauth-button--github {
  background: #24292e;
  color: white;
  border-color: #24292e;
}

.oauth-button--github:hover:not(:disabled) {
  background: #1a1e22;
  border-color: #1a1e22;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(36, 41, 46, 0.3);
}

.oauth-button--gitee {
  background: #c71d23;
  color: white;
  border-color: #c71d23;
}

.oauth-button--gitee:hover:not(:disabled) {
  background: #a01519;
  border-color: #a01519;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(199, 29, 35, 0.3);
}

.oauth-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
