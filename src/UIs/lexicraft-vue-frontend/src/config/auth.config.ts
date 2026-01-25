/**
 * 认证配置
 * 替换原有的 Logto 配置，使用直接的 Identity 服务集成
 */

import {OAuthProvider} from '@/types/auth'

// OAuth 提供商配置
export const oauthProviders: Record<OAuthProvider, {
    name: string
    icon: string
    color: string
    displayName: string
}> = {
    github: {
        name: 'GitHub',
        icon: 'mdi:github',
        color: '#24292e',
        displayName: 'GitHub'
    },
    gitee: {
        name: 'Gitee',
        icon: 'simple-icons:gitee',
        color: '#c71d23',
        displayName: 'Gitee'
    }
}

// 路径配置
export const AUTH_PATHS = {
    LOGIN: '/login',
    REGISTER: '/register',
    CALLBACK: '/callback',
    REDIRECT: '/app/dashboard',
    FORGOT_PASSWORD: '/forgot-password',
    RESET_PASSWORD: '/reset-password'
} as const

// 认证相关常量
export const AUTH_CONFIG = {
    // Token 存储键名
    ACCESS_TOKEN_KEY: 'lexicraft_access_token',
    REFRESH_TOKEN_KEY: 'lexicraft_refresh_token',

    // Token 过期时间配置
    TOKEN_REFRESH_THRESHOLD: 300, // 提前 5 分钟刷新

    // 密码强度要求
    PASSWORD_MIN_LENGTH: 8,
    PASSWORD_MAX_LENGTH: 20,
    PASSWORD_REQUIRE_UPPERCASE: true,
    PASSWORD_REQUIRE_LOWERCASE: true,
    PASSWORD_REQUIRE_NUMBERS: true,
    PASSWORD_REQUIRE_SYMBOLS: false,

    // 用户名要求
    USERNAME_MIN_LENGTH: 3,
    USERNAME_MAX_LENGTH: 20,
    USERNAME_PATTERN: /^[a-zA-Z0-9_-]+$/,

    // 邮箱验证
    EMAIL_PATTERN: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,

    // OAuth 回调超时
    OAUTH_CALLBACK_TIMEOUT: 30000, // 30 秒

    // 自动登出时间（无操作）
    AUTO_LOGOUT_TIME: 24 * 60 * 60 * 1000, // 24 小时
} as const

// 错误消息配置
export const AUTH_ERROR_MESSAGES = {
    INVALID_CREDENTIALS: '用户名或密码错误',
    USER_NOT_FOUND: '用户不存在',
    EMAIL_ALREADY_EXISTS: '邮箱已被注册',
    USERNAME_ALREADY_EXISTS: '用户名已被使用',
    TOKEN_EXPIRED: '登录已过期，请重新登录',
    OAUTH_ERROR: 'OAuth 登录失败',
    NETWORK_ERROR: '网络连接失败，请检查网络设置',
    VALIDATION_ERROR: '输入信息格式不正确',
    UNAUTHORIZED: '未授权访问',
    FORBIDDEN: '权限不足',
    SERVER_ERROR: '服务器错误，请稍后重试',
    PASSWORD_TOO_WEAK: '密码强度不够，请包含大小写字母和数字',
    EMAIL_INVALID: '邮箱格式不正确',
    USERNAME_INVALID: '用户名只能包含字母、数字、下划线和连字符',
    CONFIRM_PASSWORD_MISMATCH: '两次输入的密码不一致'
} as const

// 表单验证规则
export const VALIDATION_RULES = {
    email: [
        {required: true, message: '请输入邮箱地址', trigger: 'blur'},
        {
            pattern: AUTH_CONFIG.EMAIL_PATTERN,
            message: AUTH_ERROR_MESSAGES.EMAIL_INVALID,
            trigger: 'blur'
        }
    ],
    password: [
        {required: true, message: '请输入密码', trigger: 'blur'},
        {
            min: AUTH_CONFIG.PASSWORD_MIN_LENGTH,
            max: AUTH_CONFIG.PASSWORD_MAX_LENGTH,
            message: `密码长度应在 ${AUTH_CONFIG.PASSWORD_MIN_LENGTH}-${AUTH_CONFIG.PASSWORD_MAX_LENGTH} 位之间`,
            trigger: 'blur'
        }
    ],
    confirmPassword: [
        {required: true, message: '请确认密码', trigger: 'blur'}
    ],
    username: [
        {required: true, message: '请输入用户名', trigger: 'blur'},
        {
            min: AUTH_CONFIG.USERNAME_MIN_LENGTH,
            max: AUTH_CONFIG.USERNAME_MAX_LENGTH,
            message: `用户名长度应在 ${AUTH_CONFIG.USERNAME_MIN_LENGTH}-${AUTH_CONFIG.USERNAME_MAX_LENGTH} 位之间`,
            trigger: 'blur'
        },
        {
            pattern: AUTH_CONFIG.USERNAME_PATTERN,
            message: AUTH_ERROR_MESSAGES.USERNAME_INVALID,
            trigger: 'blur'
        }
    ],
    captchaCode: [
        {required: true, message: '请输入验证码', trigger: 'blur'},
        {
            len: 4,
            message: '验证码长度为4位',
            trigger: 'blur'
        }
    ]
} as const

// 导出便捷访问的路径
export const {
    LOGIN: LOGIN_PATH,
    REGISTER: REGISTER_PATH,
    CALLBACK: CALLBACK_PATH,
    REDIRECT: REDIRECT_PATH,
    FORGOT_PASSWORD: FORGOT_PASSWORD_PATH,
    RESET_PASSWORD: RESET_PASSWORD_PATH
} = AUTH_PATHS

// 兼容旧代码的导出
export const LOGIN_CONFIG = {
    loginPath: LOGIN_PATH,
    registerPath: REGISTER_PATH,
    redirectPath: REDIRECT_PATH,
    callbackPath: CALLBACK_PATH
}