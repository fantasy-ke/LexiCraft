# LexiCraft 项目长期记忆

## 记录范围

本文件只保存稳定的项目事实、架构决策、长期风险和已经确认的协作约定。临时调试过程、当日命令输出和未确认假设写入 `.agents/memory/YYYY-MM-DD.md`。

## 稳定架构事实

- 当前默认开发解决方案是 `src/LexiCraft.slnx`，只显式包含生产项目；所有测试项目统一放入 `src/LexiCraft.Tests.slnx`。当前测试解决方案包含 Authorization、Caching、Messaging、Persistence 四个基础组件测试项目。默认运行入口是 `src/LexiCraft.Aspire.Host/AppHost.cs`。
- 仓库当前不跟踪根目录 `LexiCraft.sln`；生产项目构建使用 `src/LexiCraft.slnx`，测试构建与执行使用 `src/LexiCraft.Tests.slnx`。
- Aspire Host 目标框架为 `net10.0`，当前使用 `Aspire.AppHost.Sdk/13.4.6`。
- 系统由 API Gateway、Identity、Vocabulary、Practice、Files gRPC 和 Aspire ServiceDefaults 组成；`src/BuildingBlocks/` 提供跨服务基础能力。
- Identity 使用 PostgreSQL 与 Redis；Vocabulary 使用 PostgreSQL 与 Redis；Practice 使用 MongoDB 与 Redis；Files 使用 OSS 抽象并提供 gRPC/内容读取能力。
- Files 服务保留 protobuf-net Code First gRPC，并在 `api/v{version:apiVersion}/files` 下提供 8 个 v1 Minimal API HTTP 门面；Code First `[OperationContract]` 不会自动成为 OpenAPI 路径。版本化 HTTP 门面使用 Scalar/OpenAPI 展示，原 `/content` 兼容路由继续保留但不进入文档。
- OSS 基础库仅支持通过 `Providers`、`DefaultProvider` 和字符串 `Type` 配置命名实例，旧根级单提供商字段与 `OSSProvider` 枚举已移除；`IOSSServiceFactory` 按名称缓存实例，`AddOssProvider<TService>(type)` 用于扩展新提供商，不再由工厂维护厂商 `switch`。Provider 构造函数统一接收 `OSSProviderOptions`。OSS 禁用时仍注册可解析的 `IOSSService`，Files 据 `DefaultBucket` 回退本地存储；业务层不通过工厂读取 `AccessKey`、`SecretKey` 等连接凭据。
- 公共运行能力通过 `AddServiceDefaults()` 提供，包括 AgileConfig 接入、服务发现、HttpClient resilience、健康检查和 OpenTelemetry。
- 网关通过 YARP 的 `ReverseProxy` 配置转发请求，并附带限流、安全头和 CORS。仓库内的 `ReverseProxy` 节点目前是空结构，真实路由来源必须在运行环境或 AgileConfig 中确认，不能从 README 推断。
- 后端模块前缀由代码定义为：`api/v{version:apiVersion}/identity`、`api/v{version:apiVersion}/vocabulary`、`api/v{version:apiVersion}/practice`、`api/v{version:apiVersion}/files`。
- Identity、Vocabulary、Practice 业务端点通常经过 `ResultEndPointFilter` 包装为 `ResultDto`；Files 的版本化 HTTP 门面直接返回文件 DTO、列表或文件流，不使用 `ResultDto`。
- 认证采用 JWT Bearer 和权限声明；前端应使用 `Authorization: Bearer`，并统一令牌字段和刷新策略。
- 授权采用“各服务本地验证 JWT、Identity 集中验证当前会话与权限”的边界：Practice、Vocabulary、Files 携带原始 Bearer Token 调用 Identity 内部权限验证端点，不直接读取授权 Redis 或 Identity 数据库。
- 全量权限树由 `LexiCraft.Shared` 的 `LexiCraftPermissionDefinitionProvider` 统一注册；权限使用精确匹配，父节点只作分组，不隐式授予子权限；管理员旁路只依据持久化的 `admin` 角色。
- Identity 使用独立命名 Redis 实例 `OAuthRedis` 保存哈希后的令牌会话和用户权限完整快照；会话/刷新令牌使用 `authorization:v2:*` 版本化键，旧格式不会被新验证器读取；授权缓存禁用进程本地副本，权限回源和变更使用分布式锁，Redis 或 Identity 不可用时授权链路 fail closed 并返回 503。
- Files 的 8 个版本化 HTTP 端点已纳入权限验证；公开 `/uploads`、旧 `/content` 和 Code First gRPC 是兼容边界，只能用于公开内容或内部网络，不能作为私有文件对外入口。
- HTTP 响应 DTO 不应直接暴露 `UserId` 等领域值对象；用户 ID 在传输层使用基础 `Guid`。前端归一化逻辑暂时兼容旧 `{ value }` 或 `{ Value }` 结构，并在路径参数中进行 URL 编码。
- OAuth 创建的用户仍须满足 `PasswordHash` 非空约束：未提供本地密码时生成不可知随机值并写入 BCrypt 哈希，不修改数据库约束，也不向客户端返回随机明文。
- 前端是独立的 Vue 3/Vite 工程，使用 TypeScript、Pinia、Vue Router、Axios、UnoCSS、Vue Macros 和自动组件/图标导入；前端同时存在新认证客户端与旧业务 API 封装。
- 前端主题由 `src/UIs/lexicraft-vue-frontend/src/assets/css/themes.scss` 的语义变量统一管理，支持 Editorial、Zen、Playful Ink 三种内部主题及明亮、暗色、跟随系统模式；主题状态由 setting store 的 `themeStyle` 和 `src/hooks/theme.ts` 应用到根元素属性。
- 首页和登录页固定使用 Editorial 视觉，不随内部主题切换。内部布局中 Editorial 使用左侧书签式导航，Zen 使用顶部命令栏，仅 Playful Ink 使用底部浮动托盘。
- Vue scoped 样式结合根主题属性时，完整选择器必须放入 `:global(...)`，例如 `:global(html[data-theme-style=...] .selector)`；拆写为 `:global(html[...]) .selector` 会错误影响根元素的 opacity 或 transform。
- 新品牌标识由 `src/UIs/lexicraft-vue-frontend/src/components/BrandLogo.vue` 提供。新核心表面优先复用品牌组件和 `DoodleIcon.vue`，但不为统一图标而批量改写历史业务页面。
- MassTransit 采用显式启用策略：未配置 `MassTransit` 节或 `Enabled=false` 时，`AddCustomMassTransit` 不注册消息总线、事件发布器和本地事件后台服务；Identity 与 Practice 因存在发布、Saga 或事件溯源行为而显式启用，Vocabulary 当前无发布或消费行为，不注册 MassTransit。
- `BuildingBlocks.EventBus` 与 MassTransit 本地领域事件都使用默认容量 `1024` 的有界 Channel；队列满时发布方异步等待并接受取消，Host 停止时关闭写入端。EventBus 的 Redis Pub/Sub 消费回调写入有界缓冲区，过载时丢弃新消息并告警；需要持久化、重试或错误队列的跨服务事件必须使用 MassTransit/RabbitMQ。
- MassTransit 的 Saga 与 Event Sourcing 均默认关闭；事件溯源启用后使用独立 Redis 连接和分页流式回放，不复用缓存或其他消息组件的 `IConnectionMultiplexer`。Redis Stream 的版本检查与多事件追加不是跨进程原子事务，业务必须采用单写者、Lua/锁或等价协调，并自行管理不会破坏聚合重建的保留策略。
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
6. 维护解决方案边界：生产项目使用 `src/LexiCraft.slnx`，全部测试项目使用 `src/LexiCraft.Tests.slnx`；后续应在自动化门禁中校验测试项目无遗漏且不回流主解决方案。
7. 收敛配置与凭据：连接字符串、AgileConfig 参数、OAuth、RabbitMQ 和 OSS 凭据不再提交到仓库；部署文档使用占位符，并安排已暴露凭据轮换。
8. 分拆超大活跃文件：`MinioOssService` 已按客户端初始化、Minio 管理与策略、Bucket、Object 拆为同一 `partial` 类型，公开契约和实现逻辑保持不变；当前仍需处理后端 `CacheService.cs`（923 行）、`DistributedCacheService.cs`（841 行）及 6 个超过 800 行的前端 Vue 文件。缓存拆分前先补锁、降级、TTL、Hash 和序列化测试，前端拆分必须配合真实页面核验；不为满足行数规则进行无关重构。
9. 评估生产探针与可观测性：当前默认健康检查端点主要在 Development 映射，生产暴露策略需要结合部署平台明确配置。
10. 清理前端既有构建债务：`build-tsc` 当前受多处 Vue/TypeScript 类型错误和缺失的 `vitest` 类型阻塞；Vite 仍存在静态/动态重复导入和主分块过大的提示。
11. 授权长期应从多服务共享对称 JWT 密钥迁移到 Identity 私钥签名、业务服务仅持公钥的非对称方案，降低单个业务服务泄漏后伪造令牌的风险。
12. Identity 权限验证端点必须限制为服务网络访问；生产环境还需为 Identity 和授权 Redis 提供高可用、延迟/503 监控与故障演练，因为受保护业务请求同步依赖该链路。
13. 授权发布前必须在真实 Aspire/部署环境完成登录、并发刷新、旧令牌失效、赋权撤权、多实例缓存一致性和 Redis/Identity 故障的端到端验收。
14. `authorization:v2:*` 会话键升级会使旧 access/refresh token 失效；部署时必须协调更新并重启全部 Identity 实例，不能让新旧会话格式长期混跑，用户需重新登录。

