<script setup lang="tsx">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseButton from '@/components/BaseButton.vue'
import { APP_NAME } from '@/config/env.ts'
import { accountRules, passwordRules } from '@/utils/validation.ts'
import Toast from '@/components/base/toast/Toast.ts'
import FormItem from '@/components/base/form/FormItem.vue'
import Form from '@/components/base/form/Form.vue'
import Notice from '@/components/user/Notice.vue'
import { FormInstance } from '@/components/base/form/types.ts'
import { useAuth } from '@/hooks/useAuth'
import { oauthProviders, REGISTER_PATH, REDIRECT_PATH } from '@/config/auth.config'

// 状态管理
const route = useRoute()
const router = useRouter()
const { signInWithAccount, signInWithOAuth, isLoading } = useAuth()

// 页面状态
const loading = ref(false)
const oauthLoading = ref<string | null>(null)

// 登录表单
const loginForm = ref({ userAccount: '', password: '' })
const loginFormRef = ref<FormInstance>()
const loginFormRules = {
  userAccount: [
    { required: true, message: '请输入邮箱或用户名', trigger: 'blur' }
  ],
  password: passwordRules,
}

// 登录处理
async function handleLogin() {
  if (!loginFormRef.value) return
  
  loginFormRef.value.validate(async (valid) => {
    if (!valid) return
    try {
      loading.value = true
      
      // 特殊账户处理: admin@qq.com / 123456789
      if (loginForm.value.userAccount === 'admin@qq.com' && loginForm.value.password === '123456789') {
        // 使用新的认证系统处理管理员登录
        await signInWithAccount(loginForm.value.userAccount, loginForm.value.password)
        return
      }

      await signInWithAccount(loginForm.value.userAccount, loginForm.value.password)
    } catch (error: any) {
      // 错误提示已经在 useAuth 中处理，这里不需要再次提示
      console.error('Login failed:', error)
    } finally {
      loading.value = false
    }
  })
}

// OAuth 登录处理
async function handleOAuthLogin(provider: string) {
  try {
    oauthLoading.value = provider
    await signInWithOAuth(provider as any)
  } catch (error: any) {
    // 错误提示已经在 useAuth 中处理，这里不需要再次提示
    console.error('OAuth login failed:', error)
  } finally {
    oauthLoading.value = null
  }
}

const goToRegister = () => {
  router.push(REGISTER_PATH)
}

const goToForgot = () => {
  // TODO: 实现忘记密码跳转
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 p-0 overflow-hidden">
    <div class="flex flex-row w-full h-screen bg-white shadow-2xl overflow-hidden">
      <!-- 左侧插画区域 (3/7) -->
      <div class="hidden lg:flex lg:basis-[42.86%] relative overflow-hidden bg-gradient-to-br from-indigo-600 via-purple-600 to-blue-700 items-center justify-center p-8">
        <div class="relative z-10 text-center text-white max-w-sm">
          <img 
            src="@/assets/img/login_promo.png" 
            alt="Promo" 
            class="w-full h-auto rounded-xl shadow-2xl mb-6 transform hover:scale-105 transition-transform duration-500"
          />
          <h2 class="text-3xl font-bold mb-3">掌握语言,探索世界</h2>
          <p class="text-lg text-indigo-100 italic opacity-90">"每一门新语言,都是看世界的又一双眼睛。"</p>
        </div>
        <!-- 装饰性元素 -->
        <div class="absolute top-[-10%] left-[-10%] w-64 h-64 bg-white/10 rounded-full blur-3xl"></div>
        <div class="absolute bottom-[-10%] right-[-10%] w-96 h-96 bg-blue-400/20 rounded-full blur-3xl"></div>
      </div>

      <!-- 右侧登录表单区域 (4/7) -->
      <div class="w-full lg:basis-[57.14%] flex flex-col justify-center items-center p-6 md:p-12">
        <div class="w-full max-w-sm">
          <div class="mb-8 text-center lg:text-left">
            <h1 class="text-3xl font-extrabold text-gray-900 mb-1">登 录</h1>
            <p class="text-gray-500 text-base">欢迎回来,请登录您的账号</p>
          </div>

          <Form ref="loginFormRef" :rules="loginFormRules" :model="loginForm" class="space-y-4">
            <FormItem prop="userAccount" label="邮箱/用户名">
              <BaseInput
                v-model="loginForm.userAccount"
                type="text"
                size="large"
                placeholder="请输入邮箱地址或用户名"
              />
            </FormItem>
            <FormItem prop="password" label="账号密码">
              <BaseInput
                v-model="loginForm.password"
                type="password"
                size="large"
                placeholder="请输入登录密码"
              />
            </FormItem>
          </Form>

          <div class="flex justify-end mt-1.5">
            <span class="text-sm text-indigo-600 hover:underline cursor-pointer" @click="goToForgot">忘记密码?</span>
          </div>

          <Notice class="my-4">
            <span class="text-xs">登录即表示同意我们的服务条款和隐私政策</span>
          </Notice>

          <BaseButton 
            class="w-full py-3.5 text-base font-bold shadow-lg shadow-indigo-100" 
            size="large" 
            :loading="loading || isLoading" 
            @click="handleLogin"
          >
            开启学习
          </BaseButton>

          <!-- 社交登录入口 -->
          <div class="mt-8">
            <div class="relative mb-5">
              <div class="absolute inset-0 flex items-center">
                <div class="w-full border-t border-gray-100"></div>
              </div>
              <div class="relative flex justify-center text-xs">
                <span class="px-3 bg-white text-gray-400 font-medium">第三方账号快捷登录</span>
              </div>
            </div>

            <div class="flex justify-center gap-5">
              <button 
                class="social-btn github" 
                @click="handleOAuthLogin('github')"
                :disabled="!!oauthLoading"
                title="使用 GitHub 登录"
              >
                <IconMdiGithub />
                <span class="loading-overlay" v-if="oauthLoading === 'github'"></span>
              </button>
              <button 
                class="social-btn gitee" 
                @click="handleOAuthLogin('gitee')"
                :disabled="!!oauthLoading"
                title="使用 Gitee 登录"
              >
                <IconSimpleIconsGitee />
                <span class="loading-overlay" v-if="oauthLoading === 'gitee'"></span>
              </button>
            </div>
          </div>

          <div class="mt-8 text-center text-sm text-gray-600">
            还没有账号? 
            <span class="text-indigo-600 font-bold hover:underline cursor-pointer" @click="router.push(REGISTER_PATH)">立即注册</span>
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
  border: 1px solid #d1d5db; /* 加深边框颜色 */
  background-color: white;
  transition: all 0.3s;
  &:focus-within {
    border-color: #4f46e5;
    box-shadow: 0 0 0 4px rgba(79, 70, 229, 0.1);
  }
}

:deep(.base-button) {
  border-radius: 8px;
  background: linear-gradient(135deg, #4f46e5, #3b82f6);
  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3);
  }
}

.social-btn {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  transition: all 0.3s;
  border: 1px solid #e5e7eb;
  background: white;
  position: relative;
  overflow: hidden;

  &:hover:not(:disabled) {
    transform: scale(1.1);
    border-color: #d1d5db;
    background: #f9fafb;
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  &.github {
    color: #24292e;
    &:hover:not(:disabled) {
      color: #000;
      box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }
  }

  &.gitee {
    color: #c71d23;
    &:hover:not(:disabled) {
      color: #bb131a;
      box-shadow: 0 4px 12px rgba(199,29,35,0.1);
    }
  }
}

.loading-overlay {
  position: absolute;
  inset: 0;
  background: rgba(255,255,255,0.7);
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
