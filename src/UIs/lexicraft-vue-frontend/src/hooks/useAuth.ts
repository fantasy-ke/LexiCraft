/**
 * 认证组合式函数
 * 替换原有的 useLogto，提供与 Identity 服务直接集成的认证功能
 */

import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { 
  LoginRequest, 
  RegisterRequest, 
  OAuthProvider, 
  OAuthCallbackParams,
  UpdateProfileRequest 
} from '@/types/auth'
import { AUTH_PATHS } from '@/config/auth.config'

/**
 * 认证组合式函数
 */
export function useAuth() {
  const router = useRouter()
  const authStore = useAuthStore()

  // 计算属性
  const isAuthenticated = computed(() => authStore.isAuthenticated)
  const isLoading = computed(() => authStore.isLoading)
  const user = computed(() => authStore.user)
  const authState = computed(() => authStore.authState)

  /**
   * 初始化认证状态
   */
  const initAuth = async () => {
    await authStore.initializeAuth()
  }

  /**
   * 检查认证状态
   */
  const checkAuthStatus = () => {
    return authStore.checkAuthStatus()
  }

  /**
   * 用户账号登录（支持邮箱或用户名）
   */
  const signInWithAccount = async (userAccount: string, password: string) => {
    const credentials: LoginRequest = {
      userAccount,
      password
    }
    
    try {
      await authStore.login(credentials)
      
      // 登录成功后跳转
      const redirect = router.currentRoute.value.query.redirect as string
      router.push(redirect || AUTH_PATHS.REDIRECT)
    } catch (error: any) {
      // 在这里统一处理错误提示
      const { parseAuthError } = await import('@/utils/authHelpers')
      const errorMessage = parseAuthError(error)
      
      // 导入 Toast（避免循环依赖）
      const Toast = (await import('@/components/base/toast/Toast')).default
      Toast.error(errorMessage)
      
      throw error
    }
  }

  /**
   * 邮箱密码登录（保持向后兼容）
   */
  const signInWithEmail = async (email: string, password: string) => {
    return signInWithAccount(email, password)
  }

  /**
   * 用户名密码登录
   */
  const signInWithUsername = async (username: string, password: string) => {
    const credentials: LoginRequest = {
      userAccount: username,
      password
    }
    
    await authStore.login(credentials)
    
    // 登录成功后跳转
    const redirect = router.currentRoute.value.query.redirect as string
    router.push(redirect || AUTH_PATHS.REDIRECT)
  }

  /**
   * OAuth 登录
   */
  const signInWithOAuth = async (provider: OAuthProvider) => {
    try {
      await authStore.loginWithOAuth(provider)
    } catch (error: any) {
      // 在这里统一处理错误提示
      const { parseAuthError } = await import('@/utils/authHelpers')
      const errorMessage = parseAuthError(error)
      
      // 导入 Toast（避免循环依赖）
      const Toast = (await import('@/components/base/toast/Toast')).default
      Toast.error(errorMessage)
      
      throw error
    }
  }

  /**
   * 用户注册
   */
  const signUp = async (userData: RegisterRequest) => {
    try {
      await authStore.register(userData)
      
      // 注册成功后跳转到登录页或直接跳转到应用
      router.push(AUTH_PATHS.REDIRECT)
    } catch (error: any) {
      // 在这里统一处理错误提示
      const { parseAuthError } = await import('@/utils/authHelpers')
      const errorMessage = parseAuthError(error)
      
      // 导入 Toast（避免循环依赖）
      const Toast = (await import('@/components/base/toast/Toast')).default
      Toast.error(errorMessage)
      
      throw error
    }
  }

  /**
   * 处理 OAuth 回调
   */
  const handleSignInCallback = async (params?: OAuthCallbackParams) => {
    if (!params) {
      // 从 URL 参数中提取回调信息
      const urlParams = new URLSearchParams(window.location.search)
      const code = urlParams.get('code')
      const state = urlParams.get('state')
      const provider = urlParams.get('provider') as OAuthProvider
      
      if (!code || !state || !provider) {
        throw new Error('OAuth 回调参数不完整')
      }
      
      params = { code, state, provider }
    }
    
    await authStore.handleOAuthCallback(params)
    
    // 回调成功后跳转
    const redirect = router.currentRoute.value.query.redirect as string
    router.push(redirect || AUTH_PATHS.REDIRECT)
    
    return authStore.user
  }

  /**
   * 登出
   */
  const signOut = async () => {
    await authStore.logout()
    router.push(AUTH_PATHS.LOGIN)
  }

  /**
   * 获取用户信息
   */
  const getUserInfo = async () => {
    await authStore.fetchUserProfile()
    return authStore.user
  }

  /**
   * 更新用户资料
   */
  const updateUserProfile = async (profile: UpdateProfileRequest) => {
    await authStore.updateProfile(profile)
  }

  /**
   * 获取访问令牌
   */
  const getAccessToken = () => {
    return authStore.tokens?.accessToken || null
  }

  /**
   * 刷新令牌
   */
  const refreshToken = async () => {
    // Token 刷新由 tokenManager 自动处理
    // 这里只是触发一次检查
    const { tokenManager } = await import('@/utils/tokenManager')
    return await tokenManager.refreshTokenIfNeeded()
  }

  /**
   * 检查是否需要登录
   */
  const requireAuth = () => {
    if (!isAuthenticated.value) {
      router.push({
        path: AUTH_PATHS.LOGIN,
        query: { redirect: router.currentRoute.value.fullPath }
      })
      return false
    }
    return true
  }

  /**
   * 导航守卫辅助函数
   */
  const authGuard = async (to: any, from: any, next: any) => {
    // 检查路由是否需要认证
    const requiresAuth = to.matched.some((record: any) => record.meta.requiresAuth)
    const isPublicRoute = to.meta?.public === true

    if (isPublicRoute) {
      next()
      return
    }

    if (requiresAuth) {
      if (!isAuthenticated.value) {
        // 尝试初始化认证状态
        await initAuth()
      }

      if (!isAuthenticated.value) {
        next({
          path: AUTH_PATHS.LOGIN,
          query: { redirect: to.fullPath }
        })
        return
      }
    }

    next()
  }

  return {
    // 状态
    isAuthenticated,
    isLoading,
    user,
    authState,
    
    // 方法
    initAuth,
    checkAuthStatus,
    signInWithAccount,
    signInWithEmail,
    signInWithUsername,
    signInWithOAuth,
    signUp,
    handleSignInCallback,
    signOut,
    getUserInfo,
    updateUserProfile,
    getAccessToken,
    refreshToken,
    requireAuth,
    authGuard,
    
    // 兼容旧代码的别名
    initLogto: initAuth,
    handleSignInCallback: handleSignInCallback
  }
}