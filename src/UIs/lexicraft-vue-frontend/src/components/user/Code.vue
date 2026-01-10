<script setup lang="ts">

import BaseButton from "@/components/BaseButton.vue";
import {sendCode} from "@/apis/user.ts";
import {PHONE_CONFIG} from "@/config/auth.ts";
import Toast from "@/components/base/toast/Toast.ts";
import {CodeType} from "@/types/enum.ts";

let isSendingCode = $ref(false)
let codeCountdown = $ref(0)

interface IProps {
  validateField: Function,
  type: CodeType
  val: any
  size?: any
}

const props = withDefaults(defineProps<IProps>(), {
  size: 'large',
})

// 发送验证码
async function sendVerificationCode() {
  let res = props.validateField()
  if (res) {
    try {
      isSendingCode = true
      const res = await sendCode({val: props.val, type: props.type})
      if (res.success) {
        codeCountdown = PHONE_CONFIG.sendInterval
        const timer = setInterval(() => {
          codeCountdown--
          if (codeCountdown <= 0) {
            clearInterval(timer)
          }
        }, 1000)
      } else {
        Toast.error(res.msg || '发送失败')
      }
    } catch (error) {
      console.error('Send code error:', error)
      Toast.error('发送验证码失败')
    } finally {
      isSendingCode = false
    }
  }
}

</script>

<template>
  <BaseButton
    @click="sendVerificationCode"
    :disabled="isSendingCode || codeCountdown > 0"
    type="info"
    :size="props.size"
    style="border: 1px solid var(--color-input-border)"
  >
    {{ codeCountdown > 0 ? `${codeCountdown}s` : (isSendingCode ? '发送中' : '发送验证码') }}
  </BaseButton>
</template>

<style scoped lang="scss">

</style>
