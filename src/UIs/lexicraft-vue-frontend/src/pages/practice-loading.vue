<template>
  <LoadingScreen 
    :progress="loadingProgress" 
    :loading-text="loadingText"
  />
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import LoadingScreen from '@/components/LoadingScreen.vue'

const router = useRouter()
const route = useRoute()

const loadingProgress = ref(0)
const loadingText = ref('正在准备学习内容...')

// 模拟加载过程
const simulateLoading = async () => {
  const steps = [
    { progress: 20, text: '正在加载词典数据...' },
    { progress: 40, text: '正在准备练习内容...' },
    { progress: 60, text: '正在初始化学习环境...' },
    { progress: 80, text: '正在优化学习体验...' },
    { progress: 100, text: '准备完成，即将开始学习...' }
  ]

  for (const step of steps) {
    await new Promise(resolve => setTimeout(resolve, 300 + Math.random() * 200))
    loadingProgress.value = step.progress
    loadingText.value = step.text
  }

  // 加载完成后跳转到实际的练习页面
  await new Promise(resolve => setTimeout(resolve, 500))
  
  // 获取目标路由参数
  const targetPath = route.query.target as string
  const practiceId = route.params.id as string
  
  if (targetPath && practiceId) {
    // 跳转到实际的练习页面，使用练习布局
    router.replace(`${targetPath}/${practiceId}`)
  } else {
    // 如果参数不完整，返回主页
    router.replace('/app/dashboard')
  }
}

onMounted(() => {
  simulateLoading()
})
</script>