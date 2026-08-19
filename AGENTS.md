# AGENTS.md

本文件适用于仓库根目录及其全部子目录。除非更深层目录存在独立的 `AGENTS.md`，否则所有自动化代理和协作者都必须遵循本规范。

## 1. 项目概况

LexiCraft 是一个以 .NET 微服务为后端、Vue 3 为前端的英语词汇与练习平台。代码事实优先于 README、部署文档和外部配置；文档与运行时行为不一致时，先记录差异，再以可验证的代码和运行配置为准。

### 1.1 默认入口与解决方案

- 生产解决方案：`src/Fantasy.slnx`；测试解决方案：`src/Fantasy.Tests.slnx`。两者拆分维护，构建生产代码用前者，跑测试用后者。
- 仓库中已不存在根目录 `LexiCraft.sln`，`src/LexiCraft.sln.DotSettings.user` 只是 Rider 的个人设置残留，不是解决方案。
- 默认编排入口：`src/LexiCraft.Aspire.Host/AppHost.cs`。
- API 网关入口：`src/ApiGateway/Fantasy.ApiGateway/`。
- 前端入口：`src/UIs/lexicraft-vue-frontend/`。

### 1.1.1 命名边界（2026-08-18 起生效）

命名迁移是分阶段的，当前真实状态如下，不要按“全仓统一品牌”的假设改名：

- 已改为 `Fantasy.*`：`src/microservices/Identity/Fantasy.Services.Identity{,.Api}`、`src/microservices/Files/Fantasy.Files.Grpc`、`src/microservices/Shared/Fantasy.Shared`、`src/Fantasy.Aspire.ServiceDefaults`、`src/ApiGateway/Fantasy.ApiGateway`、两个 `.slnx`。
- 保持 `LexiCraft.*` 不变：`src/microservices/Vocabulary/`、`src/microservices/Practice/`（目录、项目名、程序集名、命名空间、Aspire 资源名）、`src/LexiCraft.Aspire.Host/`、前端 `lexicraft-vue-frontend`。
- `src/BuildingBlocks/` 保持品牌中性的 `BuildingBlocks.*`。BuildingBlocks、Identity、Files 和 Gateway 的受控源码、项目文件及 README 不得保留旧项目、程序集或命名空间；唯一允许的旧品牌标识是兼容配置 AppId：`lexicraft-identity-api`、`lexicraft-files-grpc`、`lexicraft-api-gateway`。
- Aspire 资源名分别为 `fantasy-identity-api`、`fantasy-files-grpc`、`fantasy-api-gateway`；AgileConfig AppId 是独立的外部配置契约，继续使用上一条列出的旧值，不随资源名或程序集改名。
- Compose 服务名为 `fantasy.apigateway`、`identity-api`、`files-grpc`、`vocabulary-api`、`practice-api`；后四者的 DNS、HTTP 路由前缀和 Code First gRPC 契约都未随改名变化。

### 1.2 技术栈与服务边界

- 后端：.NET 10、ASP.NET Core Minimal API、C#、.NET Aspire。
- 网关：YARP 反向代理，附带限流、安全响应头和 CORS。
- 数据：Identity 与 Vocabulary 使用 PostgreSQL；Practice 使用 MongoDB；Redis 用于缓存及部分分布式能力。
- 通信：服务默认配置提供服务发现、HttpClient resilience、健康检查和 OpenTelemetry；项目同时存在 `BuildingBlocks.EventBus` 与 `BuildingBlocks.MassTransit`，新增跨服务消息前先确认使用的总线和幂等策略。
- 文件：`Fantasy.Files.Grpc` 通过 gRPC 契约和 OSS 抽象处理文件，并提供内容读取和静态文件入口。
- 认证：JWT Bearer、权限定义和 OAuth 提供商由 Identity 及 `BuildingBlocks.Authorization` 负责。
- 日志与可观测性：Serilog、OpenTelemetry、Aspire 默认能力。
- 前端：Vue 3、TypeScript、Vite、Vue Router、Pinia、Axios、UnoCSS、Vue Macros、自动图标/组件导入。

主要目录：

