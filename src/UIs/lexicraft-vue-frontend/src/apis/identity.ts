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
import {authGet, authPost, authPut, authRequest} from '@/utils/authHttp'

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
        return authPost<LoginResponse>('/v1/login', credentials)
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

        return authPost<RegisterResponse>('/v1/register', registerData)
    }

    /**
     * 获取验证码
     */
    async getCaptcha(): Promise<ResultDto<CaptchaResponse>> {
        return authGet<CaptchaResponse>('/v1/users/captcha')
    }

    /**
     * 用户登出
     */
    async logout(): Promise<ResultDto<void>> {
        return authPost<void>('/v1/logout')
    }

    /**
     * 获取用户资料
     */
    async getUserProfile(): Promise<ResultDto<UserProfile>> {
        return authGet<UserProfile>('/v1/users/info')
    }

    /**
     * 更新用户资料
     */
    async updateUserProfile(profile: UpdateProfileRequest): Promise<ResultDto<UserProfile>> {
        return authPut<UserProfile>('/v1/users/info', profile)
    }

    async uploadAvatar(file: File): Promise<ResultDto<UploadAvatarResponse>> {
        const formData = new FormData()
        formData.append('Avatar', file)

        return authRequest<UploadAvatarResponse>({
            method: 'POST',
            url: '/v1/uploadAvatar',
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
        return authGet<OAuthInitResponse>(`/v1/oauth/${provider}/initiate`)
    }

    /**
     * 处理 OAuth 回调
     */
    async handleOAuthCallback(params: OAuthCallbackParams): Promise<ResultDto<LoginResponse>> {
        return authPost<LoginResponse>(`/v1/oauth/${params.provider}/callback`, {
            code: params.code,
            state: params.state
        })
    }

    /**
     * 刷新访问令牌
     */
    async refreshToken(refreshToken: string): Promise<ResultDto<TokenPair>> {
        return authPost<TokenPair>('/v1/refresh-token', {refreshToken})
    }

    /**
     * 查询用户权限列表
     */
    async getUserPermissions(userId: string): Promise<ResultDto<UserPermissionsResponse>> {
        const normalizedUserId = normalizeUserId(userId)
        return authGet<UserPermissionsResponse>(`/v1/permissions/${encodeURIComponent(normalizedUserId)}`)
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
