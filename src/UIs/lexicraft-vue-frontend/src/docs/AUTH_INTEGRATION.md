# 前端认证集成文档

本文档说明如何使用新的认证系统，该系统直接与 LexiCraft.Services.Identity 服务集成，替换了原有的 Logto 组件。

## 🏗️ 架构概览

新的认证系统采用分层架构：

```
Vue.js 组件
    ↓
认证 Store (Pinia)
    ↓
认证 API 客户端
    ↓
HTTP 客户端 (Axios)
    ↓
Identity 服务
```

## 📁 文件结构

```
src/
├── types/
│   └── auth.ts                 # 认证相关类型定义
├── utils/
│   ├── tokenManager.ts         # JWT Token 管理
│   ├── authHttp.ts            # 认证专用 HTTP 客户端
│   ├── authHelpers.ts         # 认证工具函数
│   └── authValidation.ts      # 输入验证函数
├── apis/
│   └── auth.ts                # 认证 API 客户端
├── stores/
│   └── auth.ts                # 认证状态管理
├── hooks/
│   └── useAuth.ts             # 认证组合式函数
├── config/
│   └── auth.config.ts         # 认证配置
└── pages/(user)/
    ├── login.vue              # 登录页面
    ├── register.vue           # 注册页面
    └── callback.vue           # OAuth 回调页面
```

## 🚀 快速开始

### 1. 基本使用

```vue
<script setup>
import { useAuth } from '@/hooks/useAuth'

const { 
  isAuthenticated, 
  user, 
  signInWithEmail, 
  signOut 
} = useAuth()

// 登录
const handleLogin = async () => {
  await signInWithEmail('user@example.com', 'password')
}

// 登出
const handleLogout = async () => {
  await signOut()
}
</script>

<template>
  <div v-if="isAuthenticated">
    <p>欢迎，{{ user?.username }}!</p>
    <button @click="handleLogout">登出</button>
  </div>
  <div v-else>
    <button @click="handleLogin">登录</button>
  </div>
</template>
```

### 2. 在组件中使用认证状态

```vue
<script setup>
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

// 响应式状态
const isLoading = computed(() => authStore.isLoading)
const user = computed(() => authStore.user)
</script>
```

### 3. 路由守卫

```typescript
import { useAuth } from '@/hooks/useAuth'

router.beforeEach(async (to, from, next) => {
  const { authGuard } = useAuth()
  await authGuard(to, from, next)
})
```

## 🔧 API 使用

### 认证 API 客户端

```typescript
import { authAPI } from '@/apis/auth'

// 登录
const loginResponse = await authAPI.login({
  userAccount: 'user@example.com',
  password: 'password'
})

// 注册
const registerResponse = await authAPI.register({
  email: 'user@example.com',
  password: 'password',
  confirmPassword: 'password',
  username: 'username'
})

// OAuth 登录
const oauthResponse = await authAPI.initiateOAuth('github')
```

### Token 管理

```typescript
import { tokenManager } from '@/utils/tokenManager'

// 获取访问令牌
const token = tokenManager.getAccessToken()

// 检查令牌是否过期
const isExpired = tokenManager.isTokenExpired(token)

// 自动刷新令牌
const refreshed = await tokenManager.refreshTokenIfNeeded()
```

## 🎯 核心功能

### 1. 本地认证

- **邮箱/用户名 + 密码登录**
- **用户注册**
- **密码重置**
- **邮箱验证**

### 2. OAuth 集成

支持的提供商：

- GitHub
- Gitee

### 3. Token 管理

- **自动 Token 注入**
- **自动 Token 刷新**
- **Token 过期检测**
- **安全存储**

### 4. 状态管理

- **响应式认证状态**
- **用户信息管理**
- **跨页面状态持久化**

## ⚙️ 配置

### 环境配置

在 `src/config/env.ts` 中配置 Identity 服务地址：

```typescript
const gatewayBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
const map = {
  DEV: {
    IDENTITY_API: `${gatewayBaseUrl}/identity`,
    // 其他配置...
  }
}
```

