# LexiCraft 项目长期记忆

## 记录范围

本文件只保存稳定的项目事实、架构决策、长期风险和已经确认的协作约定。临时调试过程、当日命令输出和未确认假设写入 `.agents/memory/YYYY-MM-DD.md`。

## 稳定架构事实

- 当前默认开发解决方案是 `src/LexiCraft.slnx`，默认运行入口是 `src/LexiCraft.Aspire.Host/AppHost.cs`。
- 根目录 `LexiCraft.sln` 属于较早的解决方案文件，项目清单与 `src/LexiCraft.slnx` 存在潜在漂移；涉及构建时优先使用 `.slnx`，并在发现差异时记录。
- 系统由 API Gateway、Identity、Vocabulary、Practice、Files gRPC 和 Aspire ServiceDefaults 组成；`src/BuildingBlocks/` 提供跨服务基础能力。
- Identity 使用 PostgreSQL 与 Redis；Vocabulary 使用 PostgreSQL 与 Redis；Practice 使用 MongoDB 与 Redis；Files 使用 OSS 抽象并提供 gRPC/内容读取能力。
- 公共运行能力通过 `AddServiceDefaults()` 提供，包括 AgileConfig 接入、服务发现、HttpClient resilience、健康检查和 OpenTelemetry。
- 网关通过 YARP 的 `ReverseProxy` 配置转发请求，并附带限流、安全头和 CORS。仓库内的 `ReverseProxy` 节点目前是空结构，真实路由来源必须在运行环境或 AgileConfig 中确认，不能从 README 推断。
- 后端模块前缀由代码定义为：`api/v{version:apiVersion}/identity`、`api/v{version:apiVersion}/vocabulary`、`api/v{version:apiVersion}/practice`。
- 业务端点通常经过 `ResultEndPointFilter` 包装为 `ResultDto`；文件流端点不使用相同包络。
- 认证采用 JWT Bearer 和权限声明；前端应使用 `Authorization: Bearer`，并统一令牌字段和刷新策略。
- 前端是独立的 Vue 3/Vite 工程，使用 TypeScript、Pinia、Vue Router、Axios、UnoCSS、Vue Macros 和自动组件/图标导入；前端同时存在新认证客户端与旧业务 API 封装。

## 已确认的长期协作约定

- 新任务开始前读取本文件和最新每日记忆，记录 `git status --short`，保留用户已有修改。
- 每个自然日创建一个新的 `.agents/memory/YYYY-MM-DD.md`；同日任务追加，不覆盖。
- 任务完成并通过必要验证后默认创建 Git 提交，使用 `<type>: <中文描述>`，只暂存本次任务文件，不推送。
- 不在代码、配置、文档或记忆中复制真实凭据；仓库已存在的敏感配置应通过后续任务迁移到环境变量、用户机密或部署平台密钥管理，并安排轮换。
- 前端默认只通过网关访问业务服务；直接访问服务端口必须是明确的本地诊断例外。
- API 路由、响应包络、令牌字段和错误格式在前后端之间必须有单一、可审查的契约；未确认的 AgileConfig 路由不得作为联调依据。

## 长期技术债与优化方向

1. 收敛网关路由配置：为 AgileConfig 路由建立脱敏、可审查的版本化结构或导出校验，确保前缀、路径转换、目标服务名和容器编排一致。
2. 统一前端 API 基址：集中管理网关地址，移除新旧客户端之间分散的 `localhost` 回退和直接服务地址，建立按服务域划分的 API 模块。
3. 统一认证契约：对齐后端 `TokenResponse`、前端 `LoginResponse`/`TokenPair`、刷新令牌返回值、过期时间和错误包络；同步对齐注册密码规则。
4. 补齐前后端契约测试：后端增加服务/网关集成测试，前端让 Vitest 依赖和测试脚本真实可执行，并覆盖 401 刷新、429、错误包络和关键业务流程。
5. 迁移旧业务 API：核对前端仍使用的 `user/*`、`word/*`、`dict/*` 等路径，逐一映射到当前 Identity/Vocabulary/Practice API，确认后再删除或隔离旧封装。
6. 清理解决方案漂移：统一根 `.sln` 与 `src/LexiCraft.slnx` 的职责，处理旧项目清单和活动项目清单不一致的问题。
7. 收敛配置与凭据：连接字符串、AgileConfig 参数、OAuth 和 OSS 凭据不再提交到仓库；部署文档使用占位符，并安排已暴露凭据轮换。
8. 分拆超大活跃文件：`MinioOssService` 已按客户端初始化、Minio 管理与策略、Bucket、Object 拆为同一 `partial` 类型，公开契约和实现逻辑保持不变；当前仍需处理后端 `CacheService.cs`（923 行）、`DistributedCacheService.cs`（841 行）及 6 个超过 800 行的前端 Vue 文件。缓存拆分前先补锁、降级、TTL、Hash 和序列化测试，前端拆分必须配合真实页面核验；不为满足行数规则进行无关重构。
9. 评估生产探针与可观测性：当前默认健康检查端点主要在 Development 映射，生产暴露策略需要结合部署平台明确配置。

## 构建与契约陷阱

- `BuildingBlocks.EventBus` 项目目录可能残留已删除子项目的 `Tests/obj` 等生成文件；父项目必须排除任意层级的 `bin/obj`，否则 SDK 默认源码通配符会编译嵌套生成的程序集属性并触发 `CS0579`。
- Identity 前端客户端只暴露当前后端端点能够证明的能力；邮箱验证、密码重置、OAuth 绑定和会话管理等功能必须先有后端契约和测试，不能先在前端添加猜测式方法。
