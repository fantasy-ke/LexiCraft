<script lang="ts" setup>
import {ref} from 'vue'
import {useRouter} from 'vue-router'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseButton from '@/components/BaseButton.vue'
import FormItem from '@/components/base/form/FormItem.vue'
import Form from '@/components/base/form/Form.vue'
import Notice from '@/components/user/Notice.vue'
import BrandLogo from '@/components/BrandLogo.vue'
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
  email: [...VALIDATION_RULES.email],
  password: [...VALIDATION_RULES.password],
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
  username: [...VALIDATION_RULES.username],
  captchaCode: [...VALIDATION_RULES.captchaCode]
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

const goHome = () => {
  router.push('/')
}
</script>

<template>
  <div class="public-editorial register-page">
    <header class="register-header">
      <button aria-label="返回 LexiCraft 首页" class="register-brand" type="button" @click="goHome">
        <BrandLogo tagline="language journal"/>
      </button>
      <button class="back-home" type="button" @click="goHome">返回首页 <span>→</span></button>
    </header>

    <main class="register-manuscript">
      <section class="welcome-copy" aria-labelledby="register-title">
        <p class="issue-line">REGISTER EDITION · VOL. 01</p>
        <h1 id="register-title">写下名字，<br/><em>开始这一册。</em></h1>
        <p class="welcome-deck">从一个词、一篇文章开始，把每次练习积累成属于你的学习记录。</p>
        <blockquote>
          “A different language is a different vision of life.”
          <cite>— Federico Fellini</cite>
        </blockquote>
        <div class="page-note" aria-hidden="true">
          <span>02</span>
          <div><strong>CREATE</strong><strong>LEARN</strong><strong>RETURN</strong></div>
        </div>
      </section>

      <section class="register-entry" aria-label="注册表单">
        <div class="entry-heading">
          <span>CREATE ACCOUNT</span>
          <h2>创建学习账号</h2>
          <p>填写基础信息，建立你的学习书页。</p>
        </div>

        <Form ref="registerFormRef" :model="registerForm" :rules="registerFormRules" class="register-form">
          <FormItem label="电子邮箱" prop="email">
            <BaseInput
                v-model="registerForm.email"
                autocomplete="email"
                placeholder="name@example.com"
                size="large"
                type="email"
            />
          </FormItem>

          <FormItem label="用户名" prop="username">
            <BaseInput
                v-model="registerForm.username"
                autocomplete="username"
                placeholder="请输入用户名"
                size="large"
                type="text"
            />
          </FormItem>

          <FormItem label="密码" prop="password">
            <BaseInput
                v-model="registerForm.password"
                autocomplete="new-password"
                placeholder="请输入密码（8-20位）"
                size="large"
                type="password"
            />
          </FormItem>

          <FormItem label="确认密码" prop="confirmPassword">
            <BaseInput
                v-model="registerForm.confirmPassword"
                autocomplete="new-password"
                placeholder="请再次输入密码"
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

        <Notice class="terms-note">
          <span>注册即表示你同意服务条款与隐私说明。</span>
        </Notice>

        <BaseButton
            :loading="loading || isLoading"
            class="register-submit"
            size="large"
            @click="handleRegister"
        >
          创建学习账号 <span aria-hidden="true">→</span>
        </BaseButton>

        <p class="login-line">已有账号？ <button type="button" @click="goToLogin">返回登录，继续学习</button></p>
      </section>
    </main>

    <footer class="register-footer"><span>LEXICRAFT LANGUAGE JOURNAL</span><span>PAGE 02 / REGISTER</span></footer>
  </div>
</template>

