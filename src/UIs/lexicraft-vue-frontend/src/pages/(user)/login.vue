<script lang="tsx" setup>
import {ref} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseButton from '@/components/BaseButton.vue'
import {passwordRules} from '@/utils/validation.ts'
import FormItem from '@/components/base/form/FormItem.vue'
import Form from '@/components/base/form/Form.vue'
import Notice from '@/components/user/Notice.vue'
import {FormInstance} from '@/components/base/form/types.ts'
import {useAuth} from '@/hooks/useAuth'
import {REGISTER_PATH} from '@/config/auth.config'
import LoadingScreen from '@/components/LoadingScreen.vue'

// 状态管理
const route = useRoute()
const router = useRouter()
const {signInWithAccount, signInWithOAuth, isLoading} = useAuth()

// 页面状态
const loading = ref(false)
const oauthLoading = ref<string | null>(null)
const isRedirecting = ref(false)

// 登录表单
const loginForm = ref({userAccount: '', password: ''})
const loginFormRef = ref<FormInstance>()
const loginFormRules = {
  userAccount: [
    {required: true, message: '请输入邮箱或用户名', trigger: 'blur'}
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
      isRedirecting.value = true
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
    isRedirecting.value = true
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
  <div class="login-page-wrapper">
    <LoadingScreen v-if="isRedirecting" :progress="100" loading-text="登录成功，正在进入学习世界..."/>
    <div class="login-container">
      <!-- 左侧插画区域 (3/7) -->
      <div class="promo-section">
        <div class="promo-content">
          <img
              alt="Promo"
              class="promo-image"
              src="@/assets/img/login_promo.png"
          />
          <h2 class="promo-title">掌握语言,探索世界</h2>
          <p class="promo-subtitle">"每一门新语言,都是看世界的又一双眼睛。"</p>
        </div>
        <!-- 装饰性元素 -->
        <div class="decoration decoration-1"></div>
        <div class="decoration decoration-2"></div>
      </div>

      <!-- 右侧登录表单区域 (4/7) -->
      <div class="form-section">
        <div class="form-wrapper">
          <div class="mb-8 text-center lg:text-left">
            <h1 class="text-3xl font-extrabold text-gray-900 mb-1">登 录</h1>
            <p class="text-gray-500 text-base">欢迎回来,请登录您的账号</p>
          </div>

          <Form ref="loginFormRef" :model="loginForm" :rules="loginFormRules" class="space-y-4">
            <FormItem label="邮箱/用户名" prop="userAccount">
              <BaseInput
                  v-model="loginForm.userAccount"
                  placeholder="请输入邮箱地址或用户名"
                  size="large"
                  type="text"
              />
            </FormItem>
            <FormItem label="账号密码" prop="password">
              <BaseInput
                  v-model="loginForm.password"
                  placeholder="请输入登录密码"
                  size="large"
                  type="password"
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
              :loading="loading || isLoading"
              class="w-full py-3.5 text-base font-bold shadow-lg shadow-indigo-100"
              size="large"
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
                  :disabled="!!oauthLoading"
                  class="social-btn github"
                  title="使用 GitHub 登录"
                  @click="handleOAuthLogin('github')"
              >
                <IconMdiGithub/>
                <span v-if="oauthLoading === 'github'" class="loading-overlay"></span>
              </button>
              <button
                  :disabled="!!oauthLoading"
                  class="social-btn gitee"
                  title="使用 Gitee 登录"
                  @click="handleOAuthLogin('gitee')"
              >
                <IconSimpleIconsGitee/>
                <span v-if="oauthLoading === 'gitee'" class="loading-overlay"></span>
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

<style lang="scss" scoped>
.login-page-wrapper {
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

.login-container {
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
    background: linear-gradient(135deg, #4f46e5, #7c3aed, #2563eb);
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

.promo-image {
  width: 100%;
  height: auto;
  border-radius: 0.75rem;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  margin-bottom: 1.5rem;
  transform: scale(1);
  transition: transform 0.5s;

  &:hover {
    transform: scale(1.05);
  }
}

.promo-title {
  font-size: 1.875rem;
  font-weight: 700;
  margin-bottom: 0.75rem;
}

.promo-subtitle {
  font-size: 1.125rem;
  color: #c7d2fe;
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
    background: rgba(96, 165, 250, 0.2);
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
  position: relative;
  z-index: 10; /* 确保表单项在最上层 */

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
  position: relative;
  z-index: 10; /* 确保输入框在最上层 */

  &:focus-within {
    border-color: #4f46e5;
    box-shadow: 0 0 0 4px rgba(79, 70, 229, 0.1);
  }
}

:deep(.base-button) {
  border-radius: 8px;
  background: linear-gradient(135deg, #4f46e5, #3b82f6);
  position: relative;
  z-index: 10; /* 确保按钮在最上层 */

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
  z-index: 10; /* 确保社交登录按钮在最上层 */

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
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }
  }

  &.gitee {
    color: #c71d23;

    &:hover:not(:disabled) {
      color: #bb131a;
      box-shadow: 0 4px 12px rgba(199, 29, 35, 0.1);
    }
  }
}

.loading-overlay {
  position: absolute;
  inset: 0;
  background: rgba(255, 255, 255, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 20; /* 加载遮罩层级更高 */
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
