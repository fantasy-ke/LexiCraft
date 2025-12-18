# CODEBUDDY.md This file provides guidance to CodeBuddy Code when working with code in this repository.

## 项目概述

LexiCraft 是一个基于 .NET 10 的微服务架构项目，使用 .NET Aspire 进行服务编排。项目采用领域驱动设计（DDD）和清洁架构原则，支持 gRPC 服务间通信、JWT 认证、Redis 缓存和 PostgreSQL 数据库。

## 常用命令

### 构建和运行

```bash
# 在项目根目录运行 Aspire Host（推荐方式，会启动所有服务）
cd src
dotnet run --project LexiCraft.Aspire.Host/LexiCraft.Aspire.Host.csproj

# 构建整个解决方案
cd src
dotnet build LexiCraft.sln

# 还原依赖
dotnet restore LexiCraft.sln

# 清理构建输出
dotnet clean LexiCraft.sln
```

### 单独运行服务

```bash
# 运行认证服务
cd src/microservices/AuthServer/LexiCraft.AuthServer.Api
dotnet run

# 运行文件服务（gRPC）
cd src/microservices/FliesServer/LexiCraft.Files.Grpc
dotnet run
```

### 数据库迁移

```bash
# 添加新迁移（在 Infrastructure 项目目录）
cd src/microservices/AuthServer/LexiCraft.AuthServer.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../LexiCraft.AuthServer.Api/LexiCraft.AuthServer.Api.csproj

# 应用迁移
dotnet ef database update --startup-project ../LexiCraft.AuthServer.Api/LexiCraft.AuthServer.Api.csproj

# 回滚迁移
dotnet ef database update <PreviousMigrationName> --startup-project ../LexiCraft.AuthServer.Api/LexiCraft.AuthServer.Api.csproj

# 移除最后一次迁移
dotnet ef migrations remove --startup-project ../LexiCraft.AuthServer.Api/LexiCraft.AuthServer.Api.csproj
```

## 项目架构

### 整体结构

```
src/
├── LexiCraft.Aspire.Host/           # Aspire 编排主机，管理所有服务
├── LexiCraft.Aspire.ServiceDefaults/ # 服务默认配置（遥测、健康检查、服务发现）
├── microservices/                    # 微服务
│   ├── AuthServer/                   # 认证服务（DDD 分层架构）
│   │   ├── Api/                      # API 层（Minimal API + Source Generator）
│   │   ├── Application/              # 应用层（业务逻辑）
│   │   ├── Application.Contract/     # 应用契约（接口定义、DTO、事件）
│   │   ├── Domain/                   # 领域层（实体、领域逻辑）
│   │   └── Infrastructure/           # 基础设施层（EF Core、仓储实现）
│   ├── FliesServer/                  # 文件服务（gRPC）
│   └── UserServer/                   # 用户服务（待开发）
├── BuildingBlocks/                   # 共享构建块
│   ├── BuildingBlocks/               # 核心共享库
│   ├── BuildingBlocks.Authorization/ # 授权组件（JWT、权限检查）
│   ├── BuildingBlocks.EntityFrameworkCore/ # EF Core 仓储基类
│   └── BuildingBlocks.Grpc.Contracts/ # gRPC 契约
└── framework/                        # 自定义框架
    ├── Z.EventBus/                   # 事件总线接口
    ├── Z.Local.EventBus/             # 本地事件总线实现
    ├── Z.FreeRedis/                  # Redis 客户端封装
    └── Z.OSSCore/                    # 对象存储服务
```

### 微服务分层架构（以 AuthServer 为例）

项目采用 **DDD（领域驱动设计）+ 清洁架构**：

1. **Domain 层**：领域实体、领域逻辑、值对象
   - 无外部依赖
   - 包含核心业务规则

2. **Application.Contract 层**：应用服务接口、DTO、事件定义
   - 定义应用层契约
   - 包含异常类型、事件传输对象（ETO）

3. **Application 层**：应用服务实现、事件处理器
   - 依赖 Application.Contract 和 Domain
   - 实现业务用例

4. **Infrastructure 层**：数据访问、外部服务集成
   - EF Core DbContext 和仓储实现
   - 数据库迁移
   - 权限检查实现

5. **Api 层**：HTTP 端点、中间件、配置
   - 使用 **ZAnalyzers.MinimalApiSG**（Source Generator）自动生成 Minimal API 路由
   - 依赖所有下层

### 关键技术栈

