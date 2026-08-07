/**
 * 认证专用 HTTP 客户端
 * 专门用于与 Identity 服务通信，包含自动 Token 注入和刷新
 */

import axios, {AxiosInstance, AxiosRequestConfig, AxiosResponse} from 'axios'
import {AuthErrorCode, ResultDto} from '@/types/auth'
import {tokenManager} from './tokenManager'
import {ENV} from '@/config/env'

// 在开发环境中导入调试工具
if (import.meta.env.DEV) {
    import('./authDebug')
}

// 认证 HTTP 客户端配置
const AUTH_API_CONFIG = {
    baseURL: ENV.IDENTITY_API,
    timeout: 15000,
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
}

type JsonObject = Record<string, any>

function normalizeTokenData<T>(data: unknown): T {
    if (!data || typeof data !== 'object' || Array.isArray(data)) {
        return data as T
    }

    const source = data as JsonObject
    const token = source.token ?? source.Token ?? source.accessToken ?? source.AccessToken
    const refreshToken = source.refreshToken ?? source.RefreshToken

    if (!token && !refreshToken) {
        return data as T
    }

    return {
        ...source,
        ...(token ? {token} : {}),
        ...(refreshToken ? {refreshToken} : {})
    } as T
}

export function normalizeResultDto<T>(data: unknown): ResultDto<T> | null {
    if (!data || typeof data !== 'object' || Array.isArray(data)) {
        return null
    }

    const source = data as JsonObject
    const status = source.status ?? source.Status
    if (typeof status !== 'boolean') {
        return null
    }

    return {
        status,
        data: normalizeTokenData<T>(source.data ?? source.Data),
        message: source.message ?? source.Message ?? '',
        statusCode: source.statusCode ?? source.StatusCode ?? 200,
        extensions: source.extensions ?? source.Extensions
    }
}

/**
 * 创建认证专用的 axios 实例
 */
export const authHttpClient: AxiosInstance = axios.create(AUTH_API_CONFIG)

/**
 * 请求拦截器 - 自动注入 Token
 */
authHttpClient.interceptors.request.use(
    async (config) => {
        // 检查是否是不需要 Token 的接口
        const isPublicEndpoint = config.url && (
            config.url.includes('/login') ||
            config.url.includes('/register') ||
            config.url.includes('/refresh-token') ||
            config.url.includes('/oauth') ||
            config.url.includes('/forgot-password') ||
            config.url.includes('/reset-password')
        )

        // 如果是公开接口，不添加 Token
        if (isPublicEndpoint) {
            return config
        }

        // 获取访问令牌
        const token = tokenManager.getAccessToken()

        if (token) {
            // 检查是否需要刷新 Token
            if (tokenManager.isTokenExpired(token)) {
                const refreshed = await tokenManager.refreshTokenIfNeeded()
                if (refreshed) {
                    // 使用新的 Token
                    const newToken = tokenManager.getAccessToken()
                    if (newToken) {
                        config.headers.Authorization = `Bearer ${newToken}`
                    }
                } else {
                    // 刷新失败，清除 Token
                    tokenManager.clearTokens()
                }
            } else {
                config.headers.Authorization = `Bearer ${token}`
            }
        }

        return config
    },
    (error) => {
        console.error('Request interceptor error:', error)
        return Promise.reject(error)
    }
)

/**
 * 响应拦截器 - 处理认证错误和统一响应格式
 */
authHttpClient.interceptors.response.use(
    (response: AxiosResponse) => {
        // 成功响应直接返回
        return response
    },
    async (error) => {
        const originalRequest = error.config

        // 检查是否是登录相关的接口，这些接口的 401 错误不应该触发 Token 刷新
        const isAuthEndpoint = originalRequest.url && (
            originalRequest.url.includes('/login') ||
            originalRequest.url.includes('/register') ||
            originalRequest.url.includes('/refresh-token') ||
            originalRequest.url.includes('/oauth') ||
            originalRequest.url.includes('/forgot-password') ||
            originalRequest.url.includes('/reset-password')
        )

        // 处理 401 未授权错误
        if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint) {
            originalRequest._retry = true

            try {
                // 尝试刷新 Token
                const refreshed = await tokenManager.refreshTokenIfNeeded(true)

                if (refreshed) {
                    // 重新发送原始请求
                    const newToken = tokenManager.getAccessToken()
                    if (newToken) {
                        originalRequest.headers.Authorization = `Bearer ${newToken}`
                        return authHttpClient(originalRequest)
                    }
                }
            } catch (refreshError) {
                console.error('Token refresh failed:', refreshError)
            }

            // 刷新失败，记录错误但不强制登出
            console.error('RefreshToken failed, but keeping session as per user request')

            return Promise.reject(createAuthError(AuthErrorCode.UNAUTHORIZED, '认证已过期，请重新登录'))
        }

        // 处理其他 HTTP 错误
        return Promise.reject(handleHttpError(error))
    }
)

