import http from '@/utils/http.ts'
import {CodeType} from '@/types/enum.ts'

/**
 * 尚无当前微服务端契约可验证的旧用户接口。
 * 仅保留仍有页面调用的兼容能力，禁止在此新增接口。
 */
export interface SendCodeParams {
    val: string
    type: CodeType
}

export function sendCode(params: SendCodeParams) {
    return http<boolean>('user/sendCode', null, params, 'get')
}

export function setPassword(data: Record<string, string>) {
    return http('user/setPassword', data, null, 'post')
}

export function changeEmailApi(data: Record<string, string>) {
    return http('user/changeEmail', data, null, 'post')
}

export function changePhoneApi(data: Record<string, string>) {
    return http('user/changePhone', data, null, 'post')
}
