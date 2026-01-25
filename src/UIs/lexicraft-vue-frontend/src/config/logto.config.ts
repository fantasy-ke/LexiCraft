// Logto 配置
export const logtoConfig = {
    endpoint: import.meta.env.VITE_LOGTO_ENDPOINT || '',
    appId: import.meta.env.VITE_LOGTO_APP_ID || '',
    resources: [],
    scopes: ['email', 'profile', 'openid'],
}

// OAuth 提供商配置
export const oauthProviders = {
    github: {
        name: 'GitHub',
        icon: 'mdi:github',
        connectorId: 'github',
        color: '#24292e',
    },
    gitee: {
        name: 'Gitee',
        icon: 'simple-icons:gitee',
        connectorId: 'gitee',
        color: '#c71d23',
    },
}

// 路径配置
export const LOGIN_PATH = '/login'
export const REGISTER_PATH = '/register'
export const REDIRECT_PATH = '/app/dashboard'
export const CALLBACK_PATH = '/callback'

export const LOGIN_CONFIG = {
    loginPath: LOGIN_PATH,
    registerPath: REGISTER_PATH,
    redirectPath: REDIRECT_PATH,
    callbackPath: CALLBACK_PATH,
}

