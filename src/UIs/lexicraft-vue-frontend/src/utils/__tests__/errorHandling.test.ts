/**
 * 错误处理测试 - 确保错误提示只出现一次
 */

import {beforeEach, describe, expect, it, vi} from 'vitest'
import {parseAuthError} from '../authHelpers'
import {AuthErrorCode} from '@/types/auth'

// Mock Toast
const mockToast = {
    error: vi.fn(),
    success: vi.fn(),
    warning: vi.fn(),
    info: vi.fn()
}

vi.mock('@/components/base/toast/Toast', () => ({
    default: mockToast
}))

describe('错误处理', () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it('应该正确解析网络错误', () => {
        const networkError = {
            message: 'Network Error',
            code: 'NETWORK_ERROR'
        }

        const message = parseAuthError(networkError)
        expect(message).toBe('Network Error')
    })

    it('应该正确解析 ResultDto 格式的错误', () => {
        const resultError = {
            status: false,
            message: '网络连接失败，请检查网络设置',
            statusCode: 0,
            extensions: {
                errorCode: AuthErrorCode.NETWORK_ERROR
            }
        }

        const message = parseAuthError(resultError)
        expect(message).toBe('网络连接失败，请检查网络设置')
    })

    it('应该正确解析 HTTP 响应错误', () => {
        const httpError = {
            response: {
                status: 401,
                data: {
                    message: '用户名或密码错误'
                }
            }
        }

        const message = parseAuthError(httpError)
        expect(message).toBe('用户名或密码错误')
    })

    it('应该为未知错误提供默认消息', () => {
        const unknownError = {}

        const message = parseAuthError(unknownError)
        expect(message).toBe('操作失败，请重试')
    })

    it('应该正确处理字符串错误', () => {
        const stringError = '自定义错误消息'

        const message = parseAuthError(stringError)
        expect(message).toBe('自定义错误消息')
    })

    it('应该正确处理 Error 对象', () => {
        const error = new Error('标准错误对象')

        const message = parseAuthError(error)
        expect(message).toBe('标准错误对象')
    })
})