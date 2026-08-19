/**
 * 统一网关 HTTP 客户端测试
 */

import {beforeEach, describe, expect, it, vi} from 'vitest'
import {API_ROUTES} from '@/config/apiRoutes'
import {ENV} from '@/config/env'
import {apiHttpClient, isPublicIdentityEndpoint} from '../apiClient'

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

describe('apiClient', () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it('uses the single gateway base URL', () => {
        expect(apiHttpClient.defaults.baseURL).toBe(ENV.API)
    })

    it('recognizes only verified anonymous Identity routes', () => {
        expect(isPublicIdentityEndpoint(API_ROUTES.identity.login)).toBe(true)
        expect(isPublicIdentityEndpoint(API_ROUTES.identity.captcha)).toBe(true)
        expect(isPublicIdentityEndpoint(API_ROUTES.identity.oauthInitiate('github'))).toBe(true)
        expect(isPublicIdentityEndpoint('/vocabulary/v1/login-history')).toBe(false)
    })

    it('does not add Authorization for login', async () => {
        const {tokenManager} = await import('../tokenManager')
        tokenManager.getAccessToken = vi.fn().mockReturnValue('mock-token')
        const adapter = vi.fn(async config => successResponse(config))
        apiHttpClient.defaults.adapter = adapter

        await apiHttpClient.post(API_ROUTES.identity.login, {userAccount: 'test', password: 'test'})

        expect(adapter.mock.calls[0][0].headers.get('Authorization')).toBeUndefined()
        expect(tokenManager.getAccessToken).not.toHaveBeenCalled()
    })

    it('adds Authorization for protected service routes even when the path contains login', async () => {
        const {tokenManager} = await import('../tokenManager')
        tokenManager.getAccessToken = vi.fn().mockReturnValue('mock-token')
        tokenManager.isTokenExpired = vi.fn().mockReturnValue(false)
        const adapter = vi.fn(async config => successResponse(config))
        apiHttpClient.defaults.adapter = adapter

        await apiHttpClient.get('/vocabulary/v1/login-history')

        expect(adapter.mock.calls[0][0].headers.get('Authorization')).toBe('Bearer mock-token')
    })

    it('does not retry an anonymous Identity request on 401', async () => {
        const {tokenManager} = await import('../tokenManager')
        tokenManager.refreshTokenIfNeeded = vi.fn().mockResolvedValue(true)
        apiHttpClient.defaults.adapter = vi.fn(async config => {
            throw {
                response: {status: 401, data: {message: 'Invalid credentials'}},
                config
            }
        })

        await expect(apiHttpClient.post(API_ROUTES.identity.login, {})).rejects.toBeTruthy()
        expect(tokenManager.refreshTokenIfNeeded).not.toHaveBeenCalled()
    })

    it('retries a protected service request once after forced refresh', async () => {
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

            return successResponse(config)
        })
        apiHttpClient.defaults.adapter = adapter

        await apiHttpClient.get(API_ROUTES.vocabulary.words)

        expect(tokenManager.refreshTokenIfNeeded).toHaveBeenCalledWith(true)
        expect(callCount).toBe(2)
        expect(adapter.mock.calls[1][0].headers.get('Authorization')).toBe('Bearer new-token')
    })
})
