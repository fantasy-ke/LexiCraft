export interface MemberProfile {
    levelDesc: string
    status: string
    active: boolean
    endDate: number | null
    autoRenew: boolean
    plan: string
    planDesc: string
}

/**
 * 旧页面使用的过渡视图模型。
 * Identity 用户资料不包含会员字段，会员信息仍由尚未迁移的旧接口补充。
 */
export interface UserViewModel {
    id?: string
    userId?: string
    email?: string
    phone?: string | null
    username?: string
    userName?: string
    avatar?: string
    hasPwd?: boolean
    role?: string
    member?: MemberProfile
}
