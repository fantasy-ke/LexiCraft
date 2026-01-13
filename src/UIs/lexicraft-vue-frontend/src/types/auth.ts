/**
 * 认证相关类型定义
 * 与后端 ResultDto 格式保持一致
 */

// 标准化响应格式 - 与后端 ResultDto 一致
export interface ResultDto<T = any> {
  status: boolean
  data: T
  message: string
  statusCode: number
  extensions?: Record<string, any>
}

// 登录请求
export interface LoginRequest {
  userAccount: string  // 邮箱或用户名
  password: string
}

// 注册请求
export interface RegisterRequest {
  email: string
  password: string
  confirmPassword: string
  username?: string
  firstName?: string  // 保持可选，但前端不使用
  lastName?: string   // 保持可选，但前端不使用
  captchaKey: string  // 验证码Key
  captchaCode: string // 验证码
}

// 登录响应
export interface LoginResponse {
  token: string
  refreshToken: string
}

// 注册响应
export interface RegisterResponse {
  user: UserProfile
  message: string
}

// 用户资料
export interface UserProfile {
  userId: string    // UUID
  userName: string
  email: string
  phone?: string | null
  avatar: string
  // 兼容旧命名的别名（如果需要后期在 store 中映射）
  id?: string
  username?: string
}

// OAuth 提供商类型
export type OAuthProvider = 'github' | 'gitee'

// OAuth 初始化响应
export interface OAuthInitResponse {
  authUrl: string
  state: string
}

// OAuth 回调参数
export interface OAuthCallbackParams {
  code: string
  state: string
  provider: OAuthProvider
}

// Token 对
export interface TokenPair {
  accessToken: string
  refreshToken: string
  expiresIn: number
}

// 认证状态
export interface AuthState {
  user: UserProfile | null
  isAuthenticated: boolean
  isLoading: boolean
  tokens: TokenPair | null
}

// 认证配置
export interface AuthConfig {
  identityApiUrl: string
  tokenStorageKey: string
  refreshTokenStorageKey: string
  autoRefreshThreshold: number // 提前多少秒刷新 token
}

// 认证错误
export interface AuthError {
  code: string
  message: string
  details?: Record<string, any>
}

// 认证错误代码枚举
export enum AuthErrorCode {
  INVALID_CREDENTIALS = 'INVALID_CREDENTIALS',
  USER_NOT_FOUND = 'USER_NOT_FOUND',
  EMAIL_ALREADY_EXISTS = 'EMAIL_ALREADY_EXISTS',
  TOKEN_EXPIRED = 'TOKEN_EXPIRED',
  OAUTH_ERROR = 'OAUTH_ERROR',
  NETWORK_ERROR = 'NETWORK_ERROR',
  VALIDATION_ERROR = 'VALIDATION_ERROR',
  UNAUTHORIZED = 'UNAUTHORIZED',
  FORBIDDEN = 'FORBIDDEN',
  SERVER_ERROR = 'SERVER_ERROR'
}

// 认证 API 接口
export interface IAuthAPI {
  // 本地认证
  login(credentials: LoginRequest): Promise<ResultDto<LoginResponse>>
  register(userData: RegisterRequest): Promise<ResultDto<RegisterResponse>>
  logout(): Promise<ResultDto<void>>
  
  // 验证码
  getCaptcha(): Promise<ResultDto<CaptchaResponse>>
  
  // 用户资料
  getUserProfile(): Promise<ResultDto<UserProfile>>
  updateUserProfile(profile: Partial<UserProfile>): Promise<ResultDto<UserProfile>>
  
  // OAuth 流程
  initiateOAuth(provider: OAuthProvider): Promise<ResultDto<OAuthInitResponse>>
  handleOAuthCallback(params: OAuthCallbackParams): Promise<ResultDto<LoginResponse>>
  
  // Token 管理
  refreshToken(): Promise<ResultDto<TokenPair>>

  // 权限管理
  getUserPermissions(userId: string): Promise<ResultDto<UserPermissionsResponse>>
}

// 认证 Store Actions 接口
export interface IAuthActions {
  // 认证操作
  login(credentials: LoginRequest): Promise<void>
  register(userData: RegisterRequest): Promise<void>
  logout(): Promise<void>
  
  // OAuth 操作
  loginWithOAuth(provider: OAuthProvider): Promise<void>
  handleOAuthCallback(params: OAuthCallbackParams): Promise<void>
  
  // 用户资料操作
  fetchUserProfile(): Promise<void>
  updateProfile(profile: Partial<UserProfile>): Promise<void>
  
  // 状态管理
  initializeAuth(): Promise<void>
  checkAuthStatus(): boolean
}

// Token 管理器接口
export interface ITokenManager {
  // Token 存储
  setTokens(tokens: TokenPair): void
  getAccessToken(): string | null
  getRefreshToken(): string | null
  clearTokens(): void
  
  // Token 验证
  isTokenValid(token: string): boolean
  isTokenExpired(token: string): boolean
  
  // 自动刷新
  refreshTokenIfNeeded(): Promise<boolean>
}

// 更新用户资料请求
export interface UpdateProfileRequest {
  username?: string
  firstName?: string
  lastName?: string
  avatar?: string
}

// Token 响应
export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
}

// 验证码响应
export interface CaptchaResponse {
  captchaKey: string
  captchaData: string // Base64 图片数据
}

/**
 * 用户权限响应
 */
export interface UserPermissionsResponse {
  userId: string
  permissions: string[]
}
