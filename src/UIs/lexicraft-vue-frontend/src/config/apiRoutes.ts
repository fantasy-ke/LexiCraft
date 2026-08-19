export const DEFAULT_API_VERSION = 'v1' as const

export type GatewayService = 'identity' | 'vocabulary' | 'practice'

function trimPath(path: string): string {
    return path.replace(/^\/+|\/+$/g, '')
}

export function buildGatewayRoute(
    service: GatewayService,
    path = '',
    version = DEFAULT_API_VERSION
): string {
    const suffix = trimPath(path)
    const prefix = `/${service}/${version}`
    return suffix ? `${prefix}/${suffix}` : prefix
}

export const API_PREFIXES = {
    identity: buildGatewayRoute('identity'),
    vocabulary: buildGatewayRoute('vocabulary'),
    practice: buildGatewayRoute('practice'),
    files: '/files'
} as const

export const API_ROUTES = {
    identity: {
        login: buildGatewayRoute('identity', 'login'),
        register: buildGatewayRoute('identity', 'register'),
        captcha: buildGatewayRoute('identity', 'users/captcha'),
        logout: buildGatewayRoute('identity', 'logout'),
        profile: buildGatewayRoute('identity', 'users/info'),
        uploadAvatar: buildGatewayRoute('identity', 'uploadAvatar'),
        refreshToken: buildGatewayRoute('identity', 'refresh-token'),
        oauthInitiate: (provider: string) =>
            buildGatewayRoute('identity', `oauth/${encodeURIComponent(provider)}/initiate`),
        oauthCallback: (provider: string) =>
            buildGatewayRoute('identity', `oauth/${encodeURIComponent(provider)}/callback`),
        permissions: (userId: string) =>
            buildGatewayRoute('identity', `permissions/${encodeURIComponent(userId)}`)
    },
    vocabulary: {
        wordLists: buildGatewayRoute('vocabulary', 'word-lists'),
        wordsByList: (wordListId: number) =>
            buildGatewayRoute('vocabulary', `word-lists/${wordListId}/words`),
        words: buildGatewayRoute('vocabulary', 'words'),
        weakWords: buildGatewayRoute('vocabulary', 'user-words/weak'),
        wordState: buildGatewayRoute('vocabulary', 'user-words/state'),
        importWords: buildGatewayRoute('vocabulary', 'word-lists/import')
    },
    practice: {
        tasks: buildGatewayRoute('practice', 'tasks'),
        submitAssessment: buildGatewayRoute('practice', 'assessments/submit'),
        completeTask: (taskId: string) =>
            buildGatewayRoute('practice', `tasks/${encodeURIComponent(taskId)}/complete`)
    },
    files: {
        content: '/files/content'
    }
} as const
