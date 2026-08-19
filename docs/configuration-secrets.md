# 本地敏感配置

仓库内的服务配置仅保留结构、非敏感标识和安全默认值。连接字符串、AgileConfig 地址与密钥、OAuth 客户端密钥、消息队列凭据和对象存储凭据必须从 User Secrets、环境变量或部署平台密钥管理中注入。

## Aspire 本地启动

在 `src/LexiCraft.Aspire.Host` 项目中配置以下 User Secrets（示例值均为占位符）：

```powershell
dotnet user-secrets --project src/LexiCraft.Aspire.Host set "ConnectionStrings:redis" "<redis-connection-string>"
dotnet user-secrets --project src/LexiCraft.Aspire.Host set "ConnectionStrings:postgres-identity" "<identity-postgres-connection-string>"
dotnet user-secrets --project src/LexiCraft.Aspire.Host set "ConnectionStrings:postgres-vocabulary" "<vocabulary-postgres-connection-string>"
dotnet user-secrets --project src/LexiCraft.Aspire.Host set "ConnectionStrings:mongo-practice" "<practice-mongodb-connection-string>"
```

每个 Aspire 资源还需要自己的 AgileConfig 配置。仓库已保留兼容 AppId 和环境名，只需至少注入 `Nodes` 与 `Secret`：

```powershell
dotnet user-secrets --project src/LexiCraft.Aspire.Host set "AgileConfig:fantasy-identity-api:Nodes" "<agileconfig-nodes>"
dotnet user-secrets --project src/LexiCraft.Aspire.Host set "AgileConfig:fantasy-identity-api:Secret" "<agileconfig-secret>"
```

其他资源按同样方式配置：

- `lexicraft-vocabulary-api`
- `lexicraft-practice-api`
- `fantasy-files-grpc`
- `fantasy-api-gateway`

若设置 `AgileConfig:UseLocalAgileConfig=true`，还必须提供：

- `ConnectionStrings:postgres-agileconfig`
- `AgileConfig:JwtSecurityKey`

## Docker Compose

运行 `src/compose.yaml` 前必须在当前进程或部署平台提供：

- `AGILECONFIG_NODES`
- `AGILECONFIG_SECRET`
- 可选：`AGILECONFIG_ENV`，默认值为 `TEST`

Compose 使用必填变量语法；缺少地址或密钥时会直接停止，而不是用仓库内的兜底凭据启动。

## 直接启动单个服务

直接启动 API 或 gRPC 项目时，使用 .NET 环境变量层级映射，例如：

- `AgileConfig__Nodes`
- `AgileConfig__Secret`
- `PostgresOptions__ConnectionString`
- `MongoOptions__ConnectionString`
- `RedisCache__DefaultConnectionString`
- `MassTransit__Host`、`MassTransit__Username`、`MassTransit__Password`
- `OSSOptions__Providers__files__AccessKey`、`OSSOptions__Providers__files__SecretKey`

实际需要的键以对应项目 Options 类型和当前配置结构为准，不要把真实值写回 `appsettings*.json`。

## 提交前扫描

在仓库根目录运行：

```powershell
node scripts/security/scan-secrets.mjs
```

扫描器只报告文件位置和规则，不回显疑似密钥内容。CI 会执行同一检查。

## 已泄露凭据的处理

从当前版本删除明文不能让历史提交中的值失效。仓库维护者仍需在对应平台轮换现有凭据，并根据仓库分发范围决定是否清理 Git 历史；历史重写会影响所有协作者，本阶段不自动执行。
