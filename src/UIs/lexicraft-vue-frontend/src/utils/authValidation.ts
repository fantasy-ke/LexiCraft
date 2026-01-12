/**
 * 认证相关的输入验证辅助函数
 */

import { AUTH_CONFIG, AUTH_ERROR_MESSAGES } from '@/config/auth.config'

/**
 * 验证邮箱格式
 */
export function validateEmail(email: string): { valid: boolean; message?: string } {
  if (!email) {
    return { valid: false, message: '请输入邮箱地址' }
  }
  
  if (!AUTH_CONFIG.EMAIL_PATTERN.test(email)) {
    return { valid: false, message: AUTH_ERROR_MESSAGES.EMAIL_INVALID }
  }
  
  return { valid: true }
}

/**
 * 验证密码强度
 */
export function validatePassword(password: string): { valid: boolean; message?: string } {
  if (!password) {
    return { valid: false, message: '请输入密码' }
  }
  
  if (password.length < AUTH_CONFIG.PASSWORD_MIN_LENGTH) {
    return { 
      valid: false, 
      message: `密码长度不能少于 ${AUTH_CONFIG.PASSWORD_MIN_LENGTH} 位` 
    }
  }
  
  if (password.length > AUTH_CONFIG.PASSWORD_MAX_LENGTH) {
    return { 
      valid: false, 
      message: `密码长度不能超过 ${AUTH_CONFIG.PASSWORD_MAX_LENGTH} 位` 
    }
  }
  
  // 检查密码强度要求
  const checks = []
  
  if (AUTH_CONFIG.PASSWORD_REQUIRE_UPPERCASE && !/[A-Z]/.test(password)) {
    checks.push('大写字母')
  }
  
  if (AUTH_CONFIG.PASSWORD_REQUIRE_LOWERCASE && !/[a-z]/.test(password)) {
    checks.push('小写字母')
  }
  
  if (AUTH_CONFIG.PASSWORD_REQUIRE_NUMBERS && !/\d/.test(password)) {
    checks.push('数字')
  }
  
  if (AUTH_CONFIG.PASSWORD_REQUIRE_SYMBOLS && !/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
    checks.push('特殊字符')
  }
  
  if (checks.length > 0) {
    return { 
      valid: false, 
      message: `密码必须包含${checks.join('、')}` 
    }
  }
  
  return { valid: true }
}

/**
 * 验证用户名格式
 */
export function validateUsername(username: string): { valid: boolean; message?: string } {
  if (!username) {
    return { valid: false, message: '请输入用户名' }
  }
  
  if (username.length < AUTH_CONFIG.USERNAME_MIN_LENGTH || 
      username.length > AUTH_CONFIG.USERNAME_MAX_LENGTH) {
    return { 
      valid: false, 
      message: `用户名长度应在 ${AUTH_CONFIG.USERNAME_MIN_LENGTH}-${AUTH_CONFIG.USERNAME_MAX_LENGTH} 位之间` 
    }
  }
  
  if (!AUTH_CONFIG.USERNAME_PATTERN.test(username)) {
    return { valid: false, message: AUTH_ERROR_MESSAGES.USERNAME_INVALID }
  }
  
  return { valid: true }
}

/**
 * 验证确认密码
 */
export function validateConfirmPassword(
  password: string, 
  confirmPassword: string
): { valid: boolean; message?: string } {
  if (!confirmPassword) {
    return { valid: false, message: '请确认密码' }
  }
  
  if (password !== confirmPassword) {
    return { valid: false, message: AUTH_ERROR_MESSAGES.CONFIRM_PASSWORD_MISMATCH }
  }
  
  return { valid: true }
}

/**
 * 验证登录表单
 */
export function validateLoginForm(data: {
  userAccount: string
  password: string
}): { valid: boolean; errors: Record<string, string> } {
  const errors: Record<string, string> = {}
  
  // 验证用户账号（邮箱或用户名）
  if (!data.userAccount) {
    errors.userAccount = '请输入邮箱或用户名'
  } else {
    // 判断是邮箱还是用户名
    if (data.userAccount.includes('@')) {
      const emailValidation = validateEmail(data.userAccount)
      if (!emailValidation.valid) {
        errors.userAccount = emailValidation.message!
      }
    } else {
      const usernameValidation = validateUsername(data.userAccount)
      if (!usernameValidation.valid) {
        errors.userAccount = usernameValidation.message!
      }
    }
  }
  
  // 验证密码
  const passwordValidation = validatePassword(data.password)
  if (!passwordValidation.valid) {
    errors.password = passwordValidation.message!
  }
  
  return {
    valid: Object.keys(errors).length === 0,
    errors
  }
}

/**
 * 验证注册表单
 */
export function validateRegisterForm(data: {
  email: string
  password: string
  confirmPassword: string
  username?: string
}): { valid: boolean; errors: Record<string, string> } {
  const errors: Record<string, string> = {}
  
  // 验证邮箱
  const emailValidation = validateEmail(data.email)
  if (!emailValidation.valid) {
    errors.email = emailValidation.message!
  }
  
  // 验证用户名（如果提供）
  if (data.username) {
    const usernameValidation = validateUsername(data.username)
    if (!usernameValidation.valid) {
      errors.username = usernameValidation.message!
    }
  }
  
  // 验证密码
  const passwordValidation = validatePassword(data.password)
  if (!passwordValidation.valid) {
    errors.password = passwordValidation.message!
  }
  
  // 验证确认密码
  const confirmPasswordValidation = validateConfirmPassword(data.password, data.confirmPassword)
  if (!confirmPasswordValidation.valid) {
    errors.confirmPassword = confirmPasswordValidation.message!
  }
  
  return {
    valid: Object.keys(errors).length === 0,
    errors
  }
}

/**
 * 清理输入数据（移除前后空格等）
 */
export function sanitizeAuthInput(data: Record<string, any>): Record<string, any> {
  const sanitized: Record<string, any> = {}
  
  for (const [key, value] of Object.entries(data)) {
    if (typeof value === 'string') {
      sanitized[key] = value.trim()
    } else {
      sanitized[key] = value
    }
  }
  
  return sanitized
}

/**
 * 检查密码强度等级
 */
export function getPasswordStrength(password: string): {
  level: 'weak' | 'medium' | 'strong' | 'very-strong'
  score: number
  suggestions: string[]
} {
  let score = 0
  const suggestions: string[] = []
  
  // 长度检查
  if (password.length >= 8) score += 1
  else suggestions.push('至少8个字符')
  
  if (password.length >= 12) score += 1
  
  // 字符类型检查
  if (/[a-z]/.test(password)) score += 1
  else suggestions.push('包含小写字母')
  
  if (/[A-Z]/.test(password)) score += 1
  else suggestions.push('包含大写字母')
  
  if (/\d/.test(password)) score += 1
  else suggestions.push('包含数字')
  
  if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) score += 1
  else suggestions.push('包含特殊字符')
  
  // 复杂性检查
  if (!/(.)\1{2,}/.test(password)) score += 1 // 没有连续重复字符
  else suggestions.push('避免连续重复字符')
  
  let level: 'weak' | 'medium' | 'strong' | 'very-strong'
  
  if (score <= 2) level = 'weak'
  else if (score <= 4) level = 'medium'
  else if (score <= 6) level = 'strong'
  else level = 'very-strong'
  
  return { level, score, suggestions }
}