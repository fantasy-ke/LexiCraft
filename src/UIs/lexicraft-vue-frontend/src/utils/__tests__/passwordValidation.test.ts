/**
 * 密码验证测试
 */

import {describe, expect, it} from 'vitest'
import {validatePassword} from '../authValidation'

describe('密码验证', () => {
    it('应该接受有效的密码长度', () => {
        const validPasswords = [
            'Aa123456', // 8 位
            'Aa123456789012345678', // 20 位
            'Password123',
            'MyPass@123'
        ]

        validPasswords.forEach(password => {
            const result = validatePassword(password)
            expect(result.valid).toBe(true)
        })
    })

    it('应该拒绝过短的密码', () => {
        const shortPasswords = [
            'Aa1',
            'Aa12345'
        ]

        shortPasswords.forEach(password => {
            const result = validatePassword(password)
            expect(result.valid).toBe(false)
            expect(result.message).toContain('不能少于 8 位')
        })
    })

    it('应该拒绝过长的密码', () => {
        const longPasswords = [
            'Aa1234567890123456789', // 21 位
            'Aa12345678901234567890123456789012345678'
        ]

        longPasswords.forEach(password => {
            const result = validatePassword(password)
            expect(result.valid).toBe(false)
            expect(result.message).toContain('不能超过 20 位')
        })
    })

    it('应该为空密码提供正确的错误消息', () => {
        const result = validatePassword('')
        expect(result.valid).toBe(false)
        expect(result.message).toBe('请输入密码')
    })
})