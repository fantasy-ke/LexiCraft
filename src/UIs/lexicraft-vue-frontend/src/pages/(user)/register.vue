<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseButton from '@/components/BaseButton.vue'
import Toast from '@/components/base/toast/Toast.ts'
import FormItem from '@/components/base/form/FormItem.vue'
import Form from '@/components/base/form/Form.vue'
import Notice from '@/components/user/Notice.vue'
import ImagePlaceholder from '@/components/common/ImagePlaceholder.vue'
import { FormInstance } from '@/components/base/form/types.ts'
import { useAuth } from '@/hooks/useAuth'
import { LOGIN_PATH, VALIDATION_RULES } from '@/config/auth.config'
import { RegisterRequest } from '@/types/auth'

const router = useRouter()
const { signUp, isLoading } = useAuth()

// 页面状态
const loading = ref(false)

// 注册表单
const registerForm = ref<RegisterRequest>({
  email: '',
  password: '',
  confirmPassword: '',
  username: ''
})

const registerFormRef = ref<FormInstance>()

// 表单验证规则
const registerFormRules = {
  email: VALIDATION_RULES.email,
  password: VALIDATION_RULES.password,
  confirmPassword: [
    ...VALIDATION_RULES.confirmPassword,
    {
      validator: (rule: any, value: string, callback: Function) => {
        if (value !== registerForm.value.password) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ],
  username: VALIDATION_RULES.username
}

// 注册处理
async function handleRegister() {
  if (!registerFormRef.value) return
  
  registerFormRef.value.validate(async (valid) => {
    if (!valid) return
    
    try {
      loading.value = true
      await signUp(registerForm.value)
    } catch (error: any) {
      // 错误提示已经在 useAuth 中处理，这里不需要再次提示
      console.error('Register failed:', error)
    } finally {
      loading.value = false
    }
  })
}

const goToLogin = () => {
  router.push(LOGIN_PATH)
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 p-0 overflow-hidden">
    <div class="flex flex-row w-full h-screen bg-white shadow-2xl overflow-hidden">
      <!-- 左侧插画区域 (3/7) -->
      <div class="hidden lg:flex lg:basis-[42.86%] relative overflow-hidden bg-gradient-to-br from-green-600 via-teal-600 to-blue-600 items-center justify-center p-8">
        <div class="relative z-10 text-center text-white max-w-sm">
          <!-- 使用通用的图片占位符组件 -->
          <div class="w-full h-64 mb-6">
            <ImagePlaceholder 
              icon="🚀"
              title="开始学习"
              width="w-48"
              height="h-48"
              bg-color="bg-white/20"
              :animated="true"
            />
          </div>
          <h2 class="text-3xl font-bold mb-3">开启学习之旅</h2>
          <p class="text-lg text-green-100 italic opacity-90">"千里之行，始于足下。"</p>
        </div>
        <!-- 装饰性元素 -->
        <div class="absolute top-[-10%] left-[-10%] w-64 h-64 bg-white/10 rounded-full blur-3xl"></div>
        <div class="absolute bottom-[-10%] right-[-10%] w-96 h-96 bg-teal-400/20 rounded-full blur-3xl"></div>
      </div>

      <!-- 右侧注册表单区域 (4/7) -->
      <div class="w-full lg:basis-[57.14%] flex flex-col justify-center items-center p-6 md:p-12">
        <div class="w-full max-w-sm">
          <div class="mb-8 text-center lg:text-left">
            <h1 class="text-3xl font-extrabold text-gray-900 mb-1">注 册</h1>
            <p class="text-gray-500 text-base">创建您的学习账号，开始词汇学习之旅</p>
          </div>

          <Form ref="registerFormRef" :rules="registerFormRules" :model="registerForm" class="space-y-4">
            <FormItem prop="email" label="电子邮箱">
              <BaseInput
                v-model="registerForm.email"
                type="email"
                size="large"
                placeholder="请输入邮箱地址"
              />
            </FormItem>
            
            <FormItem prop="username" label="用户名">
              <BaseInput
                v-model="registerForm.username"
                type="text"
                size="large"
                placeholder="请输入用户名"
              />
            </FormItem>

            <FormItem prop="password" label="密码">
              <BaseInput
                v-model="registerForm.password"
                type="password"
                size="large"
                placeholder="请输入密码（8-20位）"
              />
            </FormItem>
            
            <FormItem prop="confirmPassword" label="确认密码">
              <BaseInput
                v-model="registerForm.confirmPassword"
                type="password"
                size="large"
                placeholder="请再次输入密码（8-20位）"
              />
            </FormItem>
          </Form>

          <Notice class="my-4">
            <span class="text-xs">注册即表示同意我们的服务条款和隐私政策</span>
          </Notice>

          <BaseButton 
            class="w-full py-3.5 text-base font-bold shadow-lg shadow-green-100" 
            size="large" 
            :loading="loading || isLoading" 
            @click="handleRegister"
            style="background: linear-gradient(135deg, #10b981, #06b6d4);"
          >
            创建账号
          </BaseButton>

          <div class="mt-8 text-center text-sm text-gray-600">
            已有账号? 
            <span class="text-green-600 font-bold hover:underline cursor-pointer" @click="goToLogin">立即登录</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.min-h-screen {
  font-family: 'Inter', -apple-system, sans-serif;
}

:deep(.form-item) {
  flex-direction: column;
  gap: 0;
  .w-20 {
    width: 100%;
    justify-content: flex-start;
    font-size: 0.875rem;
    font-weight: 600;
    color: #374151;
    margin-bottom: 0.25rem;
  }
  .flex-1 {
    width: 100%;
  }
}

:deep(.base-input) {
  border-radius: 8px;
  border: 1px solid #d1d5db;
  background-color: white;
  transition: all 0.3s;
  &:focus-within {
    border-color: #10b981;
    box-shadow: 0 0 0 4px rgba(16, 185, 129, 0.1);
  }
}

:deep(.base-button) {
  border-radius: 8px;
  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
  }
}

/* 动画样式 */
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