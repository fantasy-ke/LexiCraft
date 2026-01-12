<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuth } from '@/hooks/useAuth'
import Toast from '@/components/base/toast/Toast'
import { LOGIN_PATH, REDIRECT_PATH } from '@/config/auth.config'

const router = useRouter()
const route = useRoute()
const { handleSignInCallback, getUserInfo } = useAuth()

const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    loading.value = true
    
    // 从 URL 参数中提取 OAuth 回调信息
    const code = route.query.code as string
    const state = route.query.state as string
    const provider = route.query.provider as string || 
                    route.path.includes('github') ? 'github' : 'gitee'
    
    if (!code || !state) {
      throw new Error('OAuth 回调参数不完整')
    }
    
    // 处理 OAuth 回调
    const user = await handleSignInCallback({
      code,
      state,
      provider: provider as any
    })
    
    if (user) {
      Toast.success('登录成功!')
      
      // 获取重定向路径
      const redirect = (route.query.redirect as string) || REDIRECT_PATH
      
      // 延迟跳转,让用户看到成功提示
      setTimeout(() => {
        router.push(redirect)
      }, 1000)
    } else {
      throw new Error('登录失败,未获取到用户信息')
    }
  } catch (err: any) {
    console.error('OAuth callback error:', err)
    error.value = err.message || '登录失败,请重试'
    Toast.error(error.value)
    
    // 3秒后跳转回登录页
    setTimeout(() => {
      router.push(LOGIN_PATH)
    }, 3000)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-white">
    <div class="card-white p-8 w-96 text-center">
      <template v-if="loading">
        <IconEosIconsLoading class="text-6xl text-blue-600 mx-auto mb-4 animate-spin" />
        <h2 class="text-2xl font-bold text-gray-900 mb-2">正在登录...</h2>
        <p class="text-gray-600">请稍候,正在处理您的登录请求</p>
      </template>
      
      <template v-else-if="error">
        <IconFluentErrorCircle20Regular class="text-6xl text-red-600 mx-auto mb-4" />
        <h2 class="text-2xl font-bold text-gray-900 mb-2">登录失败</h2>
        <p class="text-gray-600 mb-4">{{ error }}</p>
        <p class="text-sm text-gray-500">即将返回登录页...</p>
      </template>
      
      <template v-else>
        <IconFluentCheckmarkCircle20Filled class="text-6xl text-green-600 mx-auto mb-4" />
        <h2 class="text-2xl font-bold text-gray-900 mb-2">登录成功!</h2>
        <p class="text-gray-600">正在跳转...</p>
      </template>
    </div>
  </div>
</template>

<style scoped>
.card-white {
  background: white;
  border-radius: 1rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.animate-spin {
  animation: spin 1s linear infinite;
}
</style>