- `src/LexiCraft.Aspire.Host/`：本地/容器服务编排。
- `src/Fantasy.Aspire.ServiceDefaults/`：服务发现、弹性、健康检查和 OpenTelemetry 公共配置。
- `src/ApiGateway/`：YARP 网关、限流、安全头和 CORS。
- `src/microservices/Identity/`：登录、注册、刷新令牌、OAuth、用户资料、头像和权限（`Fantasy.Services.Identity` 与 `Fantasy.Services.Identity.Api`）。
- `src/microservices/Vocabulary/`：词库、单词查询、导入和用户单词状态（保持 `LexiCraft.Services.Vocabulary*`）。
- `src/microservices/Practice/`：练习任务、提交答案和完成练习（保持 `LexiCraft.Services.Practice*`）。
- `src/microservices/Files/`：文件 gRPC 服务、内容读取、上传目录和 OSS（`Fantasy.Files.Grpc`）。
- `src/microservices/Shared/`：微服务共享模型和权限常量（`Fantasy.Shared`，权限定义为 `FantasyPermissionDefinitionProvider`）。
- `src/BuildingBlocks/`：授权、缓存、持久化抽象、EF Core、EF Core Postgres、MongoDB、事件、消息、幂等、OSS、OpenAPI、日志和验证等基础组件。
- `src/UIs/lexicraft-vue-frontend/src/apis/`：前端 API 模块；`auth.ts` 是较新的 Identity 客户端，部分其他模块仍保留旧接口封装。
- `src/UIs/lexicraft-vue-frontend/src/utils/http.ts` 与 `authHttp.ts`：两套 Axios 客户端，新增接口必须先判断是否应合并或迁移，不能继续复制第三套。
- `.agents/MEMORY.md`：长期项目记忆。
- `.agents/memory/YYYY-MM-DD.md`：每日工作记录。

### 1.3 当前 API 事实

业务服务使用 API 版本化 Minimal API，代码中定义的模块前缀为：

- Identity：`api/v{version:apiVersion}/identity`
- Vocabulary：`api/v{version:apiVersion}/vocabulary`
- Practice：`api/v{version:apiVersion}/practice`

端点返回值通常经过 `BuildingBlocks.Filters.ResultEndPointFilter` 包装为 `ResultDto`；文件流端点是例外。前端对字段命名、错误包络和令牌字段的假设必须通过 OpenAPI、实际响应或服务端 DTO 验证，不得仅凭 TypeScript 类型推断。

网关的 `ReverseProxy` 配置通过 `LoadFromConfig` 读取。当前仓库中的 `src/ApiGateway/Fantasy.ApiGateway/appsettings.json` 只保留空的 `ReverseProxy` 节点，实际路由可能来自 AgileConfig 或运行环境；任何前端联调前必须确认真实路由转换，不能把 README 或猜测当作路由契约。

## 2. 开始修改前

1. 阅读 `.agents/MEMORY.md` 和日期最新的 `.agents/memory/YYYY-MM-DD.md`；若目录或文件不存在，创建后再继续。
2. 执行 `git status --short`，记录任务开始前已有的未提交修改。
3. 不覆盖、格式化、暂存、回滚或提交与当前任务无关的修改；无关问题只记录。
4. 明确任务涉及的边界：后端、网关、配置、前端、部署或记忆。涉及多个边界时，先列出依赖顺序。
5. 不在代码、配置、文档或记忆中新增或复制真实密钥、密码、令牌、私钥和内部凭据。发现仓库已有敏感值时只记录风险和文件位置，并在方案中提出迁移/轮换，不在本次无授权时擅自轮换。
6. 广泛搜索时排除 `node_modules/`、`bin/`、`obj/`、`dist/`、`src-tauri/target/` 等生成目录。
7. 修改前先阅读目标文件完整内容；优先使用最小范围编辑。

## 3. 后端与微服务实现规范

