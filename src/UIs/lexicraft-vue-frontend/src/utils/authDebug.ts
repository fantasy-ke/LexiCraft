/**
 * 认证调试工具
 */

import {tokenManager} from './tokenManager'

/**
 * 认证调试信息
 */
export interface AuthDebugInfo {
    hasAccessToken: boolean
    hasRefreshToken: boolean
    accessToken?: string
    isTokenExpired: boolean
    remainingTime: number
    tokenPayload?: any
}

/**
 * 获取认证调试信息
 */
export function getAuthDebugInfo(): AuthDebugInfo {
    const accessToken = tokenManager.getAccessToken()
    const refreshToken = tokenManager.getRefreshToken()

    const info: AuthDebugInfo = {
        hasAccessToken: !!accessToken,
        hasRefreshToken: !!refreshToken,
        isTokenExpired: accessToken ? tokenManager.isTokenExpired(accessToken) : true,
        remainingTime: tokenManager.getTokenRemainingTime()
    }

    if (accessToken) {
        info.accessToken = accessToken.substring(0, 20) + '...' // 只显示前20个字符
        info.tokenPayload = tokenManager.parseTokenPayload(accessToken)
    }

    return info
}

/**
 * 打印认证调试信息到控制台
 */
export function logAuthDebugInfo(): void {
    const info = getAuthDebugInfo()

    console.group('🔐 认证调试信息')
    console.log('访问令牌:', info.hasAccessToken ? '✅ 存在' : '❌ 不存在')
    console.log('刷新令牌:', info.hasRefreshToken ? '✅ 存在' : '❌ 不存在')

    if (info.hasAccessToken) {
        console.log('令牌预览:', info.accessToken)
        console.log('是否过期:', info.isTokenExpired ? '❌ 已过期' : '✅ 有效')
        console.log('剩余时间:', `${info.remainingTime} 秒`)

        if (info.tokenPayload) {
            console.log('令牌载荷:', info.tokenPayload)
        }
    }

    console.groupEnd()
}

/**
 * 清除所有认证数据（用于调试）
 */
export function clearAllAuthData(): void {
    tokenManager.clearTokens()
    localStorage.removeItem('token') // 清除旧的 token
    console.log('🧹 已清除所有认证数据')
}

/**
 * 模拟登录状态（用于调试）
 */
export function mockAuthState(mockToken = 'mock-debug-token'): void {
    const mockTokenPair = {
        accessToken: mockToken,
        refreshToken: 'mock-refresh-token',
        expiresIn: 3600
    }

    tokenManager.setTokens(mockTokenPair)
    console.log('🎭 已设置模拟认证状态')
}

/**
 * 检查认证状态是否一致
 */
export function checkAuthConsistency(): {
    consistent: boolean
    issues: string[]
} {
    const issues: string[] = []

    const accessToken = tokenManager.getAccessToken()
    const refreshToken = tokenManager.getRefreshToken()
    const oldToken = localStorage.getItem('token')

    // 检查是否存在旧的 token 格式
    if (oldToken && oldToken !== accessToken) {
        issues.push('存在旧格式的 token，可能导致状态不一致')
    }

    // 检查 token 对的完整性
    if (accessToken && !refreshToken) {
        issues.push('存在访问令牌但缺少刷新令牌')
    }

    if (!accessToken && refreshToken) {
        issues.push('存在刷新令牌但缺少访问令牌')
    }

    // 检查 token 格式
    if (accessToken && !tokenManager.isTokenValid(accessToken)) {
        issues.push('访问令牌格式无效')
    }

    return {
        consistent: issues.length === 0,
        issues
    }
}

/**
 * 启用认证调试模式
 */
export function enableAuthDebug(): void {
    localStorage.setItem('auth:debug', 'true')

    // 监听认证相关的事件
    window.addEventListener('auth:login', () => {
        console.log('🔐 认证事件: 登录')
        logAuthDebugInfo()
    })

    window.addEventListener('auth:logout', () => {
        console.log('🔐 认证事件: 登出')
        logAuthDebugInfo()
    })

    console.log('🐛 认证调试模式已启用')
}

/**
 * 禁用认证调试模式
 */
export function disableAuthDebug(): void {
    localStorage.removeItem('auth:debug')
    console.log('🐛 认证调试模式已禁用')
}

/**
 * 检查是否启用了调试模式
 */
export function isAuthDebugEnabled(): boolean {
    return localStorage.getItem('auth:debug') === 'true'
}

// 在开发环境中自动启用调试模式
if (import.meta.env.DEV) {
    // 将调试函数暴露到全局，方便在控制台中使用
    ;(window as any).authDebug = {
        info: getAuthDebugInfo,
        log: logAuthDebugInfo,
        clear: clearAllAuthData,
        mock: mockAuthState,
        check: checkAuthConsistency,
        enable: enableAuthDebug,
        disable: disableAuthDebug
    }

    console.log('🐛 认证调试工具已加载，使用 window.authDebug 访问')
}