<style lang="scss" scoped>
.register-page { position: relative; display: flex; min-height: 100vh; min-height: 100dvh; flex-direction: column; overflow: hidden; color: var(--text-primary); background-color: var(--surface-page); background-image: radial-gradient(circle at 20% 18%, color-mix(in srgb, var(--text-primary) 4%, transparent) 0 .7px, transparent .9px); background-size: 23px 23px; font-family: var(--font-editorial); }
.register-page::before { position: absolute; top: 0; bottom: 0; left: 13%; width: 1px; background: color-mix(in srgb, var(--accent) 28%, transparent); content: ''; pointer-events: none; }
.register-header { position: relative; z-index: 3; display: flex; align-items: center; justify-content: space-between; padding: 24px clamp(20px, 5vw, 74px); }
.register-brand { display: flex; align-items: center; gap: 10px; padding: 0; border: 0; color: var(--text-primary); background: transparent; cursor: pointer; }
.register-brand :deep(.brand-logo__mark) { width: 38px; height: 38px; }
.register-brand :deep(.brand-logo__copy strong) { font-size: 17px; }
.back-home { padding: 7px 0; border: 0; border-bottom: 1px solid currentColor; color: var(--text-secondary); background: transparent; cursor: pointer; font-family: var(--font-sans); font-size: 11px; }
.register-manuscript { position: relative; z-index: 2; display: grid; width: min(1220px, calc(100% - 44px)); flex: 1; grid-template-columns: minmax(0, 1.08fr) minmax(400px, .72fr); align-items: center; gap: clamp(50px, 8vw, 130px); margin: 0 auto; padding: 32px 0 48px; }
.welcome-copy { position: relative; display: flex; align-self: stretch; flex-direction: column; justify-content: center; padding: 60px 0 80px; }
.issue-line { margin: 0 0 24px; color: var(--accent); font-family: var(--font-sans); font-size: 9px; font-weight: 800; letter-spacing: .18em; }
.welcome-copy h1 { margin: 0; font-size: clamp(58px, 7vw, 108px); font-weight: 400; letter-spacing: -.055em; line-height: .88; }
.welcome-copy h1 em { color: var(--accent); font-weight: 400; }
.welcome-deck { max-width: 570px; margin: 32px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 16px; line-height: 1.85; }
.welcome-copy blockquote { max-width: 510px; margin: auto 0 0; padding: 22px 0 0 24px; border-left: 1px solid var(--accent); color: var(--text-secondary); font-size: 17px; font-style: italic; line-height: 1.65; }
.welcome-copy cite { display: block; margin-top: 10px; color: var(--text-tertiary); font-family: var(--font-sans); font-size: 10px; font-style: normal; letter-spacing: .1em; }
.page-note { position: absolute; right: -18px; bottom: 10%; display: flex; align-items: center; gap: 14px; color: var(--text-tertiary); transform: rotate(-90deg) translateX(100%); transform-origin: right bottom; }
.page-note > span { font-size: 38px; }
.page-note div { display: flex; gap: 10px; font-family: var(--font-sans); font-size: 7px; letter-spacing: .1em; }
.register-entry { position: relative; padding: clamp(28px, 3.4vw, 46px); overflow: hidden; border: 1px solid var(--border-strong); border-top-width: 5px; color: var(--text-primary); background: var(--surface-card); box-shadow: var(--card-shadow); }
.register-entry::before { position: absolute; top: 18px; right: -42px; padding: 6px 40px; color: var(--accent-contrast); background: var(--accent); content: 'NEW COPY'; font-family: var(--font-sans); font-size: 7px; font-weight: 800; letter-spacing: .14em; transform: rotate(45deg); }
.entry-heading > span { color: var(--accent); font-family: var(--font-sans); font-size: 9px; font-weight: 800; letter-spacing: .18em; }
.entry-heading h2 { margin: 10px 0 0; font-size: 34px; font-weight: 400; }
.entry-heading p { margin: 8px 0 20px; color: var(--text-secondary); font-family: var(--font-sans); font-size: 13px; }
.register-form { display: grid; gap: 2px; }
:deep(.form-item) { position: relative; z-index: 2; align-items: stretch; flex-direction: column; gap: 4px; margin-bottom: 4px; }
:deep(.form-item .w-20) { width: auto; justify-content: flex-start; color: var(--text-secondary); font-family: var(--font-sans); font-size: 11px; font-weight: 700; }
:deep(.form-item .flex-1) { width: 100%; }
:deep(.form-error) { min-height: 12px; margin-top: 2px; font-family: var(--font-sans); font-size: 10px; }
:deep(.base-input) { padding-inline: 0; border-width: 0 0 1px; border-radius: 0; background: transparent; }
:deep(.base-input.focus) { border-bottom-color: var(--accent); box-shadow: 0 3px 0 -1px var(--focus-ring); }
:deep(.base-input .inner) { font-family: var(--font-sans); font-size: 14px; }
:deep(.captcha-container) { gap: 12px; }
:deep(.captcha-image-container) { border-color: var(--border-color) !important; border-radius: 0 !important; background: var(--surface-raised); }
.terms-note { margin-block: 8px 14px !important; font-family: var(--font-sans); font-size: 10px; }
:deep(.register-submit) { display: flex; width: 100%; min-height: 50px; border: 1px solid var(--text-primary); border-radius: 0; background: var(--text-primary); box-shadow: var(--control-shadow); font-family: var(--font-sans); }
:deep(.register-submit > span) { display: flex; align-items: center; justify-content: space-between; width: 100%; }
:deep(.register-submit:hover:not(.disabled)) { background: var(--accent); }
.login-line { margin: 20px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 11px; text-align: center; }
.login-line button { padding: 0; border: 0; border-bottom: 1px solid currentColor; color: var(--accent); background: transparent; cursor: pointer; }
.register-footer { position: relative; z-index: 2; display: flex; justify-content: space-between; padding: 16px clamp(20px, 5vw, 74px); border-top: 1px solid var(--border-color); color: var(--text-tertiary); font-family: var(--font-sans); font-size: 8px; letter-spacing: .14em; }
button:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 3px; }

@media (max-width: 900px) {
  .register-page { overflow: auto; }
  .register-manuscript { grid-template-columns: 1fr; gap: 30px; padding-top: 20px; }
  .welcome-copy { min-height: auto; padding: 45px 0 10px; }
  .welcome-copy blockquote { margin-top: 35px; }
  .page-note { display: none; }
  .register-entry { width: min(100%, 560px); margin-inline: auto; box-sizing: border-box; }
}
@media (max-width: 560px) {
  .register-page::before { left: 24px; }
  .register-header { padding: 15px; }
  .back-home { font-size: 9px; }
  .register-manuscript { width: calc(100% - 28px); padding-bottom: 35px; }
  .welcome-copy { padding-left: 18px; }
  .welcome-copy h1 { font-size: clamp(48px, 16vw, 68px); }
  .welcome-copy blockquote { display: none; }
  .register-entry { padding: 28px 20px; }
  .register-entry::before { display: none; }
  .register-footer { align-items: flex-start; flex-direction: column; gap: 5px; }
}
@media (prefers-reduced-motion: reduce) { *, *::before, *::after { transition-duration: .01ms !important; animation-duration: .01ms !important; } }
</style>
