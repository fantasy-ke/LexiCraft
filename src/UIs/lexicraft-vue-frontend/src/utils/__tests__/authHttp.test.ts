/**
 * 认证 HTTP 客户端测试
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import axios from 'axios'
import { authHttpClient } from '../authHttp'

// Mock tokenManager
vi.mock('../tokenManager', () => ({
  tokenManager: {
    getAccessToken: vi.fn(),
    getRefreshToken: vi.fn(),
    setTokens: vi.fn(),
    clearTokens: vi.fn(),
    isTokenExpired: vi.fn(),
    refreshTokenIfNeeded: vi.fn()
  }
}))

describe('authHttp', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should not add Authorization header for login endpoint', async () => {
    const mockTokenManager = await import('../tokenManager')
    mockTokenManager.tokenManager.getAccessToken = vi.fn().mockReturnValue('mock-token')
    
    // Mock axios request
    const requestSpy = vi.spyOn(authHttpClient, 'request').mockResolvedValue({
      data: { status: true },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {}
    })

    await authHttpClient.post('/api/v1/identity/login', { userAccount: 'test', password: 'test' })

    // 验证请求被调用
    expect(requestSpy).toHaveBeenCalled()
    
    // 验证没有尝试获取 token（因为是登录接口）
    expect(mockTokenManager.tokenManager.getAccessToken).not.toHaveBeenCalled()
  })

  it('should add Authorization header for protected endpoint', async () => {
    const mockTokenManager = await import('../tokenManager')
    mockTokenManager.tokenManager.getAccessToken = vi.fn().mockReturnValue('mock-token')
    mockTokenManager.tokenManager.isTokenExpired = vi.fn().mockReturnValue(false)
    
    // Mock axios request
    const requestSpy = vi.spyOn(authHttpClient, 'request').mockResolvedValue({
      data: { status: true },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {}
    })

    await authHttpClient.get('/api/v1/identity/users/info')

    // 验证请求被调用
    expect(requestSpy).toHaveBeenCalled()
    
    // 验证尝试获取 token
    expect(mockTokenManager.tokenManager.getAccessToken).toHaveBeenCalled()
  })

  it('should not retry login request on 401 error', async () => {
    const mockTokenManager = await import('../tokenManager')
    mockTokenManager.tokenManager.refreshTokenIfNeeded = vi.fn().mockResolvedValue(true)
    
    // Mock 401 error for login endpoint
    const error = {
      response: {
        status: 401,
        data: { message: 'Invalid credentials' }
      },
      config: {
        url: '/api/v1/identity/login'
      }
    }

    // Mock axios to throw error
    vi.spyOn(authHttpClient, 'request').mockRejectedValue(error)

    try {
      await authHttpClient.post('/api/v1/identity/login', { userAccount: 'test', password: 'wrong' })
    } catch (e) {
      // 验证没有尝试刷新 token
      expect(mockTokenManager.tokenManager.refreshTokenIfNeeded).not.toHaveBeenCalled()
    }
  })

  it('should retry protected request on 401 error', async () => {
    const mockTokenManager = await import('../tokenManager')
    mockTokenManager.tokenManager.refreshTokenIfNeeded = vi.fn().mockResolvedValue(true)
    mockTokenManager.tokenManager.getAccessToken = vi.fn().mockReturnValue('new-token')
    
    // Mock 401 error for protected endpoint
    const error = {
      response: {
        status: 401,
        data: { message: 'Token expired' }
      },
      config: {
        url: '/api/v1/identity/users/info',
        headers: {}
      }
    }

    let callCount = 0
    vi.spyOn(authHttpClient, 'request').mockImplementation(() => {
      callCount++
      if (callCount === 1) {
        return Promise.reject(error)
      }
      return Promise.resolve({
        data: { status: true, data: { id: '1', username: 'test' } },
        status: 200,
        statusText: 'OK',
        headers: {},
        config: {}
      })
    })

    const result = await authHttpClient.get('/api/v1/identity/users/info')

    // 验证尝试刷新 token
    expect(mockTokenManager.tokenManager.refreshTokenIfNeeded).toHaveBeenCalled()
    
    // 验证请求被重试
    expect(callCount).toBe(2)
    
    // 验证最终成功
    expect(result.data.status).toBe(true)
  })
})