/**
 * 网关 HTTP 客户端
 * 所有服务域共用同一实例，并统一处理 Bearer Token、401 刷新和 ResultDto。
 */

import axios, {AxiosInstance, AxiosRequestConfig, AxiosResponse} from 'axios'
import {AuthErrorCode} from '@/types/auth'
import type {ApiErrorDomain, ResultDto} from '@/types/api'
import {tokenManager} from './tokenManager'
import {ENV} from '@/config/env'
import {API_PREFIXES, API_ROUTES} from '@/config/apiRoutes'

// 在开发环境中导入调试工具
if (import.meta.env.DEV && typeof window !== 'undefined') {
    import('./authDebug')
}

// 认证 HTTP 客户端配置
const API_CLIENT_CONFIG = {
    baseURL: ENV.API,
    timeout: 15000,
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
}

type JsonObject = Record<string, any>
type ApiRequestConfig = AxiosRequestConfig & {
    _errorDomain?: ApiErrorDomain
    _retry?: boolean
}

const PUBLIC_IDENTITY_PATHS = new Set([
    API_ROUTES.identity.login,
    API_ROUTES.identity.register,
    API_ROUTES.identity.captcha,
    API_ROUTES.identity.refreshToken
])
const PUBLIC_OAUTH_PREFIX = `${API_PREFIXES.identity}/oauth/`

