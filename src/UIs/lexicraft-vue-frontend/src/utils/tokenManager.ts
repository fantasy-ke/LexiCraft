/**
 * Token 管理器
 * 负责 JWT Token 的存储、验证和自动刷新
 */

import { TokenPair, ITokenManager, AuthConfig } from '@/types/auth'
import { ENV } from '@/config/env'

class TokenManager implements ITokenManager {
  private config: AuthConfig
  private refreshPromise: Promise<boolean> | null = null

  constructor(config: AuthConfig) {
    this.config = config
  }

  /**
   * 存储 Token 对
   */
  setTokens(tokens: TokenPair): void {
    try {
      localStorage.setItem(this.config.tokenStorageKey, tokens.accessToken)
      localStorage.setItem(this.config.refreshTokenStorageKey, tokens.refreshToken)
      
      // 存储过期时间（当前时间 + expiresIn 秒）
      const expiresAt = Date.now() + (tokens.expiresIn * 1000)
      localStorage.setItem(`${this.config.tokenStorageKey}_expires_at`, expiresAt.toString())
    } catch (error) {
      console.error('Failed to store tokens:', error)
    }
  }

  /**
   * 获取访问令牌
   */
  getAccessToken(): string | null {
    try {
      return localStorage.getItem(this.config.tokenStorageKey)
    } catch (error) {
      console.error('Failed to get access token:', error)
      return null
    }
  }

  /**
   * 获取刷新令牌
   */
  getRefreshToken(): string | null {
    try {
      return localStorage.getItem(this.config.refreshTokenStorageKey)
    } catch (error) {
      console.error('Failed to get refresh token:', error)
      return null
    }
  }

  /**
   * 清除所有 Token
   */
  clearTokens(): void {
    try {
      localStorage.removeItem(this.config.tokenStorageKey)
      localStorage.removeItem(this.config.refreshTokenStorageKey)
      localStorage.removeItem(`${this.config.tokenStorageKey}_expires_at`)
    } catch (error) {
      console.error('Failed to clear tokens:', error)
    }
  }

  /**
   * 验证 Token 是否有效（格式检查）
   */
  isTokenValid(token: string): boolean {
    if (!token) return false
    
    try {
      // 简单的 JWT 格式检查
      const parts = token.split('.')
      if (parts.length !== 3) return false
      
      // 尝试解析 payload
      const payload = JSON.parse(atob(parts[1]))
      return payload && typeof payload === 'object'
    } catch (error) {
      return false
    }
  }

  /**
   * 检查 Token 是否已过期
   */
  isTokenExpired(token: string): boolean {
    if (!this.isTokenValid(token)) return true
    
    try {
      // 从存储中获取过期时间
      const expiresAtStr = localStorage.getItem(`${this.config.tokenStorageKey}_expires_at`)
      if (!expiresAtStr) return true
      
      const expiresAt = parseInt(expiresAtStr, 10)
      const now = Date.now()
      
      // 提前刷新阈值检查
      return (expiresAt - now) <= (this.config.autoRefreshThreshold * 1000)
    } catch (error) {
      console.error('Failed to check token expiration:', error)
      return true
    }
  }

  /**
   * 如果需要则刷新 Token
   */
  async refreshTokenIfNeeded(): Promise<boolean> {
    const accessToken = this.getAccessToken()
    const refreshToken = this.getRefreshToken()
    
    // 如果没有 refresh token，直接返回 false
    if (!refreshToken) {
      return false
    }
    
    // 如果 access token 仍然有效，返回 true
    if (accessToken && !this.isTokenExpired(accessToken)) {
      return true
    }

    // 防止并发刷新
    if (this.refreshPromise) {
      return this.refreshPromise
    }

    this.refreshPromise = this.performTokenRefresh()
    
    try {
      const result = await this.refreshPromise
      return result
    } finally {
      this.refreshPromise = null
    }
  }

  /**
   * 执行 Token 刷新
   */
  private async performTokenRefresh(): Promise<boolean> {
    const refreshToken = this.getRefreshToken()
    
    if (!refreshToken) {
      this.clearTokens()
      return false
    }

    try {
      // 动态导入避免循环依赖
      const { authAPI } = await import('@/apis/auth')
      const response = await authAPI.refreshToken()
      
      if (response.status && response.data) {
        this.setTokens(response.data)
        return true
      } else {
        this.clearTokens()
        return false
      }
    } catch (error) {
      console.error('Token refresh failed:', error)
      this.clearTokens()
      return false
    }
  }

  /**
   * 从 JWT Token 中解析用户信息
   */
  parseTokenPayload(token: string): any {
    if (!this.isTokenValid(token)) return null
    
    try {
      const parts = token.split('.')
      const payload = JSON.parse(atob(parts[1]))
      return payload
    } catch (error) {
      console.error('Failed to parse token payload:', error)
      return null
    }
  }

  /**
   * 获取 Token 剩余有效时间（秒）
   */
  getTokenRemainingTime(): number {
    try {
      const expiresAtStr = localStorage.getItem(`${this.config.tokenStorageKey}_expires_at`)
      if (!expiresAtStr) return 0
      
      const expiresAt = parseInt(expiresAtStr, 10)
      const now = Date.now()
      
      return Math.max(0, Math.floor((expiresAt - now) / 1000))
    } catch (error) {
      return 0
    }
  }
}

// 默认配置
const defaultAuthConfig: AuthConfig = {
  identityApiUrl: ENV.IDENTITY_API || 'http://localhost:5001', // Identity 服务地址
  tokenStorageKey: 'lexicraft_access_token',
  refreshTokenStorageKey: 'lexicraft_refresh_token',
  autoRefreshThreshold: 300 // 提前 5 分钟刷新
}

// 导出单例实例
export const tokenManager = new TokenManager(defaultAuthConfig)

// 导出类以便测试
export { TokenManager }
export type { AuthConfig }