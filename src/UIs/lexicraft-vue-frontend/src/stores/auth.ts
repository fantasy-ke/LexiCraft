/**
 * 认证状态管理 Store
 * 使用 Pinia 管理用户认证状态和相关操作
 */

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import {
  AuthState,
  UserProfile,
  LoginRequest,
  RegisterRequest,
  OAuthProvider,
  OAuthCallbackParams,
  UpdateProfileRequest,
  TokenPair,
  IAuthActions
} from '@/types/auth'
import { authAPI } from '@/apis/auth'
import { tokenManager } from '@/utils/tokenManager'
import Toast from '@/components/base/toast/Toast'

export const useAuthStore = defineStore('auth', () => {
  // 状态
  const user = ref<UserProfile | null>(null)
  const isAuthenticated = ref(false)
  const isLoading = ref(false)
  const tokens = ref<TokenPair | null>(null)
  const permissions = ref<string[]>([])

  // 计算属性
  const authState = computed<AuthState>(() => ({
    user: user.value,
    isAuthenticated: isAuthenticated.value,
    isLoading: isLoading.value,
    tokens: tokens.value
  }))

  const isLogin = computed(() => isAuthenticated.value)

  // 初始化认证状态
  async function initializeAuth(): Promise<void> {
    try {
      isLoading.value = true
      
      const accessToken = tokenManager.getAccessToken()
      const refreshToken = tokenManager.getRefreshToken()
      
      if (accessToken && refreshToken) {
        // 检查 Token 是否有效
        if (tokenManager.isTokenExpired(accessToken)) {
          // 尝试刷新 Token
          const refreshed = await tokenManager.refreshTokenIfNeeded()
          if (!refreshed) {
            await logout()
            return
          }
        }
        
        // 获取用户信息
        await fetchUserProfile()
      }
    } catch (error) {
      console.error('Initialize auth error:', error)
      await logout()
    } finally {
      isLoading.value = false
    }
  }

  // 检查认证状态
  function checkAuthStatus(): boolean {
    const accessToken = tokenManager.getAccessToken()
    return !!(accessToken && !tokenManager.isTokenExpired(accessToken))
  }

  // 用户登录
  async function login(credentials: LoginRequest): Promise<void> {
    try {
      isLoading.value = true
      
      const response = await authAPI.login(credentials)
      
      if (response.status && response.data) {
        const { token, refreshToken } = response.data
        
        // 存储 Token
        const tokenPair: TokenPair = {
          accessToken: token,
          refreshToken,
          expiresIn: 3600 // 后端暂未返回时长，默认1小时
        }
        
        tokenManager.setTokens(tokenPair)
        tokens.value = tokenPair
        
        // 登录成功后立即获取完整用户信息
        await fetchUserProfile()
        
        Toast.success('登录成功!')
      } else {
        throw new Error(response.message || '登录失败')
      }
    } catch (error: any) {
      console.error('Login error:', error)
      
      // 确保清除任何可能存在的无效 token
      tokenManager.clearTokens()
      tokens.value = null
      user.value = null
      isAuthenticated.value = false
      
      // 不在这里显示错误提示，让上层处理
      throw error
    } finally {
      isLoading.value = false
    }
  }

  // 用户注册
  async function register(userData: RegisterRequest): Promise<void> {
    try {
      isLoading.value = true
      
      const response = await authAPI.register(userData)
      
      if (response.status && response.data) {
        Toast.success('注册成功!')
        
        // 注册成功后自动登录
        await login({
          userAccount: userData.email,
          password: userData.password
        })
      } else {
        throw new Error(response.message || '注册失败')
      }
    } catch (error: any) {
      console.error('Register error:', error)
      // 不在这里显示错误提示，让上层处理
      throw error
    } finally {
      isLoading.value = false
    }
  }

  // 用户登出
  async function logout(): Promise<void> {
    try {
      isLoading.value = true
      
      // 调用后端登出接口
      await authAPI.logout()
    } catch (error) {
      console.error('Logout API error:', error)
      // 即使后端登出失败，也要清除本地状态
    } finally {
      // 清除本地状态
      tokenManager.clearTokens()
      user.value = null
      isAuthenticated.value = false
      tokens.value = null
      isLoading.value = false
      
      // 只在这里显示一次成功提示
      Toast.success('已退出登录')
    }
  }

  // OAuth 登录
  async function loginWithOAuth(provider: OAuthProvider): Promise<void> {
    try {
      isLoading.value = true
      
      const response = await authAPI.initiateOAuth(provider)
      
      if (response.status && response.data) {
        const { authUrl } = response.data
        
        // 跳转到 OAuth 提供商
        window.location.href = authUrl
      } else {
        throw new Error(response.message || 'OAuth 初始化失败')
      }
    } catch (error: any) {
      console.error('OAuth login error:', error)
      // 不在这里显示错误提示，让上层处理
      throw error
    } finally {
      isLoading.value = false
    }
  }

  // 处理 OAuth 回调
  async function handleOAuthCallback(params: OAuthCallbackParams): Promise<void> {
    try {
      isLoading.value = true
      
      const response = await authAPI.handleOAuthCallback(params)
      
      if (response.status && response.data) {
        const { token, refreshToken } = response.data
        
        // 存储 Token
        const tokenPair: TokenPair = {
          accessToken: token,
          refreshToken,
          expiresIn: 3600 // 后端暂未返回时长，默认1小时
        }
        
        tokenManager.setTokens(tokenPair)
        tokens.value = tokenPair
        
        // 登录成功后立即获取完整用户信息
        await fetchUserProfile()
        
        Toast.success('OAuth 登录成功!')
      } else {
        throw new Error(response.message || 'OAuth 登录失败')
      }
    } catch (error: any) {
      console.error('OAuth callback error:', error)
      // 不在这里显示错误提示，让上层处理
      throw error
    } finally {
      isLoading.value = false
    }
  }

  // 获取用户资料
  async function fetchUserProfile(): Promise<void> {
    try {
      const response = await authAPI.getUserProfile()
      
      if (response.status && response.data) {
        // 进行字段映射，兼顾后端 PascalCase 转换为 camelCase 后的新字段以及前端旧字段
        const userData = response.data
        user.value = {
          ...userData,
          id: userData.userId, // 别名兼容
          username: userData.userName // 别名兼容
        }
        isAuthenticated.value = true

        // 获取用户信息后，继续获取用户权限
        if (user.value?.id) {
          await fetchUserPermissions(user.value.id)
        }
      } else {
        throw new Error(response.message || '获取用户信息失败')
      }
    } catch (error: any) {
      console.error('Fetch user profile error:', error)
      // 根据用户要求，请求异常不需要退出登录
      // await logout()
      throw error
    }
  }

  // 获取用户权限
  async function fetchUserPermissions(userId: string): Promise<void> {
    try {
      const response = await authAPI.getUserPermissions(userId)
      if (response.status && response.data) {
        permissions.value = response.data.permissions
      }
    } catch (error) {
      console.error('Fetch user permissions error:', error)
    }
  }

  // 更新用户资料
  async function updateProfile(profile: UpdateProfileRequest): Promise<void> {
    try {
      isLoading.value = true
      
      const response = await authAPI.updateUserProfile(profile)
      
      if (response.status && response.data) {
        user.value = response.data
        Toast.success('用户资料更新成功!')
      } else {
        throw new Error(response.message || '更新用户资料失败')
      }
    } catch (error: any) {
      console.error('Update profile error:', error)
      Toast.error(error.message || '更新用户资料失败，请重试')
      throw error
    } finally {
      isLoading.value = false
    }
  }

  // 设置用户信息（兼容旧代码）
  function setUser(userInfo: UserProfile | any): void {
    user.value = userInfo
    isAuthenticated.value = true
  }

  // 设置 Token（兼容旧代码）
  function setToken(token: string): void {
    const tokenPair: TokenPair = {
      accessToken: token,
      refreshToken: '', // 如果没有 refresh token，设为空
      expiresIn: 3600 // 默认 1 小时
    }
    
    tokenManager.setTokens(tokenPair)
    tokens.value = tokenPair
    isAuthenticated.value = true
  }

  // 清除 Token（兼容旧代码）
  function clearToken(): void {
    tokenManager.clearTokens()
    user.value = null
    isAuthenticated.value = false
    tokens.value = null
  }

  // 监听全局登出事件
  if (typeof window !== 'undefined') {
    window.addEventListener('auth:logout', () => {
      logout()
    })
  }

  return {
    // 状态
    user,
    isAuthenticated,
    isLoading,
    tokens,
    authState,
    isLogin, // 兼容旧代码
    
    // 操作
    initializeAuth,
    checkAuthStatus,
    login,
    register,
    logout,
    loginWithOAuth,
    handleOAuthCallback,
    fetchUserProfile,
    fetchUserPermissions,
    updateProfile,
    
    // 兼容旧代码的方法
    setUser,
    setToken,
    clearToken,
    
    // 别名（兼容旧代码）
    init: initializeAuth
  }
})

// 导出类型
export type AuthStore = ReturnType<typeof useAuthStore>