function getRequestPath(url?: string): string {
    if (!url) return ''

    try {
        const path = new URL(url, 'http://gateway.local').pathname
        return path.length > 1 ? path.replace(/\/+$/, '') : path
    } catch {
        return url.split(/[?#]/, 1)[0].replace(/\/+$/, '')
    }
}

export function isPublicIdentityEndpoint(url?: string): boolean {
    const path = getRequestPath(url)
    return PUBLIC_IDENTITY_PATHS.has(path) || path.startsWith(PUBLIC_OAUTH_PREFIX)
}

function normalizeJsonKeys(value: unknown): unknown {
    if (Array.isArray(value)) {
        return value.map(normalizeJsonKeys)
    }

    if (!value || typeof value !== 'object' || (typeof Blob !== 'undefined' && value instanceof Blob)) {
        return value
    }

    return Object.fromEntries(
        Object.entries(value as JsonObject).map(([key, entry]) => [
            key.length > 0 ? key.charAt(0).toLowerCase() + key.slice(1) : key,
            normalizeJsonKeys(entry)
        ])
    )
}

function normalizeTokenData<T>(data: unknown): T {
    return normalizeJsonKeys(data) as T
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
 * 创建统一网关 axios 实例
 */
export const apiHttpClient: AxiosInstance = axios.create(API_CLIENT_CONFIG)

/**
 * 请求拦截器 - 自动注入 Token
 */
apiHttpClient.interceptors.request.use(
    async (config) => {
        // 公开认证请求不携带已有访问令牌。
        if (isPublicIdentityEndpoint(config.url)) {
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
apiHttpClient.interceptors.response.use(
    (response: AxiosResponse) => {
        // 成功响应直接返回
        return response
    },
    async (error) => {
        const originalRequest = error.config as ApiRequestConfig | undefined

        // 受保护请求在强制刷新令牌后最多重试一次。
        if (
            error.response?.status === 401 &&
            originalRequest &&
            !originalRequest._retry &&
            !isPublicIdentityEndpoint(originalRequest.url)
        ) {
            originalRequest._retry = true

            try {
                // 尝试刷新 Token
                const refreshed = await tokenManager.refreshTokenIfNeeded(true)

                if (refreshed) {
                    // 重新发送原始请求
                    const newToken = tokenManager.getAccessToken()
                    if (newToken) {
                        originalRequest.headers.Authorization = `Bearer ${newToken}`
                        return apiHttpClient(originalRequest)
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
        return Promise.reject(handleHttpError(error, originalRequest?._errorDomain))
    }
)

/**
 * 处理 HTTP 错误，转换为标准格式
 */
function handleHttpError(error: any, domain: ApiErrorDomain = 'identity'): ResultDto {
    if (!error.response) {
        // Network error
        return domain === 'service'
            ? createServiceError(0, {message: 'Network request failed'})
            : createAuthError(AuthErrorCode.NETWORK_ERROR, '网络连接失败，请检查网络设置')
    }

    const {status, data} = error.response

    // Normalize the backend response casing at the HTTP boundary
    const normalized = normalizeResultDto(data)
    if (normalized && !normalized.status) {
        return normalized
    }

    if (domain === 'service') {
        return createServiceError(status, data)
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
 * Create a normalized business-service error response
 */
function createServiceError(status: number, data: any): ResultDto {
    const message = data?.message ?? data?.Message ?? data?.error ?? data?.Error ?? 'Request failed'

    return {
        status: false,
        data: null,
        message,
        statusCode: status,
        extensions: {
            errorCode: `HTTP_${status}`,
            details: data
        }
    }
}

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
async function requestWithDomain<T = any>(
    config: ApiRequestConfig,
    domain: ApiErrorDomain
): Promise<ResultDto<T>> {
    try {
        const requestConfig: ApiRequestConfig = {
            ...config,
            _errorDomain: domain
        }
        const response = await apiHttpClient(requestConfig)

        // Normalize the backend response casing at the HTTP boundary
        const normalized = normalizeResultDto<T>(response.data)
        if (normalized) {
            return normalized
        }

        // Wrap a successful non-envelope response in ResultDto.
        return {
            status: true,
            data: response.data,
            message: 'Success',
            statusCode: response.status
        }
    } catch (error: any) {
        // Return an already normalized ResultDto without wrapping it again.
        if (error && typeof error === 'object' && 'status' in error) {
            return error as ResultDto<T>
        }

        // Convert unexpected errors to the selected domain error contract.
        return handleHttpError(error, domain)
    }
}

export function identityRequest<T = any>(config: AxiosRequestConfig): Promise<ResultDto<T>> {
    return requestWithDomain<T>(config, 'identity')
}

/**
 * Access a protected business service through the shared gateway client.
 * Use gateway paths such as `/vocabulary/v1/words`.
 */
export function serviceRequest<T = any>(config: AxiosRequestConfig): Promise<ResultDto<T>> {
    return requestWithDomain<T>(config, 'service')
}

export function identityGet<T = any>(url: string, params?: any): Promise<ResultDto<T>> {
    return identityRequest<T>({
        method: 'GET',
        url,
        params
    })
}

/**
 * POST 请求
 */
export function identityPost<T = any>(url: string, data?: any): Promise<ResultDto<T>> {
    return identityRequest<T>({
        method: 'POST',
        url,
        data
    })
}

/**
 * PUT 请求
 */
export function identityPut<T = any>(url: string, data?: any): Promise<ResultDto<T>> {
    return identityRequest<T>({
        method: 'PUT',
        url,
        data
    })
}

/**
 * DELETE 请求
 */
export function identityDelete<T = any>(url: string): Promise<ResultDto<T>> {
    return identityRequest<T>({
        method: 'DELETE',
        url
    })
}

export function serviceGet<T = any>(url: string, params?: any): Promise<ResultDto<T>> {
    return serviceRequest<T>({method: 'GET', url, params})
}

export function servicePost<T = any>(url: string, data?: any): Promise<ResultDto<T>> {
    return serviceRequest<T>({method: 'POST', url, data})
}

export function servicePut<T = any>(url: string, data?: any): Promise<ResultDto<T>> {
    return serviceRequest<T>({method: 'PUT', url, data})
}

export function serviceFileGet(url: string, params?: any): Promise<AxiosResponse<Blob>> {
    return apiHttpClient<Blob>({
        method: 'GET',
        url,
        params,
        responseType: 'blob',
        _errorDomain: 'service'
    } as ApiRequestConfig)
}

// 导出默认实例
export default apiHttpClient
