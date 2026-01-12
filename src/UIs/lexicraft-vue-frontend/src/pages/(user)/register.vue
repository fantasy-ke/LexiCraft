<script setup lang="tsx">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseButton from '@/components/BaseButton.vue'
import { APP_NAME } from '@/config/env.ts'
import { accountRules, passwordRules } from '@/utils/validation.ts'
import Toast from '@/components/base/toast/Toast.ts'
import FormItem from '@/components/base/form/FormItem.vue'
import Form from '@/components/base/form/Form.vue'
import Notice from '@/components/user/Notice.vue'
import { FormInstance } from '@/components/base/form/types.ts'
import { PASSWORD_CONFIG } from '@/config/auth.ts'
import { LOGIN_PATH } from '@/config/logto.config'
import { useLogto } from '@/hooks/useLogto'

const router = useRouter()
const { signUp, isLoading } = useLogto()

// 页面状态
const loading = ref(false)

// 注册表单
const registerForm = ref({
  email: '',
  password: '',
  confirmPassword: '',
})
const registerFormRef = ref<FormInstance>()
const registerFormRules = {
  email: accountRules,
  password: passwordRules,
  confirmPassword: [
    { required: true, message: '请再次输入密码', trigger: 'blur' },
    {
      validator: (rule: any, value: any) => {
        if (value !== registerForm.value.password) {
          throw new Error('两次密码输入不一致')
        }
      },
      trigger: 'blur',
    },
  ],
}

// 注册处理
async function handleRegister() {
  if (!registerFormRef.value) return
  
  registerFormRef.value.validate(async (valid) => {
    if (!valid) return
    try {
      loading.value = true
      await signUp(registerForm.value.email, registerForm.value.password)
    } catch (error: any) {
      Toast.error(error.message || '注册失败,请重试')
    } finally {
      loading.value = false
    }
  })
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
          <h2 class="text-3xl font-bold mb-3">加入 LexiCraft</h2>
          <p class="text-lg text-indigo-100 italic opacity-90">"每一门新语言,都是看世界的又一双眼睛。"</p>
        </div>
        <!-- 装饰性元素 -->
        <div class="absolute top-[-10%] left-[-10%] w-64 h-64 bg-white/10 rounded-full blur-3xl"></div>
        <div class="absolute bottom-[-10%] right-[-10%] w-96 h-96 bg-blue-400/20 rounded-full blur-3xl"></div>
      </div>

      <!-- 右侧注册表单区域 (4/7) -->
      <div class="w-full lg:basis-[57.14%] flex flex-col justify-center items-center p-6 md:p-12">
        <div class="w-full max-w-sm">
          <div class="mb-8 text-center lg:text-left">
            <h1 class="text-3xl font-extrabold text-gray-900 mb-1">注 册</h1>
            <p class="text-gray-500 text-base">开启您的语言探索之旅</p>
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
            <FormItem prop="password" label="设置密码">
              <BaseInput
                v-model="registerForm.password"
                type="password"
                size="large"
                :placeholder="`请输入密码(${PASSWORD_CONFIG.minLength}-${PASSWORD_CONFIG.maxLength}位)`"
              />
            </FormItem>
            <FormItem prop="confirmPassword" label="确认密码">
              <BaseInput
                v-model="registerForm.confirmPassword"
                type="password"
                size="large"
                placeholder="请再次输入密码"
              />
            </FormItem>
          </Form>

          <Notice class="my-4">
            <span class="text-xs">提交即视为同意我们的服用户协议</span>
          </Notice>

          <BaseButton 
            class="w-full py-3.5 text-base font-bold shadow-lg shadow-indigo-100" 
            size="large" 
            :loading="loading || isLoading" 
            @click="handleRegister"
          >
            立即注册
          </BaseButton>

          <div class="mt-8 text-center text-sm text-gray-600">
            已有账号? 
            <span class="text-indigo-600 font-bold hover:underline cursor-pointer" @click="router.push(LOGIN_PATH)">去登录</span>
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
</style>
