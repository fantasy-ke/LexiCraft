/**
 * Shared API response contract.
 * The HTTP boundary normalizes backend PascalCase fields to camelCase.
 */
export interface ResultDto<T = any> {
    status: boolean
    data: T
    message: string
    statusCode: number
    extensions?: Record<string, any>
}

export type ApiErrorDomain = 'identity' | 'service'
