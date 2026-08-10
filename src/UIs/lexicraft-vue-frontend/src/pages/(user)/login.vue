<script lang="tsx" setup>
import {ref} from 'vue'
import {useRouter} from 'vue-router'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseButton from '@/components/BaseButton.vue'
import {passwordRules} from '@/utils/validation'
import FormItem from '@/components/base/form/FormItem.vue'
import Form from '@/components/base/form/Form.vue'
import Notice from '@/components/user/Notice.vue'
import {FormInstance} from '@/components/base/form/types'
import {useAuth} from '@/hooks/useAuth'
import {REGISTER_PATH} from '@/config/auth.config'
import LoadingScreen from '@/components/LoadingScreen.vue'
import BrandLogo from '@/components/BrandLogo.vue'

const router = useRouter()
const {signInWithAccount, signInWithOAuth, isLoading} = useAuth()
const loading = ref(false)
const oauthLoading = ref<string | null>(null)
const isRedirecting = ref(false)
const loginForm = ref({userAccount: '', password: ''})
const loginFormRef = ref<FormInstance>()
const loginFormRules = {
  userAccount: [{required: true, message: '请输入用户名或邮箱', trigger: 'blur'}],
  password: passwordRules
}

async function handleLogin() {
  if (!loginFormRef.value) return
  loginFormRef.value.validate(async valid => {
    if (!valid) return
    try {
      loading.value = true
      await signInWithAccount(loginForm.value.userAccount, loginForm.value.password)
      isRedirecting.value = true
    } catch (error) {
      console.error('Login failed:', error)
    } finally {
      loading.value = false
    }
  })
}

async function handleOAuthLogin(provider: string) {
  try {
    oauthLoading.value = provider
    await signInWithOAuth(provider as any)
    isRedirecting.value = true
  } catch (error) {
    console.error('OAuth login failed:', error)
  } finally {
    oauthLoading.value = null
  }
}

const goToRegister = () => router.push(REGISTER_PATH)
const goHome = () => router.push('/')
const goToForgot = () => {
  // 忘记密码流程待后端能力接入后补充
}
</script>

<template>
  <div class="public-editorial login-page">
    <LoadingScreen v-if="isRedirecting" :progress="100" loading-text="正在整理你的学习书页..."/>

    <header class="login-header">
      <button aria-label="?? LexiCraft ??" class="login-brand" type="button" @click="goHome">
        <BrandLogo tagline="language journal"/>
      </button>
      <button class="back-home" type="button" @click="goHome">返回首页 <span>→</span></button>
    </header>

    <main class="login-manuscript">
      <section class="welcome-copy" aria-labelledby="login-title">
        <p class="issue-line">MEMBER EDITION · VOL. 01</p>
        <h1 id="login-title">欢迎回来，<br/><em>继续读下去。</em></h1>
        <p class="welcome-deck">你的词汇、阅读与练习，都在上次停下的那一页等你。</p>
        <blockquote>
          “The beautiful thing about learning is that nobody can take it away from you.”
          <cite>— B. B. King</cite>
        </blockquote>
        <div class="page-note" aria-hidden="true">
          <span>01</span>
          <div><strong>READ</strong><strong>REMEMBER</strong><strong>RETURN</strong></div>
        </div>
      </section>

      <section class="login-entry" aria-label="登录表单">
        <div class="entry-heading">
          <span>SIGN IN</span>
          <h2>翻开你的书页</h2>
          <p>使用账号继续今天的学习。</p>
        </div>

        <Form ref="loginFormRef" :model="loginForm" :rules="loginFormRules" class="login-form">
          <FormItem label="用户名或邮箱" prop="userAccount">
            <BaseInput
                v-model="loginForm.userAccount"
                autocomplete="username"
                placeholder="name@example.com"
                size="large"
                type="text"
                @enter="handleLogin"
            />
          </FormItem>
          <FormItem label="密码" prop="password">
            <BaseInput
                v-model="loginForm.password"
                autocomplete="current-password"
                placeholder="请输入密码"
                size="large"
                type="password"
                @enter="handleLogin"
            />
          </FormItem>
        </Form>

        <div class="form-assist">
          <label><input type="checkbox"/> <span>保持登录</span></label>
          <button type="button" @click="goToForgot">忘记密码</button>
        </div>

        <Notice class="terms-note">
          <span>登录即表示你同意服务条款与隐私说明。</span>
        </Notice>

        <BaseButton
            :loading="loading || isLoading"
            class="login-submit"
            size="large"
            @click="handleLogin"
        >
          进入学习空间 <span aria-hidden="true">→</span>
        </BaseButton>

        <div class="oauth-section">
          <span class="oauth-label">或者使用</span>
          <div class="oauth-actions">
            <button :disabled="!!oauthLoading" class="oauth-button" type="button" @click="handleOAuthLogin('github')">
              <IconMdiGithub/><span>GitHub</span><i v-if="oauthLoading === 'github'"></i>
            </button>
            <button :disabled="!!oauthLoading" class="oauth-button oauth-button--gitee" type="button" @click="handleOAuthLogin('gitee')">
              <IconSimpleIconsGitee/><span>Gitee</span><i v-if="oauthLoading === 'gitee'"></i>
            </button>
          </div>
        </div>

        <p class="register-line">还没有账号？ <button type="button" @click="goToRegister">创建一册新的学习手记</button></p>
      </section>
    </main>

    <footer class="login-footer"><span>LEXICRAFT LANGUAGE JOURNAL</span><span>PAGE 01 / SIGN IN</span></footer>
  </div>
