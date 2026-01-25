/**
 * 类型定义索引文件
 *
 * 此文件统一导出所有类型定义，方便应用程序其他部分导入使用
 */

// 导出现有的类型定义
export * from './types'
export * from './enum'
export * from './func'

// 导出新的认证类型定义
export * from './auth'

// 导出认证工具函数
export * from '../utils/authHelpers'
export * from '../utils/authValidation'

// 重新导出常用类型，提供更好的开发体验
export type {
    // 现有的核心类型
    Word,
    Article,
    Dict,
    Statistics,
    DictResource,
    ArticleItem,
    PracticeData,
    TaskWords,
    TranslateLanguageType,
    LanguageType
} from './types'

export type {
    // 认证相关类型
    ResultDto,
    LoginRequest,
    RegisterRequest,
    LoginResponse,
    RegisterResponse,
    UserProfile,
    OAuthProvider,
    TokenPair,
    AuthState,
    AuthConfig,
    AuthError,
    IAuthAPI,
    IAuthActions,
    ITokenManager,
    UpdateProfileRequest,
    TokenResponse,
    OAuthInitResponse,
    OAuthCallbackParams
} from './auth'

// 类型别名，提供更好的开发体验
export type {
    ResultDto as ApiResponse,
    LoginRequest as AuthLoginRequest,
    RegisterRequest as AuthRegisterRequest,
    UserProfile as User
} from './auth'

// 导出枚举
export {
    DictType,
    Sort,
    ShortcutKey,
    TranslateEngine,
    PracticeArticleWordType,
    WordPracticeMode,
    WordPracticeType,
    CodeType,
    ImportStatus,
    WordPracticeStage
} from './enum'

export {AuthErrorCode} from './auth'