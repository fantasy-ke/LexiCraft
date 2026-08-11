# LexiCraft 项目长期记忆

## 记录范围

本文件只保存稳定的项目事实、架构决策、长期风险和已经确认的协作约定。临时调试过程、当日命令输出和未确认假设写入 `.agents/memory/YYYY-MM-DD.md`。

## 稳定架构事实

- 当前默认开发解决方案是 `src/LexiCraft.slnx`，默认运行入口是 `src/LexiCraft.Aspire.Host/AppHost.cs`。
- 根目录 `LexiCraft.sln` 属于较早的解决方案文件，项目清单与 `src/LexiCraft.slnx` 存在潜在漂移；涉及构建时优先使用 `.slnx`，并在发现差异时记录。
- Aspire Host 目标框架为 `net10.0`，当前使用 `Aspire.AppHost.Sdk/13.4.6`。
- 系统由 API Gateway、Identity、Vocabulary、Practice、Files gRPC 和 Aspire ServiceDefaults 组成；`src/BuildingBlocks/` 提供跨服务基础能力。
- Identity 使用 PostgreSQL 与 Redis；Vocabulary 使用 PostgreSQL 与 Redis；Practice 使用 MongoDB 与 Redis；Files 使用 OSS 抽象并提供 gRPC/内容读取能力。
- Files 服务保留 protobuf-net Code First gRPC，并在 `api/v{version:apiVersion}/files` 下提供 8 个 v1 Minimal API HTTP 门面；Code First `[OperationContract]` 不会自动成为 OpenAPI 路径。版本化 HTTP 门面使用 Scalar/OpenAPI 展示，原 `/content` 兼容路由继续保留但不进入文档。
- 公共运行能力通过 `AddServiceDefaults()` 提供，包括 AgileConfig 接入、服务发现、HttpClient resilience、健康检查和 OpenTelemetry。
- 网关通过 YARP 的 `ReverseProxy` 配置转发请求，并附带限流、安全头和 CORS。仓库内的 `ReverseProxy` 节点目前是空结构，真实路由来源必须在运行环境或 AgileConfig 中确认，不能从 README 推断。
- 后端模块前缀由代码定义为：`api/v{version:apiVersion}/identity`、`api/v{version:apiVersion}/vocabulary`、`api/v{version:apiVersion}/practice`、`api/v{version:apiVersion}/files`。
- Identity、Vocabulary、Practice 业务端点通常经过 `ResultEndPointFilter` 包装为 `ResultDto`；Files 的版本化 HTTP 门面直接返回文件 DTO、列表或文件流，不使用 `ResultDto`。
- 认证采用 JWT Bearer 和权限声明；前端应使用 `Authorization: Bearer`，并统一令牌字段和刷新策略。
- HTTP 响应 DTO 不应直接暴露 `UserId` 等领域值对象；用户 ID 在传输层使用基础 `Guid`。前端归一化逻辑暂时兼容旧 `{ value }` 或 `{ Value }` 结构，并在路径参数中进行 URL 编码。
- OAuth 创建的用户仍须满足 `PasswordHash` 非空约束：未提供本地密码时生成不可知随机值并写入 BCrypt 哈希，不修改数据库约束，也不向客户端返回随机明文。
- 前端是独立的 Vue 3/Vite 工程，使用 TypeScript、Pinia、Vue Router、Axios、UnoCSS、Vue Macros 和自动组件/图标导入；前端同时存在新认证客户端与旧业务 API 封装。
- 前端主题由 `src/UIs/lexicraft-vue-frontend/src/assets/css/themes.scss` 的语义变量统一管理，支持 Editorial、Zen、Playful Ink 三种内部主题及明亮、暗色、跟随系统模式；主题状态由 setting store 的 `themeStyle` 和 `src/hooks/theme.ts` 应用到根元素属性。
- 首页和登录页固定使用 Editorial 视觉，不随内部主题切换。内部布局中 Editorial 使用左侧书签式导航，Zen 使用顶部命令栏，仅 Playful Ink 使用底部浮动托盘。
- Vue scoped 样式结合根主题属性时，完整选择器必须放入 `:global(...)`，例如 `:global(html[data-theme-style=...] .selector)`；拆写为 `:global(html[...]) .selector` 会错误影响根元素的 opacity 或 transform。
- 新品牌标识由 `src/UIs/lexicraft-vue-frontend/src/components/BrandLogo.vue` 提供。新核心表面优先复用品牌组件和 `DoodleIcon.vue`，但不为统一图标而批量改写历史业务页面。
- MassTransit 采用显式启用策略：未配置 `MassTransit` 节或 `Enabled=false` 时，`AddCustomMassTransit` 不注册消息总线、事件发布器和本地事件后台服务；Identity 与 Practice 因存在发布、Saga 或事件溯源行为而显式启用，Vocabulary 当前无发布或消费行为，不注册 MassTransit。
- `src/BuildingBlocks/` 下 13 个项目统一以 `net10.0` 为目标框架；仓库 SDK 基线为 `10.0.302`，`global.json` 使用 `latestFeature` 且禁用预览 SDK。
- .NET 10 OpenAPI 版本化使用 `Asp.Versioning.OpenApi` 10.2.1 和 `MapOpenApi().WithDocumentPerVersion()`；受该包 `<3.0.0` 依赖约束，`Microsoft.OpenApi` 固定在最高兼容的 2.x 正式版 2.11.0。
- MassTransit 9 已进入商业许可版本线；在项目未确认商业许可前，BuildingBlocks 保持最高 8.x 正式版 8.5.10，不把“最高正式版”误解为无条件跨主版本升级。

