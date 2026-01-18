/**
 * 认证相关的工具函数
 */

import { AuthErrorCode, ResultDto } from '@/types/auth'
import { ENV } from '@/config/env'
import { AUTH_ERROR_MESSAGES } from '@/config/auth.config'

/**
 * 解析认证错误，返回用户友好的错误消息
 */
export function parseAuthError(error: any): string {
  // 如果是 ResultDto 格式的错误
  if (error && typeof error === 'object' && 'status' in error && !error.status) {
    const resultError = error as ResultDto
    
    // 检查扩展信息中的错误代码
    if (resultError.extensions?.errorCode) {
      const errorCode = resultError.extensions.errorCode as AuthErrorCode
      return AUTH_ERROR_MESSAGES[errorCode] || resultError.message
    }
    
    return resultError.message || '操作失败'
  }
  
  // 如果是标准 Error 对象
  if (error instanceof Error) {
    return error.message
  }
  
  // 如果是字符串
  if (typeof error === 'string') {
    return error
  }
  
  // 如果是 HTTP 错误响应
  if (error?.response?.data) {
    const responseData = error.response.data
    
    if (responseData.message) {
      return responseData.message
    }
    
    if (responseData.error) {
      return responseData.error
    }
  }
  
  // 默认错误消息
  return '操作失败，请重试'
}

/**
 * 创建标准化的成功响应
 */
export function createSuccessResponse<T>(data: T, message = 'Success'): ResultDto<T> {
  return {
    status: true,
    data,
    message,
    statusCode: 200
  }
}

/**
 * 创建标准化的错误响应
 */
export function createErrorResponse(
  message: string,
  statusCode = 500,
  errorCode?: AuthErrorCode
): ResultDto {
  return {
    status: false,
    data: null,
    message,
    statusCode,
    extensions: errorCode ? { errorCode } : undefined
  }
}

/**
 * 检查响应是否成功
 */
export function isSuccessResponse(response: ResultDto): boolean {
  return response.status === true
}

/**
 * 从响应中提取数据，如果失败则抛出错误
 */
export function extractResponseData<T>(response: ResultDto<T>): T {
  if (isSuccessResponse(response)) {
    return response.data
  }
  
  throw new Error(response.message || '操作失败')
}

/**
 * 安全地执行异步操作，返回 ResultDto 格式
 */
export async function safeAsyncOperation<T>(
  operation: () => Promise<T>,
  errorMessage = '操作失败'
): Promise<ResultDto<T>> {
  try {
    const result = await operation()
    return createSuccessResponse(result)
  } catch (error) {
    const message = parseAuthError(error)
    return createErrorResponse(message || errorMessage)
  }
}

/**
 * 延迟执行函数
 */
export function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms))
}

/**
 * 重试机制
 */
export async function retryOperation<T>(
  operation: () => Promise<T>,
  maxRetries = 3,
  delayMs = 1000
): Promise<T> {
  let lastError: any
  
  for (let i = 0; i <= maxRetries; i++) {
    try {
      return await operation()
    } catch (error) {
      lastError = error
      
      if (i < maxRetries) {
        await delay(delayMs * Math.pow(2, i)) // 指数退避
      }
    }
  }
  
  throw lastError
}

/**
 * 防抖函数
 */
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout = null
  
  return (...args: Parameters<T>) => {
    if (timeout) {
      clearTimeout(timeout)
    }
    
    timeout = setTimeout(() => {
      func(...args)
    }, wait)
  }
}

/**
 * 节流函数
 */
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let inThrottle = false
  
  return (...args: Parameters<T>) => {
    if (!inThrottle) {
      func(...args)
      inThrottle = true
      setTimeout(() => {
        inThrottle = false
      }, wait)
    }
  }
}

/**
 * 生成随机字符串（用于 state 参数等）
 */
export function generateRandomString(length = 32): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
  let result = ''
  
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length))
  }
  
  return result
}

/**
 * 安全地解析 JSON
 */
export function safeJsonParse<T = any>(json: string, defaultValue: T): T {
  try {
    return JSON.parse(json)
  } catch {
    return defaultValue
  }
}

/**
 * 安全地序列化 JSON
 */
export function safeJsonStringify(obj: any, defaultValue = '{}'): string {
  try {
    return JSON.stringify(obj)
  } catch {
    return defaultValue
  }
}

/**
 * 检查是否在浏览器环境
 */
export function isBrowser(): boolean {
  return typeof window !== 'undefined'
}

/**
 * 安全地访问 localStorage
 */
export const safeLocalStorage = {
  getItem(key: string): string | null {
    if (!isBrowser()) return null
    
    try {
      return localStorage.getItem(key)
    } catch {
      return null
    }
  },
  
  setItem(key: string, value: string): boolean {
    if (!isBrowser()) return false
    
    try {
      localStorage.setItem(key, value)
      return true
    } catch {
      return false
    }
  },
  
  removeItem(key: string): boolean {
    if (!isBrowser()) return false
    
    try {
      localStorage.removeItem(key)
      return true
    } catch {
      return false
    }
  }
}

/**
 * 格式化用户显示名称
 */
export function formatUserDisplayName(user: {
  username?: string
  firstName?: string
  lastName?: string
  email?: string
}): string {
  if (user.firstName && user.lastName) {
    return `${user.firstName} ${user.lastName}`
  }
  
  if (user.username) {
    return user.username
  }
  
  if (user.email) {
    return user.email.split('@')[0]
  }
  
  return '用户'
}

/**
 * 获取用户头像 URL（如果没有则返回默认头像）
 */
export function getDefaultAvatarUrl(user: {
	email?: string
	username?: string
}): string {
	if (user.email) {
		const hash = btoa(user.email.toLowerCase().trim())
		return `https://www.gravatar.com/avatar/${hash}?d=identicon&s=200`
	}
	
	if (user.username) {
		return `https://ui-avatars.com/api/?name=${encodeURIComponent(user.username)}&background=random`
	}
	
	return 'https://ui-avatars.com/api/?name=User&background=random'
}

export function getUserAvatarUrl(user: {
	avatar?: string
	email?: string
	username?: string
}): string {
	if (user.avatar) {
		const value = user.avatar.trim()
		if (/^https?:\/\//i.test(value) || value.startsWith('//')) {
			return value
		}
		return `${ENV.FILES_API}/content?relativePath=${encodeURIComponent(value)}`
	}
	
	return getDefaultAvatarUrl(user)
}