- 保持服务边界：Identity、Vocabulary、Practice、Files 不直接访问其他服务数据库，不新增跨服务项目引用来绕过 API 或事件契约。
- 业务代码放在对应服务的功能目录；API 宿主项目只负责启动、基础中间件和服务注册。
- 新增端点沿用当前 Minimal API 的模块配置、API 版本化、权限声明、验证和 `ResultDto` 包装模式。
- 所有异步 I/O 传递 `CancellationToken`；数据库查询避免无界加载，并明确分页、排序和幂等语义。
- 新增持久化设置时同步检查 DTO/Options、默认值、环境变量、旧配置兼容、部署配置和对应文档。
- 新增服务必须接入 `AddServiceDefaults()`、健康检查、日志和必要的 OpenTelemetry；依赖资源必须在 `AppHost.cs` 和部署编排中同步登记。
- 跨服务一致性优先复用现有事件基础设施；采用事件或 Saga 时必须记录事件版本、幂等键、重试和补偿行为，不同时引入两套总线实现。
- API 版本读取支持 URL、查询参数和 `api-version` 头；对外文档和前端客户端必须选择一种稳定方式，默认使用 URL 版本段。
- OpenAPI/Scalar 只在开发或明确的文档构建流程暴露；不要为了方便在生产环境无条件开放调试端点。
- 生产 CORS 使用明确的允许来源，不使用 `AllowAnyOrigin`；开发环境的宽松策略不得复制到生产配置。

## 4. 网关、配置与安全规范

- 前端默认只访问网关，不直连 Identity、Vocabulary、Practice 或 Files 内部地址；直连仅限服务诊断和明确记录的本地开发场景。
- 网关路由、路径转换、目标服务名、容器端口和健康检查必须保持一致。路由若由 AgileConfig 提供，必须有可审查的脱敏结构、版本或导出校验，不能只存在于个人环境。
- `VITE_*` 变量是前端环境契约；不要在多个 API 客户端散落 `localhost` 回退地址。新增地址必须集中在环境配置入口，并说明开发、测试、生产值的来源。
- 所有需要登录的请求使用 `Authorization: Bearer <token>`；不要混用旧的 `token` 请求头，除非对应后端端点明确要求并有迁移计划。
- 统一处理 400、401、403、404、409、429、5xx、网络超时和刷新令牌并发；不得把 HTTP 错误静默转换成成功响应。
- 上传、下载和静态文件必须校验路径、内容类型、大小和权限；不要把任意本地路径暴露给客户端。
- 配置文件中不得提交凭据。连接字符串、AgileConfig 参数、OAuth 密钥和对象存储密钥使用环境变量、用户机密或部署平台密钥管理。
- 修改限流、安全头、CSP 或 CORS 时，必须同步核对前端域名、文件资源、OAuth 回调和网关路径。

## 5. 前端实现规范

- 延续现有 Vue 3 + TypeScript + Pinia + Vue Router + Vite 结构和 Prettier 约定；组件、页面、Store、API、类型和工具按现有目录归属。
- API 按服务域拆分，优先建立类型明确的 `identity`、`vocabulary`、`practice`、`files` 模块；不要在新模块中继续使用旧的 `user/*`、`word/*`、`dict/*` 路径，除非已确认它们仍由当前网关提供。
- 后端响应 DTO、字段大小写、分页结构、错误字段和令牌字段必须与前端类型一一对应；修改任一方时同步更新另一方和契约测试。
- 认证流程必须覆盖登录、注册、验证码、用户资料、头像、OAuth 回调、刷新令牌、401 重试和登出；刷新请求要防止并发风暴，并在失败后清理会话。
- 前端页面处理加载、空数据、网络失败、401、403、429 和重复提交；文件上传显示进度或明确的不可用状态。
- 不在组件中散落后端 URL、权限码和可翻译的错误文案；统一从配置、API 模块和现有文案机制读取。
- UI 改动除自动化检查外，必须在本地开发页面或 Aspire 运行环境中做一次真实界面核验。
- `package.json` 的测试脚本必须是真实可执行命令；不能以空脚本或未安装的测试依赖作为“测试通过”。

## 6. 文件组织与规模