### 认证配置

在 `src/config/auth.config.ts` 中自定义认证行为：

```typescript
export const AUTH_CONFIG = {
  TOKEN_REFRESH_THRESHOLD: 300, // 提前 5 分钟刷新
  PASSWORD_MIN_LENGTH: 8,
  AUTO_LOGOUT_TIME: 24 * 60 * 60 * 1000, // 24 小时
  // 其他配置...
}
```

## 🔒 安全特性

### 1. Token 安全

- **JWT Token 自动管理**
- **安全的本地存储**
- **自动过期检测和刷新**

### 2. 输入验证

- **客户端验证**
- **XSS 防护**
- **CSRF 保护**

### 3. 错误处理

- **统一错误格式**
- **用户友好的错误消息**
- **自动重试机制**

## 🧪 测试

### 单元测试示例

```typescript
import { describe, it, expect } from 'vitest'
import { validateEmail, validatePassword } from '@/utils/authValidation'

describe('认证验证', () => {
  it('应该验证有效邮箱', () => {
    const result = validateEmail('user@example.com')
    expect(result.valid).toBe(true)
  })

  it('应该拒绝无效邮箱', () => {
    const result = validateEmail('invalid-email')
    expect(result.valid).toBe(false)
  })
})
```

## 🔄 迁移指南

### 从 Logto 迁移

1. **更新导入**：
   ```typescript
   // 旧的
   import { useLogto } from '@/hooks/useLogto'
   
   // 新的
   import { useAuth } from '@/hooks/useAuth'
   ```

2. **更新方法调用**：
   ```typescript
   // 旧的
   await signInWithOAuth('github')
   
   // 新的 (相同)
   await signInWithOAuth('github')
   ```

3. **更新状态访问**：
   ```typescript
   // 旧的
   const { user, isAuthenticated } = useLogto()
   
   // 新的
   const { user, isAuthenticated } = useAuth()
   ```

## 🐛 故障排除

### 常见问题

1. **Token 刷新失败**
    - 检查 Identity 服务是否运行
    - 验证 API 端点配置
    - 检查网络连接

2. **OAuth 回调失败**
    - 确认回调 URL 配置正确
    - 检查 OAuth 提供商设置
    - 验证 state 参数

3. **登录状态丢失**
    - 检查 localStorage 是否可用
    - 验证 Token 存储配置
    - 确认浏览器设置

### 调试技巧

1. **启用详细日志**：
   ```typescript
   // 在开发环境中启用
   localStorage.setItem('auth:debug', 'true')
   ```

2. **检查 Token 状态**：
   ```typescript
   import { tokenManager } from '@/utils/tokenManager'
   
   console.log('Access Token:', tokenManager.getAccessToken())
   console.log('Remaining Time:', tokenManager.getTokenRemainingTime())
   ```

## 📚 API 参考

### useAuth Hook

| 方法                | 描述       | 参数                                | 返回值                    |
|-------------------|----------|-----------------------------------|------------------------|
| `signInWithEmail` | 邮箱登录     | `email: string, password: string` | `Promise<void>`        |
| `signInWithOAuth` | OAuth 登录 | `provider: OAuthProvider`         | `Promise<void>`        |
| `signUp`          | 用户注册     | `userData: RegisterRequest`       | `Promise<void>`        |
| `signOut`         | 用户登出     | -                                 | `Promise<void>`        |
| `getUserInfo`     | 获取用户信息   | -                                 | `Promise<UserProfile>` |

### AuthStore

| 状态                | 类型                    | 描述       |
|-------------------|-----------------------|----------|
| `user`            | `UserProfile \| null` | 当前用户信息   |
| `isAuthenticated` | `boolean`             | 是否已认证    |
| `isLoading`       | `boolean`             | 是否加载中    |
| `tokens`          | `TokenPair \| null`   | Token 信息 |

## 🤝 贡献

如需贡献代码或报告问题，请参考项目的贡献指南。

## 📄 许可证

本项目采用 MIT 许可证。