- **.NET 10**（使用最新预览版）
- **.NET Aspire**：服务编排、遥测、服务发现、健康检查
- **Entity Framework Core 10**：数据访问，支持软删除、查询过滤
- **gRPC**（code-first）：服务间通信，使用 `protobuf-net.Grpc`
- **JWT Bearer Authentication**：基于令牌的认证
- **Serilog**：结构化日志记录
- **Redis（FreeRedis）**：缓存
- **PostgreSQL**：主数据库
- **Mapster**：对象映射
- **IdGen**：分布式 ID 生成
- **Scalar**：API 文档和测试（替代 Swagger UI）

### 源代码生成器（Source Generators）

项目使用自定义的 **ZAnalyzers** 源代码生成器（可通过 `zversion.props` 配置源码或 NuGet 引用模式）：

- **ZAnalyzers.MinimalApiSG**：自动为实现特定接口的服务类生成 Minimal API 端点
- **ZAnalyzers.ServiceInjectionSG**：自动服务注册

在 `Program.cs` 中通过 `app.MapZMinimalApis()` 配置 API 路由策略（前缀、版本、复数化等）。

### 仓储模式

- **通用仓储接口**：`IRepository<TDbContext, TEntity>` 在 `BuildingBlocks.Domain`
- **基类实现**：`Repository<TDbContext, TEntity>` 在 `BuildingBlocks.EntityFrameworkCore`
- 提供：CRUD、分页、查询、软删除支持
- 每个微服务可扩展自定义仓储

### 事件总线

- **本地事件总线**：`Z.Local.EventBus` 用于进程内事件发布/订阅
- 事件类继承 `EventEto`，处理器实现 `IEventHandler<TEvent>`
- 通过 `[EventScheme]` 特性定义事件标识

### 授权和权限

- **BuildingBlocks.Authorization** 提供：
  - 自定义 `AuthorizationPolicyProvider` 和 `AuthorizeHandler`
  - `[ZAuthorize]` 特性用于权限声明式检查
  - `IPermissionCheck` 接口由各服务实现具体权限验证逻辑
  - `UserContext` 提供当前用户上下文访问

### Aspire 服务编排

- `LexiCraft.Aspire.Host/Program.cs` 定义服务引用和依赖
- 使用 `AddProject<>` 注册服务，`WithReference()` 注入连接字符串
- 运行时启动 Aspire Dashboard（默认端口查看 launchSettings.json）

### 配置文件

- `appsettings.json`：应用配置
- `serilog.json`：日志配置（独立配置文件）
- `global.json`：.NET SDK 版本锁定（10.0.0）
- `zversion.props`：全局版本和 ZAnalyzers 引用模式

## 开发注意事项

### 添加新微服务

1. 在 `src/microservices/` 创建服务目录
2. 遵循 DDD 分层结构（Domain → Application.Contract → Application → Infrastructure → Api）
3. Api 层引用 `LexiCraft.Aspire.ServiceDefaults` 并调用 `builder.AddServiceDefaults()`
4. 在 `LexiCraft.Aspire.Host/Program.cs` 注册新服务

### 添加新实体

1. 在 Domain 层创建实体类
2. 在 Infrastructure 层的 DbContext 添加 `DbSet<T>`
3. 在 `ModelBuilder` 配置（通过 Extensions）
4. 创建 EF Core 迁移

### 使用 Source Generator 创建 API

1. 在 Application.Contract 定义服务接口
2. 在 Application 实现服务（可能需要特定命名约定或特性）
3. Source Generator 自动生成 Minimal API 端点
4. 通过 `MapZMinimalApis` 配置路由规则

### 软删除

- 实体实现 `ISoftDeleted` 接口
- DbContext 自动应用查询过滤器（通过 `IsSoftDeleteFilterEnabled`）
- 可通过配置禁用软删除过滤

### Redis 使用

- 注入 `IRedisCacheBaseService`（来自 `Z.FreeRedis`）
- 提供丰富的同步/异步缓存操作方法

### 日志记录

- 使用 Serilog，配置在 `serilog.json`
- 已集成请求日志中间件（`UseSerilogRequestLogging`）
- 支持结构化日志和异步写入

### 健康检查

- 服务默认包含 `/health` 和 `/alive` 端点（仅开发环境）
- 自定义健康检查通过 `AddHealthChecks()` 添加

## 项目依赖

- 数据库：PostgreSQL（通过 Aspire 编排注入连接字符串）
- 缓存：Redis（通过 Aspire 编排注入连接字符串）
- 运行时：.NET 10 SDK（需要预览版支持）
- 容器化：支持 Docker（Dockerfile 配置为 Linux）
