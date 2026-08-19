/**
 * 认证 API 客户端
 * 提供与 Fantasy.Services.Identity 服务通信的所有认证相关方法
 */

import {
    CaptchaResponse,
    IAuthAPI,
    LoginRequest,
    LoginResponse,
    OAuthCallbackParams,
    OAuthInitResponse,
    OAuthProvider,
    RegisterRequest,
    RegisterResponse,
    ResultDto,
    TokenPair,
    UpdateProfileRequest,
    UploadAvatarResponse,
    UserPermissionsResponse,
    UserProfile
} from '@/types/auth'
import {identityGet, identityPost, identityPut, identityRequest} from '@/utils/apiClient'
import {API_ROUTES} from '@/config/apiRoutes'

export function normalizeUserId(userId: unknown): string {
    if (typeof userId === 'string' && userId.trim()) {
        return userId.trim()
    }

    if (userId && typeof userId === 'object') {
        const source = userId as Record<string, unknown>
        const value = source.value ?? source.Value
        if (typeof value === 'string' && value.trim()) {
            return value.trim()
        }
    }

    throw new TypeError('Invalid user id returned by Identity service')
}

/**
 * 认证 API 实现类
 */
class AuthAPI implements IAuthAPI {
    /**
     * 用户登录
     */
    async login(credentials: LoginRequest): Promise<ResultDto<LoginResponse>> {
        return identityPost<LoginResponse>(API_ROUTES.identity.login, credentials)
    }

    /**
     * 用户注册
     */
    async register(userData: RegisterRequest): Promise<ResultDto<RegisterResponse>> {
        // 构建后端期望的请求格式
        const registerData = {
            userAccount: userData.username || userData.email, // 使用用户名或邮箱作为账号
            email: userData.email,
            password: userData.password,
            captchaKey: userData.captchaKey,
            captchaCode: userData.captchaCode
        }

        return identityPost<RegisterResponse>(API_ROUTES.identity.register, registerData)
    }

    /**
     * 获取验证码
     */
    async getCaptcha(): Promise<ResultDto<CaptchaResponse>> {
        return identityGet<CaptchaResponse>(API_ROUTES.identity.captcha)
    }

    /**
     * 用户登出
     */
    async logout(): Promise<ResultDto<void>> {
        return identityPost<void>(API_ROUTES.identity.logout)
    }

    /**
     * 获取用户资料
     */
    async getUserProfile(): Promise<ResultDto<UserProfile>> {
        return identityGet<UserProfile>(API_ROUTES.identity.profile)
    }

    /**
     * 更新用户资料
     */
    async updateUserProfile(profile: UpdateProfileRequest): Promise<ResultDto<UserProfile>> {
        return identityPut<UserProfile>(API_ROUTES.identity.profile, profile)
    }

    async uploadAvatar(file: File): Promise<ResultDto<UploadAvatarResponse>> {
        const formData = new FormData()
        formData.append('Avatar', file)

        return identityRequest<UploadAvatarResponse>({
            method: 'POST',
            url: API_ROUTES.identity.uploadAvatar,
            data: formData,
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        })
    }

    /**
     * 初始化 OAuth 登录
     */
    async initiateOAuth(provider: OAuthProvider): Promise<ResultDto<OAuthInitResponse>> {
        return identityGet<OAuthInitResponse>(API_ROUTES.identity.oauthInitiate(provider))
    }

    /**
     * 处理 OAuth 回调
     */
    async handleOAuthCallback(params: OAuthCallbackParams): Promise<ResultDto<LoginResponse>> {
        return identityPost<LoginResponse>(API_ROUTES.identity.oauthCallback(params.provider), {
            code: params.code,
            state: params.state
        })
    }

    /**
     * 刷新访问令牌
     */
    async refreshToken(refreshToken: string): Promise<ResultDto<TokenPair>> {
        return identityPost<TokenPair>(API_ROUTES.identity.refreshToken, {refreshToken})
    }

    /**
     * 查询用户权限列表
     */
    async getUserPermissions(userId: string): Promise<ResultDto<UserPermissionsResponse>> {
        const normalizedUserId = normalizeUserId(userId)
        return identityGet<UserPermissionsResponse>(API_ROUTES.identity.permissions(normalizedUserId))
    }
}

// 导出单例实例
export const authAPI = new AuthAPI()

// 导出类以便测试
export {AuthAPI}

/**
 * 便捷方法 - 直接导出常用的 API 方法
 */
export const {
    login,
    register,
    logout,
    getCaptcha,
    getUserProfile,
    updateUserProfile,
    initiateOAuth,
    handleOAuthCallback,
    refreshToken,
    getUserPermissions,
    uploadAvatar
} = authAPI