- 新增或修改的代码文件目标不超过 800 行，绝对上限不超过 1200 行；接近上限时优先按真实职责拆分，不创建只有一层转发的空抽象。
- Markdown/配置/测试应归属对应模块；API 契约和联调说明优先放在服务或前端 API 模块附近。
- 当前已知超过 800 行的活跃文件必须作为技术债跟踪，不能为了满足检查而无关重构（行数按 2026-08-18 实测，`src/BuildingBlocks/` 已无超限文件）：
  - `src/UIs/lexicraft-vue-frontend/src/pages/(words)/dict-detail.vue`（1316 行）
  - `src/UIs/lexicraft-vue-frontend/src/components/article/components/TypingArticle.vue`（1055 行）
  - `src/UIs/lexicraft-vue-frontend/src/pages/(words)/practice-words/[id].vue`（987 行）
  - `src/UIs/lexicraft-vue-frontend/src/components/article/components/EditArticle.vue`（913 行）
  - `src/UIs/lexicraft-vue-frontend/src/components/word/components/TypeWord.vue`（884 行）
  - `src/UIs/lexicraft-vue-frontend/src/pages/(articles)/book-detail.vue`（862 行）
- 扫描行数时排除依赖包、生成目录和大型静态数据文件，并在交付记录中区分历史超限与本次新增。

## 7. 验证要求

按改动范围执行，失败时必须注明是否为本次改动引入：

```powershell
dotnet build src\Fantasy.slnx
dotnet test src\Fantasy.Tests.slnx
git diff --check
npm --prefix src\UIs\lexicraft-vue-frontend run build-tsc
npm --prefix src\UIs\lexicraft-vue-frontend run build
```

补充规则：

- 涉及单个后端服务时可先执行对应项目的定向构建，再执行活动解决方案构建。
- 涉及网关或配置时，至少验证健康检查、路由转发、CORS、限流和错误响应。
- 涉及认证时，至少验证验证码、注册/登录、获取资料、刷新令牌、401 重试和登出。
- 涉及词汇/练习/文件时，补充最小 API 冒烟测试或契约测试。
- 前端 `npm test` 已运行 Vitest；涉及前端逻辑时必须执行对应测试，并结合 `typecheck` / `build-tsc`，不能只依赖构建退出码。
- 涉及 `src/BuildingBlocks/`、Identity 或 Files 时，必须执行大小写不敏感的旧品牌零命中扫描（排除生成目录与 `fliesdb*` 数据文件）。
- 涉及配置、部署或 CI 时执行 `node scripts/security/scan-secrets.mjs`；新增前端 API 调用时执行 `npm --prefix src/UIs/lexicraft-vue-frontend run check:legacy-http`。
- 交付前再次执行 `git status --short`、`git diff --check`，确认没有临时文件、生成物或无关改动。

## 8. 项目记忆

- 每个自然日首次完成任务时创建新的 `.agents/memory/YYYY-MM-DD.md`；同一天的后续任务追加到当天文件，不覆盖历史记录。
- 每次记录至少包含：需求、代码事实、实现/文档变更、涉及文件、验证结果、已知限制、待办和 Git 提交信息。
- 记忆标题、说明和结论使用中文；路径、命令、代码标识符和协议字段保留原文。
- 只有稳定的架构决策、长期风险、统一契约和平台陷阱才整理到 `.agents/MEMORY.md`；临时调试输出只放当天文件。
- 不把密码、令牌、私钥、完整连接字符串或机器专属运行数据写入记忆。

## 9. Git 规范

- 默认在任务完成并通过与改动范围匹配的验证后创建 Git 提交；只有用户明确说“不提交”“先不要提交”或“只查看”时才不提交。
- 提交前只暂存本次任务明确的文件，禁止使用 `git add .` 混入无关改动。
- 提交信息使用 Conventional Commits：`<type>: <中文描述>`。类型前缀使用英文，标题和正文使用中文。
- 常用类型：`feat`、`fix`、`docs`、`refactor`、`perf`、`test`、`build`、`ci`、`style`、`chore`、`revert`。
- 提交正文说明实现内容、兼容/安全处理、验证结果和已知限制。
- 未经明确要求不推送、不创建标签、不发版、不创建 Pull Request。
- 提交后确认工作区只剩用户原有或明确保留的修改。

## 10. 完成标准

任务只有在以下条件满足后才算完成：

- 用户要求的规则、记忆或代码行为已落地，且没有扩大无关范围。
- 所有结论都能由仓库代码、配置、命令输出或明确标注的未决项支持。
- 相关验证已执行，失败原因已区分历史问题与本次问题。
- 记忆已按日期更新，长期决策已提炼。
- Git 提交只包含本次任务文件，最终说明包含变更、验证、限制和提交信息。