### 持久化基础组件约定

- EF/PostgreSQL 的 `AuditableEntityInterceptor` 必须由当前 DbContext scope 解析，禁止在注册阶段调用 `BuildServiceProvider()`；软删除仅标记 `IsDeleted` 和删除审计字段为已修改，避免脱离跟踪实体覆盖其他列。
- PostgreSQL 启动迁移或 seed 失败必须终止启动并向上抛出；所有数据库、仓储、工作单元和 seed 异步调用需要向底层传播 `CancellationToken`。
- MongoDB BSON convention/serializer 是进程级全局状态，只初始化一次，并保持嵌套文档 camelCase、枚举字符串和历史 Guid 表示兼容；传入的配置节名称必须同时用于注册与绑定。Mongo 异常 mapper 应替换默认主映射器并对非 Mongo 异常保留默认 fallback。事务内读写必须绑定同一 session，且只在提交或回滚成功后释放。
- MongoDB 写操作重试不能替代业务幂等；具体命令仍需唯一索引、幂等键或事务保护。进程内性能指标必须有界，不能无限保留操作记录。
- 公共持久化端口统一位于 `BuildingBlocks.Persistence.Abstractions` 的 `Repositories` / `Transactions` 命名空间；`BuildingBlocks` 根类库不引用 EF Core 或 MongoDB 驱动。业务项目若直接声明或注入仓储/工作单元，应直接引用抽象项目。
- EF、PostgreSQL、Mongo 的类型目录与命名空间按 `Abstractions`、`Repositories`、`Transactions`、`Configuration`、`Migrations`、`Context`、`Entities`、`Resilience` 等职责组织，不再把公共类型堆放在项目根目录。
- Mongo 只保留 `MongoQueryRepository<TEntity>` / `MongoRepository<TEntity>` 单一仓储层级：非事务读取可通过 `IMongoResilienceService` 重试，事务内操作绑定同一 session 且不局部重试，写入不做应用层盲重试。Mongo 专属 resilience 注册不能覆盖容器中的通用 `IResilienceService`。
- Practice 以 `PracticeTask` 为 Mongo 聚合写入边界，`AnswerRecord` 和 `PracticeTaskItem` 内嵌保存；Context 与具体仓储必须共享显式 `PracticeTasksCollectionName = "practice_tasks"`，不得注册没有写入路径的独立集合仓储。当前真实调用仅插入、按 `_id` 获取和按 `_id` 替换，内置 `_id` 索引已覆盖；新的查询接口和复合索引必须由实际端点、分页/排序契约及 `explain()` 证据驱动，禁止为假设需求预建。
- Mongo 索引初始化不得通过捕获 scoped Context 的 fire-and-forget `Task.Run` 执行；需要自动迁移时必须受 Host 生命周期管理且失败可见，线上历史集合/索引删除仍作为有统计证据和回滚方案的独立运维任务。

