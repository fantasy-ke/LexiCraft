import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getUserInfo, User } from '@/apis/user'
import { AppEnv } from "@/config/env";
import Toast from "@/components/base/toast/Toast";
import { useLogto } from '@/hooks/useLogto'

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null)
  const isLogin = ref<boolean>(false)
  const logtoUser = ref<any>(null)
  const { initLogto, signOut: logtoSignOut } = useLogto()

  // 设置token
  const setToken = (newToken: string) => {
    isLogin.value = true
    AppEnv.TOKEN = newToken
    AppEnv.IS_LOGIN = !!AppEnv.TOKEN
    AppEnv.CAN_REQUEST = AppEnv.IS_LOGIN && AppEnv.IS_OFFICIAL
    localStorage.setItem('token', newToken)
  }

  // 清除token
  const clearToken = () => {
    AppEnv.IS_LOGIN = AppEnv.CAN_REQUEST = false
    AppEnv.TOKEN = ''
    localStorage.removeItem('token')
    isLogin.value = false
    user.value = null
  }

  // 设置用户信息
  const setUser = (userInfo: User | any) => {
    user.value = userInfo
    isLogin.value = true
  }

  // 设置Logto用户信息
  const setLogtoUser = (userInfo: any) => {
    logtoUser.value = userInfo
    isLogin.value = true
  }

  // 登出
  async function logout() {
    clearToken()
    // 如果使用Logto登录,也需要退出Logto
    if (logtoUser.value) {
      await logtoSignOut()
      logtoUser.value = null
    }
    Toast.success('已退出登录')
    //这行会引起hrm失效
    // router.push('/')
  }

  // 获取用户信息
  async function fetchUserInfo() {
    if (AppEnv.TOKEN === 'admin-mock-token') {
      setUser({
        id: 'admin',
        username: '超级管理员',
        email: 'admin@qq.com',
        avatar: '',
        role: 'admin'
      })
      return true
    }

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


  // 初始化用户状态
  async function init() {
    // 先尝试初始化Logto
    try {
      const logtoClient = await initLogto()
      const isAuthenticated = await logtoClient.isAuthenticated()
      if (isAuthenticated) {
        const claims = await logtoClient.getIdTokenClaims()
        setLogtoUser(claims)
        return true
      }
    } catch (error) {
      console.error('Logto init error:', error)
    }

    // 如果Logto未认证,尝试传统方式
    const success = await fetchUserInfo()
    if (!success) {
      clearToken()
    }
  }

  return {
    user,
    isLogin,
    logtoUser,
    setToken,
    clearToken,
    setUser,
    setLogtoUser,
    logout,
    fetchUserInfo,
    init
  }
})
