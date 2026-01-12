/**
 * 密码验证测试
 */

import { describe, it, expect } from 'vitest'
import { validatePassword } from '../authValidation'

describe('密码验证', () => {
  it('应该接受有效的密码长度', () => {
    const validPasswords = [
      '12345678',     // 8位
      '123456789012345678901', // 20位
      'password123',  // 11位
      'MyPass@123'    // 10位
    ]

    validPasswords.forEach(password => {
      const result = validatePassword(password)
      expect(result.valid).toBe(true)
    })
  })

  it('应该拒绝过短的密码', () => {
    const shortPasswords = [
      '',           // 空
      '1',          // 1位
      '1234567'     // 7位
    ]

    shortPasswords.forEach(password => {
      const result = validatePassword(password)
      expect(result.valid).toBe(false)
      expect(result.message).toContain('不能少于 8 位')
    })
  })

  it('应该拒绝过长的密码', () => {
    const longPasswords = [
      '123456789012345678901',  // 21位
      '1234567890123456789012345678901234567890' // 40位
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