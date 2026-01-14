/**
 * 认证 API 客户端
 * 提供与 LexiCraft.Services.Identity 服务通信的所有认证相关方法
 */

import {
  ResultDto,
  LoginRequest,
  RegisterRequest,
  LoginResponse,
  RegisterResponse,
  UserProfile,
  OAuthProvider,
  OAuthInitResponse,
  OAuthCallbackParams,
  TokenPair,
  UpdateProfileRequest,
  UserPermissionsResponse,
  IAuthAPI,
  CaptchaResponse
} from '@/types/auth'
import { authGet, authPost, authPut } from '@/utils/authHttp'

/**
 * 认证 API 实现类
 */
class AuthAPI implements IAuthAPI {
  /**
   * 用户登录
   */
  async login(credentials: LoginRequest): Promise<ResultDto<LoginResponse>> {
    return authPost<LoginResponse>('/api/v1/identity/login', credentials)
  }

  /**
   * 用户注册
   */
  async register(userData: RegisterRequest): Promise<ResultDto<RegisterResponse>> {
    // 构建后端期望的请求格式
    const registerData = {
      userAccount: userData.username || userData.email, // 使用用户名或邮箱作为账号
      email: userData.email,
      password: userData.password,
      captchaKey: userData.captchaKey,
      captchaCode: userData.captchaCode
    }
    
    return authPost<RegisterResponse>('/api/v1/identity/register', registerData)
  }

  /**
   * 获取验证码
   */
  async getCaptcha(): Promise<ResultDto<CaptchaResponse>> {
    return authGet<CaptchaResponse>('/api/v1/identity/users/captcha')
  }

  /**
   * 用户登出
   */
  async logout(): Promise<ResultDto<void>> {
    return authPost<void>('/api/v1/identity/logout')
  }

  /**
   * 获取用户资料
   */
  async getUserProfile(): Promise<ResultDto<UserProfile>> {
    return authGet<UserProfile>('/api/v1/identity/users/info')
  }

  /**
   * 更新用户资料
   */
  async updateUserProfile(profile: UpdateProfileRequest): Promise<ResultDto<UserProfile>> {
    return authPut<UserProfile>('/api/v1/identity/users/info', profile)
  }

  /**
   * 初始化 OAuth 登录
   */
  async initiateOAuth(provider: OAuthProvider): Promise<ResultDto<OAuthInitResponse>> {
    return authGet<OAuthInitResponse>(`/api/v1/identity/oauth/${provider}/initiate`)
  }

  /**
   * 处理 OAuth 回调
   */
  async handleOAuthCallback(params: OAuthCallbackParams): Promise<ResultDto<LoginResponse>> {
    return authPost<LoginResponse>(`/api/v1/identity/oauth/${params.provider}/callback`, {
      code: params.code,
      state: params.state
    })
  }

  /**
   * 刷新访问令牌
   */
  async refreshToken(): Promise<ResultDto<TokenPair>> {
    return authPost<TokenPair>('/api/v1/identity/refresh-token')
  }

  /**
   * 验证邮箱
   */
  async verifyEmail(token: string): Promise<ResultDto<void>> {
    return authPost<void>('/api/v1/identity/verify-email', { token })
  }

  /**
   * 发送密码重置邮件
   */
  async sendPasswordResetEmail(email: string): Promise<ResultDto<void>> {
    return authPost<void>('/api/v1/identity/forgot-password', { email })
  }

  /**
   * 重置密码
   */
  async resetPassword(token: string, newPassword: string): Promise<ResultDto<void>> {
    return authPost<void>('/api/v1/identity/reset-password', { token, newPassword })
  }

  /**
   * 修改密码
   */
  async changePassword(currentPassword: string, newPassword: string): Promise<ResultDto<void>> {
    return authPost<void>('/api/v1/identity/change-password', { currentPassword, newPassword })
  }

  /**
   * 获取 OAuth 提供商列表
   */
  async getOAuthProviders(): Promise<ResultDto<OAuthProvider[]>> {
    return authGet<OAuthProvider[]>('/api/v1/identity/oauth/providers')
  }

  /**
   * 绑定 OAuth 账户
   */
  async linkOAuthAccount(provider: OAuthProvider, code: string, state: string): Promise<ResultDto<void>> {
    return authPost<void>(`/api/v1/identity/oauth/${provider}/link`, { code, state })
  }

  /**
   * 解绑 OAuth 账户
   */
  async unlinkOAuthAccount(provider: OAuthProvider): Promise<ResultDto<void>> {
    return authPost<void>(`/api/v1/identity/oauth/${provider}/unlink`)
  }

  /**
   * 获取用户的 OAuth 绑定状态
   */
  async getOAuthBindings(): Promise<ResultDto<Record<OAuthProvider, boolean>>> {
    return authGet<Record<OAuthProvider, boolean>>('/api/v1/identity/oauth/bindings')
  }

  /**
   * 检查邮箱是否已注册
   */
  async checkEmailExists(email: string): Promise<ResultDto<boolean>> {
    return authGet<boolean>('/api/v1/identity/check-email', { email })
  }

  /**
   * 检查用户名是否可用
   */
  async checkUsernameAvailable(username: string): Promise<ResultDto<boolean>> {
    return authGet<boolean>('/api/v1/identity/check-username', { username })
  }

  /**
   * 获取当前用户的会话信息
   */
  async getSessionInfo(): Promise<ResultDto<{
    sessionId: string
    createdAt: string
    expiresAt: string
    ipAddress: string
    userAgent: string
  }>> {
    return authGet('/api/v1/identity/session')
  }

  /**
   * 撤销所有会话（强制登出所有设备）
   */
  async revokeAllSessions(): Promise<ResultDto<void>> {
    return authPost<void>('/api/v1/identity/revoke-sessions')
  }

  /**
   * 查询用户权限列表
   */
  async getUserPermissions(userId: string): Promise<ResultDto<UserPermissionsResponse>> {
    return authGet<UserPermissionsResponse>(`/api/v1/identity/permissions/${userId}`)
  }
}

// 导出单例实例
export const authAPI = new AuthAPI()

// 导出类以便测试
export { AuthAPI }

/**
 * 便捷方法 - 直接导出常用的 API 方法
 */
export const {
  login,
  register,
  logout,
  getCaptcha,
  getUserProfile,
  updateUserProfile,
  initiateOAuth,
  handleOAuthCallback,
  refreshToken,
  verifyEmail,
  sendPasswordResetEmail,
  resetPassword,
  changePassword,
  getOAuthProviders,
  linkOAuthAccount,
  unlinkOAuthAccount,
  getOAuthBindings,
  checkEmailExists,
  checkUsernameAvailable,
  getSessionInfo,
  revokeAllSessions,
  getUserPermissions
} = authAPI