</template>

<style lang="scss" scoped>
.login-page { position: relative; display: flex; min-height: 100vh; min-height: 100dvh; flex-direction: column; overflow: hidden; color: var(--text-primary); background-color: var(--surface-page); background-image: radial-gradient(circle at 20% 18%, color-mix(in srgb, var(--text-primary) 4%, transparent) 0 .7px, transparent .9px); background-size: 23px 23px; font-family: var(--font-editorial); }
.login-page::before { content: ''; position: absolute; top: 0; bottom: 0; left: 13%; width: 1px; background: color-mix(in srgb, var(--accent) 28%, transparent); pointer-events: none; }
.login-header { position: relative; z-index: 3; display: flex; align-items: center; justify-content: space-between; padding: 24px clamp(20px, 5vw, 74px); }
.login-brand { display: flex; align-items: center; gap: 10px; padding: 0; border: 0; color: var(--text-primary); background: transparent; cursor: pointer; }
.login-brand :deep(.brand-logo__mark) { width: 38px; height: 38px; }
.login-brand :deep(.brand-logo__copy strong) { font-size: 17px; }
.back-home { padding: 7px 0; border: 0; border-bottom: 1px solid currentColor; color: var(--text-secondary); background: transparent; cursor: pointer; font-family: var(--font-sans); font-size: 11px; }
.login-manuscript { position: relative; z-index: 2; display: grid; width: min(1220px, calc(100% - 44px)); flex: 1; grid-template-columns: minmax(0, 1.08fr) minmax(380px, .72fr); align-items: center; gap: clamp(50px, 8vw, 130px); margin: 0 auto; padding: 44px 0 64px; }
.welcome-copy { position: relative; align-self: stretch; display: flex; flex-direction: column; justify-content: center; padding: 60px 0 80px; }
.issue-line { margin: 0 0 24px; color: var(--accent); font-family: var(--font-sans); font-size: 9px; font-weight: 800; letter-spacing: .18em; }
.welcome-copy h1 { margin: 0; font-size: clamp(58px, 7vw, 108px); font-weight: 400; letter-spacing: -.055em; line-height: .88; }
.welcome-copy h1 em { color: var(--accent); font-weight: 400; }
.welcome-deck { max-width: 570px; margin: 32px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 16px; line-height: 1.85; }
.welcome-copy blockquote { max-width: 510px; margin: auto 0 0; padding: 22px 0 0 24px; border-left: 1px solid var(--accent); color: var(--text-secondary); font-size: 17px; font-style: italic; line-height: 1.65; }
.welcome-copy cite { display: block; margin-top: 10px; color: var(--text-tertiary); font-family: var(--font-sans); font-size: 10px; font-style: normal; letter-spacing: .1em; }
.page-note { position: absolute; right: -18px; bottom: 10%; display: flex; align-items: center; gap: 14px; color: var(--text-tertiary); transform: rotate(-90deg) translateX(100%); transform-origin: right bottom; }
.page-note > span { font-size: 38px; }
.page-note div { display: flex; gap: 10px; font-family: var(--font-sans); font-size: 7px; letter-spacing: .1em; }
.login-entry { position: relative; padding: clamp(30px, 4vw, 54px); border: 1px solid var(--border-strong); border-top-width: 5px; color: var(--text-primary); background: var(--surface-card); box-shadow: var(--card-shadow); }
.login-entry::before { content: 'MEMBER COPY'; position: absolute; top: 18px; right: -42px; padding: 6px 40px; color: var(--accent-contrast); background: var(--accent); font-family: var(--font-sans); font-size: 7px; font-weight: 800; letter-spacing: .14em; transform: rotate(45deg); }
.entry-heading > span { color: var(--accent); font-family: var(--font-sans); font-size: 9px; font-weight: 800; letter-spacing: .18em; }
.entry-heading h2 { margin: 10px 0 0; font-size: 36px; font-weight: 400; }
.entry-heading p { margin: 8px 0 28px; color: var(--text-secondary); font-family: var(--font-sans); font-size: 13px; }
.login-form { display: grid; gap: 10px; }
:deep(.form-item) { position: relative; z-index: 2; align-items: stretch; flex-direction: column; gap: 6px; }
:deep(.form-item .w-20) { width: auto; justify-content: flex-start; color: var(--text-secondary); font-family: var(--font-sans); font-size: 11px; font-weight: 700; }
:deep(.form-item .flex-1) { width: 100%; }
:deep(.base-input) { border-width: 0 0 1px; border-radius: 0; padding-inline: 0; background: transparent; }
:deep(.base-input.focus) { border-bottom-color: var(--accent); box-shadow: 0 3px 0 -1px var(--focus-ring); }
:deep(.base-input .inner) { font-family: var(--font-sans); font-size: 15px; }
.form-assist { display: flex; align-items: center; justify-content: space-between; margin: 4px 0 14px; color: var(--text-tertiary); font-family: var(--font-sans); font-size: 10px; }
.form-assist label { display: flex; align-items: center; gap: 6px; }
.form-assist input { accent-color: var(--accent); }
.form-assist button { padding: 4px 0; border: 0; color: var(--accent); background: transparent; cursor: pointer; }
.terms-note { margin-block: 10px 18px !important; font-family: var(--font-sans); font-size: 10px; }
:deep(.login-submit) { display: flex; width: 100%; min-height: 50px; border: 1px solid var(--text-primary); border-radius: 0; background: var(--text-primary); box-shadow: var(--control-shadow); font-family: var(--font-sans); }
:deep(.login-submit > span) { display: flex; align-items: center; justify-content: space-between; width: 100%; }
:deep(.login-submit:hover:not(.disabled)) { background: var(--accent); transform: translateY(-2px); }
.oauth-section { margin-top: 28px; }
.oauth-label { display: flex; align-items: center; gap: 12px; color: var(--text-tertiary); font-family: var(--font-sans); font-size: 9px; }
.oauth-label::before, .oauth-label::after { content: ''; flex: 1; height: 1px; background: var(--border-color); }
.oauth-actions { display: grid; grid-template-columns: 1fr 1fr; gap: 9px; margin-top: 14px; }
.oauth-button { position: relative; display: flex; min-height: 42px; align-items: center; justify-content: center; gap: 8px; border: 1px solid var(--border-color); border-radius: 0; color: var(--text-primary); background: var(--surface-raised); cursor: pointer; font-family: var(--font-sans); font-size: 11px; }
.oauth-button:hover:not(:disabled) { border-color: var(--accent); color: var(--accent); }
.oauth-button:disabled { opacity: .6; cursor: wait; }
.oauth-button i { position: absolute; width: 16px; height: 16px; border: 2px solid var(--border-color); border-top-color: var(--accent); border-radius: 50%; animation: spin .8s linear infinite; }
.oauth-button--gitee :deep(svg) { color: var(--danger); }
.register-line { margin: 24px 0 0; color: var(--text-secondary); font-family: var(--font-sans); font-size: 11px; text-align: center; }
.register-line button { padding: 0; border: 0; border-bottom: 1px solid currentColor; color: var(--accent); background: transparent; cursor: pointer; }
.login-footer { position: relative; z-index: 2; display: flex; justify-content: space-between; padding: 16px clamp(20px, 5vw, 74px); border-top: 1px solid var(--border-color); color: var(--text-tertiary); font-family: var(--font-sans); font-size: 8px; letter-spacing: .14em; }
button:focus-visible { outline: 3px solid var(--focus-ring); outline-offset: 3px; }
@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 900px) {
  .login-page { overflow: auto; }
  .login-manuscript { grid-template-columns: 1fr; gap: 30px; padding-top: 20px; }
  .welcome-copy { min-height: auto; padding: 45px 0 10px; }
  .welcome-copy blockquote { margin-top: 35px; }
  .page-note { display: none; }
  .login-entry { width: min(100%, 560px); margin-inline: auto; box-sizing: border-box; }
}
@media (max-width: 560px) {
  .login-page::before { left: 24px; }
  .login-header { padding: 15px; }
  .back-home { font-size: 9px; }
  .login-manuscript { width: calc(100% - 28px); padding-bottom: 35px; }
  .welcome-copy { padding-left: 18px; }
  .welcome-copy h1 { font-size: clamp(48px, 16vw, 68px); }
  .welcome-copy blockquote { display: none; }
  .login-entry { padding: 28px 20px; }
  .login-entry::before { display: none; }
  .oauth-actions { grid-template-columns: 1fr; }
  .login-footer { align-items: flex-start; flex-direction: column; gap: 5px; }
}
@media (prefers-reduced-motion: reduce) { *, *::before, *::after { transition-duration: .01ms !important; animation-duration: .01ms !important; } }
</style>
