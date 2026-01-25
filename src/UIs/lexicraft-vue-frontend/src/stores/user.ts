import {defineStore} from 'pinia'
import {ref} from 'vue'
import {getUserInfo, User} from '@/apis/user'
import {AppEnv} from "@/config/env";
import {useAuthStore} from '@/stores/auth'

export const useUserStore = defineStore('user', () => {
    const user = ref<User | null>(null)
    const isLogin = ref<boolean>(false)

    // 获取新的认证 store
    const authStore = useAuthStore()

    // 设置token - 兼容旧代码，实际使用新的认证系统
    const setToken = (newToken: string) => {
        authStore.setToken(newToken)
        isLogin.value = true
        AppEnv.TOKEN = newToken
        AppEnv.IS_LOGIN = !!AppEnv.TOKEN
        AppEnv.CAN_REQUEST = AppEnv.IS_LOGIN && AppEnv.IS_OFFICIAL
        localStorage.setItem('token', newToken)
    }

    // 清除token - 兼容旧代码
    const clearToken = () => {
        authStore.clearToken()
        AppEnv.IS_LOGIN = AppEnv.CAN_REQUEST = false
        AppEnv.TOKEN = ''
        localStorage.removeItem('token')
        isLogin.value = false
        user.value = null
    }

    // 设置用户信息 - 兼容旧代码
    const setUser = (userInfo: User | any) => {
        user.value = userInfo
        authStore.setUser(userInfo)
        isLogin.value = true
    }

    // 登出 - 使用新的认证系统
    async function logout() {
        await authStore.logout()
        clearToken()
        // 不在这里显示提示，因为 authStore.logout() 已经显示了
    }

    // 获取用户信息 - 优先使用新的认证系统
    async function fetchUserInfo() {
        // 如果是管理员 mock token
        if (AppEnv.TOKEN === 'admin-mock-token') {
            const adminUser = {
                id: 'admin',
                username: '超级管理员',
                email: 'admin@qq.com',
                avatar: '',
                role: 'admin'
            }
            setUser(adminUser)
            return true
        }

        // 优先使用新的认证系统
        if (authStore.isAuthenticated) {
            try {
                await authStore.fetchUserProfile()
                if (authStore.user) {
                    user.value = authStore.user as any
                    isLogin.value = true
                    return true
                }
            } catch (error) {
                console.error('Fetch user profile from auth store error:', error)
            }
        }

        // 回退到旧的 API
        if (!AppEnv.CAN_REQUEST) return false
        try {
            const res = await getUserInfo()
            if (res.success) {
                setUser(res.data)
                return true
            }
            return false
        } catch (error) {
            console.error('Get user info error:', error)
            return false
        }
    }

    // 初始化用户状态 - 使用新的认证系统
    async function init() {
        try {
            // 优先使用新的认证系统初始化
            await authStore.initializeAuth()

            if (authStore.isAuthenticated && authStore.user) {
                user.value = authStore.user as any
                isLogin.value = true
                return true
            }
        } catch (error) {
            console.error('Auth store init error:', error)
        }

        // 回退到旧的方式
        const success = await fetchUserInfo()
        if (!success) {
            clearToken()
        }
        return success
    }

    return {
        user,
        isLogin,
        setToken,
        clearToken,
        setUser,
        logout,
        fetchUserInfo,
        init
    }
})
