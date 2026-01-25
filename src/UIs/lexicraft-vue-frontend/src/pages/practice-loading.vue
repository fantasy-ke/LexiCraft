<template>
  <LoadingScreen
      :loading-text="loadingText"
      :progress="loadingProgress"
  />
</template>

<script lang="ts" setup>
import {onMounted, ref} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import LoadingScreen from '@/components/LoadingScreen.vue'

const router = useRouter()
const route = useRoute()

const loadingProgress = ref(0)
const loadingText = ref('')

const tips = [
  '按下 Alt + S 可以快速打开设置面板',
  '每天坚持练习 15 分钟，语感会显著提升',
  '在文章练习中开启“听写模式”，挑战你的听力极限',
  '快捷键 Ctrl + \' 可以播放当前句子的发音',
  '遇到生词？按下 Ctrl + N 快速加入生词本',
  '想要更清爽的界面？按下 Ctrl + L 开启简洁模式',
  '深色模式对眼睛更友好，在设置中可以随时切换',
  '由于没有输入框，练习过程更像是钢琴弹奏',
  '如果你觉得某个单词太简单，可以按下 Ctrl + M 标记为已掌握',
  '每一个微小的进步，都是通往流利道路上的基石'
]

// 模拟加载过程
const simulateLoading = async () => {
  // 随机选一个贴士作为初始文本
  loadingText.value = tips[Math.floor(Math.random() * tips.length)]

  const steps = [20, 40, 60, 80, 100]

  for (const progress of steps) {
    await new Promise(resolve => setTimeout(resolve, 300 + Math.random() * 200))
    loadingProgress.value = progress
    // 进度过半时，可以考虑换一个提示语，增加互动感
    if (progress === 60) {
      loadingText.value = tips[Math.floor(Math.random() * tips.length)]
    }
  }

  // 加载完成后跳转到实际的练习页面
  await new Promise(resolve => setTimeout(resolve, 500))

  // 获取目标路由参数
  const targetPath = route.query.target as string
  const practiceId = route.params.id as string

  if (targetPath && practiceId) {
    const finalPath = `${targetPath}/${practiceId}`
    console.log('Redirecting to:', finalPath)
    router.replace(finalPath)
  } else {
    router.replace('/app/dashboard')
  }
}

onMounted(() => {
  simulateLoading()
})
</script>