### 缓存与授权基础组件约定

- `BuildingBlocks.Authorization` 是无 Redis 的授权核心；Identity 专用 Redis 实现统一位于 `BuildingBlocks.Authorization.Redis`。Practice、Vocabulary、Files 等业务服务不得引用 Redis 适配层，也不得传递引入 StackExchange.Redis/MemoryPack。
- `IUserContext` 位于通用 `BuildingBlocks.Contexts`，供授权和 EF 审计共同使用；持久化项目不得为了当前用户抽象依赖授权实现。
- 授权核心目录按 `Abstractions`、`Contexts`、`Options`、`Permissions`、`Policies`、`Tokens` 组织；缓存目录按 `Abstractions`、`Options`、`Locking`、`Redis`、`Services`、`Internal` 组织。公共 API 只保留接口、配置、扩展点和注册入口，具体 DI/Redis/序列化实现默认 internal。
- Identity 使用本地权威权限检查和授权 Redis；业务服务使用 Identity API 远程验证。未知权限必须先拒绝再判断管理员角色；缺失、非 Bearer 或空 Token Header 不应产生远程验证请求；依赖不可用必须关闭式失败。
- Redis 缓存未命中必须用显式状态表达，不能依赖 `default(T)`；同一次缓存操作只解析一次选项，锁后二次读取复用同一选项，本地缓存键必须包含 Redis 实例名，调用方取消不能被 `HideErrors` 吞掉。
- 每个命名 Redis 实例共享一个 `ConnectionMultiplexer`，不建立自定义连接池；Hash 数据和 TTL 应原子提交，内部时间戳必须参与部分字段读取时的有效性判断。分布式锁仅适合短缓存重建临界区，不是 Redlock，且当前没有自动续租。
- 所有 `*.Tests.csproj` 必须显式加入 `src/LexiCraft.Tests.slnx`，且不得加入 `src/LexiCraft.slnx`；全量回归使用 `dotnet test src\LexiCraft.Tests.slnx`，避免主解决方案构建混入测试类库或测试解决方案漏项。
- 真实 Redis/Aspire 多副本验证、授权链路压测、Redis 异步预热、CancellationToken 工厂重载、服务间身份、JWT 非对称签名和命名空间统一均为独立任务；构建和单元测试不能替代运行时证明。
### 幂等基础组件约定

- `BuildingBlocks.Idempotency` 是纯基础组件：通过 `IdempotentAttribute`（作用于 Class/Method）声明策略，由 `IdempotencyMiddleware` 在路由匹配后、端点执行前做幂等控制；默认 Redis 存储 `RedisIdempotencyStore` 依赖 `BuildingBlocks.Caching` 的 `IRedisConnectionFactory`，因此 `AddIdempotency()` 必须在 `AddCaching()` 之后调用，或先注册自定义 `IIdempotencyStore`。
- 三种模式 `Replay`/`Reject`/`Lock` 的行为与响应码（200/400/409/413，重放带 `Idempotency-Replayed: true`，进行中带 `Retry-After: 1`）以代码与组件 README 为准；截至 2026-08-17 该组件已完成实现与单元测试（12 项通过），但**尚未接入** Identity/Vocabulary/Practice/Files 业务端点。

## 构建与契约陷阱

- `BuildingBlocks.EventBus` 项目目录可能残留已删除子项目的 `Tests/obj` 等生成文件；父项目必须排除任意层级的 `bin/obj`，否则 SDK 默认源码通配符会编译嵌套生成的程序集属性并触发 `CS0579`。
- Identity 前端客户端只暴露当前后端端点能够证明的能力；邮箱验证、密码重置、OAuth 绑定和会话管理等功能必须先有后端契约和测试，不能先在前端添加猜测式方法。
- Vite 动态导入需要将忽略注释放在参数内部：`import(/* @vite-ignore */ url)`；放在调用上一行不能抑制动态 URL 分析警告。
- 浏览器控制台中调用栈完全位于 `moz-extension://` 的错误应先按扩展问题隔离；已确认 Immersive Translate 的 `dynamic-i18n version mismatch` 不属于项目主题代码，不应向主题实现加入第三方扩展规避逻辑。
- 常用验证命令包括 `dotnet build src\LexiCraft.slnx --no-restore`、`dotnet test src\LexiCraft.Tests.slnx --no-restore`、`npm --prefix src\UIs\lexicraft-vue-frontend run build`、`npm --prefix src\UIs\lexicraft-vue-frontend run build-tsc`、`docker compose -f src\compose.yaml config --quiet` 和 `git diff --check`。应区分任务引入的失败与仓库既有警告或类型错误。

