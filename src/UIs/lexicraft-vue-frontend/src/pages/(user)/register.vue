<script lang="ts" setup>
import {ref} from 'vue'
import {useRouter} from 'vue-router'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseButton from '@/components/BaseButton.vue'
import FormItem from '@/components/base/form/FormItem.vue'
import Form from '@/components/base/form/Form.vue'
import Notice from '@/components/user/Notice.vue'
import ImagePlaceholder from '@/components/common/ImagePlaceholder.vue'
import CaptchaInput from '@/components/auth/CaptchaInput.vue'
import {FormInstance} from '@/components/base/form/types.ts'
import {useAuth} from '@/hooks/useAuth'
import {LOGIN_PATH, VALIDATION_RULES} from '@/config/auth.config'
import {RegisterRequest} from '@/types/auth'

const router = useRouter()
const {signUp, isLoading} = useAuth()

// 页面状态
const loading = ref(false)

// 注册表单
const registerForm = ref<RegisterRequest>({
  email: '',
  password: '',
  confirmPassword: '',
  username: '',
  captchaKey: '',
  captchaCode: ''
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
  username: VALIDATION_RULES.username,
  captchaCode: VALIDATION_RULES.captchaCode
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

// 处理验证码Key更新
const handleCaptchaKeyUpdate = (key: string) => {
  registerForm.value.captchaKey = key
}

const goToLogin = () => {
  router.push(LOGIN_PATH)
}
</script>

<template>
  <div class="register-page-wrapper">
    <div class="register-container">
      <!-- 左侧插画区域 (3/7) -->
      <div class="promo-section">
        <div class="promo-content">
          <!-- 使用通用的图片占位符组件 -->
          <div class="promo-placeholder">
            <ImagePlaceholder
                :animated="true"
                bg-color="bg-white/20"
                height="h-48"
                icon="🚀"
                title="开始学习"
                width="w-48"
            />
          </div>
          <h2 class="promo-title">开启学习之旅</h2>
          <p class="promo-subtitle">"千里之行，始于足下。"</p>
        </div>
        <!-- 装饰性元素 -->
        <div class="decoration decoration-1"></div>
        <div class="decoration decoration-2"></div>
      </div>

      <!-- 右侧注册表单区域 (4/7) -->
      <div class="form-section">
        <div class="form-wrapper">
          <div class="mb-8 text-center lg:text-left">
            <h1 class="text-3xl font-extrabold text-gray-900 mb-1">注 册</h1>
            <p class="text-gray-500 text-base">创建您的学习账号，开始词汇学习之旅</p>
          </div>

          <Form ref="registerFormRef" :model="registerForm" :rules="registerFormRules" class="space-y-2">
            <FormItem label="电子邮箱" prop="email">
              <BaseInput
                  v-model="registerForm.email"
                  placeholder="请输入邮箱地址"
                  size="large"
                  type="email"
              />
            </FormItem>

            <FormItem label="用户名" prop="username">
              <BaseInput
                  v-model="registerForm.username"
                  placeholder="请输入用户名"
                  size="large"
                  type="text"
              />
            </FormItem>

            <FormItem label="密码" prop="password">
              <BaseInput
                  v-model="registerForm.password"
                  placeholder="请输入密码（8-20位）"
                  size="large"
                  type="password"
              />
            </FormItem>

            <FormItem label="确认密码" prop="confirmPassword">
              <BaseInput
                  v-model="registerForm.confirmPassword"
                  placeholder="请再次输入密码（8-20位）"
                  size="large"
                  type="password"
              />
            </FormItem>

            <FormItem label="验证码" prop="captchaCode">
              <CaptchaInput
                  v-model="registerForm.captchaCode"
                  @update:captchaKey="handleCaptchaKeyUpdate"
              />
            </FormItem>
          </Form>

          <Notice class="my-2">
            <span class="text-xs">注册即表示同意我们的服务条款和隐私政策</span>
          </Notice>

          <BaseButton
              :loading="loading || isLoading"
              class="w-full py-3.5 text-base font-bold shadow-lg shadow-green-100"
              size="large"
              style="background: linear-gradient(135deg, #10b981, #06b6d4);"
              @click="handleRegister"
          >
            创建账号
          </BaseButton>

          <div class="mt-6 text-center text-sm text-gray-600">
            已有账号?
            <span class="text-green-600 font-bold hover:underline cursor-pointer" @click="goToLogin">立即登录</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.register-page-wrapper {
  min-height: 100vh;
  min-height: 100dvh; /* 移动端使用动态视口高度 */
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #f9fafb;
  padding: 0;
  font-family: 'Inter', -apple-system, sans-serif;
  position: relative;
}

.register-container {
  display: flex;
  flex-direction: row;
  width: 100%;
  height: 100vh;
  height: 100dvh; /* 移动端使用动态视口高度 */
  background: white;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  overflow: hidden;
  position: relative;
  z-index: 1; /* 确保在正常层级 */
}

.promo-section {
  display: none;

  @media (min-width: 1024px) {
    display: flex;
    flex-basis: 42.86%;
    position: relative;
    overflow: hidden;
    background: linear-gradient(135deg, #10b981, #14b8a6, #2563eb);
    align-items: center;
    justify-content: center;
    padding: 2rem;
  }
}

.promo-content {
  position: relative;
  z-index: 10;
  text-align: center;
  color: white;
  max-width: 28rem;
}

.promo-placeholder {
  width: 100%;
  height: 16rem;
  margin-bottom: 1.5rem;
}

.promo-title {
  font-size: 1.875rem;
  font-weight: 700;
  margin-bottom: 0.75rem;
}

.promo-subtitle {
  font-size: 1.125rem;
  color: #d1fae5;
  font-style: italic;
  opacity: 0.9;
}

.decoration {
  position: absolute;
  border-radius: 50%;
  filter: blur(3rem);

  &.decoration-1 {
    top: -10%;
    left: -10%;
    width: 16rem;
    height: 16rem;
    background: rgba(255, 255, 255, 0.1);
  }

  &.decoration-2 {
    bottom: -10%;
    right: -10%;
    width: 24rem;
    height: 24rem;
    background: rgba(20, 184, 166, 0.2);
  }
}

.form-section {
  width: 100%;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 1.5rem;
  overflow-y: auto; /* 允许滚动 */
  -webkit-overflow-scrolling: touch; /* iOS 平滑滚动 */

  @media (min-width: 768px) {
    padding: 3rem;
  }

  @media (min-width: 1024px) {
    flex-basis: 57.14%;
  }
}

.form-wrapper {
  width: 100%;
  max-width: 28rem;
  position: relative;
  z-index: 10; /* 确保表单在最上层 */
}

:deep(.form-item) {
  flex-direction: column;
  gap: 0;
  margin-bottom: 0.5rem;
  position: relative;
  z-index: 10; /* 确保表单项在最上层 */

  .w-20 {
    width: 100%;
    justify-content: flex-start;
    font-size: 0.875rem;
    font-weight: 600;
    color: #374151;
    margin-bottom: 0.125rem;
  }

  .flex-1 {
    width: 100%;
  }

  /* 减少错误信息的高度 */
  .form-error {
    margin-top: 0.125rem;
    margin-bottom: 0;
    min-height: 0.875rem;
    font-size: 0.75rem;
  }
}

:deep(.base-input) {
  border-radius: 8px;
  border: 1px solid #d1d5db;
  background-color: white;
  transition: all 0.3s;
  position: relative;
  z-index: 10; /* 确保输入框在最上层 */

  &:focus-within {
    border-color: #10b981;
    box-shadow: 0 0 0 4px rgba(16, 185, 129, 0.1);
  }
}

:deep(.base-button) {
  border-radius: 8px;
  position: relative;
  z-index: 10; /* 确保按钮在最上层 */

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

/* 移动端优化 */
@media (max-width: 768px) {
  .form-section {
    padding: 1rem;
  }

  .form-wrapper {
    max-width: 100%;
  }
}
</style>