/**
 * 认证 HTTP 客户端测试
 */

import {beforeEach, describe, expect, it, vi} from 'vitest'
import {authHttpClient} from '../authHttp'

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

const successResponse = config => ({
    data: {status: true},
    status: 200,
    statusText: 'OK',
    headers: {},
    config
})

describe('authHttp', () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it('should not add Authorization header for login endpoint', async () => {
        const {tokenManager} = await import('../tokenManager')
        tokenManager.getAccessToken = vi.fn().mockReturnValue('mock-token')
        const adapter = vi.fn(async config => successResponse(config))
        authHttpClient.defaults.adapter = adapter

        await authHttpClient.post('/v1/login', {userAccount: 'test', password: 'test'})

        expect(adapter).toHaveBeenCalledOnce()
        expect(adapter.mock.calls[0][0].headers.get('Authorization')).toBeUndefined()
        expect(tokenManager.getAccessToken).not.toHaveBeenCalled()
    })

    it('should add Authorization header for protected endpoint', async () => {
        const {tokenManager} = await import('../tokenManager')
        tokenManager.getAccessToken = vi.fn().mockReturnValue('mock-token')
        tokenManager.isTokenExpired = vi.fn().mockReturnValue(false)
        const adapter = vi.fn(async config => successResponse(config))
        authHttpClient.defaults.adapter = adapter

        await authHttpClient.get('/v1/users/info')

        expect(adapter.mock.calls[0][0].headers.get('Authorization')).toBe('Bearer mock-token')
        expect(tokenManager.getAccessToken).toHaveBeenCalled()
    })

    it('should not retry login request on 401 error', async () => {
        const {tokenManager} = await import('../tokenManager')
        tokenManager.refreshTokenIfNeeded = vi.fn().mockResolvedValue(true)
        authHttpClient.defaults.adapter = vi.fn(async config => {
            throw {
                response: {status: 401, data: {message: 'Invalid credentials'}},
                config
            }
        })

        await expect(authHttpClient.post('/v1/login', {userAccount: 'test', password: 'wrong'})).rejects.toBeTruthy()
        expect(tokenManager.refreshTokenIfNeeded).not.toHaveBeenCalled()
    })

    it('should retry protected request on 401 error', async () => {
        const {tokenManager} = await import('../tokenManager')
        tokenManager.refreshTokenIfNeeded = vi.fn().mockResolvedValue(true)
        tokenManager.getAccessToken = vi.fn().mockReturnValue('new-token')
        tokenManager.isTokenExpired = vi.fn().mockReturnValue(false)
        let callCount = 0
        const adapter = vi.fn(async config => {
            callCount++
            if (callCount === 1) {
                throw {
                    response: {status: 401, data: {message: 'Token expired'}},
                    config
                }
            }

            return {...successResponse(config), data: {status: true, data: {id: '1', username: 'test'}}}
        })
        authHttpClient.defaults.adapter = adapter

        const result = await authHttpClient.get('/v1/users/info')

        expect(tokenManager.refreshTokenIfNeeded).toHaveBeenCalledWith(true)
        expect(callCount).toBe(2)
        expect(adapter.mock.calls[1][0].headers.get('Authorization')).toBe('Bearer new-token')
        expect(result.data.status).toBe(true)
    })
})
