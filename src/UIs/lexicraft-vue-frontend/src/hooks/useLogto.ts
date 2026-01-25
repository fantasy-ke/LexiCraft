import {computed, ref} from 'vue'
import {useRouter} from 'vue-router'
import {LOGIN_CONFIG, logtoConfig} from '@/config/logto.config'

// Logto 客户端实例
let logtoClient: any = null

/**
 * Logto 认证组合式函数
 */
export function useLogto() {
    const router = useRouter()
    const isAuthenticated = ref(false)
    const isLoading = ref(false)
    const user = ref<any>(null)

    // 初始化 Logto 客户端
    const initLogto = async () => {
        if (logtoClient) return logtoClient

        try {
            const {default: LogtoClient} = await import('@logto/browser')
            logtoClient = new LogtoClient(logtoConfig)

            // 检查认证状态
            isAuthenticated.value = await logtoClient.isAuthenticated()

            if (isAuthenticated.value) {
                user.value = await logtoClient.getIdTokenClaims()
            }

            return logtoClient
        } catch (error) {
            console.error('Failed to initialize Logto:', error)
            throw error
        }
    }

    /**
     * 使用 OAuth 提供商登录
     */
    const signInWithOAuth = async (connectorId: string) => {
        try {
            isLoading.value = true
            const client = await initLogto()

            await client.signIn({
                redirectUri: `${window.location.origin}${LOGIN_CONFIG.callbackPath}`,
                prompt: 'consent',
                // 使用社交连接器
                firstScreen: 'signIn',
                identifierType: 'social',
                socialConnectorId: connectorId,
            })
        } catch (error) {
            console.error('OAuth sign in error:', error)
            throw error
        } finally {
            isLoading.value = false
        }
    }

    /**
     * 使用邮箱密码登录
     */
    const signInWithEmail = async (email: string, password: string) => {
        try {
            isLoading.value = true
            const client = await initLogto()

            await client.signIn({
                redirectUri: `${window.location.origin}${LOGIN_CONFIG.callbackPath}`,
                loginHint: email,
            })

            // 注意: Logto 的邮箱密码登录需要在 Logto 控制台配置
            // 这里简化处理,实际需要根据 Logto 的具体实现调整
        } catch (error) {
            console.error('Email sign in error:', error)
            throw error
        } finally {
            isLoading.value = false
        }
    }

    /**
     * 注册新用户
     */
    const signUp = async (email: string, password: string) => {
        try {
            isLoading.value = true
            const client = await initLogto()

            await client.signIn({
                redirectUri: `${window.location.origin}${LOGIN_CONFIG.callbackPath}`,
                firstScreen: 'register',
                loginHint: email,
            })
        } catch (error) {
            console.error('Sign up error:', error)
            throw error
        } finally {
            isLoading.value = false
        }
    }

    /**
     * 处理登录回调
     */
    const handleSignInCallback = async (callbackUri?: string) => {
        try {
            isLoading.value = true
            const client = await initLogto()

            await client.handleSignInCallback(callbackUri || window.location.href)

            isAuthenticated.value = await client.isAuthenticated()

            if (isAuthenticated.value) {
                user.value = await client.getIdTokenClaims()
                return user.value
            }

            return null
        } catch (error) {
            console.error('Handle callback error:', error)
            throw error
        } finally {
            isLoading.value = false
        }
    }

    /**
     * 退出登录
     */
    const signOut = async () => {
        try {
            isLoading.value = true
            const client = await initLogto()

            await client.signOut(`${window.location.origin}${LOGIN_CONFIG.loginPath}`)

            isAuthenticated.value = false
            user.value = null
        } catch (error) {
            console.error('Sign out error:', error)
            throw error
        } finally {
            isLoading.value = false
        }
    }

    /**
     * 获取访问令牌
     */
    const getAccessToken = async (resource?: string) => {
        try {
            const client = await initLogto()
            return await client.getAccessToken(resource)
        } catch (error) {
            console.error('Get access token error:', error)
            return null
        }
    }

    /**
     * 获取用户信息
     */
    const getUserInfo = async () => {
        try {
            const client = await initLogto()

            if (await client.isAuthenticated()) {
                return await client.fetchUserInfo()
            }

            return null
        } catch (error) {
            console.error('Get user info error:', error)
            return null
        }
    }

    return {
        isAuthenticated: computed(() => isAuthenticated.value),
        isLoading: computed(() => isLoading.value),
        user: computed(() => user.value),
        initLogto,
        signInWithOAuth,
        signInWithEmail,
        signUp,
        handleSignInCallback,
        signOut,
        getAccessToken,
        getUserInfo,
    }
}