/**
 * 处理 HTTP 错误，转换为标准格式
 */
function handleHttpError(error: any): ResultDto {
    if (!error.response) {
        // 网络错误
        return createAuthError(AuthErrorCode.NETWORK_ERROR, '网络连接失败，请检查网络设置')
    }

    const {status, data} = error.response

    // Normalize the backend response casing at the HTTP boundary
    const normalized = normalizeResultDto(data)
    if (normalized && !normalized.status) {
        return normalized
    }

    switch (status) {
        case 400:
            return createAuthError(
                AuthErrorCode.VALIDATION_ERROR,
                data?.message || data?.error || '请求参数错误'
            )
        case 401:
            return createAuthError(
                AuthErrorCode.INVALID_CREDENTIALS,
                data?.message || data?.error || '用户名或密码错误'
            )
        case 403:
            return createAuthError(AuthErrorCode.FORBIDDEN, '权限不足，无法访问')
        case 404:
            return createAuthError(AuthErrorCode.USER_NOT_FOUND, '用户不存在')
        case 429:
            return createAuthError(AuthErrorCode.RATE_LIMITED, '请求过于频繁，请稍后重试')
        case 409:
            return createAuthError(AuthErrorCode.EMAIL_ALREADY_EXISTS, '邮箱已被注册')
        case 500:
        default:
            return createAuthError(
                AuthErrorCode.SERVER_ERROR,
                data?.message || data?.error || '服务器内部错误，请稍后重试'
            )
    }
}

/**
 * 创建标准化的认证错误响应
 */
function createAuthError(code: AuthErrorCode, message: string, details?: any): ResultDto {
    return {
        status: false,
        data: null,
        message,
        statusCode: getStatusCodeFromErrorCode(code),
        extensions: {
            errorCode: code,
            details
        }
    }
}

/**
 * 根据错误代码获取对应的 HTTP 状态码
 */
function getStatusCodeFromErrorCode(code: AuthErrorCode): number {
    switch (code) {
        case AuthErrorCode.INVALID_CREDENTIALS:
        case AuthErrorCode.USER_NOT_FOUND:
            return 401
        case AuthErrorCode.EMAIL_ALREADY_EXISTS:
            return 409
        case AuthErrorCode.VALIDATION_ERROR:
            return 400
        case AuthErrorCode.UNAUTHORIZED:
        case AuthErrorCode.TOKEN_EXPIRED:
            return 401
        case AuthErrorCode.FORBIDDEN:
            return 403
        case AuthErrorCode.NETWORK_ERROR:
            return 0
        case AuthErrorCode.RATE_LIMITED:
            return 429
        case AuthErrorCode.SERVER_ERROR:
        default:
            return 500
    }
}

/**
 * 通用请求方法，返回标准化的 ResultDto 格式
 */
export async function authRequest<T = any>(
    config: AxiosRequestConfig
): Promise<ResultDto<T>> {
    try {
        const response = await authHttpClient(config)

        // Normalize the backend response casing at the HTTP boundary
        const normalized = normalizeResultDto<T>(response.data)
        if (normalized) {
            return normalized
        }

        // 否则包装成 ResultDto 格式
        return {
            status: true,
            data: response.data,
            message: 'Success',
            statusCode: response.status
        }
    } catch (error: any) {
        // 如果错误已经是 ResultDto 格式，直接返回
        if (error && typeof error === 'object' && 'status' in error) {
            return error as ResultDto<T>
        }

        // 否则处理为标准错误格式
        return handleHttpError(error)
    }
}

/**
 * GET 请求
 */
export function authGet<T = any>(url: string, params?: any): Promise<ResultDto<T>> {
    return authRequest<T>({
        method: 'GET',
        url,
        params
    })
}

/**
 * POST 请求
 */
export function authPost<T = any>(url: string, data?: any): Promise<ResultDto<T>> {
    return authRequest<T>({
        method: 'POST',
        url,
        data
    })
}

/**
 * PUT 请求
 */
export function authPut<T = any>(url: string, data?: any): Promise<ResultDto<T>> {
    return authRequest<T>({
        method: 'PUT',
        url,
        data
    })
}

/**
 * DELETE 请求
 */
export function authDelete<T = any>(url: string): Promise<ResultDto<T>> {
    return authRequest<T>({
        method: 'DELETE',
        url
    })
}

// 导出默认实例
export default authHttpClient