## 已确认的长期协作约定

- 新任务开始前读取本文件和最新每日记忆，记录 `git status --short`，保留用户已有修改。
- 每个自然日创建一个新的 `.agents/memory/YYYY-MM-DD.md`；同日任务追加，不覆盖。
- 任务完成并通过必要验证后默认创建 Git 提交，使用 `<type>: <中文描述>`，只暂存本次任务文件，不推送。
- 不在代码、配置、文档或记忆中复制真实凭据；仓库已存在的敏感配置应通过后续任务迁移到环境变量、用户机密或部署平台密钥管理，并安排轮换。
- 前端默认只通过网关访问业务服务；直接访问服务端口必须是明确的本地诊断例外。
- API 路由、响应包络、令牌字段、基础类型和错误格式在前后端之间必须有单一、可审查的契约；未确认的 AgileConfig 路由不得作为联调依据。
- 连接地址或端口迁移应覆盖 Aspire Host、网关、各服务配置、Compose、部署文档及相关 BuildingBlocks 默认值和示例；不得误改包含相同数字文本的业务静态数据。

## 长期技术债与优化方向

1. 收敛网关路由配置：为 AgileConfig 路由建立脱敏、可审查的版本化结构或导出校验，确保前缀、路径转换、目标服务名和容器编排一致。
2. 统一前端 API 基址：集中管理网关地址，移除新旧客户端之间分散的 `localhost` 回退和直接服务地址，建立按服务域划分的 API 模块。
3. 统一认证契约：对齐后端 `TokenResponse`、前端 `LoginResponse`/`TokenPair`、刷新令牌返回值、过期时间和错误包络；同步对齐注册密码规则。
4. 补齐前后端契约测试：后端增加服务/网关集成测试，前端让 Vitest 依赖和测试脚本真实可执行，并覆盖 401 刷新、429、错误包络和关键业务流程。
5. 迁移旧业务 API：核对前端仍使用的 `user/*`、`word/*`、`dict/*` 等路径，逐一映射到当前 Identity/Vocabulary/Practice API，确认后再删除或隔离旧封装。
6. 清理解决方案漂移：统一根 `.sln` 与 `src/LexiCraft.slnx` 的职责，处理旧项目清单和活动项目清单不一致的问题。
7. 收敛配置与凭据：连接字符串、AgileConfig 参数、OAuth、RabbitMQ 和 OSS 凭据不再提交到仓库；部署文档使用占位符，并安排已暴露凭据轮换。
8. 分拆超大活跃文件：`MinioOssService` 已按客户端初始化、Minio 管理与策略、Bucket、Object 拆为同一 `partial` 类型，公开契约和实现逻辑保持不变；当前仍需处理后端 `CacheService.cs`（923 行）、`DistributedCacheService.cs`（841 行）及 6 个超过 800 行的前端 Vue 文件。缓存拆分前先补锁、降级、TTL、Hash 和序列化测试，前端拆分必须配合真实页面核验；不为满足行数规则进行无关重构。
9. 评估生产探针与可观测性：当前默认健康检查端点主要在 Development 映射，生产暴露策略需要结合部署平台明确配置。
10. 清理前端既有构建债务：`build-tsc` 当前受多处 Vue/TypeScript 类型错误和缺失的 `vitest` 类型阻塞；Vite 仍存在静态/动态重复导入和主分块过大的提示。

## 构建与契约陷阱

- `BuildingBlocks.EventBus` 项目目录可能残留已删除子项目的 `Tests/obj` 等生成文件；父项目必须排除任意层级的 `bin/obj`，否则 SDK 默认源码通配符会编译嵌套生成的程序集属性并触发 `CS0579`。
- Identity 前端客户端只暴露当前后端端点能够证明的能力；邮箱验证、密码重置、OAuth 绑定和会话管理等功能必须先有后端契约和测试，不能先在前端添加猜测式方法。
- Vite 动态导入需要将忽略注释放在参数内部：`import(/* @vite-ignore */ url)`；放在调用上一行不能抑制动态 URL 分析警告。
- 浏览器控制台中调用栈完全位于 `moz-extension://` 的错误应先按扩展问题隔离；已确认 Immersive Translate 的 `dynamic-i18n version mismatch` 不属于项目主题代码，不应向主题实现加入第三方扩展规避逻辑。
- 常用验证命令包括 `dotnet build src\LexiCraft.slnx --no-restore`、`npm --prefix src\UIs\lexicraft-vue-frontend run build`、`npm --prefix src\UIs\lexicraft-vue-frontend run build-tsc`、`docker compose -f src\compose.yaml config --quiet` 和 `git diff --check`。应区分任务引入的失败与仓库既有警告或类型错误。