## Pipeline Consolidated Memories
- [2026-08-11] # 2026-08-10 内网连接配置统一
- [2026-08-11] ## 需求
- [2026-08-11] 将仓库运行配置和部署说明中的旧内网地址统一迁移到用户指定的新内网地址。
- [2026-08-11] 将 MongoDB 实际连接、基础组件默认值和示例端口统一调整为用户指定的自定义端口。
- [2026-08-11] 保留仓库已有的 Secret、密码及其他端口，不扩展到业务代码重构。
- [2026-08-11] ## 代码事实
- [2026-08-11] 旧地址分布在 Aspire Host、API Gateway、Identity、Vocabulary、Practice、Files、Compose 和 Docker 部署说明中。
- [2026-08-11] MongoDB 端口除实际服务连接外，还存在于 MassTransit Saga 默认配置及两个基础组件 README 示例中。
- [2026-08-11] 前端词典静态数据中可能包含相同数字文本，不属于连接配置，未修改。
- [2026-08-11] ## 实现与文档变更
- [2026-08-11] 统一替换所有已跟踪配置和部署说明中的旧内网地址。
- [2026-08-11] 将 MongoDB URI、MassTransit Saga 默认连接和 MongoDB 示例端口统一调整为新端口。
- [2026-08-11] Redis、PostgreSQL、AgileConfig 等服务端口保持不变。
- [2026-08-11] 未删除或修改现有 Secret、用户名和密码字段。
- [2026-08-11] ## 涉及文件
- [2026-08-11] `src/LexiCraft.Aspire.Host/appsettings.json`
- [2026-08-11] `src/ApiGateway/LexiCraft.ApiGateway/appsettings.json`
- [2026-08-11] `src/compose.yaml`
- [2026-08-11] `src/README_DOCKER_DEPLOY.md`
- [2026-08-11] Identity、Vocabulary、Practice、Files 服务的 `appsettings*.json`
- [2026-08-11] `src/BuildingBlocks/BuildingBlocks.MassTransit/Options/MassTransitOptions.cs`
- [2026-08-11] `src/BuildingBlocks/BuildingBlocks.MassTransit/README.md`
- [2026-08-11] `src/BuildingBlocks/BuildingBlocks.MongoDB/README.md`
- [2026-08-11] ## 验证结果
- [2026-08-11] 旧内网地址全仓库检索无剩余。
- [2026-08-11] 后端、网关、部署配置及基础组件中不再存在旧 MongoDB 端口。
- [2026-08-11] 7 个受影响 JSON 配置均通过 JSON 解析。
- [2026-08-11] `docker compose -f src\compose.yaml config --quiet` 通过。
- [2026-08-11] `dotnet build src\BuildingBlocks\BuildingBlocks.MassTransit\BuildingBlocks.MassTransit.csproj --no-restore` 通过，0 个错误；保留既有 NuGet 多源映射警告。
- [2026-08-11] `git diff --check` 通过。
- [2026-08-11] ## 已知限制与待办
- [2026-08-11] 本次仅完成静态配置、Compose 结构和定向编译验证，未实际连接内网 Redis、PostgreSQL、MongoDB 或 AgileConfig。
- [2026-08-11] 未验证部署环境是否已开放新 MongoDB 端口，需在目标环境启动服务后执行连接健康检查。
- [2026-08-11] ## Git 提交
- [2026-08-11] 提交信息：`chore: 更新内网连接地址和 MongoDB 端口`；仅创建本地提交，不推送。
- [2026-08-11] # 2026-08-10 MassTransit 默认启用与 RabbitMQ 连接修复
- [2026-08-11] ## 需求
- [2026-08-11] 排查 Practice 与 Vocabulary 启动时主动连接 `rabbitmq://localhost/` 并因认证失败持续重试的问题。
- [2026-08-11] 确认未配置 MassTransit 时应默认禁用；实际使用消息总线的服务补齐配置，不使用的服务移除注册。
- [2026-08-11] ## 代码事实
- [2026-08-11] `AddCustomMassTransit` 原先无启用判断；缺少配置节时会使用 `localhost:5672` 与默认账号并注册、启动总线。
- [2026-08-11] Practice 的完成练习处理器通过 `IEventPublisher` 发布练习完成和错词集成事件，确实依赖 MassTransit。
- [2026-08-11] Vocabulary 未发现 `IEventPublisher`、`IPublishEndpoint`、Consumer、Saga 或 Activity 使用，仅在启动入口误注册并保留项目引用。
- [2026-08-11] 当前本机 RabbitMQ 容器监听 `172.31.21.1:5672`；端口可达，原账号配置与容器不一致。记忆中不记录具体凭据。
- [2026-08-11] ## 实现与文档变更
- [2026-08-11] 为 `MassTransitOptions` 增加默认关闭的 `Enabled`，公共扩展在未显式启用时直接返回，不注册总线及相关后台服务。
- [2026-08-11] Practice 显式启用 MassTransit，连接本机 RabbitMQ；其未使用 Saga 与事件溯源，因此明确关闭这两项附加能力。
- [2026-08-11] Vocabulary 移除 MassTransit 启动注册和无用项目引用。
- [2026-08-11] Identity 已有消息发布与事件溯源/Saga 使用，补充显式启用并对齐同一 RabbitMQ 实例配置，保持既有行为。
- [2026-08-11] README 补充 MassTransit 为 opt-in 的配置契约和 `Enabled=true` 示例。
- [2026-08-11] ## 涉及文件
- [2026-08-11] `src/BuildingBlocks/BuildingBlocks.MassTransit/Options/MassTransitOptions.cs`
- [2026-08-11] `src/BuildingBlocks/BuildingBlocks.MassTransit/Extensions/MassTransitExtensions.cs`
- [2026-08-11] `src/BuildingBlocks/BuildingBlocks.MassTransit/README.md`
- [2026-08-11] `src/microservices/Practice/LexiCraft.Services.Practice.Api/appsettings.json`
- [2026-08-11] `src/microservices/Vocabulary/LexiCraft.Services.Vocabulary.Api/Program.cs`
- [2026-08-11] `src/microservices/Vocabulary/LexiCraft.Services.Vocabulary/LexiCraft.Services.Vocabulary.csproj`
- [2026-08-11] `src/microservices/Identity/LexiCraft.Services.Identity.Api/appsettings.json`
- [2026-08-11] `.agents/MEMORY.md`
- [2026-08-11] ## 验证结果
- [2026-08-11] 独立服务注册检查通过：无配置时不存在 MassTransit Bus 注册，`Enabled=true` 时正常注册。
- [2026-08-11] RabbitMQ `172.31.21.1:5672` TCP 连通检查通过。
- [2026-08-11] Practice 实际启动日志出现 `Bus started: rabbitmq://172.31.21.1/`，未再出现本次启动的认证失败。
- [2026-08-11] Vocabulary 实际启动成功，日志中无 MassTransit/RabbitMQ 连接行为。
- [2026-08-11] BuildingBlocks.MassTransit、Practice API、Vocabulary API、Identity API 定向构建通过。
- [2026-08-11] `dotnet build src\LexiCraft.slnx --no-restore` 通过，0 个错误；保留既有 NuGet 多源映射警告。
- [2026-08-11] 两个修改后的 JSON 配置均通过解析。
- [2026-08-11] ## 已知限制与待办
- [2026-08-11] RabbitMQ 凭据仍保存在仓库配置中；本次按用户授权保留，后续应迁移到环境变量、用户机密或部署平台密钥管理。
- [2026-08-11] 当前没有现成后端测试项目；本次采用独立注册检查与真实服务启动冒烟验证覆盖回归行为。
- [2026-08-11] 工作区同时存在与本任务无关的 Identity 数据库迁移增删，已完整保留且不会纳入本次提交。
- [2026-08-11] ## Git 提交
- [2026-08-11] 提交信息：`fix: 修复 MassTransit 默认启动和 RabbitMQ 配置`；仅提交本任务文件，不推送。
- [2026-08-11] # 2026-08-10 Identity OAuth 保存与前端动态导入修复
- [2026-08-11] ## 需求
- [2026-08-11] 修复 Identity OAuth 创建用户时数据库拒绝空 `password_hash` 的问题。
- [2026-08-11] 保留现有 `PasswordHash` 非空字段约束及迁移文件，不通过修改数据库结构规避保存逻辑缺陷。
- [2026-08-11] 消除前端 `src/utils/index.ts` 的 Vite 动态导入分析警告。
- [2026-08-11] ## 代码事实
- [2026-08-11] OAuth 登录在找不到本地用户时调用 `CreateUserCommand`，密码参数明确传入 `null`。
- [2026-08-11] `CreateUserCommandHandler` 原实现仅在密码非空时调用 `User.SetPassword`，因此 OAuth 新用户的 `PasswordHash` 保持为空，并在统一 `SaveChangesAsync` 时触发 PostgreSQL 23502 非空约束异常。
- [2026-08-11] Vite 的 `@vite-ignore` 注释原本放在 `import(url)` 上一行，不符合 Vite 要求的参数内注释位置，因此无法抑制分析警告。
- [2026-08-11] ## 实现变更
- [2026-08-11] `CreateUserCommandHandler` 在未提供本地密码时使用 `RandomNumberGenerator` 生成 32 字节随机值，再统一调用 `SetPassword` 写入 BCrypt 哈希；随机明文不持久化、不返回客户端，OAuth 用户仍没有可知的本地密码。
- [2026-08-11] 将动态导入调整为 `import(/* @vite-ignore */ url)`。
- [2026-08-11] 未修改、暂存或提交任何 Identity 迁移文件。
- [2026-08-11] ## 涉及文件
- [2026-08-11] `src/microservices/Identity/LexiCraft.Services.Identity/Users/Internal/Commands/CreateUserCommand.cs`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/utils/index.ts`
- [2026-08-11] ## 验证结果
- [2026-08-11] `dotnet build src\microservices\Identity\LexiCraft.Services.Identity.Api\LexiCraft.Services.Identity.Api.csproj --no-restore` 通过，0 个错误；保留既有 NuGet 多源映射警告。
- [2026-08-11] `dotnet build src\LexiCraft.slnx --no-restore` 通过，0 个错误；保留既有 NuGet 多源映射警告。
- [2026-08-11] `npm --prefix src\UIs\lexicraft-vue-frontend run build` 通过，原 `vite:import-analysis` 动态 URL 警告不再出现；仍有既有静态/动态重复导入和大分块提示。
- [2026-08-11] `npm --prefix src\UIs\lexicraft-vue-frontend run build-tsc` 未通过，阻塞于项目既有的多处 Vue/TypeScript 类型错误和缺少 `vitest` 类型，不涉及本次两个修改文件。
- [2026-08-11] 本任务文件及全工作区 `git diff --check` 均通过。
- [2026-08-11] ## 已知限制与待办
- [2026-08-11] 未持有可用的第三方 OAuth 授权码，因此未执行 GitHub/Gitee 真实登录端到端验证；本次依据异常 SQL、调用链和定向构建验证保存前赋值逻辑。
- [2026-08-11] 前端构建刷新了原本已有并行修改的生成文件 `components.d.ts`；该文件不纳入本次提交。
- [2026-08-11] 工作区既有的 Identity 迁移增删、`package-lock.json`、Files SQLite 临时文件和其他并行修改均保留，不纳入本次提交。
- [2026-08-11] ## Git 提交
- [2026-08-11] 提交信息：`fix: 修复 OAuth 用户保存和动态导入警告`；仅提交本任务文件与当日记忆，不推送。
- [2026-08-11] # 2026-08-10 前端三套主题与首页登录页重设计
- [2026-08-11] ## 需求
- [2026-08-11] 为前端增加 A Warm Editorial、B Zen Focus、C Playful Ink 三套内部学习空间主题，并分别支持明亮、暗色和跟随系统模式。
- [2026-08-11] 首页和登录页固定使用 A 暖调书卷气视觉，不随内部 A/B/C 风格切换；登录页采用非传统的不对称编辑手稿布局。
- [2026-08-11] 加载界面、卡片、输入框、弹窗、按钮、布局导航等公共组件统一适配主题，保证文字与背景具备可读对比度。
- [2026-08-11] ## 代码事实与实现
- [2026-08-11] 新增 `src/UIs/lexicraft-vue-frontend/src/assets/css/themes.scss`，以语义化 CSS 变量集中定义 editorial、zen、ink 的明暗主题，并保留旧变量别名兼容既有页面。
- [2026-08-11] `setting` store 新增 `themeStyle`，默认 `editorial`；`theme.ts` 统一应用 `data-theme`、`data-theme-style` 和系统主题监听，避免覆盖其他根节点 class。
- [2026-08-11] 首页 `src/pages/home.vue` 改为固定 editorial 的阅读流式首页；登录页 `src/pages/(user)/login.vue` 改为固定 editorial 的非对称版式，同时保留账号、注册与 OAuth 入口。
- [2026-08-11] modern layout 增加三种内部导航形态：editorial 书签侧栏、zen 文本命令栏、ink 手绘浮动托盘；公共控件和设置页增加风格选择与明暗模式选择。
- [2026-08-11] 主题样式最初使用 Vue scoped 的 `:global(html[data-theme-style=...]) .selector` 形式，编译后会错误地把规则作用到 `html` 根节点，造成页面 opacity/transform 异常；已统一修正为完整的 `:global(html[data-theme-style=...] .selector)` 选择器，并通过浏览器检查确认根节点恢复正常。
- [2026-08-11] ## 涉及文件
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/App.vue`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/assets/css/main.scss`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/assets/css/themes.scss`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/components/` 下主题相关公共组件
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/hooks/theme.ts`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/layout/modern/` 下布局组件
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/pages/home.vue`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/pages/(user)/login.vue`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/stores/setting.ts`
- [2026-08-11] ## 验证结果
- [2026-08-11] `npm run build` 通过，1060 个模块构建成功。
- [2026-08-11] 浏览器实测首页和登录页在内部主题持久化为 ink/light 时仍保持 editorial 视觉；实测 editorial/dark、zen/dark、ink/dark、ink/light 的内部布局、字体、背景和控件可读。
- [2026-08-11] `git diff --check` 通过。
- [2026-08-11] `npm run build-tsc` 仍受项目既有 Vue/TypeScript 类型错误和缺少 `vitest` 类型阻塞，报错文件均不在本次主题改动范围内；未将其误判为本次改动引入。
- [2026-08-11] ## 已知限制与待办
- [2026-08-11] 本次未进行真实登录、OAuth 授权和后端 API 联调，只完成页面与主题运行时视觉核验。
- [2026-08-11] Vite 仍报告既有动态/静态导入分块提示及主 chunk 超过 500 kB 的提示，本次未扩大范围处理。
- [2026-08-11] ## Git 提交
- [2026-08-11] `feat: 增加三套学习空间主题并重设计首页登录页`，仅创建本地提交，不推送。
- [2026-08-11] # 2026-08-10 前端主题导航、品牌与学习页布局优化
- [2026-08-11] ## 需求
- [2026-08-11] 修复 Playful Ink 主题退出登录后的输入框外边框过重、占据背景水印的问题。
- [2026-08-11] 仅 Playful Ink 使用底部浮动菜单；Editorial 与 Zen 改为侧边/顶部导航，避免页面底部大面积空白。
- [2026-08-11] 设计并接入新的 LexiCraft Logo。
- [2026-08-11] 优化词汇与阅读页面的卡片、按钮、图标和间距，使布局更平整、清晰、协调。
- [2026-08-11] ## 实现
- [2026-08-11] 新增 `src/UIs/lexicraft-vue-frontend/src/components/BrandLogo.vue`，使用打开的书页、中心书脊和星光构成新品牌标识，并接入首页、登录页、现代布局头部及旧 Logo 包装组件。
- [2026-08-11] 调整 `LayoutSidebar.vue` 与 `LayoutContent.vue`：Editorial 使用左侧书签式导航，Zen 使用顶部命令栏，只有 Ink 预留底部浮动托盘空间；修正内容容器的 flex 高度与滚动关系。
- [2026-08-11] 调整 `BaseInput.vue` 与 `BaseButton.vue`：Ink 输入框改为低存在感单线边框和柔和焦点环，隔离登录页 Editorial 样式；按钮增加统一高度、内边距、圆角和图标对齐。
- [2026-08-11] 重做 `Book.vue`、`DictList.vue` 的网格及卡片尺寸；为词汇页和阅读页增加统一的学习页语义结构、响应式网格、进度与操作区样式。
- [2026-08-11] 仅为本地界面核验临时放开 `/app` 路由，核验结束已恢复为 `requiresAuth: true`，未纳入提交。
- [2026-08-11] ## 验证
- [2026-08-11] `npm --prefix src/UIs/lexicraft-vue-frontend run build` 通过，1063 个模块转换成功。
- [2026-08-11] `git diff --check` 通过；存在仓库既有的 LF/CRLF 提示，不影响检查结果。
- [2026-08-11] 浏览器已核验登录页 Editorial 固定视觉、新 Logo、Ink 输入框的 1px 边框、无旋转和无硬阴影；此前已核验 Ink 词汇页底部浮动菜单及无底部大空白。
- [2026-08-11] `npm --prefix src/UIs/lexicraft-vue-frontend run build-tsc` 仍被既有 Vue/TypeScript 类型错误和测试缺失的 `vitest` 类型阻断，报错集中在 `QuestionForm.vue`、基础分页/滑块、旧列表泛型、批量编辑、注册/VIP 等文件，不涉及本次修改文件。
- [2026-08-11] ## 已知限制
- [2026-08-11] 未执行真实账号登录、OAuth、后端 API 联调；本次范围为前端布局与主题视觉。
- [2026-08-11] Vite 仍提示既有动态/静态导入混用及主 chunk 超过 500 kB，未扩大范围处理。
- [2026-08-11] ## Git 提交
- [2026-08-11] 提交信息：`fix: 优化主题导航和学习页布局`
- [2026-08-11] 提交哈希：`a3f59bc`
- [2026-08-11] 仅创建本地提交，未推送；工作区中的 Files SQLite 临时文件和 `LexiCraft.sln.DotSettings.user` 未纳入本次提交。
- [2026-08-11] # 2026-08-10 Aspire 版本升级
- [2026-08-11] ## 需求
- [2026-08-11] 将项目使用的 .NET Aspire 升级到当前最新稳定版本，并保持本次变更范围最小。
- [2026-08-11] ## 代码事实
- [2026-08-11] `src/LexiCraft.Aspire.Host/LexiCraft.Aspire.Host.csproj` 原使用 `Aspire.AppHost.Sdk/13.1.0`。
- [2026-08-11] 项目目标框架为 `net10.0`；AppHost 当前使用外部 PostgreSQL、MongoDB、Redis 连接字符串，没有 `AddPostgres` 或 `WithDataVolume` 资源配置。
- [2026-08-11] Aspire 自动引用的 `Aspire.Hosting.AppHost`、`Aspire.Hosting.Orchestration.win-x64` 和 `Aspire.Dashboard.Sdk.win-x64` 会跟随 AppHost SDK 版本解析。
- [2026-08-11] ## 实现与验证
- [2026-08-11] 将 AppHost SDK 升级为 `Aspire.AppHost.Sdk/13.4.6`，未扩展到其他 NuGet 依赖升级。
- [2026-08-11] `dotnet restore src\LexiCraft.Aspire.Host\LexiCraft.Aspire.Host.csproj` 通过。
- [2026-08-11] `dotnet build src\LexiCraft.Aspire.Host\LexiCraft.Aspire.Host.csproj --no-restore` 通过：0 个错误。
- [2026-08-11] `dotnet list src\LexiCraft.Aspire.Host\LexiCraft.Aspire.Host.csproj package` 确认三个 Aspire 自动包均解析为 `13.4.6`。
- [2026-08-11] `dotnet build src\LexiCraft.slnx --no-restore` 通过：0 个错误。
- [2026-08-11] `git diff --check` 通过。
- [2026-08-11] ## 已知限制
- [2026-08-11] 构建仍报告仓库已有的 `Humanizer.Core.* 3.0.1` 与解析到的 `Humanizer.Core 3.0.10` 版本约束警告（51 条），本次未扩大范围处理。
- [2026-08-11] 未启动完整 Aspire 编排进行真实依赖联调，当前结论以还原、包解析和静态构建为依据。
- [2026-08-11] 工作区原有的 Files SQLite 文件改动及 `LexiCraft.sln.DotSettings.user` 未纳入本次提交。
- [2026-08-11] ## Git 提交
- [2026-08-11] 提交信息：`build: 升级 Aspire 到 13.4.6`
- [2026-08-11] # 2026-08-10 主题切换报错排查与权限用户 ID 传输修复
- [2026-08-11] ## 需求
- [2026-08-11] 排查切换主题时出现的 Immersive Translate `dynamic-i18n version mismatch: expected 6, got 5` 错误。
- [2026-08-11] 修复权限验证请求将用户 ID 拼接为 `[object Object]`，导致请求路径错误的问题。
- [2026-08-11] ## 代码事实
- [2026-08-11] 报错调用栈全部位于 `moz-extension://.../content_main.js`，仓库内不存在 `dynamic-i18n` 或 Immersive Translate 相关实现；项目主题切换只更新根元素的主题属性、明暗类名和 `colorScheme`。
- [2026-08-11] Identity 的用户资料响应原先直接暴露强类型 `UserId`，Minimal API 会将其序列化为对象；前端字段归一化后得到 `{ value: "..." }`，模板字符串拼接时被转换为 `[object Object]`。
- [2026-08-11] 更新用户资料响应复用了 `GetUserInfoResponse`，权限响应也使用相同的强类型 ID，因此需要同步收紧传输 DTO。
- [2026-08-11] ## 实现
- [2026-08-11] 将获取用户资料、更新用户资料和获取权限响应中的用户 ID 改为基础 `Guid`，避免领域值对象泄漏到 HTTP JSON 契约。
- [2026-08-11] 前端新增 `normalizeUserId`，优先接受字符串，同时兼容旧响应中的 `{ value }`/`{ Value }` 结构，并对权限请求路径参数执行 URL 编码。
- [2026-08-11] Auth Store 在保存用户资料和请求权限前统一归一化用户 ID，避免对象进入用户状态和 URL。
- [2026-08-11] 未修改主题实现；Immersive Translate 错误属于浏览器扩展自身版本状态，需通过更新、重载、清理扩展数据或在本地站点禁用扩展处理。
- [2026-08-11] ## 涉及文件
- [2026-08-11] `src/microservices/Identity/LexiCraft.Services.Identity/Users/Features/GetUserInfo/GetUserInfoEndpoint.cs`
- [2026-08-11] `src/microservices/Identity/LexiCraft.Services.Identity/Users/Features/UpdateUserInfo/UpdateUserInfoEndpoint.cs`
- [2026-08-11] `src/microservices/Identity/LexiCraft.Services.Identity/Permissions/Features/GetUserPermissions/GetUserPermissionsEndpoint.cs`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/apis/identity.ts`
- [2026-08-11] `src/UIs/lexicraft-vue-frontend/src/stores/auth.ts`
- [2026-08-11] ## 验证结果
- [2026-08-11] Identity API 定向构建通过，0 个警告、0 个错误。
- [2026-08-11] `dotnet build src\LexiCraft.slnx --no-restore` 通过，0 个错误；保留仓库既有的 51 条 Humanizer 版本约束警告。
- [2026-08-11] `npm --prefix src\UIs\lexicraft-vue-frontend run build` 通过，1063 个模块转换成功。
- [2026-08-11] `npm --prefix src\UIs\lexicraft-vue-frontend run build-tsc` 仍被仓库既有 Vue/TypeScript 类型错误和缺失的 `vitest` 类型阻断；错误未指向本次修改文件。
- [2026-08-11] `git diff --check` 通过。
- [2026-08-11] ## 已知限制
- [2026-08-11] 未使用真实登录账号执行权限接口联调；当前结论基于服务端响应契约、前端 URL 构造链路和构建结果。
- [2026-08-11] 浏览器扩展报错无法在项目代码内修复，且不应向主题逻辑注入针对第三方扩展的规避代码。
- [2026-08-11] 工作区原有 Files SQLite 文件改动和 `LexiCraft.sln.DotSettings.user` 未纳入本次任务。
- [2026-08-11] ## Git 提交
- [2026-08-11] 提交信息：`fix: 修复权限用户 ID 传输错误`。
- [2026-08-11] 仅创建本地提交，不推送。
- [2026-08-17] # 2026-08-16 工作记录
- [2026-08-17] ## 需求
- [2026-08-17] 继续并完成未完成的幂等处理基础组件任务，补齐实现、测试、解决方案接入与验证。
- [2026-08-17] ## 代码事实
- [2026-08-17] 新增 `BuildingBlocks.Idempotency`，通过 `IdempotencyMiddleware`、`IdempotentAttribute`、`IdempotencyOptions`、`IdempotencyMode` 和 `IIdempotencyStore` 提供幂等请求控制。
- [2026-08-17] Redis 实现使用 `BuildingBlocks.Caching` 暴露的 `IRedisConnectionFactory`，并依赖 `BuildingBlocks.Caching/Properties/AssemblyInfo.cs` 中的 `InternalsVisibleTo("BuildingBlocks.Idempotency")`。
- [2026-08-17] 中间件支持 `Replay`、`Reject`、`Lock` 三种策略，按用户、路径、方法和请求体生成指纹，并在响应缓存超过上限时降级为不可回放。
- [2026-08-17] ## 实现/文档变更
- [2026-08-17] 修复 `src/BuildingBlocks/BuildingBlocks.Idempotency/Internal/RedisIdempotencyStore.cs` 的残留转义和语法问题，保证 Redis 读写脚本与键构造可编译。
- [2026-08-17] 补充 `src/BuildingBlocks/BuildingBlocks.Idempotency.Tests`，覆盖缺少幂等头、首次执行、回放、冲突、等待完成、请求体超限、响应体超限和用户作用域隔离等场景。
- [2026-08-17] 将新项目接入 `src/LexiCraft.slnx` 和 `src/LexiCraft.Tests.slnx`。
- [2026-08-17] ## 涉及文件
- [2026-08-17] `src/BuildingBlocks/BuildingBlocks.Caching/Properties/AssemblyInfo.cs`
- [2026-08-17] `src/LexiCraft.slnx`
- [2026-08-17] `src/LexiCraft.Tests.slnx`
- [2026-08-17] `src/BuildingBlocks/BuildingBlocks.Idempotency/**`
- [2026-08-17] `src/BuildingBlocks/BuildingBlocks.Idempotency.Tests/**`
- [2026-08-17] ## 验证结果
- [2026-08-17] `dotnet build src/BuildingBlocks/BuildingBlocks.Idempotency.Tests/BuildingBlocks.Idempotency.Tests.csproj -v minimal` ✅
- [2026-08-17] `dotnet test src/BuildingBlocks/BuildingBlocks.Idempotency.Tests/BuildingBlocks.Idempotency.Tests.csproj --no-build -v minimal` ✅
- [2026-08-17] `dotnet build src/LexiCraft.slnx -v minimal` ✅
- [2026-08-17] `dotnet test src/LexiCraft.Tests.slnx -v minimal` ✅
- [2026-08-17] `git diff --check` ✅
- [2026-08-17] 现有 `OpenTelemetry` / `SQLitePCLRaw` 漏洞告警仍存在，属于仓库既有依赖问题。
- [2026-08-17] ## 已知限制
- [2026-08-17] 当前只完成基础组件与测试接入，尚未把 `AddIdempotency` 集成到具体 API 宿主或路由模块。
- [2026-08-17] `AddIdempotency` 仍要求先注册缓存/Redis 连接或自定义 `IIdempotencyStore`。
- [2026-08-17] ## 待办
- [2026-08-17] 需要时再把幂等中间件接入具体业务端点，并补充端到端冒烟验证。
- [2026-08-17] ## Git 提交信息
- [2026-08-17] 提交：`480a1bd`
- [2026-08-17] 消息：`feat: 新增幂等处理基础组件`
