<template>
  <div class="captcha-container flex items-center gap-2">
    <!-- 验证码输入框 -->
    <div class="flex-1">
      <BaseInput
        v-model="captchaCode"
        type="text"
        size="large"
        placeholder="请输入验证码"
        maxlength="4"
        @blur="handleBlur"
        :error="hasError"
      />
    </div>
    
    <!-- 验证码图片 -->
    <div 
      class="captcha-image-container cursor-pointer border border-gray-300 rounded-lg overflow-hidden hover:border-green-500 transition-colors"
      @click="refreshCaptcha"
      :title="loading ? '加载中...' : '点击刷新验证码'"
    >
      <div v-if="loading" class="w-24 h-10 flex items-center justify-center bg-gray-100">
        <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-green-500"></div>
      </div>
      <img 
        v-else-if="captchaData" 
        :src="captchaData" 
        alt="验证码"
        class="w-24 h-10 object-cover"
      />
      <div v-else class="w-24 h-10 flex items-center justify-center bg-gray-100 text-gray-500 text-xs">
        点击获取
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import BaseInput from '@/components/base/BaseInput.vue'
import { getCaptcha } from '@/apis/auth'
import Toast from '@/components/base/toast/Toast'

// Props 和 Emits
interface Props {
  modelValue?: string
  error?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: string): void
  (e: 'update:captchaKey', key: string): void
  (e: 'blur'): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  error: false
})

const emit = defineEmits<Emits>()

// 响应式数据
const captchaCode = ref(props.modelValue)
const captchaData = ref<string>('')
const captchaKey = ref<string>('')
const loading = ref(false)
const hasError = ref(props.error)

// 监听外部传入的值变化
watch(() => props.modelValue, (newValue) => {
  captchaCode.value = newValue
})

// 监听内部值变化，同步到外部
watch(captchaCode, (newValue) => {
  emit('update:modelValue', newValue)
  
  // 输入时清除错误状态
  if (hasError.value) {
    hasError.value = false
  }
})

// 监听错误状态变化
watch(() => props.error, (newError) => {
  hasError.value = newError
})

// 处理失焦
const handleBlur = () => {
  emit('blur')
}

// 获取验证码
const fetchCaptcha = async () => {
  try {
    loading.value = true
    const response = await getCaptcha()
    
    if (response.status && response.data) {
      captchaData.value = response.data.captchaData
      captchaKey.value = response.data.captchaKey
      
      // 通知父组件验证码Key更新
      emit('update:captchaKey', captchaKey.value)
    } else {
      Toast.error(response.message || '获取验证码失败')
    }
  } catch (error: any) {
    console.error('获取验证码失败:', error)
    Toast.error('获取验证码失败，请重试')
  } finally {
    loading.value = false
  }
}

// 刷新验证码
const refreshCaptcha = () => {
  if (!loading.value) {
    fetchCaptcha()
  }
}

// 组件挂载时获取验证码
onMounted(() => {
  fetchCaptcha()
})

// 暴露方法给父组件
defineExpose({
  refreshCaptcha,
  getCaptchaKey: () => captchaKey.value
})
</script>

<style scoped lang="scss">
.captcha-container {
  .captcha-image-container {
    min-width: 96px; // w-24 = 96px
    height: 40px;
    
    &:hover {
      box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.1);
    }
  }
}
</style>